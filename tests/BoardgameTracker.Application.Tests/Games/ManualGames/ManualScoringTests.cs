using BoardgameTracker.App;
using BoardgameTracker.App.Games.ManualGames;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Result;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Presentation;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Games.ManualGames;

// These tests use the platform Shell double shared with existing navigation tests.
[CollectionDefinition("Manual scoring navigation", DisableParallelization = true)]
public sealed class ManualScoringNavigationCollection;

[Collection("Manual scoring navigation")]
public sealed class ManualScoringTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed record Fixture(SliceDatabase Database, Guid GameId, string Key, Guid Id, Guid[] Players) : IAsyncDisposable
    {
        public ManualScoringQuery Query => new(Database);
        public ManualScoringUseCase Draft => new(Database);
        public CompleteManualGameSessionUseCase Complete => new(Database);
        public ManualGameSessionNavigationHandler Handler => new(new(Database), Query, Complete);
        public ManualScoringViewModel ViewModel() => new(Query, Draft, Complete, new(Database), NullLogger<ManualScoringViewModel>.Instance, new(Database));
        public ManualScoreInput[] Values(int?[] points, int?[]? ranks = null) => Players.Select((id, index) => new ManualScoreInput(id, points[index], ranks?[index])).ToArray();
        public static async Task<Fixture> CreateAsync(ManualGameOptions options, int count = 2)
        {
            SliceDatabase db = await SliceDatabase.CreateAsync();
            Guid game = await new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(new(null, "Manual", "TAG", 1, 12, options), Token);
            Guid group = await new CreateGroupUseCase(db).CreateGroupAsync(new("Scoring", Enumerable.Range(1, count)
                .Select(index => new CreateGroupPlayerInput(null, "P" + index, false)).ToArray(), [game]), Token);
            await using BoardgameTrackerDbContext context = db.CreateDbContext();
            string key = (await context.Games.SingleAsync(value => value.Id == game, Token)).Key;
            ManualPreparationData prep = await new ManualPreparationQuery(db).GetAsync(key, group, Token);
            ManualStartingPlayer[] players = prep.Players.Select((player, index) => new ManualStartingPlayer(player.PlayerId,
                options.CapturePlayerOrder ? index + 1 : null, options.CapturePlayerColors ? (PlayerColor)(index + 1) : null)).ToArray();
            Guid id = await new StartManualGameSessionUseCase(db).StartAsync(new(key, group, DateTimeOffset.UtcNow.AddHours(-2), players), Token);
            await new ManualRunningGameSessionUseCase(db).BeginScoringAsync(id, DateTimeOffset.UtcNow.AddHours(-1), Token);
            return new(db, game, key, id, players.Select(player => player.PlayerId).ToArray());
        }
        public ValueTask DisposeAsync() => Database.DisposeAsync();
    }

    [Fact]
    public async Task PartialDraft_ResumesWithZeroNullColorsOrderAndHistoricalRules()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, true, true, true, ResultFromPoints: true,
            PointsDirection: PointsDirection.Highest, CapturePlayerOrder: true, CapturePlayerColors: true));
        ManualScoringData before = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        await fixture.Draft.SaveDraftAsync(fixture.Id, fixture.Values([0, null]), Token);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Complete.CompleteAsync(fixture.Id, cancellationToken: Token));
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            (await db.ManualGames.SingleAsync(Token)).SetConfiguration(new(true, false, true, true, ResultFromPoints: true, PointsDirection: PointsDirection.Lowest));
            await db.SaveChangesAsync(Token);
        }
        ManualScoringViewModel resumed = fixture.ViewModel();
        await resumed.LoadAsync(fixture.Id, true, Token);
        Assert.False(resumed.TryReturnToCorrectionPoints());
        Assert.Equal(new[] { "0", "" }, resumed.Players.Select(player => player.PointsText));
        Assert.False(resumed.CanSubmit);
        resumed.Players[1].PointsText = "-2";
        await resumed.PointsChangedAsync();
        Assert.True(resumed.CanSubmit);
        Assert.Equal(AppResources.CompleteScoring, resumed.PrimaryText);
        Assert.Equal(ManualSubmitResult.Completed, await resumed.SubmitAsync(Token));
        ManualScoringData after = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        Assert.Equal(before.Session.ScoringRules, after.Session.ScoringRules);
        Assert.Equal(before.Session.Players, after.Session.Players);
        Assert.Equal(before.Session.EndedAt, after.Session.EndedAt);
        Assert.Equal(GameSessionStatus.Completed, after.Session.Status);
        Assert.Equal(new int?[] { 1, 2 }, after.Values.Select(player => player.Placement));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("-1", false)]
    [InlineData("1.5", false)]
    [InlineData("2147483648", false)]
    [InlineData("abc", false)]
    [InlineData("0", true)]
    [InlineData("2147483647", true)]
    public async Task PointsEntry_ValidatesIntegerRangeAndCompleteness(string text, bool valid)
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(CapturePoints: true));
        ManualScoringViewModel model = fixture.ViewModel();
        await model.LoadAsync(fixture.Id, true, Token);
        model.Players[0].PointsText = text;
        model.Players[1].PointsText = "2";
        await model.PointsChangedAsync();
        Assert.Equal(valid, model.CanSubmit);
        if (!valid)
            Assert.Null(await model.SubmitAsync(Token));
        Assert.Equal(GameSessionStatus.Scoring, (await fixture.Query.GetAsync(fixture.Id, Token))!.Session.Status);
    }

    [Fact]
    public async Task AutomaticTie_DraftsRestoreDecisionsAndPointChangesInvalidateThem()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, false, true, ResultFromPoints: true, PointsDirection: PointsDirection.Highest), 3);
        await fixture.Draft.SaveDraftAsync(fixture.Id, fixture.Values([10, 10, 2]), Token);
        ManualScoringViewModel model = fixture.ViewModel();
        await model.LoadAsync(fixture.Id, false, Token);
        Assert.Equal(fixture.Players.Take(2), model.DecisionPlayers.Select(player => player.PlayerId));
        Assert.False(model.CanSubmit);
        await model.SelectWinnerAsync(model.Players[1], true);
        Assert.True(model.CanSubmit);
        ManualScoringViewModel resumed = fixture.ViewModel();
        await resumed.LoadAsync(fixture.Id, false, Token);
        Assert.True(resumed.Players[1].IsWinner);
        await resumed.PointsChangedAsync(); // Initial control binding must retain restored decisions.
        Assert.True(resumed.Players[1].IsWinner);
        await fixture.Draft.SaveDraftAsync(fixture.Id, fixture.Values([20, 10, 2], [null, 1, null]), Token);
        Assert.All((await fixture.Query.GetAsync(fixture.Id, Token))!.Values, player => Assert.Null(player.Placement));
        await fixture.Complete.CompleteAsync(fixture.Id, cancellationToken: Token);
        Assert.Equal(new int?[] { 1, null, null }, (await fixture.Query.GetAsync(fixture.Id, Token))!.Values.Select(player => player.Placement));
    }

    [Fact]
    public async Task MultipleTieBlocks_UseOneDecisionStepAndPreserveCompetitionRanks()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, false, true, true, true, true, PointsDirection.Highest), 6);
        ManualScoringViewModel points = fixture.ViewModel();
        await points.LoadAsync(fixture.Id, true, Token);
        int[] scores = [100, 80, 80, 60, 60, 20];
        for (int index = 0; index < scores.Length; index++)
            points.Players[index].PointsText = scores[index].ToString();
        await points.PointsChangedAsync();
        Assert.Equal(AppResources.ManualContinue, points.PrimaryText);
        Assert.Equal(ManualSubmitResult.Decision, await points.SubmitAsync(Token));
        ManualScoringViewModel decisions = fixture.ViewModel();
        await decisions.LoadAsync(fixture.Id, false, Token);
        Assert.Equal(fixture.Players.Skip(1).Take(4), decisions.DecisionPlayers.Select(player => player.PlayerId));
        Assert.Equal(new[] { 2, 3 }, decisions.Players[1].Ranks.Select(rank => rank.Rank));
        Assert.Equal(new[] { 4, 5 }, decisions.Players[3].Ranks.Select(rank => rank.Rank));
        await decisions.SelectRankAsync(decisions.Players[1], 2);
        await decisions.SelectRankAsync(decisions.Players[2], 2);
        await decisions.SelectRankAsync(decisions.Players[3], 4);
        Assert.False(decisions.CanSubmit);
        await decisions.SelectRankAsync(decisions.Players[4], 5);
        Assert.True(decisions.CanSubmit);
        Assert.Equal(ManualSubmitResult.Completed, await decisions.SubmitAsync(Token));
        Assert.Equal(new int?[] { 1, 2, 2, 4, 5, 6 }, (await fixture.Query.GetAsync(fixture.Id, Token))!.Values.Select(player => player.Placement));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ManualDecision_SavesAndResumesOnlyConfiguredWinnersOrRanks(bool full, bool ties)
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(CaptureWinner: true, CapturePlacements: full, AllowTies: ties), 3);
        ManualScoringViewModel model = fixture.ViewModel();
        await model.LoadAsync(fixture.Id, false, Token);
        Assert.Equal(full ? AppResources.ManualAssignPlacements : AppResources.ManualSelectWinners, model.Title);
        Assert.False(model.CanSubmit);
        if (full)
        {
            await model.SelectRankAsync(model.Players[0], 1);
            await model.SelectRankAsync(model.Players[1], ties ? 1 : 2);
            await model.SelectRankAsync(model.Players[2], 3);
        }
        else
        {
            await model.SelectWinnerAsync(model.Players[0], true);
            await model.SelectWinnerAsync(model.Players[1], true);
            Assert.Equal(ties, model.Players[0].IsWinner);
        }
        ManualScoringViewModel resumed = fixture.ViewModel();
        await resumed.LoadAsync(fixture.Id, false, Token);
        Assert.True(resumed.CanSubmit);
        Assert.Equal(model.Players.Select(player => player.Placement), resumed.Players.Select(player => player.Placement));
        AppShell shell = MainSectionNavigationTests.CreateShell(new(), fixture.Database);
        Shell.Current = shell;
        await fixture.Handler.OpenGameSessionAsync(fixture.Id);
        Assert.Equal(ManualGameSessionNavigationHandler.DecisionRoute, shell.Calls.Last().Route);
        Assert.Equal(ManualSubmitResult.Completed, await resumed.SubmitAsync(Token));
        await fixture.Handler.ContinueAsync(fixture.Id);
        Assert.Equal("ManualResultPage", shell.Calls.Last().Route);
        ManualResultViewModel result = new(fixture.Query, NullLogger<ManualResultViewModel>.Instance);
        await result.LoadAsync(fixture.Id, Token);
        Assert.All(result.Players, player => Assert.False(player.HasPoints));
        if (!full)
            Assert.Equal(ties ? 2 : 1, result.Players.Count(player => player.IsWinner));
    }

    [Fact]
    public async Task CompletionFailure_RollsBackPointsPlacementsAndStatus_ThenRetryCannotAlterHistory()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, false, true));
        await fixture.Draft.SaveDraftAsync(fixture.Id, fixture.Values([0, null], [1, null]), Token);
        ManualScoringData before = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectCompletion BEFORE UPDATE ON GameSessions WHEN NEW.Status = 3 BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        ManualScoreInput[] final = fixture.Values([10, 20], [null, 1]);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Complete.CompleteAsync(fixture.Id, final, Token));
        ManualScoringData failed = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        Assert.Equal(before.Values, failed.Values);
        Assert.Equal(before.Session, failed.Session with { Players = before.Session.Players });
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectCompletion;", Token);
        await fixture.Complete.CompleteAsync(fixture.Id, final, Token);
        ManualScoringData completed = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        await fixture.Complete.CompleteAsync(fixture.Id, fixture.Values([99, 99], [1, null]), Token);
        ManualScoringData retried = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        Assert.Equal(completed.Values, retried.Values);
        Assert.Equal(completed.Session, retried.Session with { Players = completed.Session.Players });
        Assert.Equal(before.Session.EndedAt, retried.Session.EndedAt);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Draft.SaveDraftAsync(fixture.Id, final, Token));
    }

    [Fact]
    public async Task IncompleteOrForeignParticipants_CannotCompleteOrChangeSavedDraft()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, false, true, true));
        await fixture.Draft.SaveDraftAsync(fixture.Id, fixture.Values([1, null], [1, null]), Token);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Complete.CompleteAsync(fixture.Id, fixture.Values([2, 3], [1, null]), Token));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Complete.CompleteAsync(fixture.Id, [new(Guid.NewGuid(), 2, 1), new(fixture.Players[1], 3, 2)], Token));
        Assert.Equal(fixture.Values([1, null], [1, null]), (await fixture.Query.GetAsync(fixture.Id, Token))!.Values);
    }

    [Fact]
    public async Task Discard_WithInvalidUnsavedText_RemovesOnlySessionAndDependents()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, true, CapturePlayerColors: true));
        ManualScoringViewModel model = fixture.ViewModel();
        await model.LoadAsync(fixture.Id, true, Token);
        model.Players[0].PointsText = "0";
        await model.PointsChangedAsync();
        model.Players[1].PointsText = "invalid";
        Assert.False(await model.DiscardAsync(false, Token));
        Assert.True(await model.DiscardAsync(true, Token));
        Assert.Null(await fixture.Query.GetAsync(fixture.Id, Token));
        await using BoardgameTrackerDbContext db = fixture.Database.CreateDbContext();
        Assert.Empty(await db.GameSessionPlayers.ToArrayAsync(Token));
        Assert.Empty(await db.GameSessionPlayerColors.ToArrayAsync(Token));
        Assert.Empty(await db.ManualGameSessions.ToArrayAsync(Token));
        Assert.Empty(await db.ManualGameSessionPlayers.ToArrayAsync(Token));
        Assert.Equal(2, await db.GroupGamePlayerColors.CountAsync(Token));
        Assert.Single(await db.ManualGames.ToArrayAsync(Token));
        Assert.Equal(2, await db.Groups.CountAsync(Token));
        Assert.Equal(4, await db.Players.CountAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FlowTransitions_KeepTheOriginalGroupOrHistoryCaller(bool history)
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, false, true, ResultFromPoints: true, PointsDirection: PointsDirection.Highest));
        AppShell shell = MainSectionNavigationTests.CreateShell(new(), fixture.Database);
        Shell.Current = shell;
        if (history)
            await shell.NavigateToMainSectionAsync(BoardgameTracker.Application.Settings.Navigation.AppMainSection.History);
        else
            await shell.NavigateToGroupsForGameAsync(fixture.Key);
        Page caller = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        await fixture.Handler.OpenGameSessionAsync(fixture.Id);
        Assert.Equal(ManualGameSessionNavigationHandler.PointsRoute, shell.Calls.Last().Route);
        await fixture.Draft.SaveDraftAsync(fixture.Id, fixture.Values([10, 10]), Token);
        await fixture.Handler.ContinueAsync(fixture.Id);
        Assert.Equal(ManualGameSessionNavigationHandler.DecisionRoute, shell.Calls.Last().Route);
        await fixture.Complete.CompleteAsync(fixture.Id, fixture.Values([10, 10], [1, null]), Token);
        await fixture.Handler.ContinueAsync(fixture.Id);
        Assert.Equal("ManualResultPage", shell.Calls.Last().Route);
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
    }

    [Fact]
    public async Task EmptyResult_CompletesDirectlyAndHistoryOpensOnlySavedParticipantValues()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new());
        AppShell shell = MainSectionNavigationTests.CreateShell(new(), fixture.Database);
        Shell.Current = shell;
        await fixture.Handler.ContinueAsync(fixture.Id);
        Assert.Equal("ManualResultPage", shell.Calls.Last().Route);
        await fixture.Handler.OpenGameSessionAsync(fixture.Id);
        Assert.Equal("ManualResultPage", shell.Calls.Last().Route);
        ManualResultViewModel result = new(fixture.Query, NullLogger<ManualResultViewModel>.Instance);
        await result.LoadAsync(fixture.Id, Token);
        Assert.Null(result.ErrorMessage);
        Assert.False(result.HasStartingOrder);
        Assert.All(result.Players, player => { Assert.False(player.HasPoints); Assert.False(player.HasPlacement); Assert.Null(player.Color); });
        await using BoardgameTrackerDbContext db = fixture.Database.CreateDbContext();
        Assert.Empty(await db.ManualGameSessionPlayers.ToArrayAsync(Token));
        Assert.Single(await new BoardgameTracker.Application.History.CompletedGameSessionsQuery(fixture.Database).ListAsync(cancellationToken: Token));
    }

    [Fact]
    public async Task Result_UsesRecordedZeroPlacementsColorsAndOrderAfterConfigurationChanges()
    {
        await using Fixture fixture = await Fixture.CreateAsync(new(true, true, true, true, CapturePlayerOrder: true, CapturePlayerColors: true));
        await fixture.Complete.CompleteAsync(fixture.Id, fixture.Values([0, -5], [2, 1]), Token);
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            (await db.ManualGames.SingleAsync(Token)).SetConfiguration(new());
            await db.SaveChangesAsync(Token);
        }
        ManualResultViewModel result = new(fixture.Query, NullLogger<ManualResultViewModel>.Instance);
        await result.LoadAsync(fixture.Id, Token);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.HasStartingOrder);
        Assert.Contains("P1 → P2", result.StartingOrderText);
        Assert.Equal(fixture.Players.Reverse(), result.Players.Select(player => player.PlayerId));
        Assert.Equal(new int?[] { -5, 0 }, result.Players.Select(player => player.Points));
        Assert.All(result.Players, player => Assert.True(player.HasPoints));
        Assert.Contains("0", result.Players[1].PointsText);
        Assert.Equal(new[] { "1.", "2." }, result.Players.Select(player => player.PlacementText));
        Assert.Equal(new PlayerColor?[] { PlayerColor.Yellow, PlayerColor.Blue }, result.Players.Select(player => player.Color));
        AppShell shell = MainSectionNavigationTests.CreateShell(new(), fixture.Database);
        Shell.Current = shell;
        await fixture.Handler.OpenGameSessionAsync(fixture.Id);
        Assert.Equal("ManualResultPage", shell.Calls.Last().Route);
        Assert.Equal(fixture.Id, shell.Calls.Last().Parameters![NavigationParameters.GameSessionId]);
    }
}

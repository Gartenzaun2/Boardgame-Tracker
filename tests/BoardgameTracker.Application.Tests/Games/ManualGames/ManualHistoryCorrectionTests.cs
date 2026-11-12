using BoardgameTracker.App;
using BoardgameTracker.App.Games.ManualGames;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Result;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Scoring;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Application.Tests.Presentation;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Games.ManualGames;

[Collection("Manual scoring navigation")]
public sealed class ManualHistoryCorrectionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private sealed record Fixture(SliceDatabase Database, Guid Id, Guid GameId, string Key, Guid GroupId, Guid[] Players) : IAsyncDisposable
    {
        public ManualScoringQuery Query => new(Database);
        public ManualHistoryCorrectionUseCase Correction => new(Database);
        public ManualGameSessionNavigationHandler Handler => new(new(Database), Query, new(Database));
        public ManualScoringViewModel Editor() => new(Query, new(Database), new(Database), new(Database), NullLogger<ManualScoringViewModel>.Instance, Correction);
        public ManualResultViewModel Result() => new(Query, NullLogger<ManualResultViewModel>.Instance);
        public ManualScoreInput[] Values(int?[] points, int?[] placements) => Players.Select((id, index) => new ManualScoreInput(id, points[index], placements[index])).ToArray();
        public static async Task<Fixture> CreateAsync(int?[] points, int?[] placements, ManualGameOptions? snapshotOptions = null, GameSessionStatus status = GameSessionStatus.Completed)
        {
            SliceDatabase database = await SliceDatabase.CreateAsync();
            Guid gameId = await new ManualGameUseCase(database, new TestGameActivationStore()).SaveAsync(new(null, "Manual", "TAG", 1, 12, snapshotOptions ?? new()), Token);
            Guid groupId = await new CreateGroupUseCase(database).CreateGroupAsync(new("Correction", Enumerable.Range(1, points.Length)
                .Select(index => new CreateGroupPlayerInput(null, "P" + index, false)).ToArray(), [gameId]), Token);
            await using BoardgameTrackerDbContext db = database.CreateDbContext();
            string key = (await db.Games.SingleAsync(game => game.Id == gameId, Token)).Key;
            ManualPreparationData prep = await new ManualPreparationQuery(database).GetAsync(key, groupId, Token);
            Guid[] players = prep.Players.Select(player => player.PlayerId).ToArray();
            DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(-1);
            GameSession session = new(gameId, groupId, start, players);
            if (status != GameSessionStatus.Running)
                session.BeginScoring(start.AddHours(1));
            if (status == GameSessionStatus.Completed)
                session.Complete(start.AddHours(2));
            ManualGame config = await db.ManualGames.SingleAsync(Token);
            db.AddRange(session, new ManualGameSession(session.Id, config));
            for (int index = 0; index < players.Length; index++)
            {
                session.Players.Single(player => player.PlayerId == players[index]).SetPlacement(placements[index]);
                if (points[index].HasValue)
                    db.ManualGameSessionPlayers.Add(new(session.Id, players[index], points[index]));
                db.GameSessionPlayerColors.Add(new(session.Id, players[index], (PlayerColor)(index + 1)));
                db.GroupGamePlayerColors.Add(new(groupId, gameId, players[index], (PlayerColor)(index + 1)));
            }
            await db.SaveChangesAsync(Token);
            return new(database, session.Id, gameId, key, groupId, players);
        }
        public ValueTask DisposeAsync() => Database.DisposeAsync();
    }

    [Theory]
    [InlineData(true, ManualHistoricalPlacements.None)]
    [InlineData(false, ManualHistoricalPlacements.Winners)]
    [InlineData(false, ManualHistoricalPlacements.Full)]
    [InlineData(true, ManualHistoricalPlacements.Winners)]
    [InlineData(true, ManualHistoricalPlacements.Full)]
    public async Task Correction_PreservesRecordedFormAndMetadataEvenWithAllSettingsDisabled(bool points, ManualHistoricalPlacements placements)
    {
        int?[] ranks = placements switch { ManualHistoricalPlacements.Winners => [1, 1, null, null], ManualHistoricalPlacements.Full => [1, 1, 3, 4], _ => [null, null, null, null] };
        await using Fixture fixture = await Fixture.CreateAsync(points ? [0, -2, 7, 4] : [null, null, null, null], ranks,
            new(true, true, true, true, true, true, PointsDirection.Lowest, true, true));
        ManualScoringData before = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        DateTimeOffset originalModifiedAt;
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            originalModifiedAt = (await db.GameSessions.SingleAsync(Token)).ModifiedAt;
            (await db.ManualGames.SingleAsync(Token)).SetConfiguration(new());
            await db.SaveChangesAsync(Token);
        }
        int?[] correctedRanks = placements switch { ManualHistoricalPlacements.Winners => [null, 1, 1, null], ManualHistoricalPlacements.Full => [3, 1, 1, 4], _ => [null, null, null, null] };
        ManualScoreInput[] values = fixture.Values(points ? [-10, 0, int.MinValue, int.MaxValue] : [null, null, null, null], correctedRanks);
        await fixture.Correction.SaveCorrectionAsync(fixture.Id, values, Token);
        ManualScoringData after = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        Assert.Equal(values, after.Values);
        Assert.Equal(before.Session, after.Session with { Players = before.Session.Players });
        Assert.Equal(before.Session.Players, after.Session.Players);
        await using BoardgameTrackerDbContext verify = fixture.Database.CreateDbContext();
        GameSession session = await verify.GameSessions.SingleAsync(Token);
        Assert.Equal(GameSessionStatus.Completed, session.Status);
        Assert.Equal(fixture.GameId, session.GameId);
        Assert.Equal(fixture.GroupId, session.GroupId);
        Assert.True(session.ModifiedAt > originalModifiedAt);
        Assert.Equal(4, await verify.GroupGamePlayerColors.CountAsync(Token));
        ManualResultViewModel result = fixture.Result();
        await result.LoadAsync(fixture.Id, Token);
        Assert.True(result.CanCorrect);
        Assert.Equal(correctedRanks.Count(rank => rank == 1), result.Players.Count(player => player.Placement == 1));
        if (points)
            Assert.Contains(result.Players, player => player.Points == 0 && player.HasPoints);
        var last = (await new ManualPreparationQuery(fixture.Database).GetAsync(fixture.Key, fixture.GroupId, Token)).LastSession!;
        Assert.All(last.Players, player => Assert.Equal(values.Single(value => value.PlayerId == player.PlayerId).Points, player.Points));
        var history = Assert.Single(await new BoardgameTracker.Application.History.CompletedGameSessionsQuery(fixture.Database).ListAsync(cancellationToken: Token));
        Assert.All(history.Players, player => Assert.Equal(values.Single(value => value.PlayerId == player.PlayerId).Placement, player.Placement));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllFirstPlaces_KeepSnapshotDisambiguationAcrossEditorSaveAndResult(bool full)
    {
        await using Fixture fixture = await Fixture.CreateAsync([null, null, null], [1, 1, 1], new(CaptureWinner: true, CapturePlacements: full));
        ManualScoringViewModel editor = fixture.Editor();
        await editor.LoadCorrectionAsync(fixture.Id, Token);
        Assert.Equal(full ? AppResources.ManualAssignPlacements : AppResources.ManualSelectWinners, editor.Title);
        Assert.All(editor.Players, player => Assert.Equal(1, player.Placement));
        Assert.Equal(ManualSubmitResult.Completed, await editor.SubmitAsync(Token));
        ManualResultViewModel result = fixture.Result();
        await result.LoadAsync(fixture.Id, Token);
        Assert.All(result.Players, player =>
        {
            Assert.Equal(full ? "1." : string.Empty, player.PlacementText);
            Assert.Equal(!full, player.IsWinner);
        });
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            (await db.ManualGames.SingleAsync(Token)).SetConfiguration(new(CaptureWinner: true, CapturePlacements: !full));
            await db.SaveChangesAsync(Token);
        }
        ManualLastSession last = (await new ManualPreparationQuery(fixture.Database).GetAsync(fixture.Key, fixture.GroupId, Token)).LastSession!;
        Assert.Equal(full, last.CapturePlacements);
        Assert.Equal(!full, ManualPreparationViewModel.HasWinnerResult(last));
        string text = ManualPreparationViewModel.FormatLastSession(last);
        Assert.Contains(full ? "1. P1" : "P1", text);
        if (!full) Assert.DoesNotContain("1.", text.Split(" · ", 2)[1]);
        Assert.Equal(full ? ManualHistoricalPlacements.Full : ManualHistoricalPlacements.Winners,
            ManualHistoricalResult.FromValues((await fixture.Query.GetAsync(fixture.Id, Token))!.Values, (await fixture.Query.GetAsync(fixture.Id, Token))!.Session.ScoringRules).Placements);
    }

    [Fact]
    public async Task AutomaticHistory_UsesAnUnsavedManualDraftAndNeverRecalculatesRanksFromPointChanges()
    {
        await using Fixture fixture = await Fixture.CreateAsync([10, 2], [1, 2], new(true, false, true, true, ResultFromPoints: true, PointsDirection: PointsDirection.Highest));
        ManualScoringData before = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        ManualScoringViewModel editor = fixture.Editor();
        await editor.LoadCorrectionAsync(fixture.Id, Token);
        Assert.True(editor.IsCorrection);
        Assert.True(editor.IsPointsPage);
        Assert.Equal(new[] { "10", "2" }, editor.Players.Select(player => player.PointsText));
        editor.Players[0].PointsText = "-50";
        editor.Players[1].PointsText = "100";
        await editor.PointsChangedAsync();
        Assert.Equal(new int?[] { 1, 2 }, editor.Players.Select(player => player.Placement));
        Assert.Equal(ManualSubmitResult.Decision, await editor.SubmitAsync(Token));
        Assert.True(editor.IsDecisionPage);
        Assert.Equal(AppResources.SaveCorrection, editor.PrimaryText);
        await editor.SelectRankAsync(editor.Players[0], 2);
        await editor.SelectRankAsync(editor.Players[1], 1);
        Assert.True(editor.TryReturnToCorrectionPoints());
        Assert.True(editor.IsPointsPage);
        Assert.Equal("-50", editor.Players[0].PointsText);
        Assert.Equal(new int?[] { 2, 1 }, editor.Players.Select(player => player.Placement));
        Assert.False(editor.TryReturnToCorrectionPoints());
        Assert.Equal(ManualSubmitResult.Decision, await editor.SubmitAsync(Token));
        await editor.LoadCorrectionAsync(fixture.Id, Token); // A lifecycle reappearance keeps the same draft.
        Assert.Equal("-50", editor.Players[0].PointsText);
        Assert.Equal(before.Values, (await fixture.Query.GetAsync(fixture.Id, Token))!.Values);
        ManualScoringViewModel abandoned = fixture.Editor();
        await abandoned.LoadCorrectionAsync(fixture.Id, Token);
        Assert.Equal("10", abandoned.Players[0].PointsText);
        Assert.False(await abandoned.DiscardAsync(true, Token)); // Correction cannot delete the session.
        Assert.Equal(ManualSubmitResult.Completed, await editor.SubmitAsync(Token));
        Assert.Equal(fixture.Values([-50, 100], [2, 1]), (await fixture.Query.GetAsync(fixture.Id, Token))!.Values);
    }

    [Theory]
    [InlineData("points")]
    [InlineData("none")]
    [InlineData("winners")]
    [InlineData("ties")]
    public async Task History_PreservesMissingPlacesAndSortsOnlyRecordedRanks(string form)
    {
        int?[] ranks = form switch { "winners" => [null, 1, null], "ties" => [3, 1, 1], _ => [null, null, null] };
        await using Fixture fixture = await Fixture.CreateAsync(form == "points" ? [0, -2, 10] : [null, null, null], ranks);
        var card = Assert.Single(await new BoardgameTracker.Application.History.CompletedGameSessionsQuery(fixture.Database).ListAsync(cancellationToken: Token));
        Assert.All(card.Players, player => Assert.Equal(ranks[Array.IndexOf(fixture.Players, player.PlayerId)], player.Placement));
        Assert.Equal(ranks.OrderBy(rank => rank ?? int.MaxValue), card.Players.Select(player => player.Placement));
        Assert.DoesNotContain(card.Players, player => player.Placement == 0);
        if (form == "winners") Assert.Equal(fixture.Players[1], card.Players[0].PlayerId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelCorrection_FromEitherStepDropsDraftWithoutPersisting(bool secondStep)
    {
        await using Fixture fixture = await Fixture.CreateAsync([0, 2], [1, null]);
        ManualScoringViewModel editor = fixture.Editor();
        await editor.LoadCorrectionAsync(fixture.Id, Token);
        editor.Players[0].PointsText = "-9";
        await editor.PointsChangedAsync();
        if (secondStep)
        {
            Assert.Equal(ManualSubmitResult.Decision, await editor.SubmitAsync(Token));
            await editor.SelectWinnerAsync(editor.Players[1], true);
        }
        editor.CancelCorrection();
        Assert.False(editor.CanEdit);
        Assert.False(editor.CanSubmit);
        ManualScoringData persisted = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        Assert.Equal(fixture.Values([0, 2], [1, null]), persisted.Values);
        Assert.Equal(GameSessionStatus.Completed, persisted.Session.Status);
        await editor.LoadCorrectionAsync(fixture.Id, Token);
        Assert.Equal(new[] { "0", "2" }, editor.Players.Select(player => player.PointsText));
        Assert.True(editor.IsPointsPage);
    }

    [Theory]
    [InlineData("missing-points")]
    [InlineData("missing-rank")]
    [InlineData("out-of-range-rank")]
    [InlineData("foreign-player")]
    [InlineData("duplicate-player")]
    public async Task InvalidCorrection_LeavesSavedValuesUnchanged(string invalid)
    {
        await using Fixture fixture = await Fixture.CreateAsync([0, -1], [1, 2]);
        ManualScoreInput[] original = fixture.Values([0, -1], [1, 2]);
        ManualScoreInput[] values = fixture.Values([20, 10], [2, 1]);
        values[0] = invalid switch
        {
            "missing-points" => values[0] with { Points = null },
            "missing-rank" => values[0] with { Placement = null },
            "out-of-range-rank" => values[0] with { Placement = 3 },
            "foreign-player" => values[0] with { PlayerId = Guid.NewGuid() },
            _ => values[0] with { PlayerId = values[1].PlayerId }
        };
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Correction.SaveCorrectionAsync(fixture.Id, values, Token));
        Assert.Equal(original, (await fixture.Query.GetAsync(fixture.Id, Token))!.Values);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    public async Task InvalidIntegerDraft_CannotSaveAndCanBeAbandoned(string text)
    {
        await using Fixture fixture = await Fixture.CreateAsync([0, 2], [null, null]);
        ManualScoringViewModel editor = fixture.Editor();
        await editor.LoadCorrectionAsync(fixture.Id, Token);
        editor.Players[0].PointsText = text;
        await editor.PointsChangedAsync();
        Assert.False(editor.CanSubmit);
        Assert.Null(await editor.SubmitAsync(Token));
        Assert.Equal(fixture.Values([0, 2], [null, null]), (await fixture.Query.GetAsync(fixture.Id, Token))!.Values);
    }

    [Fact]
    public async Task MissingAndNonManualSessions_CannotBeCorrected()
    {
        await using Fixture fixture = await Fixture.CreateAsync([0, 2], [null, null]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Correction.SaveCorrectionAsync(Guid.NewGuid(), fixture.Values([1, 2], [null, null]), Token));
        await using BoardgameTrackerDbContext db = fixture.Database.CreateDbContext();
        GameSession other = new(fixture.Database.GameId, fixture.Database.Group.Id, DateTimeOffset.UtcNow.AddDays(-2), [fixture.Database.First.Id, fixture.Database.Second.Id]);
        other.BeginScoring(other.StartedAt.AddHours(1));
        other.Complete(other.StartedAt.AddHours(2));
        db.GameSessions.Add(other);
        await db.SaveChangesAsync(Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Correction.SaveCorrectionAsync(other.Id,
            [new(fixture.Database.First.Id, 1, 1), new(fixture.Database.Second.Id, 2, 2)], Token));
    }

    [Theory]
    [InlineData(GameSessionStatus.Running)]
    [InlineData(GameSessionStatus.Scoring)]
    public async Task OpenSessions_CannotEnterOrSaveHistoricalCorrection(GameSessionStatus status)
    {
        await using Fixture fixture = await Fixture.CreateAsync([0, 1], [1, 2], status: status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Correction.SaveCorrectionAsync(fixture.Id, fixture.Values([2, 3], [2, 1]), Token));
        ManualResultViewModel result = fixture.Result();
        await result.LoadAsync(fixture.Id, Token);
        Assert.False(result.CanCorrect);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.OpenCorrectionAsync(fixture.Id));
    }

    [Fact]
    public async Task EmptyResult_HasNoEditorAndCannotAcquireNewValues()
    {
        await using Fixture fixture = await Fixture.CreateAsync([null, null], [null, null]);
        ManualResultViewModel result = fixture.Result();
        await result.LoadAsync(fixture.Id, Token);
        Assert.False(result.CanCorrect);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.OpenCorrectionAsync(fixture.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Correction.SaveCorrectionAsync(fixture.Id, fixture.Values([0, 1], [1, null]), Token));
        await using BoardgameTrackerDbContext db = fixture.Database.CreateDbContext();
        Assert.Empty(await db.ManualGameSessionPlayers.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Failure_RollsBackAllChangesAndRetainsTheDraftForRetryWithoutChangingOtherSessions()
    {
        await using Fixture fixture = await Fixture.CreateAsync([0, -1], [1, 2]);
        ManualScoringData original = (await fixture.Query.GetAsync(fixture.Id, Token))!;
        Guid otherId;
        DateTimeOffset modifiedAt;
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            modifiedAt = (await db.GameSessions.SingleAsync(Token)).ModifiedAt;
            GameSession other = new(fixture.GameId, fixture.GroupId, original.Session.StartedAt.AddDays(-1), fixture.Players);
            other.BeginScoring(other.StartedAt.AddHours(1));
            other.Complete(other.StartedAt.AddHours(2));
            db.AddRange(other, new ManualGameSession(other.Id, await db.ManualGames.SingleAsync(Token)));
            db.ManualGameSessionPlayers.AddRange(fixture.Players.Select(playerId => new ManualGameSessionPlayer(other.Id, playerId, 123)));
            await db.SaveChangesAsync(Token);
            otherId = other.Id;
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectCorrection BEFORE UPDATE ON GameSessions BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        }
        ManualScoringViewModel editor = fixture.Editor();
        await editor.LoadCorrectionAsync(fixture.Id, Token);
        editor.Players[0].PointsText = "999";
        await editor.PointsChangedAsync();
        Assert.Equal(ManualSubmitResult.Decision, await editor.SubmitAsync(Token));
        await editor.SelectRankAsync(editor.Players[0], 2);
        await editor.SelectRankAsync(editor.Players[1], 1);
        Assert.Null(await editor.SubmitAsync(Token));
        Assert.Equal(AppResources.CorrectionFailed, editor.ErrorMessage);
        Assert.Equal("999", editor.Players[0].PointsText);
        Assert.Equal(original.Values, (await fixture.Query.GetAsync(fixture.Id, Token))!.Values);
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            Assert.Equal(modifiedAt, (await db.GameSessions.SingleAsync(session => session.Id == fixture.Id, Token)).ModifiedAt);
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectCorrection;", Token);
        }
        Assert.Equal(ManualSubmitResult.Completed, await editor.SubmitAsync(Token));
        Assert.All((await fixture.Query.GetAsync(otherId, Token))!.Values, player => { Assert.Equal(123, player.Points); Assert.Null(player.Placement); });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelOrSave_ReturnsToTheSameResultAndPreservesTheHistoryCaller(bool save)
    {
        await using Fixture fixture = await Fixture.CreateAsync([0, 2], [null, null]);
        MainSectionNavigationTests.SettingsStore settings = new();
        AppShell shell = MainSectionNavigationTests.CreateShell(settings, fixture.Database);
        Shell.Current = shell;
        await shell.NavigateToMainSectionAsync(AppMainSection.History);
        Page caller = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        await fixture.Handler.OpenGameSessionAsync(fixture.Id);
        Page resultPage = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        int depth = shell.CurrentItem.CurrentItem.Navigation.NavigationStack.Count;
        await fixture.Handler.OpenCorrectionAsync(fixture.Id);
        Assert.Equal(ManualGameSessionNavigationHandler.CorrectionRoute, shell.Calls.Last().Route);
        Assert.True((bool)shell.Calls.Last().Parameters[ManualGameSessionNavigationHandler.CorrectionParameter]);
        ManualScoringViewModel editor = fixture.Editor();
        await editor.LoadCorrectionAsync(fixture.Id, Token);
        editor.Players[0].PointsText = "-9";
        await editor.PointsChangedAsync();
        if (save)
            Assert.Equal(ManualSubmitResult.Completed, await editor.SubmitAsync(Token));
        await fixture.Handler.ReturnFromCorrectionAsync();
        Assert.Equal(depth, shell.CurrentItem.CurrentItem.Navigation.NavigationStack.Count);
        Assert.Same(resultPage, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        ManualResultViewModel result = fixture.Result();
        await result.LoadAsync(fixture.Id, Token);
        Assert.Equal(save ? -9 : 0, result.Players.Single(player => player.PlayerId == fixture.Players[0]).Points);
        Assert.Null(settings.Value.LastGameSessionId);
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
    }
}

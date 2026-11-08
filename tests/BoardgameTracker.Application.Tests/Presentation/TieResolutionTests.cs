using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.History;

using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.GameSessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class TieResolutionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(GameSessionStatus.Running, "ArkNovaRunningPage", false, BoardgameTracker.Application.Settings.Navigation.AppMainSection.Settings)]
    [InlineData(GameSessionStatus.Scoring, "ArkNovaScoringPage", false, BoardgameTracker.Application.Settings.Navigation.AppMainSection.Settings)]
    [InlineData(GameSessionStatus.Completed, "ArkNovaResultPage", false, BoardgameTracker.Application.Settings.Navigation.AppMainSection.Settings)]
    [InlineData(GameSessionStatus.Completed, "ArkNovaResultPage", true, BoardgameTracker.Application.Settings.Navigation.AppMainSection.History)]
    public async Task SessionUsesStatusRouteAndPreservesCaller(GameSessionStatus status, string route, bool fromHistory,
        BoardgameTracker.Application.Settings.Navigation.AppMainSection expectedSection)
    {
        await using TieDatabase database = await TieDatabase.CreateAsync(running: status == GameSessionStatus.Running);
        if (status == GameSessionStatus.Completed)
        {
            var completion = database.CreateViewModel();
            await completion.LoadAsync(database.Session.Id, Token);
            await completion.CompleteAsync(Token);
        }
        MainSectionNavigationTests.SettingsStore store = new();
        var shell = MainSectionNavigationTests.CreateShell(store, database);
        Shell.Current = shell;
        await shell.NavigateToMainSectionAsync(fromHistory
            ? BoardgameTracker.Application.Settings.Navigation.AppMainSection.History
            : BoardgameTracker.Application.Settings.Navigation.AppMainSection.Settings);
        if (!fromHistory)
            await shell.GoToAsync("PlayerListPage");
        Page caller = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        int callsBeforeOpen = shell.Calls.Count;
        var handler = new BoardgameTracker.App.Games.ArkNova.Navigation.ArkNovaGameSessionNavigationHandler(new(database, database.Scoring));

        await handler.OpenGameSessionAsync(database.Session.Id);

        Assert.Equal(route, shell.Calls[^1].Route);
        Assert.Equal(expectedSection, store.Value.LastMainSection);
        Assert.Equal(database.Session.Id, shell.Calls[^1].Parameters[BoardgameTracker.App.Navigation.NavigationParameters.GameSessionId]);
        Assert.Equal(status == GameSessionStatus.Completed,
            shell.Calls[^1].Parameters[BoardgameTracker.App.Navigation.NavigationParameters.ResultIsHistorical]);
        Assert.Equal(status == GameSessionStatus.Completed ? null : (Guid?)database.Session.Id, store.Value.LastGameSessionId);
        Assert.Equal(!fromHistory ? 3 : 2,
            shell.CurrentItem.CurrentItem.Navigation.NavigationStack.Count);
        Assert.Equal(callsBeforeOpen + 1, shell.Calls.Count);
        await shell.GoToAsync("InternalSessionTransition");
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        Assert.Equal(expectedSection, store.Value.LastMainSection);
    }

    [Fact]
    public async Task UnknownSessionStatusIsRejectedInsteadOfOpeningScoring()
    {
        var handler = new BoardgameTracker.App.Games.ArkNova.Navigation.ArkNovaGameSessionNavigationHandler(null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.OpenGroupAsync(Guid.NewGuid(), Guid.NewGuid(), (GameSessionStatus)999));
    }

    [Fact]
    public async Task PlacementGroups_MoveOnlySelectedPlayer_KeepEmptyTargetsAndPlayerColors_AfterReopening()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.Equal([1, 2, 3, 4], viewModel.PlacementGroups.Select(group => group.Placement));
        Assert.Equal(2, viewModel.PlacementGroups[1].Players.Count);
        Assert.Empty(viewModel.PlacementGroups[2].Players);

        viewModel.SelectPlacement(database.Players[1].Id, 3);
        Assert.Equal(database.Players[2].Id, Assert.Single(viewModel.PlacementGroups[1].Players).PlayerId);
        ArkNovaPlacementPlayerViewModel moved = Assert.Single(viewModel.PlacementGroups[2].Players);
        Assert.Equal(database.Players[1].Id, moved.PlayerId);
        Assert.Equal(PlayerColor.Yellow, moved.Color);
        Assert.Equal([2, 3], moved.PlacementOptions.Select(option => option.Placement));
        viewModel.SelectPlacement(database.Players[2].Id, 3);
        Assert.Empty(viewModel.PlacementGroups[1].Players);
        Assert.Equal(2, viewModel.PlacementGroups[2].Players.Count);
        Assert.True(viewModel.CanFinish);
        await viewModel.CompleteAsync(Token);

        ArkNovaTieResolutionViewModel reopened = database.CreateViewModel();
        await reopened.LoadAsync(database.Session.Id, Token);
        Assert.Empty(reopened.PlacementGroups[1].Players);
        Assert.Equal(2, reopened.PlacementGroups[2].Players.Count);
        Assert.Equal([PlayerColor.Yellow, PlayerColor.Red], reopened.PlacementGroups[2].Players.OrderBy(player => player.Name).Select(player => player.Color!.Value));
        Assert.All(reopened.PlacementGroups[2].Players, player => Assert.True(player.HasColor));
        Assert.Empty(Assert.Single(reopened.PlacementGroups[0].Players).PlacementOptions);
        Assert.Empty(Assert.Single(reopened.PlacementGroups[3].Players).PlacementOptions);
    }

    [Fact]
    public void PlayerWithoutColor_LeavesNeutralThemeStyleInEffect()
    {
        ArkNovaPlacementPlayerViewModel player = new(Guid.NewGuid(), "Player", 100, 1, [], null);
        Assert.False(player.HasColor);
        Assert.Null(player.ColorStyle);
    }

    [Fact]
    public async Task SharedPlaces_AreValidOnLoad_CanBeRestoredAndPersistInHistory()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.Equal(new[] { 1, 2, 2, 4 }, viewModel.Players.Select(player => player.Placement));
        Assert.True(viewModel.CanFinish);
        Assert.Null(viewModel.ValidationMessage);

        viewModel.SelectPlacement(database.Players[1].Id, 3);
        Assert.True(viewModel.CanFinish);
        viewModel.SelectPlacement(database.Players[2].Id, 3);
        Assert.True(viewModel.CanFinish);
        Assert.Null(viewModel.ValidationMessage);
        viewModel.SelectPlacement(database.Players[1].Id, 2);
        viewModel.SelectPlacement(database.Players[2].Id, 2);
        Assert.True(viewModel.CanFinish);
        Assert.Null(viewModel.ValidationMessage);

        await viewModel.CompleteAsync(Token);
        ArkNovaGameSessionResultData saved = (await database.Scoring.LoadGameSessionResultAsync(database.Session.Id, Token))!;
        Assert.Equal(new int?[] { 1, 2, 2, 4 }, saved.Players.OrderBy(player => player.Order).Select(player => player.Placement));
        var history = await new CompletedGameSessionsQuery(database).ListAsync(cancellationToken: Token);
        Assert.Equal(new[] { 1, 2, 2, 4 }, Assert.Single(history).Players.Select(player => player.Placement));
    }

    [Fact]
    public async Task TwoBlocks_PersistIndependentSelections_AndKeepChoicesOnRepeatedLoad()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync([100, 100, 80, 80]);
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.All(viewModel.Players.Take(2), player => Assert.Equal([1, 2], player.PlacementOptions.Select(option => option.Placement)));
        Assert.All(viewModel.Players.Skip(2), player => Assert.Equal([3, 4], player.PlacementOptions.Select(option => option.Placement)));

        viewModel.SelectPlacement(database.Players[0].Id, 2);
        Assert.Equal(new[] { 2, 1, 3, 3 }, viewModel.Players.Select(player => player.Placement));
        Assert.Equal(2, viewModel.PlacementGroups[2].Players.Count);
        viewModel.SelectPlacement(database.Players[2].Id, 4);
        viewModel.SelectPlacement(database.Players[0].Id, 4); // Foreign block is rejected.
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.Equal(new[] { 2, 1, 4, 3 }, viewModel.Players.Select(player => player.Placement));
        Assert.Equal(database.Players[1].Id, Assert.Single(viewModel.PlacementGroups[0].Players).PlayerId);
        Assert.Equal(database.Players[3].Id, Assert.Single(viewModel.PlacementGroups[2].Players).PlayerId);

        await viewModel.CompleteAsync(Token);
        ArkNovaGameSessionResultData reopened = (await database.Scoring.LoadGameSessionResultAsync(database.Session.Id, Token))!;
        Assert.Equal(new int?[] { 2, 1, 4, 3 }, reopened.Players.OrderBy(player => player.Order).Select(player => player.Placement));
        var history = await new CompletedGameSessionsQuery(database).ListAsync(cancellationToken: Token);
        Assert.Equal(new[] { database.Players[1].Id, database.Players[0].Id, database.Players[3].Id, database.Players[2].Id },
            Assert.Single(history).Players.Select(player => player.PlayerId));
    }

    [Fact]
    public async Task Completion_RejectsParallelCallWhilePersistenceIsPending()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        SaveProbe data = new() { BlockFirstSave = true };
        database.SaveInterceptor = data;
        ArkNovaTieResolutionViewModel viewModel = new(new(database, new(database, TestGameContent.Create())), database.Correction);
        await viewModel.LoadAsync(database.Session.Id, Token);
        Task completion = viewModel.CompleteAsync(Token);
        await data.FirstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        try
        {
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.CanFinish);
            await viewModel.CompleteAsync(Token);
            Assert.Equal(1, data.SaveCount);
        }
        finally
        {
            data.ReleaseFirstSave.TrySetResult();
        }
        await completion;
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.CanFinish);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task DirectPlaceSelection_ResolvesOnlyTiedPlayers_AndPersistsInHistory(int playerIndex)
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.True(viewModel.CanFinish);
        Assert.Empty(viewModel.Players.Single(player => player.PlayerId == database.Players[0].Id).PlacementOptions);
        Assert.Empty(viewModel.Players.Single(player => player.PlayerId == database.Players[3].Id).PlacementOptions);
        foreach (ArkNovaPlacementPlayerViewModel player in viewModel.Players.Skip(1).Take(2))
        {
            Assert.Equal([2, 3], player.PlacementOptions.Select(option => option.Placement));
            Assert.Equal(player.Placement, Assert.Single(player.PlacementOptions, option => option.IsSelected).Placement);
        }

        int[] expected = [1, playerIndex == 1 ? 3 : 2, playerIndex == 2 ? 3 : 2, 4];
        viewModel.SelectPlacement(database.Players[playerIndex].Id, 3);
        Assert.True(viewModel.CanFinish);
        viewModel.SelectPlacement(database.Players[2].Id, 1);
        viewModel.SelectPlacement(database.Players[2].Id, 4);
        viewModel.SelectPlacement(database.Players[0].Id, 2);
        Assert.Equal(expected, viewModel.Players.Select(player => player.Placement));

        await viewModel.CompleteAsync(Token);
        await viewModel.CompleteAsync(Token); // Repeated completion cannot write again.
        Assert.False(viewModel.CanFinish);
        ArkNovaGameSessionResultData saved = (await database.Scoring.LoadGameSessionResultAsync(database.Session.Id, Token))!;
        Assert.Equal(GameSessionStatus.Completed, saved.Status);
        Assert.Equal(expected.Select(placement => (int?)placement), saved.Players.OrderBy(p => p.Order).Select(p => p.Placement));
        Assert.Equal(new int?[] { 100, 90, 90, 80 }, saved.Players.OrderBy(p => p.Order).Select(p => p.Attractiveness));
        var history = await new CompletedGameSessionsQuery(database).ListAsync(cancellationToken: Token);
        Assert.Equal(database.Players[3 - playerIndex].Id, Assert.Single(history).Players[1].PlayerId);
    }

    [Fact]
    public async Task HistoricalTie_DirectPlacesPersistCorrectionAndKeepUnrelatedPlacement()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel initial = database.CreateViewModel();
        await initial.LoadAsync(database.Session.Id, Token);
        await initial.CompleteAsync(Token);

        ArkNovaHistoryCorrectionDraft draft = await database.Correction.LoadCorrectionDraftAsync(database.Session.Id, Token);
        draft.SetScore(database.Players[0].Id, 90, 7, null);
        ArkNovaTieResolutionViewModel correction = database.CreateViewModel();
        correction.Draft = draft;
        await correction.LoadAsync(database.Session.Id, Token);
        correction.SelectPlacement(database.Players[2].Id, 2);
        correction.SelectPlacement(database.Players[1].Id, 3);
        Assert.True(correction.CanFinish);
        await correction.CompleteAsync(Token);

        ArkNovaHistoryCorrectionDraft reopened = await database.Correction.LoadCorrectionDraftAsync(database.Session.Id, Token);
        Assert.True(draft.Saved);
        Assert.Equal(GameSessionStatus.Completed, reopened.Data.Status);
        Assert.Equal(new int?[] { 1, 2, 3, 4 }, reopened.Data.Players.Select(p => p.Placement));
        Assert.Equal(database.Players[2].Id, reopened.Data.Players[1].PlayerId);
        Assert.Equal(database.Players[3].Id, reopened.Data.Players[3].PlayerId);
    }

    [Theory]
    [InlineData(GameSessionStatus.Running, "ArkNovaRunningPage")]
    [InlineData(GameSessionStatus.Scoring, "ArkNovaScoringPage")]
    public async Task Resume_IsOneFinalShellTargetWithGroupsAsCaller(GameSessionStatus status, string route)
    {
        await using var database = await TieDatabase.CreateAsync(running: status == GameSessionStatus.Running);
        var shell = MainSectionNavigationTests.CreateShell(new(), database);
        Shell.Current = shell;
        var handler = new BoardgameTracker.App.Games.ArkNova.Navigation.ArkNovaGameSessionNavigationHandler(new(database, database.Scoring));
        await handler.ResumeAsync(shell, new(database.Session.Id, "ark-nova", status, DateTimeOffset.UtcNow));
        Assert.Equal("//GamesPage/GroupListPage/" + route, Assert.Single(shell.Calls).Route);
        Assert.False(Assert.Single(shell.Animations));
        Assert.Equal(3, shell.CurrentItem.CurrentItem.Navigation.NavigationStack.Count);
        await shell.ReturnToGameSessionCallerAsync();
        Assert.IsType<BoardgameTracker.App.Features.Groups.ListGroups.GroupListPage>(shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        await shell.GoToAsync("..");
        Assert.Single(shell.CurrentItem.CurrentItem.Navigation.NavigationStack);
    }

    [Fact]
    public async Task Result_UsesHistoricalStartOrderWhilePlacementsAndDefaultsDiffer()
    {
        await using var database = await TieDatabase.CreateAsync(scores: [80, 90, 100, 110]);
        var completion = database.CreateViewModel();
        await completion.LoadAsync(database.Session.Id, Token);
        await completion.CompleteAsync(Token);
        var result = new BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result.ArkNovaResultViewModel(new(database, database.Scoring), new ResultSettings());
        await result.LoadAsync(database.Session.Id, Token);
        Assert.Equal("Start: Anna → Bernd → Clara → Dirk", result.StartingOrderText);
        Assert.Equal(new[] { "Anna", "Bernd", "Clara", "Dirk" }, result.StartingOrderPlayers.Select(player => player.Name));
        Assert.Equal(new[] { false, true, true, true }, result.StartingOrderPlayers.Select(player => player.HasPrevious));
        Assert.Equal(new[] { "Start:", "", "", "" }, result.StartingOrderPlayers.Select(player => player.LeadingText));
        Assert.Equal(new[] { "Dirk", "Clara", "Bernd", "Anna" }, result.Players.Select(player => player.Name));
        await using (var db = database.CreateDbContext())
        {
            await db.GroupPlayers.ExecuteUpdateAsync(setters => setters.SetProperty(player => player.DefaultOrder, player => player.DefaultOrder + 100), Token);
            for (int index = 0; index < database.Players.Length; index++)
            {
                Guid id = database.Players[index].Id;
                int order = database.Players.Length - index;
                await db.GroupPlayers.Where(player => player.PlayerId == id).ExecuteUpdateAsync(setters => setters.SetProperty(player => player.DefaultOrder, order), Token);
            }
        }
        await result.LoadAsync(database.Session.Id, Token);
        Assert.Equal("Start: Anna → Bernd → Clara → Dirk", result.StartingOrderText);
        Assert.Equal(new[] { 1, 2, 3, 4 }, result.Players.Select(player => player.Placement));
        Assert.Equal(new[] { "Anna", "Bernd", "Clara", "Dirk" }, result.StartingOrderPlayers.Select(player => player.Name));
    }

    private sealed class ResultSettings : BoardgameTracker.Application.Games.ArkNova.Settings.IArkNovaSettingsStore
    {
        public BoardgameTracker.Application.Games.ArkNova.Settings.ArkNovaSettings Get() => BoardgameTracker.Application.Games.ArkNova.Settings.ArkNovaSettings.Default;
        public void Save(BoardgameTracker.Application.Games.ArkNova.Settings.ArkNovaSettings settings) { }
    }

    private sealed class TieDatabase : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        public SaveProbe SaveInterceptor { get; set; } = new();
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public Player[] Players { get; } = [new("Anna"), new("Bernd"), new("Clara"), new("Dirk")];
        public GameSession Session { get; private set; } = null!;
        public ArkNovaGameSessionResultQuery Scoring => new(this, TestGameContent.Create());
        public ArkNovaHistoryCorrectionUseCase Correction => new(this, new(this, TestGameContent.Create()));
        public ArkNovaTieResolutionViewModel CreateViewModel() => new(new(this, Scoring), Correction);
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseTestSqlite(_connection).AddInterceptors(SaveInterceptor).Options, TestGameLimits.Catalog);

        public static async Task<TieDatabase> CreateAsync(int[]? scores = null, bool running = false)
        {
            TieDatabase database = new();
            await database._connection.OpenAsync(Token);
            await new DatabaseInitializer(database, [new ArkNovaReferenceDataSeeder()]).InitializeAsync(Token);
            await using BoardgameTrackerDbContext context = database.CreateDbContext();
            Guid gameId = await context.Games.Select(game => game.Id).SingleAsync(Token);
            Group group = new("Group", database.Players.Select(player => player.Id).ToArray());
            database.Session = new(gameId, group.Id, DateTimeOffset.UtcNow.AddHours(-2), database.Players.Select(player => player.Id).ToArray());
            if (!running)
                database.Session.BeginScoring(DateTimeOffset.UtcNow.AddHours(-1));
            context.AddRange(database.Players);
            context.AddRange(group, database.Session);
            scores ??= [100, 90, 90, 80];
            for (int index = 0; index < database.Players.Length; index++)
            {
                ArkNovaGameSessionPlayer player = new(database.Session.Id, database.Players[index].Id, (PlayerColor)(index + 1), index * 2 + 1, index * 2 + 2, index * 2 + 1);
                player.SetScoreValues(scores[index], 7, null);
                context.Add(player);
            }
            await context.SaveChangesAsync(Token);
            return database;
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

using BoardgameTracker.Core.Groups;
using BoardgameTracker.App;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.Settings;
using BoardgameTracker.App.Features.Settings.MyGames;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.History;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Settings;

public sealed class GameActivationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private sealed class Handler(string key, string name) : IGameSessionNavigationHandler
    {
        public string GameKey => key;
        public string DisplayName => name;
        public string BadgeText => name;
        public Task OpenGameSessionAsync(Guid gameSessionId) => Task.CompletedTask;
        public Task OpenGroupAsync(Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus) => Task.CompletedTask;
    }

    [Fact]
    public void Adapter_DefaultsAndIndependentOptOutsPersistAcrossNewStoreInstances()
    {
        TestPreferences preferences = new();
        preferences.Set("UnrelatedSetting", "kept");
        MauiGameActivationStore first = new(preferences);
        Assert.True(first.IsEnabled("first"));
        Assert.True(first.IsEnabled("second"));
        first.SetEnabled("first", false);
        MauiGameActivationStore reopened = new(preferences);
        Assert.False(reopened.IsEnabled("first"));
        Assert.True(reopened.IsEnabled("second"));
        reopened.SetEnabled("second", false);
        reopened.SetEnabled("first", true);
        Assert.True(first.IsEnabled("first"));
        Assert.False(first.IsEnabled("second"));
        Assert.True(first.IsEnabled("later-registered"));
        Assert.Equal("kept", preferences.Get("UnrelatedSetting", ""));
        reopened.SetEnabled("second", true);
        Assert.Single(preferences.Values); // Reactivation removes opt-outs rather than saving an enabled list.
    }

    [Fact]
    public async Task Application_AllGamesDefaultEnabledAndNewGamesStartEnabled()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestPreferences preferences = new();
        MauiGameActivationStore store = new(preferences);
        GameActivationUseCase useCase = new(db, store);
        Assert.True(Assert.Single(await useCase.GetGamesAsync(Token)).IsEnabled);
        await useCase.SetEnabledAsync("ark-nova", false, Token);
        Game other = new("new-game", "new-game", "new-game", 1, 8);
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            seed.Games.Add(other);
            await seed.SaveChangesAsync(Token);
        }
        IReadOnlyList<GameActivationState> states = await new GameActivationUseCase(db, new MauiGameActivationStore(preferences)).GetGamesAsync(Token);
        Assert.False(states.Single(game => game.GameKey == "ark-nova").IsEnabled);
        Assert.True(states.Single(game => game.GameKey == other.Key).IsEnabled);
        await useCase.SetEnabledAsync("ark-nova", true, Token);
        Assert.All(await useCase.GetGamesAsync(Token), game => Assert.True(game.IsEnabled));
    }

    [Theory]
    [InlineData(GameSessionStatus.Running)]
    [InlineData(GameSessionStatus.Scoring)]
    [InlineData(GameSessionStatus.Completed)]
    public async Task Application_OnlyOpenSessionsBlockDisable(GameSessionStatus status)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await AddSessionAsync(db, status);
        TestPreferences preferences = new();
        MauiGameActivationStore store = new(preferences);
        GameActivationUseCase useCase = new(db, store);
        if (status == GameSessionStatus.Completed)
        {
            Assert.False((await useCase.SetEnabledAsync("ark-nova", false, Token)).IsEnabled);
            Assert.False(store.IsEnabled("ark-nova"));
        }
        else
        {
            await Assert.ThrowsAsync<GameActivationBlockedException>(() => useCase.SetEnabledAsync("ark-nova", false, Token));
            Assert.True(store.IsEnabled("ark-nova"));
            Assert.Empty(preferences.Values);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ViewModel_StaleStateCannotDisableOpenSessionAndSwitchIsRestored(bool scoring)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestPreferences preferences = new();
        MauiGameActivationStore store = new(preferences);
        MyGamesViewModel vm = CreateViewModel(db, store);
        await vm.LoadAsync(Token);
        GameActivationRow row = Assert.Single(vm.Games);
        Assert.Equal(BoardgameTracker.Core.Games.ArkNova.ArkNovaGame.Name, row.Name);
        Assert.True(row.IsEnabled);
        Assert.True(row.CanToggle);
        await db.AddSessionAsync(scoring);
        await vm.SetEnabledAsync(row, false, Token);
        Assert.True(vm.HasError);
        Assert.True(row.IsEnabled);
        Assert.True(row.IsLocked);
        Assert.False(row.CanToggle);
        Assert.True(store.IsEnabled("ark-nova"));
        Assert.Empty(preferences.Values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ViewModel_PreferenceFailureKeepsPersistedAndDisplayedState(bool initiallyDisabled)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestPreferences preferences = new();
        MauiGameActivationStore store = new(preferences);
        if (initiallyDisabled)
            store.SetEnabled("ark-nova", false);
        MyGamesViewModel vm = CreateViewModel(db, store);
        await vm.LoadAsync(Token);
        GameActivationRow row = Assert.Single(vm.Games);
        preferences.FailWrites = true;
        await vm.SetEnabledAsync(row, initiallyDisabled, Token);
        Assert.True(vm.HasError);
        Assert.Equal(!initiallyDisabled, row.IsEnabled);
        Assert.Equal(!initiallyDisabled, store.IsEnabled("ark-nova"));
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task PlayList_HidesDisabledAndRestoresReactivatedGamesWithSameSorting()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await AddSessionAsync(db, GameSessionStatus.Completed);
        Game other = new("second-game", "second-game", "second-game", 1, 8);
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            seed.Games.Add(other);
            await seed.SaveChangesAsync(Token);
        }
        TestPreferences preferences = new();
        MauiGameActivationStore store = new(preferences);
        PlayOverviewQuery query = new(db, store);
        string[] before = (await query.ListAsync(Token)).Select(game => game.Key).ToArray();
        Assert.Equal(new[] { "ark-nova", other.Key }, before);
        GameActivationUseCase useCase = new(db, store);
        await useCase.SetEnabledAsync("ark-nova", false, Token);
        Assert.Equal(other.Key, Assert.Single(await query.ListAsync(Token)).Key);
        GamesViewModel vm = new(query, [new Handler("ark-nova", "Game one"), new Handler(other.Key, "Game two")], NullLogger<GamesViewModel>.Instance);
        await vm.LoadAsync(Token);
        Assert.Equal(other.Key, Assert.Single(vm.Games).Key);
        await useCase.SetEnabledAsync("ark-nova", true, Token);
        Assert.Equal(before, (await query.ListAsync(Token)).Select(game => game.Key));
        await vm.LoadAsync(Token);
        Assert.Equal(before, vm.Games.Select(game => game.Key));
    }

    [Fact]
    public async Task DisablingPreservesHistoryGroupAssignmentsPlayersAndGameDefaults()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        GameSession session = await AddSessionAsync(db, GameSessionStatus.Completed);
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            var game = await seed.Games.SingleAsync(game => game.Id == db.GameId, Token);
            seed.GroupGames.Add(GroupGame.Create(db.Group, game));
            seed.GroupGamePlayerColors.Add(new(db.Group.Id, db.GameId, db.First.Id, PlayerColor.Blue));
            await seed.SaveChangesAsync(Token);
        }
        CompletedGameSessionsQuery history = new(db);
        HistoryGameSession before = Assert.Single(await history.ListAsync(cancellationToken: Token));
        MauiGameActivationStore store = new(new TestPreferences());
        GameActivationUseCase useCase = new(db, store);
        await useCase.SetEnabledAsync("ark-nova", false, Token);
        HistoryGameSession after = Assert.Single(await history.ListAsync(cancellationToken: Token));
        Assert.Equal(before.GameSessionId, after.GameSessionId);
        Assert.Equal(before.Players, after.Players);
        Assert.Equal(before.StartedAt, after.StartedAt);
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
        {
            Assert.Equal(2, await verify.Players.CountAsync(Token));
            Assert.Equal(db.Group.Id, (await verify.Groups.SingleAsync(Token)).Id);
            Assert.Equal(db.GameId, (await verify.Games.SingleAsync(Token)).Id);
            Assert.Equal(db.Group.Id, (await verify.GroupGames.SingleAsync(Token)).GroupId);
            Assert.Equal(PlayerColor.Blue, (await verify.GroupGamePlayerColors.SingleAsync(Token)).DefaultColor);
            Assert.Equal(1, (await verify.GroupPlayers.SingleAsync(player => player.PlayerId == db.First.Id, Token)).DefaultOrder);
            GameSession persisted = await verify.GameSessions.Include(candidate => candidate.Players).SingleAsync(Token);
            Assert.Equal(session.Id, persisted.Id);
            Assert.Equal(GameSessionStatus.Completed, persisted.Status);
            Assert.Equal(session.ModifiedAt, persisted.ModifiedAt);
            Assert.Equal(2, persisted.Players.Count);
        }
        await useCase.SetEnabledAsync("ark-nova", true, Token);
        Assert.Equal(db.GameId, Assert.Single(await new PlayOverviewQuery(db, store).ListAsync(Token)).Id);
        Assert.Equal(db.Group.Id, Assert.Single(await new BoardgameTracker.Application.Groups.ListGroupsQuery(db).ListGroupsAsync("ark-nova", Token)).Id);
    }

    [Fact]
    public async Task PlayRoot_IgnoresDisabledRememberedGameAndOpenSessionsRemainReachable()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        MauiGameActivationStore store = new(new TestPreferences());
        await new GameActivationUseCase(db, store).SetEnabledAsync("ark-nova", false, Token);
        var settings = new Presentation.MainSectionNavigationTests.SettingsStore();
        AppShell shell = new(new(), new(), new(), new(), settings, new(db, store));
        shell.EnableNavigationMemory();
        await shell.NavigateToMainSectionAsync(AppMainSection.Play);
        Assert.Single(shell.CurrentItem.CurrentItem.Navigation.NavigationStack);
        Assert.Empty(await new PlayOverviewQuery(db, store).ListAsync(Token));
        // A session started from a preparation already on the stack must never disappear.
        GameSession session = await db.AddSessionAsync(scoring: true);
        await shell.NavigateFromMainTabTapAsync(shell.CurrentItem.Items[0]);
        Assert.Equal("//GamesPage/GroupListPage", shell.Calls[^1].Route);
        Assert.True(Assert.Single(await new PlayOverviewQuery(db, store).ListAsync(Token)).HasOpenSession);
        Assert.Equal(session.Id, (await new OpenGameSessionQuery(db).GetAsync(session.Id, Token))!.GameSessionId);
        GameActivationState state = Assert.Single(await new GameActivationUseCase(db, store).GetGamesAsync(Token));
        Assert.True(state.IsEnabled);
        Assert.True(state.HasOpenSessions);
    }

    [Fact]
    public async Task ViewModel_CanDisableAndReactivateAndReloadWithoutASecondRegistration()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestPreferences preferences = new();
        MauiGameActivationStore store = new(preferences);
        MyGamesViewModel vm = CreateViewModel(db, store);
        await vm.LoadAsync(Token);
        GameActivationRow row = Assert.Single(vm.Games);
        await vm.SetEnabledAsync(row, false, Token);
        Assert.False(vm.HasError);
        Assert.False(row.IsEnabled);
        MyGamesViewModel reopened = CreateViewModel(db, new MauiGameActivationStore(preferences));
        await reopened.LoadAsync(Token);
        Assert.False(Assert.Single(reopened.Games).IsEnabled);
        await reopened.SetEnabledAsync(reopened.Games[0], true, Token);
        Assert.True(reopened.Games[0].IsEnabled);
        Assert.Empty(preferences.Values);
    }

    private static MyGamesViewModel CreateViewModel(SliceDatabase db, IGameActivationStore store) =>
        new(new(db, store), NullLogger<MyGamesViewModel>.Instance);

    private static async Task<GameSession> AddSessionAsync(SliceDatabase db, GameSessionStatus status)
    {
        GameSession session = await db.AddSessionAsync(scoring: status != GameSessionStatus.Running);
        if (status == GameSessionStatus.Completed)
        {
            await using BoardgameTrackerDbContext context = db.CreateDbContext();
            GameSession tracked = await context.GameSessions.Include(candidate => candidate.Players).SingleAsync(Token);
            foreach (GameSessionPlayer player in tracked.Players)
                player.SetPlacement(1);
            tracked.Complete(tracked.EndedAt!.Value);
            await context.SaveChangesAsync(Token);
            session = tracked;
        }
        return session;
    }
}

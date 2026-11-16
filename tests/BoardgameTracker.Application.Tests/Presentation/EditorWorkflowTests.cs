using BoardgameTracker.App.Features.Groups;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings.Navigation;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class EditorWorkflowTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("group")]
    [InlineData("existing-player")]
    [InlineData("new-player")]
    public async Task Save_PersistenceFailureKeepsDraftAndSkipsNavigation(string kind) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        EditGroupViewModel group = new(new(db), new(db, new TestGameActivationStore()), navigation, NullLogger<EditGroupViewModel>.Instance);
        EditPlayerViewModel player = new(new(db), new(db), navigation, NullLogger<EditPlayerViewModel>.Instance);
        if (kind == "group") {
            await group.LoadAsync(db.Group.Id, Token);
            group.GroupName = "Kept group";
        }
        else {
            if (kind == "new-player")
                player.InitializeNew();
            else
                await player.LoadAsync(db.First.Id, Token);
            player.PlayerName = "Kept player";
            player.IsPlayerGuest = true;
        }
        await using (BoardgameTrackerDbContext reject = db.CreateDbContext()) {
            string trigger = kind switch {
                "group" => "CREATE TRIGGER reject_write BEFORE UPDATE ON Groups BEGIN SELECT RAISE(ABORT, 'test write failure'); END",
                "new-player" => "CREATE TRIGGER reject_write BEFORE INSERT ON Players BEGIN SELECT RAISE(ABORT, 'test write failure'); END",
                _ => "CREATE TRIGGER reject_write BEFORE UPDATE ON Players BEGIN SELECT RAISE(ABORT, 'test write failure'); END"
            };
            await reject.Database.ExecuteSqlRawAsync(trigger, Token);
        }

        if (kind == "group") {
            await group.SaveCommand.ExecuteAsync(null);
            Assert.False(group.IsSaveCompleted);
            Assert.False(group.IsBusy);
            Assert.True(group.CanEdit);
            Assert.True(group.HasError);
            Assert.StartsWith(AppResources.SaveGroupFailed, group.ErrorMessage);
            Assert.Equal("Kept group", group.GroupName);
        }
        else {
            Assert.False(await player.SaveAsync(Token));
            Assert.False(player.IsSaveCompleted);
            Assert.False(player.IsBusy);
            Assert.True(player.CanEdit);
            Assert.Equal(AppResources.PlayerSaveFailed, player.ErrorMessage);
            Assert.Equal("Kept player", player.PlayerName);
            Assert.True(player.IsPlayerGuest);
        }
        Assert.Empty(navigation.Calls);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(db.Group.Name, (await verify.Groups.SingleAsync(Token)).Name);
        Assert.Equal("Anna", (await verify.Players.SingleAsync(value => value.Id == db.First.Id, Token)).Name);
        Assert.Equal(2, await verify.Players.CountAsync(Token));
    }

    [Theory]
    [InlineData("group")]
    [InlineData("existing-player")]
    [InlineData("new-player")]
    public async Task Save_BlocksCompetingActionsDuringWriteAndNavigatesAfterSuccess(string kind) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        GatedContextFactory factory = new(db);
        EditorNavigationFake navigation = new();
        EditGroupViewModel group = new(new(factory), new(db, new TestGameActivationStore()), navigation, NullLogger<EditGroupViewModel>.Instance);
        EditPlayerViewModel player = new(new(factory), new(factory), navigation, NullLogger<EditPlayerViewModel>.Instance);
        if (kind == "group") {
            await group.LoadAsync(db.Group.Id, Token);
            group.GroupName = "Saved group";
        }
        else {
            if (kind == "new-player")
                player.InitializeNew();
            else
                await player.LoadAsync(db.First.Id, Token);
            player.PlayerName = "Saved player";
        }
        factory.IsGated = true;
        IAsyncRelayCommand save = kind == "group" ? group.SaveCommand : player.SaveCommand;
        IAsyncRelayCommand back = kind == "group" ? group.GoBackCommand : player.GoBackCommand;
        Task pending = save.ExecuteAsync(null);
        await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        try {
            Assert.False(pending.IsCompleted);
            Assert.False(save.CanExecute(null));
            Assert.False(back.CanExecute(null));
            Assert.True(kind == "group" ? group.IsBusy : player.IsBusy);
            if (kind == "group")
                await save.ExecuteAsync(null);
            else
                Assert.False(await player.SaveAsync(Token).WaitAsync(TimeSpan.FromSeconds(5), Token));
            await back.ExecuteAsync(null);
            if (kind == "group") {
                GroupMemberDraft first = group.Players[0];
                group.MovePlayerDownCommand.Execute(first);
                group.AddGameCommand.Execute(group.GameSelections[0]);
                group.ChooseGameCommand.Execute(null);
                Assert.Same(first, group.Players[0]);
                Assert.Empty(group.SelectedVisibleGames);
                Assert.False(group.IsChoosingGame);
            }
            else {
                Assert.False(await player.SaveAndAddAnotherAsync(Token).WaitAsync(TimeSpan.FromSeconds(5), Token));
                player.InitializeNew();
                Assert.Equal("Saved player", player.PlayerName);
            }
            Assert.Empty(navigation.Calls);
        }
        finally {
            factory.Release.TrySetResult();
        }
        await pending;

        Assert.False(kind == "group" ? group.IsBusy : player.IsBusy);
        Assert.True(kind == "group" ? group.IsSaveCompleted : player.IsSaveCompleted);
        Assert.False(kind == "group" ? group.CanEdit : player.CanEdit);
        Assert.Null(kind == "group" ? group.ErrorMessage : player.ErrorMessage);
        var call = Assert.Single(navigation.Calls);
        Assert.Equal(kind == "new-player" ? "main" : "..", call.Route);
        Assert.Equal(kind == "new-player" ? AppMainSection.Participants : (AppMainSection?)null, call.Section);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(kind == "new-player" ? 3 : 2, await verify.Players.CountAsync(Token));
        if (kind == "group")
            Assert.Equal("Saved group", (await verify.Groups.SingleAsync(Token)).Name);
        else
            Assert.Equal("Saved player", (await verify.Players.SingleAsync(value => value.Id == player.PlayerId, Token)).Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Load_BlocksSaveAndBackAndKeepsEditsOnReappearance(bool editGroup) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        GatedContextFactory factory = new(db) { IsGated = true };
        EditorNavigationFake navigation = new();
        EditGroupViewModel group = new(new(factory), new(db, new TestGameActivationStore()), navigation, NullLogger<EditGroupViewModel>.Instance);
        EditPlayerViewModel player = new(new(factory), new(factory), navigation, NullLogger<EditPlayerViewModel>.Instance);
        Task loading = editGroup ? group.LoadAsync(db.Group.Id, Token) : player.LoadAsync(db.First.Id, Token);
        await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        try {
            if (editGroup) {
                await group.SaveCommand.ExecuteAsync(null);
                await group.GoBackCommand.ExecuteAsync(null);
                Assert.Equal(Guid.Empty, group.GroupId);
            }
            else {
                Assert.False(await player.SaveAsync(Token));
                Assert.False(await player.SaveAndAddAnotherAsync(Token));
                await player.GoBackCommand.ExecuteAsync(null);
                player.InitializeNew();
                Assert.False(player.IsCreateMode);
            }
            Assert.Empty(navigation.Calls);
        }
        finally {
            factory.Release.TrySetResult();
        }
        await loading;
        if (editGroup) {
            group.GroupName = "Kept edit";
            await group.LoadAsync(db.Group.Id, Token);
            Assert.Equal("Kept edit", group.GroupName);
            Assert.True(group.SaveCommand.CanExecute(null));
        }
        else {
            player.PlayerName = "Kept edit";
            await player.LoadAsync(db.First.Id, Token);
            Assert.Equal("Kept edit", player.PlayerName);
            Assert.True(player.SaveCommand.CanExecute(null));
        }
    }

    [Fact]
    public async Task GameSelection_NotifiesProjectionsOnlyAfterSuccessfulCommands() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditGroupViewModel group = new(new(db), new(db, new TestGameActivationStore()), new EditorNavigationFake(), NullLogger<EditGroupViewModel>.Instance);
        List<string?> changes = [];
        group.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        await group.LoadAsync(db.Group.Id, Token);
        Assert.Equal(1, changes.Count(value => value == nameof(group.SelectedVisibleGames)));
        GroupGameSelection game = Assert.Single(group.GamesAvailableToAdd);
        changes.Clear();
        group.ChooseGameCommand.Execute(null);
        group.AddGameCommand.Execute(game);
        Assert.False(group.IsChoosingGame);
        Assert.Same(game, Assert.Single(group.SelectedVisibleGames));
        Assert.Empty(group.GamesAvailableToAdd);
        Assert.False(group.HasGamesAvailableToAdd);
        Assert.Equal(1, changes.Count(value => value == nameof(group.SelectedVisibleGames)));
        changes.Clear();
        group.AddGameCommand.Execute(game);
        group.RemoveGameCommand.Execute(new GroupGameSelection(game.Id, game.Name, true));
        Assert.Empty(changes);
        group.RemoveGameCommand.Execute(game);
        Assert.Empty(group.SelectedVisibleGames);
        Assert.Same(game, Assert.Single(group.GamesAvailableToAdd));
        Assert.True(group.HasGamesAvailableToAdd);
        Assert.Equal(new[] { nameof(group.SelectedVisibleGames), nameof(group.GamesAvailableToAdd), nameof(group.HasGamesAvailableToAdd) }, changes);
        changes.Clear();
        group.RemoveGameCommand.Execute(game);
        Assert.Empty(changes);
    }

    [Fact]
    public async Task GroupCanExecute_NotifiesNameAndCountChangesWithoutRedundantLoadNotification() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditGroupViewModel group = new(new(db), new(db, new TestGameActivationStore()), new EditorNavigationFake(), NullLogger<EditGroupViewModel>.Instance);
        List<bool> saveStates = [];
        group.SaveCommand.CanExecuteChanged += (_, _) => saveStates.Add(group.SaveCommand.CanExecute(null));
        await group.LoadAsync(db.Group.Id, Token);
        Assert.Equal(new[] { false, false, false, true }, saveStates);
        saveStates.Clear();
        group.GroupName = "";
        group.GroupName = "Valid";
        GroupMemberDraft first = group.Players[0];
        group.Players.Clear();
        group.Players.Add(first);
        Assert.Equal(new[] { false, true, false, true }, saveStates);
        saveStates.Clear();
        group.Players.Add(new(db.Second.Id, "Ben", false));
        saveStates.Clear();
        group.MovePlayerDownCommand.Execute(first);
        Assert.Empty(saveStates);
        Assert.Equal(new[] { db.Second.Id, db.First.Id }, group.Players.Select(value => value.PlayerId!.Value));
        Assert.Equal(new[] { 1, 2 }, group.Players.Select(value => value.Position));
        Assert.False(group.Players[0].CanMoveUp);
        Assert.True(group.Players[0].CanMoveDown);
        Assert.True(group.Players[1].CanMoveUp);
        Assert.False(group.Players[1].CanMoveDown);
        group.MovePlayerUpCommand.Execute(first);
        group.MovePlayerUpCommand.Execute(first);
        group.MovePlayerDownCommand.Execute(group.Players[^1]);
        Assert.Equal(new[] { db.First.Id, db.Second.Id }, group.Players.Select(value => value.PlayerId!.Value));
        Assert.Equal(new[] { 1, 2 }, group.Players.Select(value => value.Position));
        Assert.Empty(saveStates);
    }

    [Fact]
    public async Task PlayerCommands_NotifyBusyAndNewPlayerDependenciesOnce() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditPlayerViewModel player = new(new(db), new(db), new EditorNavigationFake(), NullLogger<EditPlayerViewModel>.Instance);
        List<bool> saveStates = [];
        List<bool> addStates = [];
        List<bool> backStates = [];
        player.SaveCommand.CanExecuteChanged += (_, _) => saveStates.Add(player.SaveCommand.CanExecute(null));
        player.SaveAndAddAnotherCommand.CanExecuteChanged += (_, _) => addStates.Add(player.SaveAndAddAnotherCommand.CanExecute(null));
        player.GoBackCommand.CanExecuteChanged += (_, _) => backStates.Add(player.GoBackCommand.CanExecute(null));
        Assert.False(player.SaveAndAddAnotherCommand.CanExecute(null));
        player.InitializeNew();
        Assert.Equal(new[] { true }, addStates);
        Assert.Empty(saveStates);
        Assert.Empty(backStates);
        addStates.Clear();
        player.PlayerName = "Next player";
        Assert.True(await player.SaveAndAddAnotherAsync(Token));
        Assert.Equal(new[] { false, true }, saveStates);
        Assert.Equal(new[] { false, true }, addStates);
        Assert.Equal(new[] { false, true }, backStates);
    }

    [Fact]
    public async Task AddAnother_AfterNavigationFailureKeepsSavedIdentityAndStartsFreshDraft() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new() { FailNavigation = true };
        EditPlayerViewModel player = new(new(db), new(db), navigation, NullLogger<EditPlayerViewModel>.Instance);
        player.InitializeNew();
        player.PlayerName = "Saved guest";
        player.IsPlayerGuest = true;
        Assert.False(await player.SaveAsync(Token));
        Guid savedId = player.PlayerId;
        await using (BoardgameTrackerDbContext saved = db.CreateDbContext())
            await saved.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_insert BEFORE INSERT ON Players BEGIN SELECT RAISE(ABORT, 'unexpected insert'); END", Token);
        await player.SaveAndAddAnotherCommand.ExecuteAsync(null);
        Assert.True(player.IsCreateMode);
        Assert.False(player.IsBusy);
        Assert.False(player.IsSaveCompleted);
        Assert.Equal(Guid.Empty, player.PlayerId);
        Assert.Empty(player.PlayerName);
        Assert.False(player.IsPlayerGuest);
        Assert.Null(player.ErrorMessage);
        Assert.Single(navigation.Calls);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(3, await verify.Players.CountAsync(Token));
        Assert.Equal("Saved guest", (await verify.Players.SingleAsync(value => value.Id == savedId, Token)).Name);
    }

    [Theory]
    [InlineData("group")]
    [InlineData("existing-player")]
    [InlineData("new-player")]
    public async Task UnsavedBack_DoesNotWriteAndSystemBackRetainsFrameworkBehavior(string kind) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        EditGroupViewModel group = new(new(db), new(db, new TestGameActivationStore()), navigation, NullLogger<EditGroupViewModel>.Instance);
        EditPlayerViewModel player = new(new(db), new(db), navigation, NullLogger<EditPlayerViewModel>.Instance);
        EventBoundaryTestHost page;
        IAsyncRelayCommand back;
        if (kind == "group") {
            await group.LoadAsync(db.Group.Id, Token);
            group.GroupName = "Unsaved";
            group.ChooseGameCommand.Execute(null);
            page = EventBoundaryTests.Create("EditGroupPage.xaml.cs", groupEditor: group);
            await page.InvokeAsync("OnBackButtonPressed");
            Assert.False(group.IsChoosingGame);
            Assert.Empty(navigation.Calls);
            back = group.GoBackCommand;
        } else {
            if (kind == "new-player")
                player.InitializeNew();
            else
                await player.LoadAsync(db.First.Id, Token);
            player.PlayerName = "Unsaved";
            page = EventBoundaryTests.Create("EditPlayerPage.xaml.cs", player);
            back = player.GoBackCommand;
        }
        var systemBack = page.GetType().GetMethod("OnBackButtonPressed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Assert.False((bool)systemBack.Invoke(page, null)!);
        navigation.FailNavigation = true;
        await back.ExecuteAsync(null);
        Assert.Equal("..", Assert.Single(navigation.Calls).Route);
        Assert.Equal(AppResources.NavigationFailed, kind == "group" ? group.ErrorMessage : player.ErrorMessage);
        Assert.False(kind == "group" ? group.IsSaveCompleted : player.IsSaveCompleted);
        Assert.True(kind == "group" ? group.CanEdit : player.CanEdit);
        navigation.FailNavigation = false;
        navigation.OnNavigating = () => Assert.Null(kind == "group" ? group.ErrorMessage : player.ErrorMessage);
        await back.ExecuteAsync(null);
        Assert.Equal(2, navigation.Calls.Count);
        Assert.All(navigation.Calls, call => Assert.Equal("..", call.Route));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(db.Group.Name, (await verify.Groups.SingleAsync(Token)).Name);
        Assert.Equal("Anna", (await verify.Players.SingleAsync(value => value.Id == db.First.Id, Token)).Name);
        Assert.Equal(2, await verify.Players.CountAsync(Token));
    }

    private sealed class GatedContextFactory(IDbContextFactory<BoardgameTrackerDbContext> inner) : IDbContextFactory<BoardgameTrackerDbContext> {
        public bool IsGated { get; set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public BoardgameTrackerDbContext CreateDbContext() => inner.CreateDbContext();
        public async Task<BoardgameTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) {
            if (IsGated) {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return await inner.CreateDbContextAsync(cancellationToken);
        }
    }
}

using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Settings.MyGames;
using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class DialogWorkflowTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static CreateGroupViewModel CreateDraft(SliceDatabase db, EditorNavigationFake navigation)
        => new(new(db), new(db), new(db), new(db, new TestGameEnabledStore()), navigation, NullLogger<CreateGroupViewModel>.Instance);

    [Fact]
    public async Task Subdialogs_ReplaceEachOtherAndBackDiscardsOnlyNewPlayerInput() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        CreateGroupViewModel draft = CreateDraft(db, navigation);
        await draft.LoadAsync(Token);
        draft.GroupName = "Kept";
        draft.ChooseExistingPlayerCommand.Execute(null);
        Assert.Equal(GroupCreationSubdialog.SelectExistingPlayer, draft.ActiveSubdialog);
        draft.AddExistingPlayerCommand.Execute(draft.AvailablePlayers[0]);
        draft.ChooseGameCommand.Execute(null);
        Assert.Equal(GroupCreationSubdialog.SelectGame, draft.ActiveSubdialog);
        draft.BeginNewPlayerCommand.Execute(null);
        Assert.Equal(GroupCreationSubdialog.CreatePlayer, draft.ActiveSubdialog);
        draft.NewPlayerName = "Discarded";
        draft.NewPlayerIsGuest = true;
        await draft.GoBackCommand.ExecuteAsync(null);
        Assert.Equal(GroupCreationSubdialog.None, draft.ActiveSubdialog);
        Assert.Empty(draft.NewPlayerName);
        Assert.False(draft.NewPlayerIsGuest);
        Assert.Equal("Kept", draft.GroupName);
        Assert.Single(draft.Players);
        Assert.Empty(navigation.Calls);
        await draft.LoadAsync(Token);
        Assert.Equal("Kept", draft.GroupName);
        Assert.Single(draft.Players);
    }

    [Fact]
    public async Task Collections_NotifyCountChangesAndRefreshGamesOnceAfterMembershipMutation() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        CreateGroupViewModel draft = CreateDraft(db, new());
        await draft.LoadAsync(Token);
        List<string?> changes = [];
        draft.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        var first = draft.AvailablePlayers[0];
        draft.AddExistingPlayerCommand.Execute(first);
        Assert.False(draft.HasNoPlayers);
        Assert.True(draft.HasAvailablePlayers);
        Assert.Contains(nameof(draft.HasNoPlayers), changes);
        Assert.Contains(nameof(draft.HasAvailablePlayers), changes);
        Assert.Single(changes, name => name == nameof(draft.AvailableGames));
        changes.Clear();
        draft.AddExistingPlayerCommand.Execute(first);
        Assert.Single(draft.Players);
        Assert.Empty(changes);
        draft.AddExistingPlayerCommand.Execute(draft.AvailablePlayers[0]);
        Assert.False(draft.HasAvailablePlayers);
        changes.Clear();
        draft.MovePlayerUpCommand.Execute(draft.Players[1]);
        Assert.DoesNotContain(nameof(draft.AvailableGames), changes);
        Assert.DoesNotContain(nameof(draft.HasNoPlayers), changes);
        Assert.Equal(new[] { 1, 2 }, draft.Players.Select(player => player.Position));
        changes.Clear();
        draft.AddGameCommand.Execute(draft.AvailableGames[0]);
        Assert.Single(changes, name => name == nameof(draft.SelectedGames));
        Assert.Single(draft.SelectedGames);
        changes.Clear();
        draft.RemoveGameCommand.Execute(draft.SelectedGames[0]);
        Assert.Single(changes, name => name == nameof(draft.SelectedGames));
        Assert.Empty(draft.SelectedGames);
        draft.RemovePlayerCommand.Execute(draft.Players[0]);
        Assert.True(draft.HasAvailablePlayers);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DuplicateDecision_NavigationRetryNeverRepeatsCompletedWrite(bool archived, bool createAdditional) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync(archived);
        EditorNavigationFake navigation = new() { FailNavigation = true };
        CreateGroupViewModel draft = CreateDraft(db, navigation);
        await draft.LoadAsync(Token);
        draft.GroupName = "Additional";
        foreach (var player in draft.AvailablePlayers.ToArray())
            draft.AddExistingPlayerCommand.Execute(player);
        await draft.SaveCommand.ExecuteAsync(null);
        if (createAdditional)
            await draft.CreateAdditionalGroupCommand.ExecuteAsync(null);
        else
            await draft.ReuseDuplicateCommand.ExecuteAsync(Assert.Single(draft.Duplicates));
        Assert.True(draft.IsSaveCompleted);
        Assert.False(draft.IsBusy);
        Assert.False(draft.CanEdit);
        Assert.True(draft.HasError);
        Guid savedId = Assert.Single(navigation.GroupOutcomes).GroupId;
        await using (BoardgameTrackerDbContext reject = db.CreateDbContext()) {
            await reject.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_insert BEFORE INSERT ON Groups BEGIN SELECT RAISE(ABORT, 'unexpected insert'); END", Token);
            await reject.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_update BEFORE UPDATE ON Groups BEGIN SELECT RAISE(ABORT, 'unexpected update'); END", Token);
        }
        navigation.FailNavigation = false;
        await draft.ReturnToDraftCommand.ExecuteAsync(null);
        Assert.False(draft.HasError);
        Assert.Equal(2, navigation.GroupOutcomes.Count);
        Assert.All(navigation.GroupOutcomes, outcome => {
            Assert.Equal(savedId, outcome.GroupId);
            Assert.True(outcome.FromDuplicates);
        });
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(createAdditional ? 2 : 1, await verify.Groups.CountAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.False((await verify.Groups.SingleAsync(group => group.Id == savedId, Token)).IsArchived);
    }

    [Fact]
    public async Task DuplicateCancellation_FailedBackRetainsDecisionAndSuccessfulBackClearsItWithoutWriting() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        CreateGroupViewModel draft = CreateDraft(db, navigation);
        await draft.LoadAsync(Token);
        draft.GroupName = "Kept";
        foreach (var player in draft.AvailablePlayers.ToArray())
            draft.AddExistingPlayerCommand.Execute(player);
        await draft.SaveCommand.ExecuteAsync(null);
        navigation.FailNavigation = true;
        await draft.ReturnToDraftCommand.ExecuteAsync(null);
        Assert.Single(draft.Duplicates);
        Assert.True(draft.HasError);
        navigation.FailNavigation = false;
        await draft.ReturnToDraftCommand.ExecuteAsync(null);
        Assert.Empty(draft.Duplicates);
        Assert.Equal("Kept", draft.GroupName);
        Assert.Equal(2, draft.Players.Count);
        await draft.CreateAdditionalGroupCommand.ExecuteAsync(null);
        Assert.Empty(navigation.GroupOutcomes);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Creation_BlocksCompetingActionsThroughoutCompletionNavigation() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EditorNavigationFake navigation = new() { NavigationGate = release.Task };
        CreateGroupViewModel draft = CreateDraft(db, navigation);
        await draft.LoadAsync(Token);
        draft.GroupName = "Saved once";
        draft.AddExistingPlayerCommand.Execute(draft.AvailablePlayers[0]);
        Task saving = draft.SaveCommand.ExecuteAsync(null);
        await navigation.NavigationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        try {
            Assert.True(draft.IsBusy);
            Assert.True(draft.IsSaveCompleted);
            Assert.False(draft.SaveCommand.CanExecute(null));
            Assert.False(draft.GoBackCommand.CanExecute(null));
            draft.BeginNewPlayerCommand.Execute(null);
            draft.RemovePlayerCommand.Execute(draft.Players[0]);
            await draft.GoBackCommand.ExecuteAsync(null);
            Assert.Equal(GroupCreationSubdialog.None, draft.ActiveSubdialog);
            Assert.Single(draft.Players);
            Assert.Single(navigation.GroupOutcomes);
        } finally {
            release.TrySetResult();
        }
        await saving;
        Assert.False(draft.IsBusy);
    }

    [Fact]
    public async Task EditGroup_BackClosesSelectionWithoutSavingOrNavigating() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        EditGroupViewModel editor = new(new(db), new(db, new TestGameEnabledStore()), navigation, NullLogger<EditGroupViewModel>.Instance);
        await editor.LoadAsync(db.Group.Id, Token);
        Assert.Equal(EditGroupSubdialog.None, editor.ActiveSubdialog);
        editor.ChooseGameCommand.Execute(null);
        Assert.Equal(EditGroupSubdialog.SelectGame, editor.ActiveSubdialog);
        editor.CloseGameSelectionCommand.Execute(null);
        Assert.Equal(EditGroupSubdialog.None, editor.ActiveSubdialog);
        editor.ChooseGameCommand.Execute(null);
        await editor.GoBackCommand.ExecuteAsync(null);
        Assert.Equal(EditGroupSubdialog.None, editor.ActiveSubdialog);
        Assert.Empty(navigation.Calls);
        Assert.False(editor.IsSaveCompleted);
    }

    [Fact]
    public async Task ManualGame_SaveNavigationRetryIgnoresStaleInvalidInputsAndNeverWritesAgain() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new() { FailNavigation = true };
        TestGameEnabledStore store = new();
        ManualGameEditorViewModel editor = new(new(db, store), new(db, store), new(db, store), navigation, NullLogger<ManualGameEditorViewModel>.Instance);
        await editor.LoadAsync(null, Token);
        editor.GameName = "Saved manual game";
        editor.BadgeText = "S";
        Assert.False(await editor.SaveAsync(Token));
        Assert.True(editor.IsSaveCompleted);
        Assert.False(editor.CanEdit);
        Assert.Equal(AppResources.NavigationFailed, editor.ErrorMessage);
        await using (BoardgameTrackerDbContext reject = db.CreateDbContext())
            await reject.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_write BEFORE UPDATE ON Games BEGIN SELECT RAISE(ABORT, 'unexpected update'); END", Token);
        editor.GameName = "";
        editor.MinimumPlayers = "invalid";
        navigation.FailNavigation = false;
        Assert.True(await editor.SaveAsync(Token));
        Assert.Null(editor.ErrorMessage);
        Assert.Equal(2, navigation.Calls.Count);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Single(await verify.ManualGames.ToArrayAsync(Token));
        Assert.True(await verify.Games.AnyAsync(game => game.Name == "Saved manual game", Token));
    }

    [Fact]
    public async Task MyGames_FailedBackReleasesBusyGuardAndCanBeRetried() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new() { FailNavigation = true };
        MyGamesViewModel model = new(new(db, new TestGameEnabledStore()), new(db, new TestGameEnabledStore()), [], navigation, NullLogger<MyGamesViewModel>.Instance);
        await model.GoBackCommand.ExecuteAsync(null);
        Assert.False(model.IsBusy);
        Assert.Equal(AppResources.NavigationFailed, model.ErrorMessage);
        Assert.True(model.GoBackCommand.CanExecute(null));
        navigation.FailNavigation = false;
        await model.GoBackCommand.ExecuteAsync(null);
        Assert.Null(model.ErrorMessage);
        Assert.Equal(2, navigation.Calls.Count);
    }

    [Fact]
    public async Task ExistingGroup_NavigationRetryDoesNotWriteAssignmentTwice() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        FailingPreparationNavigation handler = new();
        GroupListViewModel model = new(new(db), new(db), new(db, new TestGameEnabledStore()), NullLogger<GroupListViewModel>.Instance, [handler], new EditorNavigationFake());
        model.SetGameContext("ark-nova");
        await model.LoadAsync(Token);
        AvailablePlayGroup group = Assert.Single(model.AvailableGroups);
        model.ChooseExistingCommand.Execute(null);
        Assert.Equal(GroupListSubdialog.SelectExistingGroup, model.ActiveSubdialog);
        await model.SelectExistingAsync(group);
        Assert.Equal(GroupListSubdialog.None, model.ActiveSubdialog);
        Assert.True(model.HasError);
        await using (BoardgameTrackerDbContext reject = db.CreateDbContext())
            await reject.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_write BEFORE INSERT ON GroupGames BEGIN SELECT RAISE(ABORT, 'unexpected insert'); END", Token);
        Assert.Empty(model.AvailableGroups);
        Assert.Equal(group.Id, Assert.Single(model.Groups).Id);
        handler.Fail = false;
        await model.OpenGroupCommand.ExecuteAsync(group.Id);
        Assert.False(model.HasError);
        Assert.False(model.IsWorking);
        Assert.False(model.CanChoose);
        Assert.Empty(model.AvailableGroups);
        Assert.Equal(2, handler.Attempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingGroup_SelectionClosesBeforeNavigationAndRespectsBusy(bool useBack) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        GroupListViewModel model = new(new(db), new(db), new(db, new TestGameEnabledStore()), NullLogger<GroupListViewModel>.Instance, [], navigation);
        model.SetGameContext("ark-nova");
        await model.LoadAsync(Token);
        List<string?> changes = [];
        model.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        Assert.Equal(GroupListSubdialog.None, model.ActiveSubdialog);
        Assert.True(model.CanInteract);

        model.ChooseExistingCommand.Execute(null);
        Assert.Equal(GroupListSubdialog.SelectExistingGroup, model.ActiveSubdialog);
        Assert.False(model.CanInteract);
        Assert.Contains(nameof(model.CanInteract), changes);
        Assert.True(model.TryBeginNavigation());
        model.CloseSelectionCommand.Execute(null);
        await model.GoBackCommand.ExecuteAsync(null);
        Assert.Equal(GroupListSubdialog.SelectExistingGroup, model.ActiveSubdialog);
        Assert.Empty(navigation.Calls);

        model.EndNavigation();
        changes.Clear();
        if (useBack)
            await model.GoBackCommand.ExecuteAsync(null);
        else
            model.CloseSelectionCommand.Execute(null);
        Assert.Equal(GroupListSubdialog.None, model.ActiveSubdialog);
        Assert.True(model.CanInteract);
        Assert.Contains(nameof(model.CanInteract), changes);
        Assert.Empty(navigation.Calls);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));

        await model.GoBackCommand.ExecuteAsync(null);
        Assert.Single(navigation.Calls);
    }

    [Fact]
    public async Task ManualGame_ModalDialogBlocksCommandsAndNotifiesTheirAvailability() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        TestGameEnabledStore store = new();
        ManualGameEditorViewModel editor = new(new(db, store), new(db, store), new(db, store), navigation, NullLogger<ManualGameEditorViewModel>.Instance);
        await editor.LoadAsync(null, Token);
        editor.GameName = "Unsaved";
        editor.BadgeText = "U";
        int saveChanges = 0;
        int backChanges = 0;
        editor.SaveCommand.CanExecuteChanged += (_, _) => saveChanges++;
        editor.GoBackCommand.CanExecuteChanged += (_, _) => backChanges++;

        editor.IsModalDialogOpen = true;
        Assert.False(editor.CanInteract);
        Assert.False(editor.CanEdit);
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.False(editor.GoBackCommand.CanExecute(null));
        Assert.Equal(1, saveChanges);
        Assert.Equal(1, backChanges);
        Assert.False(await editor.SaveAsync(Token));
        await editor.GoBackCommand.ExecuteAsync(null);
        Assert.Empty(navigation.Calls);
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
            Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));

        int saveChangesBeforeClose = saveChanges;
        int backChangesBeforeClose = backChanges;
        editor.IsModalDialogOpen = false;
        Assert.True(editor.CanInteract);
        Assert.True(editor.CanEdit);
        Assert.True(editor.SaveCommand.CanExecute(null));
        Assert.True(editor.GoBackCommand.CanExecute(null));
        Assert.Equal(saveChangesBeforeClose + 1, saveChanges);
        Assert.Equal(backChangesBeforeClose + 1, backChanges);
    }

    [Theory]
    [InlineData("EditGroupPage", "EditGroupSubdialog.SelectGame")]
    [InlineData("GroupListPage", "GroupListSubdialog.SelectExistingGroup")]
    public void OwnSelectionOverlayUsesEnumConverter(string pageName, string enumMember) {
        var page = AppPageHeaderTests.ReadPage(pageName);
        var overlay = Assert.Single(page.Descendants(), element => (string?)element.Attribute("BackgroundColor") == "{DynamicResource ModalOverlay}");
        Assert.Equal("{Binding ActiveSubdialog, Converter={StaticResource EnumEquals}, ConverterParameter={x:Static local:" + enumMember + "}}", (string?)overlay.Attribute("IsVisible"));
        if (pageName == "EditGroupPage") {
            var condition = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Binding") == "{Binding ActiveSubdialog}");
            Assert.Equal("{x:Static local:EditGroupSubdialog.None}", (string?)condition.Attribute("Value"));
        } else {
            Assert.Contains(page.Descendants(), element => (string?)element.Attribute("IsEnabled") == "{Binding CanInteract}");
        }
    }

    private sealed class FailingPreparationNavigation : IGameSessionNavigationHandler {
        public bool Fail { get; set; } = true;
        public int Attempts { get; private set; }
        public bool CanHandle(string gameKey) => gameKey == "ark-nova";
        public string DisplayName => "Game";
        public string BadgeText => "G";
        public Task OpenGameSessionAsync(Guid gameSessionId) => Task.CompletedTask;
        public Task OpenGroupAsync(string gameKey, Guid groupId, Guid? openGameSessionId, GameSessionStatus? status) {
            Attempts++;
            return Fail ? Task.FromException(new InvalidOperationException("Preparation unavailable")) : Task.CompletedTask;
        }
    }
}

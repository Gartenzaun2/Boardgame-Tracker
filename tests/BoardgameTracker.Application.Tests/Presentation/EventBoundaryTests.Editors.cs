using System.Xml.Linq;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed partial class EventBoundaryTests
{
    private static CancellationToken EditorToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PlayerSave_WithoutExistingPlayerIdShowsAndLogsFailure() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorLogger<EditPlayerViewModel> logger = new();
        EditorNavigationFake navigation = new();
        EditPlayerViewModel editor = new(new(db), new(db), navigation, logger);
        editor.PlayerName = "Unloaded player";

        Assert.False(await editor.SaveAsync(EditorToken));

        Assert.Equal(AppResources.PlayerSaveFailed, editor.ErrorMessage);
        Assert.True(editor.HasError);
        Assert.False(editor.IsBusy);
        Assert.False(editor.IsSaveCompleted);
        Assert.Empty(navigation.Calls);
        Assert.Equal("Unloaded player", editor.PlayerName);
        Assert.Contains("no player ID", Assert.Single(logger.ErrorMessages));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(2, await verify.Players.CountAsync(EditorToken));
        Assert.Equal("Anna", (await verify.Players.SingleAsync(player => player.Id == db.First.Id, EditorToken)).Name);
        Assert.Equal("Ben", (await verify.Players.SingleAsync(player => player.Id == db.Second.Id, EditorToken)).Name);
    }

    [Fact]
    public async Task PlayerAddAnother_SavesAndResetsWithoutNavigating() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        EditPlayerViewModel editor = new(new(db), new(db), navigation, NullLogger<EditPlayerViewModel>.Instance);
        editor.InitializeNew();
        editor.PlayerName = "Another guest";
        editor.IsPlayerGuest = true;
        await editor.SaveAndAddAnotherCommand.ExecuteAsync(null);

        Assert.Empty(navigation.Calls);
        Assert.True(editor.CanEdit);
        Assert.True(editor.IsCreateMode);
        Assert.False(editor.IsSaveCompleted);
        Assert.Empty(editor.PlayerName);
        Assert.False(editor.IsPlayerGuest);
        Assert.Equal(Guid.Empty, editor.PlayerId);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(3, await verify.Players.CountAsync(EditorToken));
        Assert.True((await verify.Players.SingleAsync(player => player.Name == "Another guest", EditorToken)).IsGuest);
    }

    [Theory]
    [InlineData("save")]
    [InlineData("back")]
    [InlineData("OnBackButtonPressed")]
    public async Task GroupCompletion_AwaitsNavigationAndRetriesWithoutWritingAgain(string retryAction)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorLogger<EditGroupViewModel> logger = new();
        EditorNavigationFake navigation = new();
        EditGroupViewModel editor = new(new(db), new(db, new TestGameEnabledStore()), navigation, logger);
        await editor.LoadAsync(db.Group.Id, EditorToken);
        editor.GroupName = "Updated group";
        editor.MovePlayerDownCommand.Execute(editor.Players[0]);
        EventBoundaryTestHost page = Create("EditGroupPage.xaml.cs", groupEditor: editor);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        navigation.NavigationGate = release.Task;
        navigation.FailNavigation = true;
        Task pending = editor.SaveCommand.ExecuteAsync(null);
        await navigation.NavigationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), EditorToken);
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.True(editor.IsBusy);
            Assert.True(editor.IsSaveCompleted);
            Assert.False(editor.CanEdit);
            Assert.False(editor.SaveCommand.CanExecute(null));
            await using BoardgameTrackerDbContext saved = db.CreateDbContext();
            Assert.Equal("Updated group", (await saved.Groups.SingleAsync(EditorToken)).Name);
            Assert.Equal(db.Second.Id, (await saved.GroupPlayers.SingleAsync(player => player.DefaultOrder == 1, EditorToken)).PlayerId);
            await saved.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectRepeatedGroupWrite BEFORE UPDATE ON GroupPlayers BEGIN SELECT RAISE(ABORT, 'unexpected repeated write'); END;", EditorToken);
            await editor.SaveCommand.ExecuteAsync(null);
            await editor.GoBackCommand.ExecuteAsync(null);
            await page.InvokeAsync("OnBackButtonPressed");
            Assert.Single(navigation.Calls);
        }
        finally { release.TrySetResult(); }
        await pending;
        Assert.False(editor.IsBusy);
        Assert.False(editor.CanEdit);
        Assert.True(editor.SaveCommand.CanExecute(null));
        Assert.Equal(AppResources.Done, editor.SaveText);
        Assert.Equal(AppResources.GroupEditorNavigationFailed, editor.ErrorMessage);
        Assert.Same(navigation.NavigationFailure, Assert.Single(logger.Errors));
        editor.GroupName = "";
        editor.Players.Clear();
        Assert.True(editor.SaveCommand.CanExecute(null));
        navigation.FailNavigation = false;
        navigation.OnNavigating = () => Assert.Null(editor.ErrorMessage);
        if (retryAction == "save")
            await editor.SaveCommand.ExecuteAsync(null);
        else if (retryAction == "back")
            await editor.GoBackCommand.ExecuteAsync(null);
        else
        {
            await page.InvokeAsync(retryAction);
            await editor.GoBackCommand.ExecutionTask!;
        }
        Assert.Null(editor.ErrorMessage);
        Assert.Equal(2, navigation.Calls.Count);
        Assert.All(navigation.Calls, call => Assert.Equal("..", call.Route));
        Assert.Single(logger.Errors);
        await using BoardgameTrackerDbContext retried = db.CreateDbContext();
        Assert.Equal(db.Group.Id, (await retried.Groups.SingleAsync(EditorToken)).Id);
        Assert.Equal("Updated group", (await retried.Groups.SingleAsync(EditorToken)).Name);
    }

    [Theory]
    [InlineData(true, "save")]
    [InlineData(false, "save")]
    [InlineData(true, "back")]
    [InlineData(false, "back")]
    [InlineData(true, "OnBackButtonPressed")]
    [InlineData(false, "OnBackButtonPressed")]
    public async Task PlayerCompletion_FreezesSavedInputsAndRetriesSameIdentity(bool create, string retryAction)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditorNavigationFake navigation = new();
        EditorLogger<EditPlayerViewModel> logger = new();
        EditPlayerViewModel editor = new(new(db), new(db), navigation, logger);
        Assert.Equal(AppResources.Save, editor.SaveText);
        if (create)
            editor.InitializeNew();
        else
            await editor.LoadAsync(db.First.Id, EditorToken);
        editor.PlayerName = "Saved player";
        editor.IsPlayerGuest = create;
        editor.IsPlayerArchived = !create;
        EventBoundaryTestHost page = Create("EditPlayerPage.xaml.cs", editor);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        navigation.NavigationGate = release.Task;
        navigation.FailNavigation = true;
        Task pending = editor.SaveCommand.ExecuteAsync(null);
        await navigation.NavigationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), EditorToken);
        Guid savedId = editor.PlayerId;
        try
        {
            Assert.False(pending.IsCompleted);
            Assert.True(editor.IsSaveCompleted);
            Assert.False(editor.CanEdit);
            Assert.True(editor.IsBusy);
            Assert.False(editor.SaveCommand.CanExecute(null));
            Assert.False(editor.GoBackCommand.CanExecute(null));
            Assert.False(editor.SaveAndAddAnotherCommand.CanExecute(null));
            await editor.SaveCommand.ExecuteAsync(null);
            await editor.GoBackCommand.ExecuteAsync(null);
            Assert.False(await editor.SaveAndAddAnotherAsync(EditorToken));
            await page.InvokeAsync("OnBackButtonPressed");
            editor.InitializeNew();
            Assert.Equal(savedId, editor.PlayerId);
            Assert.Single(navigation.Calls);
        }
        finally { release.TrySetResult(); }
        await pending;
        Assert.False(editor.IsBusy);
        Assert.False(editor.CanEdit);
        Assert.Equal(AppResources.Done, editor.SaveText);
        Assert.True(editor.HasError);
        Assert.Equal(AppResources.PlayerEditorNavigationFailed, editor.ErrorMessage);
        Assert.Empty(page.Messages);
        Assert.Same(navigation.NavigationFailure, Assert.Single(logger.Errors));
        await using (BoardgameTrackerDbContext saved = db.CreateDbContext())
        {
            Player player = await saved.Players.SingleAsync(player => player.Id == savedId, EditorToken);
            Assert.Equal("Saved player", player.Name);
            Assert.Equal(create, player.IsGuest);
            Assert.Equal(!create, player.IsArchived);
            await saved.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectRepeatedPlayerInsert BEFORE INSERT ON Players BEGIN SELECT RAISE(ABORT, 'unexpected repeated insert'); END;", EditorToken);
            await saved.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectRepeatedPlayerUpdate BEFORE UPDATE ON Players BEGIN SELECT RAISE(ABORT, 'unexpected repeated update'); END;", EditorToken);
        }
        editor.PlayerName = "";
        navigation.FailNavigation = false;
        navigation.OnNavigating = () => Assert.Null(editor.ErrorMessage);
        if (retryAction == "save")
            await editor.SaveCommand.ExecuteAsync(null);
        else if (retryAction == "back")
            await editor.GoBackCommand.ExecuteAsync(null);
        else {
            await page.InvokeAsync(retryAction);
            await editor.GoBackCommand.ExecutionTask!;
        }
        Assert.Equal(savedId, editor.PlayerId);
        Assert.True(create || savedId == db.First.Id);
        Assert.Equal(2, navigation.Calls.Count);
        Assert.All(navigation.Calls, call => {
            Assert.Equal(create ? "main" : "..", call.Route);
            Assert.Equal(create ? AppMainSection.Participants : (AppMainSection?)null, call.Section);
        });
        Assert.Null(editor.ErrorMessage);
        Assert.Empty(page.Messages);
        Assert.Single(logger.Errors);
        await using BoardgameTrackerDbContext retried = db.CreateDbContext();
        Assert.Equal(create ? 3 : 2, await retried.Players.CountAsync(EditorToken));
        Assert.Equal("Saved player", (await retried.Players.SingleAsync(player => player.Id == savedId, EditorToken)).Name);
    }

    [Theory]
    [InlineData(false, 0, 0, false)]
    [InlineData(false, 0, 3, false)]
    [InlineData(false, 100, 0, true)]
    [InlineData(false, 100, 3, true)]
    [InlineData(false, 101, 0, false)]
    [InlineData(false, 101, 3, false)]
    [InlineData(true, 0, 0, false)]
    [InlineData(true, 0, 3, false)]
    [InlineData(true, 100, 0, true)]
    [InlineData(true, 100, 3, true)]
    [InlineData(true, 101, 0, false)]
    [InlineData(true, 101, 3, false)]
    public async Task PlayerNameValidation_UsesTrimmedLengthWithoutChangingRejectedData(bool create, int length, int padding, bool valid)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditPlayerViewModel editor = new(new(db), new(db), new EditorNavigationFake(), NullLogger<EditPlayerViewModel>.Instance);
        if (create)
            editor.InitializeNew();
        else
            await editor.LoadAsync(db.First.Id, EditorToken);
        string name = new('N', length);
        editor.PlayerName = new string(' ', padding) + name + new string(' ', padding);
        editor.IsPlayerArchived = true;
        Assert.Equal(valid, await editor.SaveAsync(EditorToken));
        Assert.Equal(valid, editor.IsSaveCompleted);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(create && valid ? 3 : 2, await verify.Players.CountAsync(EditorToken));
        if (valid)
        {
            Player player = await verify.Players.SingleAsync(player => player.Id == editor.PlayerId, EditorToken);
            Assert.Equal(name, player.Name);
            Assert.Equal(!create, player.IsArchived);
            Assert.Null(editor.ErrorMessage);
        }
        else
        {
            Assert.True(editor.CanEdit);
            Assert.Equal(length == 0 ? AppResources.PlayerNameRequired : AppResources.PlayerNameTooLong, editor.ErrorMessage);
            Player player = await verify.Players.SingleAsync(player => player.Id == db.First.Id, EditorToken);
            Assert.Equal("Anna", player.Name);
            Assert.False(player.IsArchived);
        }
    }

    [Theory]
    [InlineData("PlayerFeature.EditPlayerPage.xaml", "IsSaveCompleted")]
    [InlineData("GeneralGroupFeature.EditGroupPage.xaml", "IsSaveCompleted")]
    public void SavedEditor_BindsInputLockAndVisibleCompletionHint(string resource, string savedProperty)
    {
        using Stream stream = typeof(EventBoundaryTests).Assembly.GetManifestResourceStream(resource)!;
        XDocument page = XDocument.Load(stream);
        XElement name = Assert.Single(page.Descendants(), element => element.Name.LocalName == "Entry");
        Assert.Contains(name.Ancestors(), element => (string?)element.Attribute("IsEnabled") == "{Binding CanEdit}");
        Assert.Contains(page.Descendants(), element => element.Name.LocalName == "Label"
            && (string?)element.Attribute("IsVisible") == "{Binding " + savedProperty + "}"
            && (string?)element.Attribute("Text") == "{x:Static res:AppResources.EditorSavedHint}");
        Assert.Contains(page.Descendants(), element => element.Name.LocalName == "Button" && (string?)element.Attribute("Text") == "{Binding SaveText}");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PlayerNameValidation_DuplicateDoesNotChangeSavedPlayers(bool create)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditPlayerViewModel editor = new(new(db), new(db), new EditorNavigationFake(), NullLogger<EditPlayerViewModel>.Instance);
        if (create)
            editor.InitializeNew();
        else
            await editor.LoadAsync(db.First.Id, EditorToken);
        editor.PlayerName = "  Ben  ";
        editor.IsPlayerArchived = true;
        Assert.False(await editor.SaveAsync(EditorToken));
        Assert.False(editor.IsSaveCompleted);
        Assert.True(editor.CanEdit);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(2, await verify.Players.CountAsync(EditorToken));
        Player first = await verify.Players.SingleAsync(player => player.Id == db.First.Id, EditorToken);
        Assert.Equal("Anna", first.Name);
        Assert.False(first.IsArchived);
    }

    [Fact]
    public async Task PlayerStateChanges_NotifyBindingsAndRejectConcurrentSave() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditPlayerViewModel editor = new(new(db), new(db), new EditorNavigationFake(), NullLogger<EditPlayerViewModel>.Instance);
        List<string?> changed = [];
        List<bool> editingStates = [];
        List<Task<bool>> concurrentSaves = [];
        editor.PropertyChanged += (_, args) => {
            changed.Add(args.PropertyName);
            if (args.PropertyName == nameof(editor.CanEdit))
                editingStates.Add(editor.CanEdit);
            if (args.PropertyName == nameof(editor.IsBusy) && editor.IsBusy)
                concurrentSaves.Add(editor.SaveAsync(EditorToken));
        };

        editor.InitializeNew();
        Assert.Contains(nameof(editor.Title), changed);
        Assert.Contains(nameof(editor.IsCreateMode), changed);
        Assert.Equal(AppResources.AddPlayer, editor.Title);
        Assert.False(await editor.SaveAsync(EditorToken));
        Assert.Contains(nameof(editor.HasError), changed);
        Assert.True(editor.HasError);

        changed.Clear();
        concurrentSaves.Clear();
        editingStates.Clear();
        editor.PlayerName = "New guest";
        editor.IsPlayerGuest = true;
        Assert.Contains(nameof(editor.PlayerName), changed);
        Assert.Contains(nameof(editor.IsPlayerGuest), changed);
        Assert.True(await editor.SaveAsync(EditorToken));
        Assert.False(await Assert.Single(concurrentSaves));
        Assert.False(editor.IsBusy);
        Assert.True(editor.IsSaveCompleted);
        Assert.False(editor.CanEdit);
        Assert.False(editor.HasError);
        Assert.Equal(AppResources.Done, editor.SaveText);
        Assert.NotEqual(Guid.Empty, editor.PlayerId);
        Assert.Contains(nameof(editor.PlayerId), changed);
        Assert.Contains(nameof(editor.IsSaveCompleted), changed);
        Assert.Contains(nameof(editor.SaveText), changed);
        Assert.Contains(nameof(editor.HasError), changed);
        Assert.NotEmpty(editingStates);
        Assert.All(editingStates, state => Assert.False(state));

        changed.Clear();
        editor.InitializeNew();
        Assert.Equal(Guid.Empty, editor.PlayerId);
        Assert.False(editor.IsSaveCompleted);
        Assert.True(editor.CanEdit);
        Assert.Empty(editor.PlayerName);
        Assert.False(editor.IsPlayerGuest);
        Assert.Equal(AppResources.Save, editor.SaveText);
        Assert.Contains(nameof(editor.PlayerId), changed);
        Assert.Contains(nameof(editor.IsSaveCompleted), changed);
        Assert.Contains(nameof(editor.CanEdit), changed);
        Assert.Contains(nameof(editor.SaveText), changed);

        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Player saved = await verify.Players.SingleAsync(player => player.Name == "New guest", EditorToken);
        Assert.True(saved.IsGuest);
        Assert.Equal(3, await verify.Players.CountAsync(EditorToken));
    }

    [Fact]
    public void PlayerEditor_BindsNegationsToOriginalObservableState() {
        using Stream stream = typeof(EventBoundaryTests).Assembly.GetManifestResourceStream("PlayerFeature.EditPlayerPage.xaml")!;
        XDocument page = XDocument.Load(stream);
        Assert.DoesNotContain(page.Descendants(), element => element.Name.LocalName == "ContentPage.Resources");
        XElement[] buttons = page.Descendants().Where(element => element.Name.LocalName == "Button").ToArray();
        Assert.Equal(3, buttons.Length);
        Assert.Equal(new[] { "{Binding SaveAndAddAnotherCommand}", "{Binding SaveCommand}", "{Binding SaveCommand}" },
            buttons.Select(button => (string?)button.Attribute("Command")));
        Assert.All(buttons, button => Assert.Null(button.Attribute("Clicked")));
        XElement content = page.Root!.Elements().Single();
        Assert.Equal("{Binding IsBusy, Converter={StaticResource NegateBool}}", (string?)content.Attribute("IsEnabled"));
        XElement archivedSwitch = Assert.Single(page.Descendants(), element => (string?)element.Attribute("IsToggled") == "{Binding IsPlayerArchived}");
        Assert.Equal("{Binding IsCreateMode, Converter={StaticResource NegateBool}}", (string?)archivedSwitch.Parent!.Attribute("IsVisible"));
    }

    private sealed class EditorLogger<T> : ILogger<T>
    {
        public List<Exception> Errors { get; } = [];
        public List<string> ErrorMessages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
                ErrorMessages.Add(formatter(state, exception));
            if (exception is not null)
                Errors.Add(exception);
        }
    }
}

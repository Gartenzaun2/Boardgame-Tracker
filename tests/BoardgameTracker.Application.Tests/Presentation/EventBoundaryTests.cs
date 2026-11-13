using System.Reflection;
using System.Runtime.Loader;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Settings.Navigation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed partial class EventBoundaryTests
{
    [Theory]
    [InlineData("GamesPage.xaml.cs", "OnGameTapped", "groups")]
    [InlineData("GroupListPage.xaml.cs", "OnCreateGroupClicked", "CreateGroupPage")]
    [InlineData("iOSThemeShellRenderer.cs", "IosTapAsync", "tab")]
    public async Task RemainingNavigationBoundary_LogsOneTechnicalErrorAndAllowsRetry(string source, string action, string route)
    {
        EventBoundaryTestHost page = Create(source);
        page.Navigation.FailNavigation = true;
        await page.InvokeAsync(action);
        Assert.Same(page.Navigation.NavigationFailure, Assert.Single(page.Logger.Errors));
        Assert.Equal(source == "GamesPage.xaml.cs" ? AppResources.OpenGroupFailed : AppResources.NavigationFailed,
            Assert.Single(page.Messages));
        page.Navigation.FailNavigation = false;
        await page.InvokeAsync(action);
        Assert.Equal(2, page.Navigation.Calls.Count);
        Assert.All(page.Navigation.Calls, call => Assert.Equal(route, call.Route));
        Assert.Single(page.Logger.Errors);
        Assert.Single(page.Messages);
        Assert.True(page.IsEnabled);
        Assert.False(page.ViewModel.IsBusy);
    }

    [Theory]
    [InlineData("database initialization")]
    [InlineData("Shell creation")]
    [InlineData("startup navigation resolution")]
    [InlineData("session restoration")]
    public async Task StartupFailure_LogsTheActualStageAndShowsErrorPage(string stage)
    {
        EventBoundaryTestHost page = Create("App.xaml.cs");
        page.StartupFailureStage = stage;
        await page.InvokeAsync("InitializeApplicationAsync");
        Assert.Same(page.StartupFailure, Assert.Single(page.Logger.Errors));
        Assert.Equal(stage, Assert.Single(page.Logger.StartupStages));
        Assert.Same(page.StartupErrorPage, page.StartupWindow.Page);
        Assert.False(page.Navigation.MemoryEnabled);
    }

    [Fact]
    public async Task StartupResumeFailure_RetryRunsTheSameTargetAndEnablesMemoryOnlyAfterSuccess()
    {
        EventBoundaryTestHost page = Create("App.xaml.cs");
        page.StartupFailureStage = "session restoration";
        await page.InvokeAsync("InitializeApplicationAsync");
        Assert.False(page.Navigation.MemoryEnabled);
        page.StartupFailureStage = null;
        await page.InvokeAsync("InitializeApplicationAsync");
        Assert.Same(page.Navigation, page.StartupWindow.Page);
        Assert.True(page.Navigation.MemoryEnabled);
        Assert.Equal(2, page.ResumeAttempts.Count);
        Assert.All(page.ResumeAttempts, id => Assert.Equal(page.StartupSettings.LastGameSessionId, id));
        Assert.Single(page.Logger.Errors);
    }

    [Fact]
    public async Task StartedSession_NavigationRetryReusesSavedIdentityWithoutStartingAgain()
    {
        EventBoundaryTestHost page = Create("ArkNovaPreparationPage.xaml.cs");
        page.Navigation.FailNavigation = true;
        await page.InvokeAsync("OnStartClicked");
        Assert.Equal(1, page.ViewModel.StartCalls);
        Assert.Same(page.Navigation.NavigationFailure, Assert.Single(page.Logger.Errors));
        Assert.Equal(AppResources.NavigationAfterStartFailed, Assert.Single(page.Messages));

        page.ViewModel.CanStart = false;
        page.Navigation.FailNavigation = false;
        await page.InvokeAsync("OnStartClicked");
        Assert.Equal(1, page.ViewModel.StartCalls);
        Assert.Equal(2, page.Navigation.Calls.Count);
        Assert.All(page.Navigation.Calls, call =>
        {
            Assert.Equal("ArkNovaRunningPage", call.Route);
            Assert.Equal(page.ViewModel.GameSessionId, call.Parameters![NavigationParameters.GameSessionId]);
        });
        Assert.Single(page.Messages);
        Assert.Single(page.Logger.Errors);
    }

    [Theory]
    [InlineData("OnEndClicked")]
    [InlineData("OnDiscardClicked")]
    public async Task RunningWrite_NavigationRetryDoesNotRepeatPersistedOperation(string action)
    {
        EventBoundaryTestHost page = Create("ArkNovaRunningPage.xaml.cs");
        page.Navigation.FailNavigation = true;
        await page.InvokeAsync(action);
        Assert.Same(page.Navigation.NavigationFailure, Assert.Single(page.Logger.Errors));
        Assert.Equal(action == "OnEndClicked" ? AppResources.NavigationAfterScoringStartedFailed : AppResources.NavigationAfterDiscardFailed,
            Assert.Single(page.Messages));

        page.Navigation.FailNavigation = false;
        await page.InvokeAsync(action);
        Assert.Equal(action == "OnEndClicked" ? 1 : 0, page.ViewModel.EndCalls);
        Assert.Equal(action == "OnDiscardClicked" ? 1 : 0, page.ViewModel.DiscardCalls);
        Assert.Equal(action == "OnDiscardClicked" ? 1 : 0, page.ConfirmationCalls);
        Assert.Equal(2, page.Navigation.Calls.Count);
        Assert.All(page.Navigation.Calls, call => Assert.Equal(action == "OnEndClicked" ? "ArkNovaScoringPage" : "return", call.Route));
        Assert.Single(page.Logger.Errors);
        Assert.Single(page.Messages);
    }

    [Fact]
    public async Task CancelledDiscard_DoesNotWriteAndReleasesGuardForNextAttempt()
    {
        EventBoundaryTestHost page = Create("ArkNovaRunningPage.xaml.cs");
        page.ConfirmDiscard = false;
        await page.InvokeAsync("OnDiscardClicked");
        Assert.Equal(0, page.ViewModel.DiscardCalls);
        Assert.Empty(page.Navigation.Calls);
        Assert.Empty(page.Messages);
        Assert.Empty(page.Logger.Errors);

        page.ConfirmDiscard = true;
        await page.InvokeAsync("OnDiscardClicked");
        Assert.Equal(1, page.ViewModel.DiscardCalls);
        Assert.Equal("return", Assert.Single(page.Navigation.Calls).Route);
        Assert.Equal(2, page.ConfirmationCalls);
    }

    [Theory]
    [InlineData("ArkNovaPreparationPage.xaml.cs", "OnStartClicked")]
    [InlineData("ArkNovaRunningPage.xaml.cs", "OnEndClicked")]
    [InlineData("ArkNovaRunningPage.xaml.cs", "OnDiscardClicked")]
    public async Task HandledWriteFailure_DoesNotNavigateOrAddAnotherError(string source, string action)
    {
        EventBoundaryTestHost page = Create(source);
        page.ViewModel.WriteSucceeds = false;
        await page.InvokeAsync(action);
        Assert.Empty(page.Navigation.Calls);
        Assert.Empty(page.Logger.Errors);
        Assert.Empty(page.Messages);

        page.ViewModel.WriteSucceeds = true;
        await page.InvokeAsync(action);
        Assert.Single(page.Navigation.Calls);
        Assert.Equal(2, page.ViewModel.StartCalls + page.ViewModel.EndCalls + page.ViewModel.DiscardCalls);
    }

    [Theory]
    [InlineData("ArkNovaPreparationPage.xaml.cs", "OnStartClicked")]
    [InlineData("ArkNovaRunningPage.xaml.cs", "OnEndClicked")]
    [InlineData("ArkNovaRunningPage.xaml.cs", "OnDiscardClicked")]
    public async Task ConcurrentTap_DoesNotDuplicateWriteOrNavigation(string source, string action)
    {
        EventBoundaryTestHost page = Create(source);
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        page.ViewModel.WriteGate = gate.Task;
        Task first = page.InvokeAsync(action);
        Assert.False(first.IsCompleted);
        await page.InvokeAsync(action);
        gate.SetResult();
        await first;
        Assert.Equal(1, page.ViewModel.StartCalls + page.ViewModel.EndCalls + page.ViewModel.DiscardCalls);
        Assert.Single(page.Navigation.Calls);
    }

    [Theory]
    [InlineData("AppSettingsPage.xaml.cs", "OnMyGamesClicked", "MyGamesPage", false)]
    [InlineData("AppSettingsPage.xaml.cs", "OnLicensesClicked", "LicenseListPage", false)]
    [InlineData("ArkNovaRunningPage.xaml.cs", "OnBackClicked", "return", false)]
    [InlineData("ArkNovaRunningPage.xaml.cs", "OnBackButtonPressed", "return", false)]
    [InlineData("ArkNovaScoringPage.xaml.cs", "OnBackClicked", "return", false)]
    [InlineData("ArkNovaScoringPage.xaml.cs", "OnBackButtonPressed", "return", false)]
    [InlineData("ArkNovaScoringPage.xaml.cs", "OnBackButtonPressed", "..", true)]
    [InlineData("AndroidThemeShellRenderer.cs", "AndroidTapAsync", "tab", false)]
    public async Task NavigationBoundary_LogsShowsErrorAndAllowsRetry(string source, string action, string route, bool historical)
    {
        EventBoundaryTestHost page = Create(source);
        page.ViewModel.IsHistorical = historical;
        page.Navigation.FailNavigation = true;
        await page.InvokeAsync(action);
        Assert.Same(page.Navigation.NavigationFailure, Assert.Single(page.Logger.Errors));
        Assert.Equal(AppResources.NavigationFailed, Assert.Single(page.Messages));

        page.Navigation.FailNavigation = false;
        await page.InvokeAsync(action);
        Assert.Equal(2, page.Navigation.Calls.Count);
        Assert.All(page.Navigation.Calls, call => Assert.Equal(route, call.Route));
        Assert.Single(page.Messages);
        Assert.Single(page.Logger.Errors);
        Assert.False(page.ViewModel.IsBusy);
    }

    [Theory]
    [InlineData("ArkNovaPreparationPage.xaml.cs", "OnStartClicked")]
    [InlineData("ArkNovaRunningPage.xaml.cs", "OnEndClicked")]
    [InlineData("ArkNovaScoringPage.xaml.cs", "OnBackButtonPressed")]
    [InlineData("AppSettingsPage.xaml.cs", "OnLicensesClicked")]
    [InlineData("AndroidThemeShellRenderer.cs", "AndroidTapAsync")]
    public async Task ErrorDialogFailure_IsLoggedWithoutEscapingBoundary(string source, string action)
    {
        EventBoundaryTestHost page = Create(source);
        page.Navigation.FailNavigation = true;
        page.FailErrorDialog = true;
        await page.InvokeAsync(action);
        Assert.Equal(2, page.Logger.Errors.Count);
        Assert.Same(page.Navigation.NavigationFailure, page.Logger.Errors[0]);
        Assert.Same(page.DialogFailure, page.Logger.Errors[1]);
        Assert.Single(page.Messages);
    }

    internal static EventBoundaryTestHost Create(string file, EditPlayerViewModel? playerEditor = null, EditGroupViewModel? groupEditor = null)
    {
        Assembly assembly = typeof(EventBoundaryTests).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name =>
            (name.EndsWith("." + file, StringComparison.Ordinal) || name.EndsWith("/" + file, StringComparison.Ordinal) || name.EndsWith("\\" + file, StringComparison.Ordinal))
            && (name.StartsWith("PresentationBoundary.", StringComparison.Ordinal) || name.StartsWith("EventBoundary.", StringComparison.Ordinal)
                || name.StartsWith("GeneralGroupFeature.", StringComparison.Ordinal)
                || name.StartsWith("PlayerFeature.", StringComparison.Ordinal)));
        using StreamReader reader = new(assembly.GetManifestResourceStream(resource)!);
        SyntaxNode root = CSharpSyntaxTree.ParseText(reader.ReadToEnd()).GetRoot();
        string[] names = file switch
        {
            "ArkNovaPreparationPage.xaml.cs" => ["OnStartClicked"],
            "ArkNovaRunningPage.xaml.cs" => ["OnBackClicked", "OnBackButtonPressed", "ReturnAsync", "OnEndClicked", "OnDiscardClicked", "ShowNavigationErrorAsync"],
            "ArkNovaScoringPage.xaml.cs" => ["OnBackClicked", "OnBackButtonPressed", "ReturnAsync"],
            "GamesPage.xaml.cs" => ["OnGameTapped"],
            "GroupListPage.xaml.cs" => ["OnCreateGroupClicked"],
            "App.xaml.cs" => ["InitializeApplicationAsync"],
            "EditPlayerPage.xaml.cs" => ["OnSaveClicked", "OnAddAnotherClicked", "SaveAsync", "OnBackClicked", "OnBackButtonPressed"],
            "EditGroupPage.xaml.cs" => ["OnSavedAsync", "OnBackClicked", "OnBackButtonPressed"],
            _ => ["OnMyGamesClicked", "OnLicensesClicked", "ShowNavigationErrorAsync"]
        };
        IEnumerable<string> fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(field => field.Declaration.Type.ToString() is "bool" or "Guid?").Select(field => field.ToFullString());
        IEnumerable<string> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => names.Contains(method.Identifier.ValueText)).Select(method =>
            {
                // Await the original event body deterministically, without a native UI loop.
                if (method.Modifiers.Any(SyntaxKind.AsyncKeyword) && method.ReturnType.ToString() == "void")
                    method = method.WithReturnType(SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space));
                method = method.WithModifiers(SyntaxFactory.TokenList(method.Modifiers.Where(token => !token.IsKind(SyntaxKind.OverrideKeyword))));
                return method.ToFullString();
            });
        if (file is "AndroidThemeShellRenderer.cs" or "iOSThemeShellRenderer.cs")
        {
            BlockSyntax body = root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>()
                .Single(lambda => lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)).Block!;
            string method = file == "AndroidThemeShellRenderer.cs" ? "AndroidTapAsync" : "IosTapAsync";
            methods = ["private async Task " + method + "() { var shell = Navigation; var logger = Logger; object tab = new(); object target = tab; " + body.ToFullString() + " }"];
        }
        string code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using BoardgameTracker.App.Navigation;
            using BoardgameTracker.App.Resources.Localization;
            using BoardgameTracker.Core.Games.ArkNova;
            using BoardgameTracker.Application.GameSessions;
            using BoardgameTracker.Application.Settings.Navigation;
            using BoardgameTracker.Application.Tests.Presentation;
            using Shell = BoardgameTracker.Application.Tests.Presentation.BoundaryShell;
            using AppShell = BoardgameTracker.Application.Tests.Presentation.BoundaryShell;
            using AppDialogPage = BoardgameTracker.Application.Tests.Presentation.BoundaryDialogs;
            using Window = BoardgameTracker.Application.Tests.Presentation.BoundaryWindow;
            using TappedEventArgs = BoardgameTracker.Application.Tests.Presentation.BoundaryTappedEventArgs;
            using IGameSessionResumeHandler = BoardgameTracker.Application.Tests.Presentation.EventBoundaryTestHost;
            using ShellNavigationQueryParameters = System.Collections.Generic.Dictionary<string, object>;
            public sealed class Handler : EventBoundaryTestHost
            {
            """ + (file == "EditPlayerPage.xaml.cs" ? "private new BoardgameTracker.App.Features.Players.EditPlayer.EditPlayerViewModel _viewModel => PlayerEditor!;\n" : string.Empty)
            + (file == "EditGroupPage.xaml.cs" ? "private new BoardgameTracker.App.Features.Groups.EditGroup.EditGroupViewModel _viewModel => GroupEditor!;\n" : string.Empty)
            + string.Join("\n", fields.Concat(methods)) + """
            }
            public sealed class ArkNovaRunningPage;
            public sealed class ArkNovaScoringPage;
            public sealed class MyGamesPage;
            public sealed class LicenseListPage;
            public sealed class CreateGroupPage;
            """;
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        MetadataReference[] references = paths.Append(assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        using StreamReader parametersReader = new(assembly.GetManifestResourceStream("LicenseNavigation.NavigationParameters.cs")!);
        CSharpCompilation compilation = CSharpCompilation.Create("EventBoundary_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(code), CSharpSyntaxTree.ParseText(parametersReader.ReadToEnd())], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream output = new();
        var result = compilation.Emit(output);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        output.Position = 0;
        Assembly compiled = AssemblyLoadContext.Default.LoadFromStream(output);
        EventBoundaryTestHost page = (EventBoundaryTestHost)Activator.CreateInstance(compiled.GetType("Handler")!)!;
        page.PlayerEditor = playerEditor;
        page.GroupEditor = groupEditor;
        BoundaryShell.Current = page.Navigation;
        page.Navigation.CurrentPage = page;
        return page;
    }
}

// Only the platform/UI dependencies are doubles; the executed action bodies and guards come from embedded production source.
public class EventBoundaryTestHost
{
    public EditPlayerViewModel? PlayerEditor { get; set; }
    public EditGroupViewModel? GroupEditor { get; set; }
    public BoundaryElement EditorContent { get; } = new();
    protected virtual bool OnBackButtonPressed() => false;
    public BoundaryViewModel ViewModel { get; } = new();
    public BoundaryLogger Logger { get; } = new();
    public BoundaryShell Navigation { get; } = new();
    public BoundaryDispatcher Dispatcher { get; } = new();
    public List<string> Messages { get; } = [];
    public int ConfirmationCalls { get; set; }
    public bool ConfirmDiscard { get; set; } = true;
    public bool FailErrorDialog { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? StartupFailureStage { get; set; }
    public Exception StartupFailure { get; } = new InvalidOperationException("Startup unavailable");
    public BoundaryWindow StartupWindow { get; } = new();
    public object StartupErrorPage { get; } = new();
    public AppNavigationSettings StartupSettings { get; set; } = AppNavigationSettings.Default with { LastGameSessionId = Guid.NewGuid() };
    public List<Guid> ResumeAttempts { get; } = [];
    protected EventBoundaryTestHost _databaseInitializer => this;
    protected EventBoundaryTestHost _services => this;
    protected EventBoundaryTestHost _navigationSettingsStore => this;
    protected EventBoundaryTestHost _startupNavigation => this;
    protected EventBoundaryTestHost[] _resumeHandlers => [this];
    public Task InitializeAsync() => StartupFailureStage == "database initialization" ? Task.FromException(StartupFailure) : Task.CompletedTask;
    public T GetRequiredService<T>() => StartupFailureStage == "Shell creation" ? throw StartupFailure : (T)(object)Navigation;
    public AppNavigationSettings Get() => StartupSettings;
    public void Save(AppNavigationSettings state) => StartupSettings = state;
    public Task<StartupNavigation> ResolveAsync(AppNavigationSettings state) => StartupFailureStage == "startup navigation resolution"
        ? Task.FromException<StartupNavigation>(StartupFailure)
        : Task.FromResult(new StartupNavigation(AppMainSection.Play, new(state.LastGameSessionId!.Value, "game", BoardgameTracker.Core.GameSessions.GameSessionStatus.Running, DateTimeOffset.UtcNow)));
    public bool CanHandle(string key) => true;
    public Task ResumeAsync(BoundaryShell shell, OpenGameSession session)
    {
        ResumeAttempts.Add(session.GameSessionId);
        return StartupFailureStage == "session restoration" ? Task.FromException(StartupFailure) : Task.CompletedTask;
    }
    protected object CreateStartupErrorPage(BoundaryWindow window, Exception exception) => StartupErrorPage;
    public Exception DialogFailure { get; } = new InvalidOperationException("Dialog unavailable");
    protected BoundaryViewModel _viewModel => ViewModel;
    protected BoundaryLogger _logger => Logger;
    public async Task InvokeAsync(string method)
    {
        MethodInfo action = GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!;
        object?[]? arguments = action.GetParameters().Length switch
        {
            0 => null,
            1 => [StartupWindow],
            _ => [null, action.GetParameters()[1].ParameterType == typeof(BoundaryTappedEventArgs) ? new BoundaryTappedEventArgs() : EventArgs.Empty]
        };
        object? result = action.Invoke(this, arguments);
        if (result is Task task) await task;
        else await Dispatcher.Pending;
    }
}

public sealed class BoundaryElement { public bool IsEnabled { get; set; } = true; }

public sealed class BoundaryViewModel
{
    public bool CanCreate => true;
    public string SelectedGameKey => "game";
    public List<BoundaryGame> Games { get; } = [new()];
    public bool CanStart { get; set; } = true;
    public bool IsBusy { get; set; }
    public bool IsHistorical { get; set; }
    public bool IsScoringContentEnabled => !IsBusy;
    public bool IsIdle => !IsBusy;
    public bool IsSpecialCardPickerVisible => false;
    public bool IsPhotoSourceVisible => false;
    public bool IsPhotoReviewVisible => false;
    public void CloseSpecialCardPicker() { }
    public void ClosePhotoSource() { }
    public Task CancelPhotoReviewAsync() => Task.CompletedTask;
    public bool TryBeginNavigation() { if (IsBusy) return false; IsBusy = true; return true; }
    public void EndNavigation() => IsBusy = false;
    public List<BoundaryPlayer> Players { get; } = [];
    public Guid GameSessionId { get; } = Guid.NewGuid();
    public bool WriteSucceeds { get; set; } = true;
    public Task WriteGate { get; set; } = Task.CompletedTask;
    public int StartCalls { get; private set; }
    public int EndCalls { get; private set; }
    public int DiscardCalls { get; private set; }
    public async Task<Guid?> StartAsync() { StartCalls++; await WriteGate; return WriteSucceeds ? GameSessionId : null; }
    public async Task<bool> BeginScoringAsync() { EndCalls++; await WriteGate; return WriteSucceeds; }
    public async Task<bool> DiscardAsync() { DiscardCalls++; await WriteGate; return WriteSucceeds; }
}
public sealed class BoundaryPlayer { public int Color => 0; }
public sealed class BoundaryGame { public string Key => "game"; public bool CanOpen => true; }
public sealed class BoundaryTappedEventArgs : EventArgs { public object Parameter => "game"; }
public sealed class BoundaryWindow { public object? Page { get; set; } }
public sealed class BoundaryLogger
{
    public List<Exception> Errors { get; } = [];
    public List<object> StartupStages { get; } = [];
    public void LogError(Exception exception, string message, params object[] args) => Errors.Add(exception);
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        Errors.Add(exception);
        StartupStages.Add(args.Single());
    }
}
public sealed class BoundaryShell
{
    public static BoundaryShell Current { get; set; } = null!;
    public EventBoundaryTestHost CurrentPage { get; set; } = null!;
    public bool MemoryEnabled { get; private set; }
    public void EnableNavigationMemory() => MemoryEnabled = true;
    public Task NavigateToGroupsForGameAsync(string key) => GoToAsync("groups", false);
    public Task NavigateToMainSectionAsync(AppMainSection section, bool remember = true) => GoToAsync("main", false);
    public Task<bool> DisplayAlertAsync(string title, string message, string accept) => BoundaryDialogs.ShowAsync(CurrentPage, title, message, accept);
    public bool FailNavigation { get; set; }
    public Exception NavigationFailure { get; } = new InvalidOperationException("Navigation unavailable");
    public List<(string Route, Dictionary<string, object>? Parameters)> Calls { get; } = [];
    public Task NavigationGate { get; set; } = Task.CompletedTask;
    public TaskCompletionSource NavigationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public async Task GoToAsync(string route, bool animate, Dictionary<string, object>? parameters = null)
    {
        Calls.Add((route, parameters));
        NavigationStarted.TrySetResult();
        await NavigationGate;
        if (FailNavigation)
            throw NavigationFailure;
    }
    public Task ReturnToGameSessionCallerAsync() => GoToAsync("return", false);
    public Task NavigateFromMainTabTapAsync(object tab) => GoToAsync("tab", false);
    public void CaptureGameSessionCaller(bool useRoot) { }
    public void RememberGameSession(Guid id, string key) { }
}
public sealed class BoundaryDispatcher
{
    public Task Pending { get; private set; } = Task.CompletedTask;
    public void Dispatch(Func<Task> callback) => Pending = callback();
}
public static class BoundaryDialogs
{
    public static Task<bool> ShowAsync(object owner, string title, string message, string accept, string? cancel = null, bool destructive = false)
    {
        EventBoundaryTestHost page = (EventBoundaryTestHost)owner;
        if (cancel is not null) { page.ConfirmationCalls++; return Task.FromResult(page.ConfirmDiscard); }
        page.Messages.Add(message);
        return page.FailErrorDialog ? Task.FromException<bool>(page.DialogFailure) : Task.FromResult(true);
    }
}

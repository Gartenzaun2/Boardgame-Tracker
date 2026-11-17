using System.Reflection;
using System.Runtime.Loader;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class HistoryTransferErrorTests {
    private static readonly Lazy<Type> HandlerType = new(CompileHandler);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TransferFailure_LogsDialogFailureFallsBackAndAllowsRetry(bool import, bool failErrorDialog) {
        HistoryTransferHost page = (HistoryTransferHost)Activator.CreateInstance(HandlerType.Value)!;
        page.Transfer.FailAnalyze = true;
        page.Export.Fail = true;
        page.Dialogs.FailError = failErrorDialog;
        await page.InvokeAsync(import);
        Assert.Same(import ? page.Transfer.Failure : page.Export.Failure, page.Logger.Errors[0]);
        Assert.Equal(1, page.Dialogs.ErrorAttempts);
        Assert.Equal(AppResources.TransferFailed, page.Dialogs.LastMessage);
        Assert.Equal(failErrorDialog ? 2 : 1, page.Logger.Errors.Count);
        if (failErrorDialog) {
            Assert.Same(page.Dialogs.Failure, page.Logger.Errors[1]);
            Assert.True(page.ErrorLabel.IsVisible);
            Assert.Equal(AppResources.TransferFailed, page.ErrorLabel.Text);
        }
        Assert.False(page.IsTransferring);
        Assert.False(page.BusyOverlay.IsVisible);
        Assert.Equal(0, page.Transfer.Commits);
        Assert.Equal(0, page.Reloads);

        page.Transfer.FailAnalyze = false;
        page.Export.Fail = false;
        await page.InvokeAsync(import);
        Assert.False(page.IsTransferring);
        Assert.False(page.BusyOverlay.IsVisible);
        Assert.False(page.ErrorLabel.IsVisible);
        Assert.Equal(import ? 1 : 0, page.Transfer.Commits);
        Assert.Equal(import ? 1 : 0, page.Reloads);
        Assert.Equal(import ? 2 : 0, page.Transfer.Analyses);
        Assert.Equal(import ? 0 : 2, page.Export.Attempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommittedImport_PresentationFailureNeverRepeatsCommitAndPreservesFallbackAfterReload(bool failErrorDialog) {
        HistoryTransferHost page = (HistoryTransferHost)Activator.CreateInstance(HandlerType.Value)!;
        page.Dialogs.FailSuccess = true;
        page.Dialogs.FailError = failErrorDialog;
        await page.InvokeAsync(true);
        Assert.Equal(1, page.Transfer.Commits);
        Assert.Equal(1, page.Reloads);
        Assert.Equal(1, page.Dialogs.SuccessAttempts);
        Assert.Equal(1, page.Dialogs.ErrorAttempts);
        Assert.Same(page.Dialogs.SuccessFailure, page.Logger.Errors[0]);
        Assert.Contains(AppResources.TransferSuccess, page.Dialogs.LastMessage);
        Assert.NotEqual(AppResources.TransferFailed, page.Dialogs.LastMessage);
        Assert.Equal(failErrorDialog ? 2 : 1, page.Logger.Errors.Count);
        if (failErrorDialog) {
            Assert.Same(page.Dialogs.Failure, page.Logger.Errors[1]);
            Assert.True(page.ErrorLabel.IsVisible);
            Assert.Equal(page.Dialogs.LastMessage, page.ErrorLabel.Text);
        }
        Assert.False(page.IsTransferring);
        Assert.False(page.BusyOverlay.IsVisible);
        page.Export.Fail = false;
        await page.InvokeAsync(false);
        Assert.Equal(1, page.Export.Attempts);
        Assert.Equal(1, page.Transfer.Commits);
        Assert.False(page.ErrorLabel.IsVisible);
    }

    private static Type CompileHandler() {
        Assembly assembly = typeof(HistoryTransferErrorTests).Assembly;
        string resource = Assert.Single(assembly.GetManifestResourceNames(), name => name.EndsWith("HistoryPage.Transfer.cs", StringComparison.Ordinal));
        using StreamReader reader = new(assembly.GetManifestResourceStream(resource)!);
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(reader.ReadToEnd()).GetCompilationUnitRoot();
        string[] names = ["StartTransfer", "OnImportClicked", "OnExportClicked", "ShowTransferErrorAsync"];
        IEnumerable<string> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => names.Contains(method.Identifier.ValueText)).Select(method => {
                // Await the actual async-void bodies without requiring a native UI synchronization context.
                if (method.ReturnType.ToString() == "void")
                    method = method.WithReturnType(SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space));
                if (method.Identifier.ValueText.StartsWith("On", StringComparison.Ordinal))
                    method = method.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxFactory.Space), SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
                return method.ToFullString();
            });
        string code = "using System; using System.IO; using System.Linq; using System.Collections.Generic; using System.Threading.Tasks; using Microsoft.Extensions.Logging; using BoardgameTracker.App.Resources.Localization; using BoardgameTracker.Application.Tests.Presentation; using ImportPlan = BoardgameTracker.Application.Tests.Presentation.HistoryTestPlan; using FileResult = BoardgameTracker.Application.Tests.Presentation.HistoryTestFile; using HistoryExportFlow = BoardgameTracker.Application.Tests.Presentation.HistoryExportDouble; public sealed class Handler : HistoryTransferHost { " + string.Join("\n", methods) + " }";
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        MetadataReference[] references = paths.Append(assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        CSharpCompilation compilation = CSharpCompilation.Create("HistoryTransfer_" + Guid.NewGuid().ToString("N"), [CSharpSyntaxTree.ParseText(code)], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream output = new();
        var result = compilation.Emit(output);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        output.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(output).GetType("Handler")!;
    }
}

// Only native presentation and already-tested Application phases are replaced. Event/error bodies are production source.
public class HistoryTransferHost {
    protected bool _isTransferring, _isLoading = false, _isNavigating = false, _isFilterSelectionOpen = false;
    protected Guid[] _visibleIds = [Guid.NewGuid()];
    public HistoryTestLabel BusyOverlay { get; } = new();
    public HistoryTestLabel ErrorLabel { get; } = new();
    public HistoryTransferLogger Logger { get; } = new();
    protected ILogger _logger => Logger;
    public HistoryTransferDouble Transfer { get; } = new();
    protected HistoryTransferDouble _transfer => Transfer;
    public HistoryExportDouble Export { get; } = new();
    protected HistoryExportDouble _exportFlow => Export;
    public HistoryDialogDouble Dialogs { get; } = new();
    protected HistoryDialogDouble AppDialogPage => Dialogs;
    protected HistoryTestPicker FilePicker { get; } = new();
    protected HistoryTestAppInfo AppInfo { get; } = new();
    public bool IsTransferring => _isTransferring;
    public int Reloads { get; private set; }
    public Task InvokeAsync(bool import) => (Task)GetType().GetMethod(import ? "OnImportClicked" : "OnExportClicked")!.Invoke(this, [null, EventArgs.Empty])!;
    protected Task<HistoryTestPlan?> ResolveImportGroupsAsync(HistoryTestPlan plan) => Task.FromResult<HistoryTestPlan?>(plan);
    protected Task<HistoryTestPlan?> ResolveImportGamesAsync(HistoryTestPlan plan) => Task.FromResult<HistoryTestPlan?>(plan);
    protected Task<bool> ConfirmImportAsync(HistoryTestPlan plan) => Task.FromResult(true);
    protected Task ReloadAsync() {
        Reloads++;
        ErrorLabel.IsVisible = false;
        return Task.CompletedTask;
    }
}
public sealed class HistoryTestLabel { public bool IsVisible { get; set; } public string Text { get; set; } = string.Empty; }
public sealed class HistoryTestPlan;
public sealed class HistoryTestFile { public Task<Stream> OpenReadAsync() => Task.FromResult<Stream>(new MemoryStream()); }
public sealed class PickOptions { public string? PickerTitle { get; set; } public FilePickerFileType? FileTypes { get; set; } }
public sealed class FilePickerFileType(Dictionary<DevicePlatform, IEnumerable<string>> types) { public Dictionary<DevicePlatform, IEnumerable<string>> Types { get; } = types; }
public enum DevicePlatform { Android, iOS, WinUI }
public sealed class HistoryTestPicker {
    public HistoryTestPicker Default => this;
    public Task<HistoryTestFile?> PickAsync(PickOptions options) => Task.FromResult<HistoryTestFile?>(new());
}
public sealed class HistoryTestAppInfo { public HistoryTestAppInfo Current => this; public string VersionString => "test"; }
public sealed class HistoryTransferDouble {
    public bool FailAnalyze { get; set; }
    public int Analyses { get; private set; }
    public int Commits { get; private set; }
    public Exception Failure { get; } = new InvalidOperationException("Transfer unavailable");
    public Task<HistoryTestPlan> AnalyzeAsync(Stream stream) {
        Analyses++;
        return FailAnalyze ? Task.FromException<HistoryTestPlan>(Failure) : Task.FromResult(new HistoryTestPlan());
    }
    public Task CommitAsync(HistoryTestPlan plan) { Commits++; return Task.CompletedTask; }
}
public sealed class HistoryExportDouble {
    public bool Fail { get; set; }
    public int Attempts { get; private set; }
    public Exception Failure { get; } = new InvalidOperationException("Export unavailable");
    public static bool CanExport(Guid[] ids) => ids.Length > 0;
    public Task RunAsync(Guid[] ids, string version, Func<IReadOnlyList<HistoryTarget>, Task<int>> choose) {
        Attempts++;
        return Fail ? Task.FromException(Failure) : Task.CompletedTask;
    }
}
public sealed record HistoryTarget(string DisplayName);
public sealed class HistoryDialogDouble {
    public bool FailError { get; set; }
    public bool FailSuccess { get; set; }
    public int ErrorAttempts { get; private set; }
    public int SuccessAttempts { get; private set; }
    public string LastMessage { get; private set; } = string.Empty;
    public Exception Failure { get; } = new InvalidOperationException("Error dialog unavailable");
    public Exception SuccessFailure { get; } = new InvalidOperationException("Success dialog unavailable");
    public Task<int> ChooseAsync(object owner, string title, string[] options, int selected, string? cancel = null) => Task.FromResult(0);
    public Task<bool> ShowAsync(object owner, string title, string message, string accept) {
        if (title == AppResources.History) {
            ErrorAttempts++;
            LastMessage = message;
            if (FailError)
                return Task.FromException<bool>(Failure);
        } else {
            SuccessAttempts++;
            if (FailSuccess)
                return Task.FromException<bool>(SuccessFailure);
        }
        return Task.FromResult(true);
    }
}
public sealed class HistoryTransferLogger : ILogger {
    public List<Exception> Errors { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (exception is not null)
            Errors.Add(exception);
    }
}

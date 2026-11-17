using System.Reflection;
using System.Runtime.Loader;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class AppDialogDismissalTests {
    private static readonly Lazy<Type> HarnessType = new(CompileActualCloseFlow);

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(2)]
    public async Task FailedDismissal_KeepsResultPendingAndAllowsRetryWithExplicitChoice(int result) {
        IDialogDismissalHarness dialog = Create();
        dialog.Navigation.Fail = true;
        await dialog.Close(result);
        Assert.False(dialog.Completion.IsCompleted);
        Assert.True(dialog.HasError);
        Assert.Equal(AppResources.NavigationFailed, dialog.ErrorText);
        Assert.Equal(1, dialog.Navigation.Attempts);
        dialog.Navigation.Fail = false;
        await dialog.Close(result);
        Assert.Equal(result, await dialog.Completion);
        Assert.Equal(2, dialog.Navigation.Attempts);
    }

    [Fact]
    public async Task ConcurrentDismissal_OnlyFirstExplicitActionPopsModalAndCompletesResult() {
        IDialogDismissalHarness dialog = Create();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.Navigation.Gate = release.Task;
        Task closing = dialog.Close(-1);
        try {
            await dialog.Close(0);
            Assert.Equal(1, dialog.Navigation.Attempts);
            Assert.False(dialog.Completion.IsCompleted);
        } finally {
            release.TrySetResult();
        }
        await closing;
        Assert.Equal(-1, await dialog.Completion);
    }

    private static IDialogDismissalHarness Create()
        => (IDialogDismissalHarness)Activator.CreateInstance(HarnessType.Value)!;

    private static Type CompileActualCloseFlow() {
        using StreamReader reader = new(typeof(AppDialogDismissalTests).Assembly.GetManifestResourceStream("DialogContract.AppDialogPage.cs")!);
        MethodDeclarationSyntax close = CSharpSyntaxTree.ParseText(reader.ReadToEnd()).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "CloseAsync");
        // Compile the production method unchanged; only MAUI navigation and labels are replaced.
        string source = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Logging;
            using BoardgameTracker.App.Resources.Localization;
            using BoardgameTracker.Application.Tests.Presentation;
            public sealed class AppDialogPage : IDialogDismissalHarness {
                private bool _closing;
                private readonly TaskCompletionSource<int> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                private readonly DialogErrorLabel _navigationError = new();
                public DialogHandler? Handler => null;
                public DialogModalNavigation Navigation { get; } = new();
                public Task<int> Completion => _completion.Task;
                public bool HasError => _navigationError.IsVisible;
                public string? ErrorText => _navigationError.Text;
                public Task Close(int result) => CloseAsync(result);
            """ + close.ToFullString() + "}";
        string[] platformReferences = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        string[] references = platformReferences.Concat(AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location)).Select(assembly => assembly.Location))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        CSharpCompilation compilation = CSharpCompilation.Create("DialogDismissal_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source)], references.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using MemoryStream output = new();
        var emitted = compilation.Emit(output);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        output.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(output).GetType("AppDialogPage")!;
    }
}

public interface IDialogDismissalHarness {
    DialogModalNavigation Navigation { get; }
    Task<int> Completion { get; }
    bool HasError { get; }
    string? ErrorText { get; }
    Task Close(int result);
}

public sealed class DialogModalNavigation {
    public bool Fail { get; set; }
    public Task Gate { get; set; } = Task.CompletedTask;
    public int Attempts { get; private set; }
    public async Task PopModalAsync(bool animated) {
        Attempts++;
        await Gate;
        if (Fail)
            throw new InvalidOperationException("Modal navigation failed");
    }
}

public sealed class DialogErrorLabel {
    public string? Text { get; set; }
    public bool IsVisible { get; set; }
}

public sealed class DialogHandler {
    public DialogContext? MauiContext { get; }
}

public sealed class DialogContext {
    public IServiceProvider Services { get; } = null!;
}

using System.Reflection;
using System.Runtime.CompilerServices;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Tests.Architecture;

public sealed class GameSessionSliceBoundaryTests
{
    [Theory]
    [InlineData("global using Microsoft.EntityFrameworkCore;", "class ExampleViewModel { DbContext context; }")]
    [InlineData("global using Store = Microsoft.EntityFrameworkCore.DbContext;", "class ExamplePage { Store context; }")]
    [InlineData("global using Microsoft.EntityFrameworkCore;", "class ExampleViewModel { IDbContextFactory<DbContext> factory; }")]
    [InlineData("global using Connection = Microsoft.Data.Sqlite.SqliteConnection;", "class ExamplePage { Connection connection; }")]
    public void IndirectPersistenceDependenciesAreRejected(string imports, string presentation)
    {
        // All three previous text checks pass despite a real persistence dependency.
        Assert.DoesNotContain("BoardgameTrackerDbContext", presentation, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", presentation, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", presentation, StringComparison.Ordinal);
        GameModuleBoundary.Source[] sources = [new("GlobalUsings.cs", imports), new("Presentation.cs", presentation)];
        string[] violations = PresentationBoundary.FindViolations(sources, ["Presentation.cs"]);
        Assert.Contains(violations, violation => violation.Contains("Presentation.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplicationUseCasesAndReadModelsRemainAllowed()
    {
        GameModuleBoundary.Source[] sources =
        [
            new("Application.cs", "using Microsoft.EntityFrameworkCore; public class LoadPlayers { private DbContext db; public string[] Load() => []; }"),
            new("Presentation.cs", "class ExampleViewModel(LoadPlayers query) { public string[] Load() => query.Load(); }")
        ];
        Assert.Empty(PresentationBoundary.FindViolations(sources, ["Presentation.cs"]));
    }

    [Fact]
    public void PagesAndViewModelsDoNotUseEntityFramework()
    {
        Assembly assembly = typeof(GameSessionSliceBoundaryTests).Assembly;
        string[] sources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("PresentationBoundary.", StringComparison.Ordinal)
                || name.StartsWith("GeneralGroupFeature.", StringComparison.Ordinal)
                || name.StartsWith("PlayerFeature.", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(sources);
        foreach (string source in sources)
        {
            using Stream stream = assembly.GetManifestResourceStream(source)!;
            using StreamReader reader = new(stream);
            string code = reader.ReadToEnd();
            Assert.DoesNotContain("BoardgameTrackerDbContext", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.Data.Sqlite", code, StringComparison.Ordinal);
        }
        AssertPresentationTypesArePersistenceFree();
    }

    private static void AssertPresentationTypesArePersistenceFree([CallerFilePath] string sourcePath = "")
    {
        string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "../../.."));
        GameModuleBoundary.Source[] sources = Directory.GetFiles(Path.Combine(root, "src", "BoardgameTracker.App"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Select(path => new GameModuleBoundary.Source(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path))).ToArray();
        string[] presentationPaths = sources.Select(source => source.Path)
            .Where(path => path.Contains("/Features/", StringComparison.Ordinal)
                && !path.EndsWith("/HistoryExportTargets.cs", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(presentationPaths);
        string[] violations = new[] { new[] { "DEBUG", "ANDROID" }, new[] { "DEBUG", "IOS" } }
            .SelectMany(symbols => PresentationBoundary.FindViolations(sources, presentationPaths, symbols)).Distinct().ToArray();
        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void HistoryCorrectionDoesNotDependOnLiveScoringPortsOrModels()
    {
        foreach (Type type in typeof(ArkNovaHistoryCorrectionUseCase).Assembly.GetExportedTypes()
                     .Where(type => type.Namespace == typeof(ArkNovaHistoryCorrectionUseCase).Namespace))
        {
            IEnumerable<Type> dependencies = type.GetConstructors().SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetProperties().Select(property => property.PropertyType))
                .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)));
            foreach (Type dependency in dependencies)
                AssertOutsideSlice(dependency, "BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring");
        }
    }

    [Fact]
    public void SharedResultProjectionHasNoLiveOrHistorySliceOwnership()
    {
        foreach (Type type in new[] { typeof(ArkNovaGameSessionResultData), typeof(ArkNovaGameSessionResultPlayerData), typeof(ArkNovaGameSessionCompletionPlan) })
        {
            Assert.Equal("BoardgameTracker.Application.Games.ArkNova.GameSessions.Results", type.Namespace);
            foreach (PropertyInfo property in type.GetProperties())
            {
                AssertOutsideSlice(property.PropertyType, "BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring");
                AssertOutsideSlice(property.PropertyType, "BoardgameTracker.Application.Games.ArkNova.GameSessions.History");
            }
        }
    }

    [Fact]
    public void GeneralGroupFeatureDoesNotKnowConcreteGameModulesOrRoutes()
    {
        Assembly assembly = typeof(GameSessionSliceBoundaryTests).Assembly;
        string[] sources = assembly.GetManifestResourceNames().Where(name => name.StartsWith("GeneralGroupFeature.", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(sources);
        foreach (string source in sources)
        {
            using Stream stream = assembly.GetManifestResourceStream(source)!;
            using StreamReader reader = new(stream);
            Assert.DoesNotContain("ArkNova", reader.ReadToEnd(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GeneralSettingsUsesOnlyRegisteredModuleEntries()
    {
        Assembly assembly = typeof(GameSessionSliceBoundaryTests).Assembly;
        foreach (string extension in new[] { ".xaml", ".xaml.cs" })
        {
            using StreamReader reader = new(assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(name => name.EndsWith("AppSettingsPage" + extension, StringComparison.Ordinal)))!);
            string source = reader.ReadToEnd();
            Assert.DoesNotContain("ArkNova", source, StringComparison.Ordinal);
        }
        using StreamReader module = new(assembly.GetManifestResourceStream("UxContract.ArkNovaServiceCollectionExtensions.cs")!);
        Assert.Contains("AddTransient<IGameSettingsNavigation, ArkNovaSettingsNavigation>", module.ReadToEnd(), StringComparison.Ordinal);
    }

    [Fact]
    public void CoreAndApplicationDoNotReferenceInfrastructureOrMaui()
    {
        foreach (Assembly assembly in new[] { typeof(Player).Assembly, typeof(ArkNovaScoringUseCase).Assembly })
        {
            string[] names = assembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToArray();
            Assert.DoesNotContain(names, name => name.StartsWith("BoardgameTracker.Infrastructure", StringComparison.Ordinal));
            Assert.DoesNotContain(names, name => name.StartsWith("Microsoft.Maui", StringComparison.Ordinal));
            Assert.DoesNotContain(names, name => name.StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal));
            Assert.DoesNotContain(names, name => name.Contains("Sqlite", StringComparison.Ordinal));
            if (assembly == typeof(Player).Assembly)
                Assert.DoesNotContain(names, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        }
    }

    private static void AssertOutsideSlice(Type type, string forbiddenNamespace)
    {
        Assert.False(type.Namespace?.StartsWith(forbiddenNamespace, StringComparison.Ordinal) == true, type.FullName);
        if (type.HasElementType)
            AssertOutsideSlice(type.GetElementType()!, forbiddenNamespace);
        foreach (Type argument in type.GenericTypeArguments)
            AssertOutsideSlice(argument, forbiddenNamespace);
    }
}

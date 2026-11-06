using System.Runtime.CompilerServices;
using static BoardgameTracker.Application.Tests.Architecture.GameModuleBoundary;

namespace BoardgameTracker.Application.Tests.Architecture;

public sealed class GameModuleBoundaryTests
{
    [Fact]
    public void ProductionSourcesRespectGameModuleBoundaries()
    {
        string root = RepositoryRoot();
        string[] projects = ["BoardgameTracker.Core", "BoardgameTracker.Application", "BoardgameTracker.App"];
        List<Source> sources = [];
        foreach (string project in projects)
        {
            string[] files = Directory.GetFiles(Path.Combine(root, "src", project), "*", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar)
                    .Any(part => part is "obj" or "bin"))
                .Where(path => Path.GetExtension(path) is ".cs" or ".xaml").ToArray();
            Assert.NotEmpty(files);
            sources.AddRange(files.Select(path => new Source(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path))));
        }
        // Each platform branch is inspected, including code disabled on the test host.
        string[] violations = new[] { new[] { "DEBUG", "ANDROID" }, new[] { "DEBUG", "IOS" }, Array.Empty<string>() }
            .SelectMany(symbols => FindViolations(sources, symbols)).Distinct().ToArray();
        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Theory]
    [InlineData("src/BoardgameTracker.Application/Feature.cs", "class Feature { BoardgameTracker.Core.Games.Orion.Token token; }", "concrete symbol")]
    [InlineData("src/BoardgameTracker.Core/Feature.cs", "using Alias = BoardgameTracker.Core.Games.Orion.Token; class Feature { Alias token; }", "game namespace")]
    [InlineData("src/BoardgameTracker.Core/Games/Lyra/Feature.cs", "class Feature { BoardgameTracker.Core.Games.Orion.Token token; }", "concrete symbol")]
    [InlineData("src/BoardgameTracker.Core/Feature.cs", "using BoardgameTracker.Core.Games.Orion; class Feature { Token token; }", "concrete symbol")]
    [InlineData("src/BoardgameTracker.App/Feature.cs", "class Feature { void Open() { GoToAsync(\"OrionPage\"); } }", "direct game route")]
    [InlineData("src/BoardgameTracker.Application/Feature.cs", "class Feature { bool CanHandle(string gameKey) => gameKey == \"orion\"; }", "game-key decision")]
    [InlineData("src/BoardgameTracker.Application/Feature.cs", "class Feature { int Choose(string gameKey) => gameKey switch { \"orion\" => 1, _ => 0 }; }", "game-key decision")]
    [InlineData("src/BoardgameTracker.Core/Moved.cs", "namespace BoardgameTracker.Core.Games.Orion; class Moved {}", "code belongs under Games/Orion/")]
    [InlineData("src/BoardgameTracker.App/MauiProgram.cs", "class Feature { BoardgameTracker.Core.Games.Orion.Token token; }", "concrete symbol")]
    [InlineData("src/BoardgameTracker.Application/Feature.cs", "class Feature { object Read(Shared shared) => shared.Tokens; }", "concrete symbol")]
    [InlineData("src/BoardgameTracker.Application/Games/Lyra/Feature.cs", "class Feature { object Read(Shared shared) => shared.Tokens; }", "concrete symbol")]
    [InlineData("src/BoardgameTracker.App/Feature.xaml", "<Page xmlns=\"urn:test\" xmlns:game=\"clr-namespace:BoardgameTracker.App.Games.Orion\" />", "concrete XAML game reference")]
    public void RejectsViolationsWithPathAndRule(string path, string code, string rule)
    {
        string[] violations = FindViolations([.. Fixture(), new Source(path, code)]);
        Assert.Contains(violations, violation => violation.Contains(path + ":", StringComparison.Ordinal)
            && violation.Contains(rule, StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptsContractsRegistrationHostAssetsAndModuleOwnedPartials()
    {
        Source[] sources = [.. Fixture(),
            new("src/BoardgameTracker.App/MauiProgram.cs", "using BoardgameTracker.App.Games.Orion; class Startup { void Register() { new object().AddOrion(); } }"),
            new("src/BoardgameTracker.Core/Feature.cs", "// OrionPage and Games.Orion are documentation.\nclass Feature { Shared shared; string note = \"Orion Token\"; bool Match(string gameKey, string registeredKey) => gameKey == registeredKey; }"),
            new("src/BoardgameTracker.App/BoardgameTracker.App.csproj", "<Project><ItemGroup><None Include=\"Games/Orion/native.asset\" /></ItemGroup></Project>"),
            new("src/BoardgameTracker.Core/Games/Orion/UseContract.cs", "class UseContract { Shared shared; BoardgameTracker.Core.Games.Orion.Token token; }")];
        Assert.Empty(FindViolations(sources));
    }

    private static Source[] Fixture() =>
    [
        new("src/BoardgameTracker.Core/Shared.cs", "public partial class Shared {}"),
        new("src/BoardgameTracker.Core/Games/Game.cs", "namespace BoardgameTracker.Core.Games; public class Game {}"),
        new("src/BoardgameTracker.Core/Games/Orion/Token.cs", "namespace BoardgameTracker.Core.Games.Orion; public class Token {} public static class Identity { public const string Key = \"orion\"; }"),
        new("src/BoardgameTracker.Application/Games/Orion/Shared.cs", "public partial class Shared { public BoardgameTracker.Core.Games.Orion.Token Tokens => null; }"),
        new("src/BoardgameTracker.App/Games/Orion/Registration.cs", "namespace BoardgameTracker.App.Games.Orion; public static class Registration { public static void AddOrion(this object services) {} void Routes() { Routing.RegisterRoute(nameof(OrionPage), typeof(OrionPage)); } } public class OrionPage {}")
    ];

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "../../.."));
}

using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BoardgameTracker.Application.Tests.Architecture;

// Source ownership also covers App without building MAUI or any native assets.
internal static class GameModuleBoundary
{
    internal sealed record Source(string Path, string Text);

    internal static string? Module(string path)
    {
        string[] parts = path.Replace('\\', '/').Split('/');
        return parts.Length > 4 && parts[2] == "Games" ? parts[3] : null;
    }

    internal static CSharpCompilation CreateCompilation(IReadOnlyList<Source> sources, params string[] symbols)
    {
        SyntaxTree[] trees = sources.Where(source => source.Path.EndsWith(".cs", StringComparison.Ordinal))
            .Select(source => CSharpSyntaxTree.ParseText(source.Text,
                new CSharpParseOptions(preprocessorSymbols: symbols), source.Path)).ToArray();
        // Framework references resolve strings and constants; project symbols come from source.
        MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(System.IO.Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        return CSharpCompilation.Create("ModuleBoundaries", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    internal static string[] FindViolations(IReadOnlyList<Source> sources, params string[] symbols)
    {
        CSharpCompilation compilation = CreateCompilation(sources, symbols);
        SyntaxTree[] trees = compilation.SyntaxTrees.ToArray();
        string[] modules = sources.Select(source => Module(source.Path)).OfType<string>().Distinct().ToArray();
        Dictionary<string, string> keys = new(StringComparer.Ordinal);
        Dictionary<string, string> routes = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in trees.Where(tree => Module(tree.FilePath) is not null))
        {
            string module = Module(tree.FilePath)!;
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (VariableDeclaratorSyntax variable in tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                if (variable.Identifier.ValueText is "Key" or "GameKey"
                    && model.GetDeclaredSymbol(variable) is IFieldSymbol { HasConstantValue: true, ConstantValue: string key })
                    keys[key] = module;
            }
            foreach (InvocationExpressionSyntax call in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                         .Where(call => call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RegisterRoute" }))
            {
                if (call.ArgumentList.Arguments.FirstOrDefault() is { } argument
                    && model.GetConstantValue(argument.Expression) is { HasValue: true, Value: string route })
                    routes[route] = module;
            }
        }
        List<string> violations = [];
        foreach (SyntaxTree tree in trees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                if (node is BaseNamespaceDeclarationSyntax declaration)
                    CheckNamespace(declaration.Name.ToString(), declaration);
                if (node is not SimpleNameSyntax name)
                    continue;
                SymbolInfo info = model.GetSymbolInfo(name);
                foreach (ISymbol symbol in info.Symbol is { } resolved ? [resolved] : info.CandidateSymbols)
                {
                    if (symbol is INamespaceSymbol ns)
                    {
                        if (!IsRegistrationImport(name, tree.FilePath))
                            CheckNamespace(ns.ToDisplayString(), name);
                        continue;
                    }
                    ISymbol dependency = symbol is IAliasSymbol alias ? alias.Target : symbol;
                    string[] owners = dependency.Locations.Where(location => location.IsInSource)
                        .Select(location => location.SourceTree!.FilePath).ToArray();
                    // A shared partial type is neutral; its module-owned members are not.
                    if (owners.Any(path => Module(path) is null))
                        continue;
                    foreach (string owner in owners)
                    {
                        if (!IsRegistrationCall(name, dependency, tree.FilePath, Module(owner)))
                            Check(Module(owner), node, $"concrete symbol '{dependency.ToDisplayString()}'");
                    }
                }
            }
            foreach (LiteralExpressionSyntax literal in tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>())
            {
                if (literal.Token.Value is not string value)
                    continue;
                foreach (string segment in value.Split(['/', '?', '&', '='], StringSplitOptions.RemoveEmptyEntries))
                    if (routes.TryGetValue(segment, out string? routeOwner))
                        Check(routeOwner, literal, $"direct game route '{segment}'");
                if (keys.TryGetValue(value, out string? keyOwner)
                    && literal.Ancestors().Any(ancestor => ancestor is BinaryExpressionSyntax or SwitchStatementSyntax
                        or SwitchExpressionSyntax or ConditionalExpressionSyntax or IfStatementSyntax
                        || ancestor is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Equals" } }))
                    Check(keyOwner, literal, $"game-key decision '{value}'");
            }

            void CheckNamespace(string value, SyntaxNode node)
            {
                foreach (string module in modules)
                foreach (string layer in new[] { "Core", "Application", "App" })
                {
                    string prefix = $"BoardgameTracker.{layer}.Games.{module}";
                    if (value == prefix || value.StartsWith(prefix + ".", StringComparison.Ordinal))
                        Check(module, node, $"game namespace '{value}' (code belongs under Games/{module}/)");
                }
            }

            void Check(string? target, SyntaxNode node, string rule)
            {
                if (target is not null && target != Module(tree.FilePath))
                    violations.Add($"{tree.FilePath}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {rule}");
            }
        }

        foreach (Source source in sources.Where(source => source.Path.EndsWith(".xaml", StringComparison.Ordinal)))
        {
            XDocument document = XDocument.Parse(source.Text, LoadOptions.SetLineInfo);
            foreach (XAttribute attribute in document.Descendants().Attributes())
            {
                foreach (string module in modules.Where(module => module != Module(source.Path)))
                {
                    if (new[] { "Core", "Application", "App" }.Any(layer =>
                            attribute.Value.Contains($"BoardgameTracker.{layer}.Games.{module}", StringComparison.Ordinal))
                        || routes.Any(route => route.Value == module && attribute.Value == route.Key))
                        violations.Add($"{source.Path}:{((System.Xml.IXmlLineInfo)attribute).LineNumber}: concrete XAML game reference '{attribute.Value}'");
                }
            }
        }
        return violations.Distinct().Order().ToArray();
    }

    private static bool IsRegistrationImport(SimpleNameSyntax name, string path) =>
        path == "src/BoardgameTracker.App/MauiProgram.cs"
        && name.Ancestors().OfType<UsingDirectiveSyntax>().Any(use => use.Alias is null
            && use.StaticKeyword.IsKind(SyntaxKind.None)
            && use.Name?.ToString().Split('.').Length == 4);

    private static bool IsRegistrationCall(SimpleNameSyntax name, ISymbol symbol, string path, string? module) =>
        path == "src/BoardgameTracker.App/MauiProgram.cs"
        && symbol is IMethodSymbol { DeclaredAccessibility: Accessibility.Public } method
        && method.Name == "Add" + module
        && name.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax };
}

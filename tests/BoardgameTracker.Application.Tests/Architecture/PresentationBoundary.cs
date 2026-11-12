using BoardgameTracker.Application.Persistence;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static BoardgameTracker.Application.Tests.Architecture.GameModuleBoundary;

namespace BoardgameTracker.Application.Tests.Architecture;

internal static class PresentationBoundary
{
    internal static string[] FindViolations(IReadOnlyList<Source> sources, IReadOnlyCollection<string> presentationPaths,
        params string[] symbols)
    {
        var compilation = CreateCompilation(sources, symbols);
        // Resolve persistence types from real assemblies even when the MAUI sources are incomplete.
        string[] dependencies = [typeof(DbContext).Assembly.Location, typeof(SqliteConnection).Assembly.Location,
            typeof(BoardgameTrackerDbContext).Assembly.Location, typeof(Core.Players.Player).Assembly.Location];
        compilation = compilation.AddReferences(dependencies.Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => !compilation.References.Any(reference => string.Equals(reference.Display, path, StringComparison.OrdinalIgnoreCase)))
            .Select(path => MetadataReference.CreateFromFile(path)));
        List<string> violations = [];
        foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(tree => presentationPaths.Contains(tree.FilePath)))
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (ExpressionSyntax expression in tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>())
            {
                HashSet<ITypeSymbol> visited = new(SymbolEqualityComparer.Default);
                CheckType(model.GetTypeInfo(expression).Type);
                SymbolInfo info = model.GetSymbolInfo(expression);
                foreach (ISymbol symbol in info.Symbol is { } resolved ? [resolved] : info.CandidateSymbols)
                {
                    switch (symbol)
                    {
                        case ITypeSymbol type: CheckType(type); break;
                        case IAliasSymbol { Target: ITypeSymbol type }: CheckType(type); break;
                        case IMethodSymbol method:
                            CheckType(method.ContainingType);
                            CheckType(method.ReturnType);
                            foreach (ITypeSymbol argument in method.TypeArguments) CheckType(argument);
                            break;
                        case IPropertySymbol property: CheckType(property.Type); break;
                        case IFieldSymbol field: CheckType(field.Type); break;
                        case ILocalSymbol local: CheckType(local.Type); break;
                        case IParameterSymbol parameter: CheckType(parameter.Type); break;
                    }
                }

                void CheckType(ITypeSymbol? type)
                {
                    if (type is null || !visited.Add(type))
                        return;
                    string assembly = type.ContainingAssembly?.Name ?? string.Empty;
                    if (assembly == "Microsoft.EntityFrameworkCore" || assembly.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.Ordinal)
                        || assembly == "Microsoft.Data.Sqlite" || assembly.StartsWith("Microsoft.Data.Sqlite.", StringComparison.Ordinal)
                        || assembly.StartsWith("SQLitePCLRaw.", StringComparison.Ordinal))
                        violations.Add($"{tree.FilePath}:{expression.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: persistence type '{type.ToDisplayString()}'");
                    if (type is IArrayTypeSymbol array) CheckType(array.ElementType);
                    if (type is INamedTypeSymbol named)
                    {
                        foreach (ITypeSymbol argument in named.TypeArguments) CheckType(argument);
                        CheckType(named.BaseType);
                        foreach (INamedTypeSymbol contract in named.Interfaces) CheckType(contract);
                    }
                }
            }
        }
        return violations.Distinct().Order().ToArray();
    }
}

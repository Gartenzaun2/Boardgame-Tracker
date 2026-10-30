using System.Reflection;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.GameSessions.Results;

namespace BoardgameTracker.Application.Tests.Architecture;

public sealed class GameSessionSliceBoundaryTests
{
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
        Assert.True(typeof(IArkNovaScoringStore).IsAssignableFrom(typeof(SqliteArkNovaGameSessionResultStore)));
        Assert.True(typeof(IArkNovaHistoryCorrectionStore).IsAssignableFrom(typeof(SqliteArkNovaGameSessionResultStore)));
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
    public void CoreAndApplicationDoNotReferenceInfrastructureOrMaui()
    {
        foreach (Assembly assembly in new[] { typeof(Player).Assembly, typeof(ArkNovaScoringUseCase).Assembly })
        {
            string[] names = assembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToArray();
            Assert.DoesNotContain(names, name => name.StartsWith("BoardgameTracker.Infrastructure", StringComparison.Ordinal));
            Assert.DoesNotContain(names, name => name.StartsWith("Microsoft.Maui", StringComparison.Ordinal));
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

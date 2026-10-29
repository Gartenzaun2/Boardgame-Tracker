using System.Reflection;
using BoardgameTracker.Application.Groups.EditGroup;

namespace BoardgameTracker.Application.Tests.Architecture;

public sealed class GroupModuleBoundaryTests
{
    [Fact]
    public void GeneralGroupContracts_DoNotExposeGameSpecificTypes()
    {
        var types = typeof(EditGroupCommand).Assembly.GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith("BoardgameTracker.Application.Groups.", StringComparison.Ordinal) == true);
        foreach (var type in types)
        {
            AssertNeutral(type.BaseType);
            foreach (var property in type.GetProperties()) AssertNeutral(property.PropertyType);
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                AssertNeutral(method.ReturnType);
                foreach (var parameter in method.GetParameters()) AssertNeutral(parameter.ParameterType);
            }
            foreach (var constructor in type.GetConstructors())
                foreach (var parameter in constructor.GetParameters()) AssertNeutral(parameter.ParameterType);
        }
    }

    private static void AssertNeutral(Type? type)
    {
        if (type is null) return;
        Assert.False(type.Namespace?.StartsWith("BoardgameTracker.Core.Games.", StringComparison.Ordinal) == true, type.FullName);
        Assert.False(type.Namespace?.StartsWith("BoardgameTracker.Application.Games.", StringComparison.Ordinal) == true, type.FullName);
        if (type.HasElementType) AssertNeutral(type.GetElementType());
        foreach (var argument in type.GenericTypeArguments) AssertNeutral(argument);
    }
}

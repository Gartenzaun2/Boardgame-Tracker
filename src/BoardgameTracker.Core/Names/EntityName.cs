namespace BoardgameTracker.Core.Names;

public static class EntityName
{
    public static StringComparer Comparer => StringComparer.OrdinalIgnoreCase;
    public static string Normalize(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string result = name.Trim();
        if (result.Length > 100)
            throw new ArgumentException("The name is too long.", nameof(name));
        return result;
    }
    public static bool Equals(string left, string right) => Comparer.Equals(left.Trim(), right.Trim());
}

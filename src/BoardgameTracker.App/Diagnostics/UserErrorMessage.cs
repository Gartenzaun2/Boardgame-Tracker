namespace BoardgameTracker.App.Diagnostics;

/// <summary>
/// Builds user-facing error text while keeping technical exception details restricted to DEBUG builds.
/// </summary>
public static class UserErrorMessage
{
    public static string Create(string friendlyMessage, Exception exception)
    {
#if DEBUG
        return $"{friendlyMessage}{Environment.NewLine}{Environment.NewLine}{exception.Message}";
#else
        return friendlyMessage;
#endif
    }
}

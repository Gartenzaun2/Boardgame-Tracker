namespace BoardgameTracker.App.Diagnostics;

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

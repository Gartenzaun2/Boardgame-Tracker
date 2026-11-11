using BoardgameTracker.Core.Groups;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Diagnostics;

/// <summary>
/// Builds user-facing error text while keeping technical exception details restricted to DEBUG builds.
/// </summary>
public static class UserErrorMessage
{
    public static string Create(string friendlyMessage, Exception exception)
    {
        if (exception is GroupPlayerLimitExceededException or GroupPlayerMinimumNotMetException)
            return AppResources.GroupPlayerLimitExceeded;
#if DEBUG
        return $"{friendlyMessage}{Environment.NewLine}{Environment.NewLine}{exception.Message}";
#else
        return friendlyMessage;
#endif
    }
}

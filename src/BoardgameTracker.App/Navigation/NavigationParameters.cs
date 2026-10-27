namespace BoardgameTracker.App.Navigation;

/// <summary>
/// Defines the shared Shell parameter names for stable domain identifiers.
/// The same identifier uses the same key across every sender and receiver.
/// </summary>
internal static class NavigationParameters
{
    public const string GroupId = nameof(GroupId);
    public const string SessionId = nameof(SessionId);
}

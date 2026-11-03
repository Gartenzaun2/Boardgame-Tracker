using BoardgameTracker.Core.Players;
namespace BoardgameTracker.App.Navigation;

/// <summary>
/// Defines the shared Shell parameter names for stable domain identifiers.
/// The same identifier uses the same key across every sender and receiver.
/// </summary>
internal static class NavigationParameters
{
    public const string GroupManagement = nameof(GroupManagement);
    public const string GroupDraft = nameof(GroupDraft);
    public const string NewPlayer = nameof(NewPlayer);
    public const string GameKey = nameof(GameKey);
    public const string GroupId = nameof(GroupId);
    public const string GameSessionId = nameof(GameSessionId);
    public const string PlayerId = nameof(PlayerId);
    public const string SpecialCardId = nameof(SpecialCardId);
    public const string CorrectionDraft = nameof(CorrectionDraft);
    public const string ResultIsHistorical = nameof(ResultIsHistorical);
    public const string LicenseComponentId = nameof(LicenseComponentId);
}

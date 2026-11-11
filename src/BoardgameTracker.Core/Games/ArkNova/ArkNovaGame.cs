namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Identifies the Ark Nova game across application and persistence slices.
/// The stable key is persisted and must not depend on the current UI language.
/// </summary>
public static class ArkNovaGame
{
    public const string Key = "ark-nova";
    public const string Name = "Arche Nova";
    public const string BadgeText = "Arche Nova";
    public const int MinPlayers = 1;
    public const int MaxPlayers = 4;
}

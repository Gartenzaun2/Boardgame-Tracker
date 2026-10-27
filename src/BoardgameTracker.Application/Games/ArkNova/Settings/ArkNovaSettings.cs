namespace BoardgameTracker.Application.Games.ArkNova.Settings;

public enum ArkNovaScoringDisplayMode
{
    New = 1,
    Old = 2
}

/// <summary>
/// Local presentation/input preferences for Ark Nova. These settings are not historical game data.
/// </summary>
public sealed record ArkNovaSettings(
    ArkNovaScoringDisplayMode ScoringDisplayMode,
    bool CaptureReputation)
{
    public static ArkNovaSettings Default { get; } = new(ArkNovaScoringDisplayMode.New, false);
}

public interface IArkNovaSettingsStore
{
    ArkNovaSettings Get();
    void Save(ArkNovaSettings settings);
}

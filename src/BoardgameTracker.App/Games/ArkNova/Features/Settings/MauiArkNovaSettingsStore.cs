using BoardgameTracker.Application.Games.ArkNova.Settings;
using Microsoft.Maui.Storage;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

/// <summary>
/// Stores local Ark Nova preferences through MAUI Preferences, which maps to the native
/// platform preference store on Android/iOS.
/// </summary>
public sealed class MauiArkNovaSettingsStore : IArkNovaSettingsStore
{
    private const string ScoringModeKey = "ArkNova.ScoringDisplayMode";
    private const string CaptureReputationKey = "ArkNova.CaptureReputation";

    public ArkNovaSettings Get()
    {
        int rawMode = Preferences.Default.Get(ScoringModeKey, (int)ArkNovaScoringDisplayMode.New);
        ArkNovaScoringDisplayMode mode = Enum.IsDefined(typeof(ArkNovaScoringDisplayMode), rawMode)
            ? (ArkNovaScoringDisplayMode)rawMode
            : ArkNovaScoringDisplayMode.New;

        return new ArkNovaSettings(
            mode,
            Preferences.Default.Get(CaptureReputationKey, false));
    }

    public void Save(ArkNovaSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Preferences.Default.Set(ScoringModeKey, (int)settings.ScoringDisplayMode);
        Preferences.Default.Set(CaptureReputationKey, settings.CaptureReputation);
    }
}

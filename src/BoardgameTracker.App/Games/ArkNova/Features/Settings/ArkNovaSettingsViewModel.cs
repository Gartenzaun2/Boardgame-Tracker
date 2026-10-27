using BoardgameTracker.Application.Games.ArkNova.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

public partial class ArkNovaSettingsViewModel(IArkNovaSettingsStore settingsStore) : ObservableObject
{
    [ObservableProperty]
    public partial bool CaptureReputation { get; set; }

    [ObservableProperty]
    public partial bool UseOldScoring { get; set; }

    public void Load()
    {
        ArkNovaSettings settings = settingsStore.Get();
        CaptureReputation = settings.CaptureReputation;
        UseOldScoring = settings.ScoringDisplayMode == ArkNovaScoringDisplayMode.Old;
    }

    public void Save() => settingsStore.Save(new ArkNovaSettings(
        UseOldScoring ? ArkNovaScoringDisplayMode.Old : ArkNovaScoringDisplayMode.New,
        CaptureReputation));
}

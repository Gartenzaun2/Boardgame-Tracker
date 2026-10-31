using BoardgameTracker.Application.Settings;
using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

public partial class ArkNovaSettingsViewModel(IArkNovaSettingsStore settingsStore, GameContentLanguagePreference contentPreference, ArkNovaContent content) : ObservableObject
{
    [ObservableProperty]
    public partial bool CaptureReputation { get; set; }

    [ObservableProperty]
    public partial bool UseOldScoring { get; set; }

    public IReadOnlyList<ContentLanguageOption> ContentLanguages { get; } = content.Languages.SupportedLanguages
        .Select(code => new ContentLanguageOption(code, code switch
        {
            "de" => AppResources.ContentLanguageGerman,
            _ => throw new InvalidOperationException("Add a UI label for the supported content language.")
        })).ToArray();

    [ObservableProperty]
    public partial ContentLanguageOption? SelectedContentLanguage { get; set; }

    partial void OnSelectedContentLanguageChanged(ContentLanguageOption? value)
    {
        if (value is not null)
            contentPreference.Save(content.Languages, value.Code);
    }

    public void Load()
    {
        SelectedContentLanguage = ContentLanguages.Single(option => option.Code == contentPreference.Get(content.Languages));
        ArkNovaSettings settings = settingsStore.Get();
        CaptureReputation = settings.CaptureReputation;
        UseOldScoring = settings.ScoringDisplayMode == ArkNovaScoringDisplayMode.Old;
    }

    public void Save() => settingsStore.Save(new ArkNovaSettings(
        UseOldScoring ? ArkNovaScoringDisplayMode.Old : ArkNovaScoringDisplayMode.New,
        CaptureReputation));
}

public sealed record ContentLanguageOption(string Code, string Name);

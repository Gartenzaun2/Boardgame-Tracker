using BoardgameTracker.Application.Settings.Navigation;
using Microsoft.Maui.Storage;

namespace BoardgameTracker.App.Services;

/// <summary>
/// Persists lightweight navigation state in the native MAUI preference store.
/// </summary>
public sealed class MauiAppNavigationSettingsStore : IAppNavigationSettingsStore
{
    private const string ThemeKey = "Appearance.Theme";
    private const string DefaultGameKey = "Navigation.DefaultGameKey";
    private const string SelectedGameKey = "Navigation.SelectedGameKey";
    private const string LastMainSectionKey = "Navigation.LastMainSection";
    private const string LastGameSessionIdKey = "Navigation.LastGameSessionId";

    public AppNavigationSettings Get()
    {
        string? defaultGame = GetNullableString(DefaultGameKey);
        string? selectedGame = GetNullableString(SelectedGameKey);
        int rawSection = Preferences.Default.Get(LastMainSectionKey, 0);
        AppMainSection? section = Enum.IsDefined(typeof(AppMainSection), rawSection) && rawSection != 0
            ? (AppMainSection)rawSection
            : null;

        Guid? gameSessionId = Guid.TryParse(GetNullableString(LastGameSessionIdKey), out Guid parsedGameSessionId)
            ? parsedGameSessionId
            : null;

        return new AppNavigationSettings(defaultGame, selectedGame, section, gameSessionId,
            AppThemeSelection.FromStoredValue(Preferences.Default.ContainsKey(ThemeKey)
                ? Preferences.Default.Get(ThemeKey, 0) : null));
    }

    public void Save(AppNavigationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Preferences.Default.Set(ThemeKey, (int)AppThemeSelection.FromStoredValue((int)settings.Theme));
        SetNullableString(DefaultGameKey, settings.DefaultGameKey);
        SetNullableString(SelectedGameKey, settings.SelectedGameKey);

        if (settings.LastMainSection is AppMainSection section)
        {
            Preferences.Default.Set(LastMainSectionKey, (int)section);
        }
        else
        {
            Preferences.Default.Remove(LastMainSectionKey);
        }

        SetNullableString(LastGameSessionIdKey, settings.LastGameSessionId?.ToString("D"));
    }

    private static string? GetNullableString(string key)
    {
        string value = Preferences.Default.Get(key, string.Empty);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void SetNullableString(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Preferences.Default.Remove(key);
        }
        else
        {
            Preferences.Default.Set(key, value);
        }
    }
}

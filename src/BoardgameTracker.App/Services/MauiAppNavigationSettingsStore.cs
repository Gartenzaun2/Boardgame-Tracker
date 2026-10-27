using BoardgameTracker.Application.Settings.Navigation;
using Microsoft.Maui.Storage;

namespace BoardgameTracker.App.Services;

/// <summary>
/// Persists lightweight navigation state in the native MAUI preference store.
/// </summary>
public sealed class MauiAppNavigationSettingsStore : IAppNavigationSettingsStore
{
    private const string DefaultGameKey = "Navigation.DefaultGameKey";
    private const string SelectedGameKey = "Navigation.SelectedGameKey";
    private const string LastMainSectionKey = "Navigation.LastMainSection";
    private const string LastSessionIdKey = "Navigation.LastSessionId";

    public AppNavigationSettings Get()
    {
        string? defaultGame = GetNullableString(DefaultGameKey);
        string? selectedGame = GetNullableString(SelectedGameKey);
        int rawSection = Preferences.Default.Get(LastMainSectionKey, 0);
        AppMainSection? section = Enum.IsDefined(typeof(AppMainSection), rawSection) && rawSection != 0
            ? (AppMainSection)rawSection
            : null;

        Guid? sessionId = Guid.TryParse(GetNullableString(LastSessionIdKey), out Guid parsedSessionId)
            ? parsedSessionId
            : null;

        return new AppNavigationSettings(defaultGame, selectedGame, section, sessionId);
    }

    public void Save(AppNavigationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
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

        SetNullableString(LastSessionIdKey, settings.LastSessionId?.ToString("D"));
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

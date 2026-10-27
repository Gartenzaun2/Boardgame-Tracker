namespace BoardgameTracker.Application.Settings.Navigation;

/// <summary>
/// Identifies the four persistent main areas of the mobile application.
/// </summary>
public enum AppMainSection
{
    Games = 1,
    Groups = 2,
    History = 3,
    Settings = 4
}

/// <summary>
/// Local navigation preferences. They describe UI context only and are not historical game data.
/// </summary>
public sealed record AppNavigationSettings(
    string? DefaultGameKey,
    string? SelectedGameKey,
    AppMainSection? LastMainSection,
    Guid? LastSessionId)
{
    public static AppNavigationSettings Default { get; } = new(null, null, null, null);
}

/// <summary>
/// Technical boundary for persisting local app-navigation context outside the game database.
/// </summary>
public interface IAppNavigationSettingsStore
{
    AppNavigationSettings Get();
    void Save(AppNavigationSettings settings);
}

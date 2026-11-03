namespace BoardgameTracker.Application.Settings.Navigation;

public enum AppMainSection
{
    Play = 1,
    History = 3,
    Settings = 4,
    Participants = 5
}

public static class AppMainSections
{
    // Old Games (1) and Groups (2) both lead to the current Play root.
    public static AppMainSection? FromStoredValue(int? value) => value switch
    {
        1 or 2 => AppMainSection.Play,
        3 => AppMainSection.History,
        4 => AppMainSection.Settings,
        5 => AppMainSection.Participants,
        _ => null
    };
}

public sealed record AppNavigationSettings(string? SelectedGameKey, AppMainSection? LastMainSection,
    Guid? LastGameSessionId, AppTheme Theme = AppTheme.Dark)
{
    public static AppNavigationSettings Default { get; } = new(null, null, null);
}

public interface IAppNavigationSettingsStore
{
    AppNavigationSettings Get();
    void Save(AppNavigationSettings settings);
}

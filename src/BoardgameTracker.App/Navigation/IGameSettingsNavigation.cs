namespace BoardgameTracker.App.Navigation;

/// <summary>A module-owned settings entry displayed by the general settings page.</summary>
public interface IGameSettingsNavigation
{
    string DisplayName { get; }
    Task OpenAsync();
}

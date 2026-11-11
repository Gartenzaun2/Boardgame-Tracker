namespace BoardgameTracker.App.Navigation;

/// <summary>A module resolves settings for its fixed identity or dynamic key namespace.</summary>
public interface IGameSettingsNavigation
{
    bool CanHandle(string gameKey);
    string? KindText => null;
    Task OpenAsync(string gameKey);
}

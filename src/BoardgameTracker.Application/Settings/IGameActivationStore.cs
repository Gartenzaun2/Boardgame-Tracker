namespace BoardgameTracker.Application.Settings;

/// <summary>Local host preferences. Games without an explicit opt-out are enabled.</summary>
public interface IGameActivationStore
{
    bool IsEnabled(string gameKey);
    void SetEnabled(string gameKey, bool enabled);
}

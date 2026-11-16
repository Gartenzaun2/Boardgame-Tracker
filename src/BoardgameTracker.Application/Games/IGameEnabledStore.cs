namespace BoardgameTracker.Application.Games;

/// <summary>Local host preferences. Games without an explicit opt-out are enabled.</summary>
public interface IGameEnabledStore {
    bool IsEnabled(string gameKey);
    void SetEnabled(string gameKey, bool enabled);
}

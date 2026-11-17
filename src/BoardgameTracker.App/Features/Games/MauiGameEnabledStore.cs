using BoardgameTracker.Application.Games;
using Microsoft.Maui.Storage;

namespace BoardgameTracker.App.Features.Games;

public sealed class MauiGameEnabledStore(IPreferences preferences) : IGameEnabledStore {
    private const string DisabledPrefix = "Games.Disabled.";

    public bool IsEnabled(string gameKey) {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        return !preferences.Get(DisabledPrefix + gameKey, false);
    }

    public void SetEnabled(string gameKey, bool enabled) {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        if (enabled)
            preferences.Remove(DisabledPrefix + gameKey);
        else
            preferences.Set(DisabledPrefix + gameKey, true);
    }
}

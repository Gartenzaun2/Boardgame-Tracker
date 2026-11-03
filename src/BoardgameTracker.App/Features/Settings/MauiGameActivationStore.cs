using BoardgameTracker.Application.Settings;
using Microsoft.Maui.Storage;

namespace BoardgameTracker.App.Features.Settings;

public sealed class MauiGameActivationStore(IPreferences preferences) : IGameActivationStore
{
    private const string DisabledPrefix = "Games.Disabled.";

    public bool IsEnabled(string gameKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        return !preferences.Get(DisabledPrefix + gameKey, false);
    }

    public void SetEnabled(string gameKey, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        if (enabled)
            preferences.Remove(DisabledPrefix + gameKey);
        else
            preferences.Set(DisabledPrefix + gameKey, true);
    }
}

using BoardgameTracker.Application.Settings;

namespace BoardgameTracker.Application.Tests.Persistence;

internal sealed class TestGameActivationStore : IGameActivationStore
{
    private readonly HashSet<string> _disabled = new(StringComparer.Ordinal);
    public bool IsEnabled(string gameKey) => !_disabled.Contains(gameKey);
    public void SetEnabled(string gameKey, bool enabled)
    {
        if (enabled)
            _disabled.Remove(gameKey);
        else
            _disabled.Add(gameKey);
    }
}

using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Games.ArkNova.Navigation;

public sealed class ArkNovaSettingsNavigation : IGameSettingsNavigation
{
    public bool CanHandle(string gameKey) => gameKey == ArkNovaGame.Key;
    public Task OpenAsync(string gameKey) => Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage), false);
}

using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Games.ArkNova.Navigation;

public sealed class ArkNovaSettingsNavigation : IGameSettingsNavigation
{
    public string DisplayName => ArkNovaGameSessionNavigationHandler.GameDisplayName;
    public Task OpenAsync() => Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage), false);
}

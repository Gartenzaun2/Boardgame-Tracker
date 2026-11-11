using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Games.ManualGames;

namespace BoardgameTracker.App.Games.ManualGames;

public sealed class ManualGamesNavigation : IGameSettingsNavigation, IGameCreationNavigation
{
    public bool CanHandle(string gameKey) => ManualGame.MatchesKey(gameKey);
    public string KindText => AppResources.ManualGameLabel;
    public Task OpenCreateAsync() => Shell.Current.GoToAsync(nameof(ManualGameEditorPage), false);
    public Task OpenAsync(string gameKey)
    {
        if (!CanHandle(gameKey))
            throw new ArgumentException("The key does not belong to ManualGames.", nameof(gameKey));
        return Shell.Current.GoToAsync(nameof(ManualGameEditorPage), false,
            new ShellNavigationQueryParameters { [NavigationParameters.GameKey] = gameKey });
    }
}

using System.Collections.ObjectModel;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Games.ListGames;

public partial class GamesViewModel(PlayOverviewQuery query, IEnumerable<IGameSessionNavigationHandler> handlers, ILogger<GamesViewModel> logger) : ObservableObject
{
    public ObservableCollection<PlayGameRow> Games { get; } = [];
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty] public partial string? ErrorMessage { get; private set; }
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            IReadOnlyList<PlayGame> games = await query.ListAsync(cancellationToken);
            Games.Clear();
            foreach (PlayGame game in games)
            {
                IGameSessionNavigationHandler? handler = handlers.SingleOrDefault(handler => handler.GameKey == game.Key);
                if (handler is not null) Games.Add(new(game.Key, handler.DisplayName, game.HasOpenSession));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load games.");
            ErrorMessage = AppResources.LoadGamesFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
public sealed record PlayGameRow(string Key, string Name, bool HasOpenSession);

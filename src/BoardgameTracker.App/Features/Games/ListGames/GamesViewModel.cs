using System.Collections.ObjectModel;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Games.ListGames;

public partial class GamesViewModel : ObservableObject {
    private readonly PlayOverviewQuery _query;
    private readonly IEnumerable<IGameSessionNavigationHandler> _handlers;
    private readonly ILogger<GamesViewModel> _logger;

    public GamesViewModel(PlayOverviewQuery query, IEnumerable<IGameSessionNavigationHandler> handlers, ILogger<GamesViewModel> logger) {
        _query = query;
        _handlers = handlers;
        _logger = logger;
    }

    public ObservableCollection<PlayGameRow> Games { get; } = [];
    [ObservableProperty]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }
    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        if (IsBusy)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try {
            IReadOnlyList<PlayGame> games = await _query.ListAsync(cancellationToken);
            Games.Clear();
            foreach (PlayGame game in games) {
                IGameSessionNavigationHandler? handler = _handlers.SingleOrDefault(handler => handler.CanHandle(game.Key));
                Games.Add(new(game.Key, game.Name, game.HasOpenSession, handler is not null));
            }
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Could not load games.");
            ErrorMessage = AppResources.LoadGamesFailed;
        }
        finally {
            IsBusy = false;
        }
    }
}
public sealed record PlayGameRow(string Key, string Name, bool HasOpenSession, bool CanOpen);

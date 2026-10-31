using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.ListPlayers;

public partial class PlayerListViewModel(ListPlayersQuery query, IEnumerable<IGameSessionNavigationHandler> navigationHandlers, ILogger<PlayerListViewModel> logger) : ObservableObject
{
    public ObservableCollection<PlayerListRow> Players { get; } = [];
    [ObservableProperty] public partial bool IncludeGuests { get; set; }
    [ObservableProperty] public partial bool IncludeArchived { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => ErrorMessage is not null;
    public bool IsIdle => !IsBusy;
    public bool IsEmpty => Players.Count == 0 && !IsBusy && !HasError;
    private bool _reloadRequested;

    partial void OnIncludeGuestsChanged(bool value) => _ = LoadAsync();
    partial void OnIncludeArchivedChanged(bool value) => _ = LoadAsync();

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        if(IsBusy) {
            _reloadRequested = true;
            return;
        }

        try {
            IsBusy = true;

            do {
                _reloadRequested = false;
                ErrorMessage = null;

                IReadOnlyList<PlayerListItem> rows = await query.ListPlayersAsync(IncludeGuests, IncludeArchived, cancellationToken);
                Players.Clear();

                foreach(PlayerListItem row in rows) {
                    Players.Add(new PlayerListRow(row));
                }
            }
            while(_reloadRequested && !cancellationToken.IsCancellationRequested);
        } catch(Exception exception) {
            logger.LogError(exception, "Could not load players.");
            ErrorMessage = AppResources.PlayersLoadFailed;
        } finally {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public async Task OpenLastGameSessionAsync(PlayerListRow row)
    {
        if (IsBusy || !row.HasLastGameSession)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            IGameSessionNavigationHandler handler = navigationHandlers.FirstOrDefault(handler => handler.GameKey == row.Data.LastGameKey)
                ?? throw new InvalidOperationException("No history handler is registered for this game.");
            await handler.OpenGameSessionAsync(row.Data.LastGameSessionId!.Value);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not open the player's last game session.");
            ErrorMessage = AppResources.HistoryOpenFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record PlayerListRow(PlayerListItem Data)
{
    public string Name => Data.Name;
    public bool IsGuest => Data.IsGuest;
    public bool IsArchived => Data.IsArchived;
    public bool HasStatus => IsGuest || IsArchived;
    public bool HasLastGameSession => Data.LastGameSessionId is Guid id && id != Guid.Empty && !string.IsNullOrWhiteSpace(Data.LastGameKey);
    public bool HasNoGameSession => !HasLastGameSession;
    public string CreatedText => string.Format(AppResources.PlayerCreatedFormat, Data.CreatedAt.ToLocalTime().ToString("d", CultureInfo.CurrentCulture));
    public string LastPlayedText => Data.LastPlayedAt is DateTimeOffset date
        ? string.Format(AppResources.PlayerLastPlayedFormat, date.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)) : AppResources.PlayerNoGameSession;
}

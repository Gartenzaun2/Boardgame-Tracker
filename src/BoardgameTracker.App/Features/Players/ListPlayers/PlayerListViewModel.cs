using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.ListPlayers;

public partial class PlayerListViewModel : ObservableObject {
    private readonly ListPlayersQuery _query;
    private readonly ListManagedGroupsQuery _groupsQuery;
    private readonly ILogger<PlayerListViewModel> _logger;

    public PlayerListViewModel(ListPlayersQuery query, ListManagedGroupsQuery groupsQuery, ILogger<PlayerListViewModel> logger) {
        _query = query;
        _groupsQuery = groupsQuery;
        _logger = logger;
    }

    /// <summary>Filter changes during a load request one more pass so the latest input is not lost while busy.</summary>
    private bool _reloadRequested;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => ErrorMessage is not null;

    [ObservableProperty]
    public partial bool ShowGroups { get; set; }

    [ObservableProperty]
    public partial bool HasPlayers { get; private set; }

    public ObservableCollection<PlayerListRow> Players { get; } = [];
    public bool IsEmpty => Players.Count == 0 && !IsBusy && !HasError;

    [ObservableProperty]
    public partial bool IncludeGuests { get; set; }

    [ObservableProperty]
    public partial bool IncludeArchived { get; set; }

    public ObservableCollection<ManagedGroupRow> Groups { get; } = [];

    [ObservableProperty]
    public partial bool IncludeArchivedGroups { get; set; }

    public void SelectPlayers() {
        if (!IsBusy)
            ShowGroups = false;
    }

    public void SelectGroups() {
        if (!IsBusy && HasPlayers)
            ShowGroups = true;
    }

    partial void OnIncludeGuestsChanged(bool value)
        => _ = LoadAsync();

    partial void OnIncludeArchivedChanged(bool value)
        => _ = LoadAsync();

    partial void OnIncludeArchivedGroupsChanged(bool value)
        => _ = LoadAsync();

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        if (IsBusy) {
            _reloadRequested = true;
            return;
        }

        try {
            IsBusy = true;

            do {
                _reloadRequested = false;
                ErrorMessage = null;

                IReadOnlyList<PlayerListItem> rows = await _query.ListPlayersAsync(IncludeGuests, IncludeArchived, cancellationToken);
                HasPlayers = await _query.HasPlayersAsync(cancellationToken);
                if (!HasPlayers)
                    ShowGroups = false;

                IReadOnlyList<ManagedGroup> groups = await _groupsQuery.ListAsync(IncludeArchivedGroups, cancellationToken);
                Groups.Clear();
                foreach (ManagedGroup group in groups)
                    Groups.Add(new(group.Id, group.Name, string.Join(" · ", group.PlayerNames), group.IsArchived));

                Players.Clear();
                foreach (PlayerListItem row in rows)
                    Players.Add(new PlayerListRow(row));
            } while (_reloadRequested && !cancellationToken.IsCancellationRequested);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load players.");
            ErrorMessage = AppResources.PlayersLoadFailed;
        } finally {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
}

public sealed record PlayerListRow(PlayerListItem Data) {
    public string Name => Data.Name;
    public bool IsGuest => Data.IsGuest;
    public bool IsArchived => Data.IsArchived;
    public bool HasStatus => IsGuest || IsArchived;
    public string CreatedText => string.Format(AppResources.PlayerCreatedFormat, Data.CreatedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
}

public sealed record ManagedGroupRow(Guid Id, string Name, string Members, bool IsArchived);

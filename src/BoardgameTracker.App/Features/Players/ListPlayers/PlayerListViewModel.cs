using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.ListPlayers;

public partial class PlayerListViewModel(ListPlayersQuery query, ListManagedGroupsQuery groupsQuery, ILogger<PlayerListViewModel> logger) : ObservableObject
{
    public ObservableCollection<PlayerListRow> Players { get; } = [];
    public ObservableCollection<ManagedGroupRow> Groups { get; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlayers))]
    public partial bool ShowGroups { get; set; }
    public bool ShowPlayers => !ShowGroups;
    [ObservableProperty] public partial bool HasPlayers { get; private set; }
    public bool HasNoPlayers => !HasPlayers;
    [ObservableProperty] public partial bool IncludeArchivedGroups { get; set; }
    partial void OnIncludeArchivedGroupsChanged(bool value) => _ = LoadAsync();
    public void SelectPlayers()
    {
        if (!IsBusy)
            ShowGroups = false;
    }
    public void SelectGroups()
    {
        if (!IsBusy && HasPlayers)
            ShowGroups = true;
    }
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
                HasPlayers = await query.HasPlayersAsync(cancellationToken);
                OnPropertyChanged(nameof(HasNoPlayers));
                if (!HasPlayers)
                    ShowGroups = false;
                IReadOnlyList<ManagedGroup> groups = await groupsQuery.ListAsync(IncludeArchivedGroups, cancellationToken);
                Groups.Clear();
                foreach (ManagedGroup group in groups)
                    Groups.Add(new(group.Id, group.Name, string.Join(" · ", group.PlayerNames), group.IsArchived));
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

}

public sealed record PlayerListRow(PlayerListItem Data)
{
    public string Name => Data.Name;
    public bool IsGuest => Data.IsGuest;
    public bool IsArchived => Data.IsArchived;
    public bool HasStatus => IsGuest || IsArchived;
    public string CreatedText => string.Format(AppResources.PlayerCreatedFormat, Data.CreatedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
}

public sealed record ManagedGroupRow(Guid Id, string Name, string Members, bool IsArchived);

using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Core.GameSessions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

/// <summary>
/// 1. Load groups assigned to the selected game and eligible groups available for assignment.
/// 2. Select an existing group or create a draft, preserving the selected game's context.
/// 3. Keep successful assignments across preparation-navigation failures so retrying only
///    opens preparation; returning later reloads the filtered lists from persisted data.
/// </summary>
public partial class GroupListViewModel : ObservableObject {
    private readonly INavigationService _navigationService;
    private readonly ListGroupsQuery _query;
    private readonly AddGroupGameUseCase _addAssignment;
    private readonly GamesQuery _gamesQuery;
    private readonly ILogger<GroupListViewModel> _logger;
    private readonly IEnumerable<IGameSessionNavigationHandler> _handlers;

    public GroupListViewModel(ListGroupsQuery query, AddGroupGameUseCase addAssignment, GamesQuery gamesQuery, ILogger<GroupListViewModel> logger, IEnumerable<IGameSessionNavigationHandler> handlers, INavigationService navigationService) {
        _query = query;
        _addAssignment = addAssignment;
        _gamesQuery = gamesQuery;
        _logger = logger;
        _handlers = handlers;
        _navigationService = navigationService;
        AvailableGroups.CollectionChanged += (_, args) => {
            if (args.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Move)
                OnPropertyChanged(nameof(CanChoose));
        };
        Groups.CollectionChanged += (_, args) => {
            if (args.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Move)
                OnPropertyChanged(nameof(IsEmpty));
        };
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract), nameof(IsWorking))]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract), nameof(IsWorking))]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    public partial bool IsNavigating { get; private set; }

    public bool IsWorking => IsBusy || IsNavigating;
    private bool IsNotWorking => !IsWorking;
    public bool CanInteract => !IsBusy && !IsNavigating && ActiveSubdialog == GroupListSubdialog.None;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string SelectedGameKey { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedGameName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string BadgeText { get; private set; } = string.Empty;

    public ObservableCollection<GroupListItemViewModel> Groups { get; } = [];
    public bool IsEmpty => Groups.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChoose))]
    public partial bool CanCreate { get; private set; }

    public ObservableCollection<AvailablePlayGroup> AvailableGroups { get; } = [];
    public bool CanChoose => CanCreate && AvailableGroups.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    public partial GroupListSubdialog ActiveSubdialog { get; private set; }

    #region Localized UI Text

    [ObservableProperty]
    public partial string EmptyMessage { get; private set; } = string.Empty;

    #endregion

    public void SetGameContext(string key)
        => SelectedGameKey = key;

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        if (IsBusy || string.IsNullOrWhiteSpace(SelectedGameKey))
            return;

        IsBusy = true;
        ErrorMessage = null;

        try {
            GameInfo game = await _gamesQuery.GetAsync(SelectedGameKey, cancellationToken);
            SelectedGameName = game.Name;
            BadgeText = game.BadgeText;

            GroupPresence presence = await _query.GetPresenceAsync(cancellationToken);
            CanCreate = presence.HasPlayers;
            if (!presence.HasPlayers)
                EmptyMessage = AppResources.FirstPlayerHint;
            else if (!presence.HasGroups)
                EmptyMessage = AppResources.FirstGroupHint;
            else
                EmptyMessage = AppResources.NoGroupsForGame;

            IReadOnlyList<GroupListItem> rows = await _query.ListGroupsAsync(SelectedGameKey, cancellationToken);
            Groups.Clear();
            foreach (GroupListItem row in rows) {
                string lastPlayed = row.LastPlayedAt is DateTimeOffset date ? string.Format(CultureInfo.CurrentCulture,
                    AppResources.LastPlayedFormat, date.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)) : string.Empty;
                Groups.Add(new(row.Id, row.Name, string.Join(" · ", row.PlayerNames), lastPlayed, row.OpenGameSessionId, row.OpenGameSessionStatus));
            }

            AvailableGroups.Clear();
            foreach (ManagedGroup group in await _query.GetAvailableGroupsAsync(SelectedGameKey, cancellationToken))
                AvailableGroups.Add(new(group.Id, group.Name, string.Join(" · ", group.PlayerNames)));
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load groups for game.");
            ErrorMessage = AppResources.LoadGroupsFailed;
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateGroupAsync() {
        if (!CanCreate || !CanInteract || !TryBeginNavigation())
            return;

        ErrorMessage = null;

        try {
            await _navigationService.OpenGroupCreationAsync(SelectedGameKey);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not open group creation.");
            ErrorMessage = AppResources.NavigationFailed;
        } finally {
            EndNavigation();
        }
    }

    [RelayCommand]
    private void ChooseExisting() {
        if (CanChoose && CanInteract)
            ActiveSubdialog = GroupListSubdialog.SelectExistingGroup;
    }

    [RelayCommand]
    public async Task SelectExistingAsync(AvailablePlayGroup group) {
        if (!AvailableGroups.Contains(group) || !TryBeginNavigation())
            return;

        ErrorMessage = null;

        try {
            Guid gameId = (await _gamesQuery.GetEnabledAsync()).Single(game => game.Key == SelectedGameKey).Id;
            await _addAssignment.AddGroupGameAsync(group.Id, gameId);
            // The assignment is complete even if preparation cannot open. Retry from the
            // assigned list rather than offering an already assigned group for another write.
            AvailableGroups.Remove(group);
            await LoadAsync();
            ActiveSubdialog = GroupListSubdialog.None;
            await _handlers.Single(handler => handler.CanHandle(SelectedGameKey)).OpenGroupAsync(SelectedGameKey, group.Id, null, null);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not assign and open game group.");
            ErrorMessage = UserErrorMessage.Create(AppResources.OpenGroupFailed, exception);
        } finally {
            EndNavigation();
        }
    }

    [RelayCommand]
    private void CloseSelection() {
        if (!IsWorking)
            ActiveSubdialog = GroupListSubdialog.None;
    }

    [RelayCommand]
    public async Task OpenGroupAsync(Guid groupId) {
        if (!TryBeginNavigation())
            return;

        ErrorMessage = null;

        try {
            GroupListItem group = (await _query.ListGroupsAsync(SelectedGameKey)).Single(group => group.Id == groupId);
            await _handlers.Single(handler => handler.CanHandle(SelectedGameKey)).OpenGroupAsync(SelectedGameKey, group.Id, group.OpenGameSessionId, group.OpenGameSessionStatus);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not open game group.");
            ErrorMessage = UserErrorMessage.Create(AppResources.OpenGroupFailed, exception);
        } finally {
            EndNavigation();
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotWorking))]
    private async Task GoBackAsync() {
        if (!IsNotWorking)
            return;
        if (ActiveSubdialog != GroupListSubdialog.None) {
            CloseSelection();
            return;
        }
        if (!TryBeginNavigation())
            return;

        ErrorMessage = null;

        try {
            await _navigationService.GoBackAsync();
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not leave group selection.");
            ErrorMessage = AppResources.NavigationFailed;
        } finally {
            EndNavigation();
        }
    }

    /// <returns>True when navigation acquired the existing busy guard; false while loading or another navigation is pending.</returns>
    public bool TryBeginNavigation() {
        if (IsBusy || IsNavigating)
            return false;

        IsNavigating = true;
        return true;
    }

    public void EndNavigation()
        => IsNavigating = false;

}

public sealed record AvailablePlayGroup(Guid Id, string Name, string Members);
public sealed record GroupListItemViewModel(Guid Id, string Name, string PlayerNames, string LastPlayedText, Guid? OpenGameSessionId, GameSessionStatus? OpenGameSessionStatus) {
    public bool HasOpenGameSession => OpenGameSessionId.HasValue;
    public bool HasLastPlayed => !string.IsNullOrWhiteSpace(LastPlayedText);
}

public enum GroupListSubdialog {
    None,
    SelectExistingGroup
}

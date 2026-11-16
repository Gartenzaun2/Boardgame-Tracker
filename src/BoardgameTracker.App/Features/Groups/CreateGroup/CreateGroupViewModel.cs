using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.Groups;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

/// <summary>
/// 1. Build an unsaved group draft from existing players and new player inputs.
/// 2. Assign enabled games that support the membership; the play origin preselects its game.
/// 3. Find groups with the same player identities and offer reuse, reactivation or deliberate
///    creation of another group. Keep the complete checked draft until that decision succeeds.
/// 4. Persist the chosen action atomically, then return to management or open game preparation.
///    Retain the saved identity if navigation fails so departure never repeats the write.
/// </summary>
public partial class CreateGroupViewModel : ObservableObject {
    private readonly CreateGroupUseCase _createGroupUseCase;
    private readonly FindGroupDuplicatesQuery _duplicatesQuery;
    private readonly ReuseGroupUseCase _reuseGroupUseCase;
    private readonly GamesQuery _gamesQuery;
    private readonly INavigationService _navigationService;
    private readonly ILogger<CreateGroupViewModel> _logger;

    public CreateGroupViewModel(CreateGroupUseCase createGroupUseCase, FindGroupDuplicatesQuery duplicatesQuery, ReuseGroupUseCase reuseGroupUseCase, GamesQuery gamesQuery, INavigationService navigationService, ILogger<CreateGroupViewModel> logger) {
        _createGroupUseCase = createGroupUseCase;
        _duplicatesQuery = duplicatesQuery;
        _reuseGroupUseCase = reuseGroupUseCase;
        _gamesQuery = gamesQuery;
        _navigationService = navigationService;
        _logger = logger;
        Players.CollectionChanged += (_, args) => {
            GroupMemberDraft.RefreshPositionsAndMoveAvailability(Players);
            if (args.Action != NotifyCollectionChangedAction.Move) {
                OnPropertyChanged(nameof(HasNoPlayers));
                SaveCommand.NotifyCanExecuteChanged();
            }
        };
        AvailablePlayers.CollectionChanged += (_, args) => {
            if (args.Action != NotifyCollectionChangedAction.Move)
                OnPropertyChanged(nameof(HasAvailablePlayers));
        };
    }

    /// <summary>
    /// A later OnAppearing occurs after overlays or duplicate selection. Busy only protects
    /// concurrent loads; remembering a successful initial load prevents replacing the draft later.
    /// </summary>
    private bool _isInitialLoadCompleted;
    private string? _gameKey;

    /// <summary>
    /// Retains the persisted identity and origin across navigation failures. Retrying departure
    /// must open the same group and remove the correct form pages without another write.
    /// </summary>
    private GroupSaveOutcome? _savedOutcome;

    /// <summary>
    /// Freezes the complete unsaved input while duplicate selection is open, so reuse,
    /// reactivation and deliberate creation act on the exact draft that was checked.
    /// </summary>
    private CreateGroupCommand? _pendingCommand;

    /// <summary>Remembers successful duplicate navigation so completion removes both form pages.</summary>
    private bool _isDuplicateSelectionOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(GoBackCommand), nameof(ReturnToDraftCommand))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    public partial bool IsBusy { get; private set; }
    private bool IsNotBusy => !IsBusy;

    public bool IsSaveCompleted => _savedOutcome is not null;
    public bool CanEdit => IsNotBusy && !IsSaveCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    public partial GroupCreationSubdialog ActiveSubdialog { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    public ObservableCollection<GroupMemberDraft> Players { get; } = [];
    public bool HasNoPlayers => Players.Count == 0;

    public ObservableCollection<SelectablePlayer> AvailablePlayers { get; } = [];
    public bool HasAvailablePlayers => AvailablePlayers.Count > 0;

    [ObservableProperty]
    public partial string NewPlayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewPlayerIsGuest { get; set; }

    public ObservableCollection<GroupGameSelection> Games { get; } = [];
    public IReadOnlyList<GroupGameSelection> SelectedGames => Games.Where(game => game.IsSelected).ToArray();
    public IReadOnlyList<GroupGameSelection> AvailableGames => Games.Where(game => !game.IsSelected && game.SupportsPlayerCount(Players.Count)).ToArray();
    public bool HasAvailableGames => AvailableGames.Count > 0;

    public ObservableCollection<DuplicateGroupRow> Duplicates { get; } = [];

    #region Localized UI Text

    public string SaveText => IsSaveCompleted ? AppResources.Done : AppResources.Save;

    #endregion

    public void SetManagementOrigin()
        => _gameKey = null;

    public void SetGameContext(string gameKey)
        => _gameKey = gameKey;

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        if (!IsNotBusy || _isInitialLoadCompleted)
            return;

        try {
            IsBusy = true;
            ErrorMessage = null;

            IReadOnlyList<GameInfo> games = await _gamesQuery.GetEnabledAsync(cancellationToken);
            string? selectedGameKey = _gameKey;
            Games.Clear();
            foreach (GameInfo game in games)
                Games.Add(new(game.Id, game.Name, game.Key == selectedGameKey, maximumPlayers: game.MaximumPlayers, minimumPlayers: game.MinimumPlayers));
            RefreshGameSelectionBindings();

            IReadOnlyList<SelectablePlayer> players = await _createGroupUseCase.GetSelectablePlayersAsync(cancellationToken);

            AvailablePlayers.Clear();
            foreach (SelectablePlayer player in players) {
                if (Players.All(candidate => candidate.PlayerId != player.Id))
                    AvailablePlayers.Add(player);
            }

            _isInitialLoadCompleted = true;
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load selectable players for group creation.");
            ErrorMessage = UserErrorMessage.Create(AppResources.LoadPlayersFailed, exception);
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ChooseExistingPlayer() {
        if (CanEdit && HasAvailablePlayers)
            ActiveSubdialog = GroupCreationSubdialog.SelectExistingPlayer;
    }

    [RelayCommand]
    private void AddExistingPlayer(SelectablePlayer? player) {
        if (!CanEdit || player is null || !AvailablePlayers.Contains(player) || Players.Any(candidate => candidate.PlayerId == player.Id))
            return;

        Players.Add(new GroupMemberDraft(player.Id, player.Name, player.IsGuest));
        AvailablePlayers.Remove(player);
        ActiveSubdialog = GroupCreationSubdialog.None;
        RefreshGameSelectionsAndValidation();
    }

    private void SortAvailablePlayers() {
        IReadOnlyList<SelectablePlayer> ordered = [.. AvailablePlayers.OrderBy(player => player.Name, StringComparer.CurrentCultureIgnoreCase)];

        AvailablePlayers.Clear();
        foreach (SelectablePlayer player in ordered)
            AvailablePlayers.Add(player);
    }

    [RelayCommand]
    public void BeginNewPlayer() {
        if (!CanEdit)
            return;

        NewPlayerName = string.Empty;
        NewPlayerIsGuest = false;
        ActiveSubdialog = GroupCreationSubdialog.CreatePlayer;
    }

    [RelayCommand]
    private void AddNewPlayer() {
        if (!CanEdit)
            return;

        string name = NewPlayerName.Trim();

        if (string.IsNullOrWhiteSpace(name)) {
            ErrorMessage = AppResources.EnterPlayerName;
            return;
        }

        Players.Add(new GroupMemberDraft(null, name, NewPlayerIsGuest));
        ActiveSubdialog = GroupCreationSubdialog.None;
        NewPlayerName = string.Empty;
        NewPlayerIsGuest = false;
        ErrorMessage = null;
        RefreshGameSelectionsAndValidation();
    }

    [RelayCommand]
    private void RemovePlayer(GroupMemberDraft? player) {
        if (!CanEdit || player is null || !Players.Remove(player))
            return;

        if (player.PlayerId is Guid playerId) {
            AvailablePlayers.Add(new SelectablePlayer(playerId, player.Name, player.IsGuest));

            SortAvailablePlayers();
        }

        RefreshGameSelectionsAndValidation();
    }

    [RelayCommand]
    private void MovePlayerUp(GroupMemberDraft? player) {
        if (!CanEdit || player is null)
            return;

        int index = Players.IndexOf(player);
        if (index > 0)
            Players.Move(index, index - 1);
    }

    [RelayCommand]
    private void MovePlayerDown(GroupMemberDraft? player) {
        if (!CanEdit || player is null)
            return;

        int index = Players.IndexOf(player);
        if (index >= 0 && index < Players.Count - 1)
            Players.Move(index, index + 1);
    }

    [RelayCommand]
    private void ChooseGame() {
        if (CanEdit && HasAvailableGames)
            ActiveSubdialog = GroupCreationSubdialog.SelectGame;
    }

    [RelayCommand]
    private void AddGame(GroupGameSelection? game) {
        if (!CanEdit || game is null || game.IsSelected || !Games.Contains(game) || !game.SupportsPlayerCount(Players.Count))
            return;

        game.IsSelected = true;
        RefreshGameSelectionsAndValidation();
        ActiveSubdialog = GroupCreationSubdialog.None;
    }

    [RelayCommand]
    private void RemoveGame(GroupGameSelection? game) {
        if (!CanEdit || game is null || !game.IsSelected || !Games.Contains(game) || !game.CanChange)
            return;
        game.IsSelected = false;
        RefreshGameSelectionsAndValidation();
    }

    private void RefreshGameSelectionBindings() {
        OnPropertyChanged(nameof(SelectedGames));
        OnPropertyChanged(nameof(AvailableGames));
        OnPropertyChanged(nameof(HasAvailableGames));
    }

    private void RefreshGameSelectionsAndValidation() {
        RefreshGameSelectionBindings();
        ErrorMessage = Games.Any(game => game.IsSelected && !game.SupportsPlayerCount(Players.Count))
            ? AppResources.GroupPlayerLimitExceeded : null;
    }

    [RelayCommand]
    public void CloseSubdialog() {
        if (!IsNotBusy)
            return;

        if (ActiveSubdialog == GroupCreationSubdialog.CreatePlayer) {
            NewPlayerName = string.Empty;
            NewPlayerIsGuest = false;
        }

        ActiveSubdialog = GroupCreationSubdialog.None;
    }

    private bool CanSave()
        => IsNotBusy && (IsSaveCompleted || !string.IsNullOrWhiteSpace(GroupName) && Players.Count > 0);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync() {
        if (!CanSave())
            return;

        try {
            IsBusy = true;
            ErrorMessage = null;

            if (_savedOutcome is not null) {
                await CompleteSaveAsync(_savedOutcome.GroupId, _savedOutcome.FromDuplicates);
                return;
            }
            if (Games.Any(game => game.IsSelected && !game.SupportsPlayerCount(Players.Count))) {
                ErrorMessage = AppResources.GroupPlayerLimitExceeded;
                return;
            }

            _pendingCommand = new CreateGroupCommand(GroupName,
                [.. Players.Select(player => new CreateGroupPlayerInput(player.PlayerId, player.Name, player.IsGuest))],
                Games.Where(game => game.IsSelected).Select(game => game.Id).ToArray());
            IReadOnlyList<GroupDuplicate> matches = await _duplicatesQuery.FindDuplicatesAsync(_pendingCommand.Players);
            Duplicates.Clear();
            foreach (GroupDuplicate match in matches)
                Duplicates.Add(new(match.Id, match.Name, string.Join(" · ", match.PlayerNames), match.IsArchived));
            if (Duplicates.Count > 0) {
                await _navigationService.ShowGroupDuplicatesAsync(this);
                _isDuplicateSelectionOpen = true;
                return;
            }

            Guid groupId = await _createGroupUseCase.CreateGroupAsync(_pendingCommand);
            await CompleteSaveAsync(groupId, false);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not create group.");
            ErrorMessage = UserErrorMessage.Create(_savedOutcome is null ? AppResources.SaveGroupFailed : AppResources.GroupNavigationFailed, exception);
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReuseDuplicateAsync(DuplicateGroupRow? row) {
        if (IsBusy || _pendingCommand is null || row is null || !Duplicates.Contains(row))
            return;

        IsBusy = true;
        ErrorMessage = null;

        try {
            if (_savedOutcome is not null) {
                await CompleteSaveAsync(_savedOutcome.GroupId, _savedOutcome.FromDuplicates);
                return;
            }

            Guid groupId = await _reuseGroupUseCase.ReuseGroupAsync(new(row.Id,
                _pendingCommand.Players.Select(player => player.ExistingPlayerId!.Value).ToArray(), _pendingCommand.GameIds,
                row.IsArchived ? GroupReuseAction.Reactivate : GroupReuseAction.UseExisting));
            await CompleteSaveAsync(groupId, true);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not reuse selected group.");
            ErrorMessage = UserErrorMessage.Create(_savedOutcome is null ? AppResources.SaveGroupFailed : AppResources.GroupNavigationFailed, exception);
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateAdditionalGroupAsync() {
        if (IsBusy || _pendingCommand is null)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try {
            if (_savedOutcome is not null) {
                await CompleteSaveAsync(_savedOutcome.GroupId, _savedOutcome.FromDuplicates);
                return;
            }

            Guid groupId = await _createGroupUseCase.CreateGroupAsync(_pendingCommand);
            await CompleteSaveAsync(groupId, true);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not create additional group.");
            ErrorMessage = UserErrorMessage.Create(_savedOutcome is null ? AppResources.SaveGroupFailed : AppResources.GroupNavigationFailed, exception);
        } finally {
            IsBusy = false;
        }
    }

    private async Task CompleteSaveAsync(Guid groupId, bool fromDuplicates) {
        _savedOutcome ??= new(groupId, _gameKey, fromDuplicates && _isDuplicateSelectionOpen);
        OnPropertyChanged(nameof(IsSaveCompleted));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(SaveText));
        await _navigationService.CompleteGroupCreationAsync(this, _savedOutcome);
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task GoBackAsync() {
        if (!IsNotBusy)
            return;
        if (ActiveSubdialog != GroupCreationSubdialog.None) {
            CloseSubdialog();
            return;
        }

        await LeaveCreationAsync(false);
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task ReturnToDraftAsync() {
        if (!IsNotBusy)
            return;
        await LeaveCreationAsync(true);
    }

    private async Task LeaveCreationAsync(bool cancelDuplicates) {
        IsBusy = true;
        ErrorMessage = null;

        try {
            if (_savedOutcome is not null)
                await _navigationService.CompleteGroupCreationAsync(this, _savedOutcome);
            else {
                await _navigationService.GoBackAsync();
                if (cancelDuplicates) {
                    _pendingCommand = null;
                    Duplicates.Clear();
                    _isDuplicateSelectionOpen = false;
                }
            }
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not leave group creation.");
            ErrorMessage = UserErrorMessage.Create(IsSaveCompleted ? AppResources.GroupNavigationFailed : AppResources.NavigationFailed, exception);
        } finally {
            IsBusy = false;
        }
    }

}

public sealed record GroupSaveOutcome(Guid GroupId, string? GameKey, bool FromDuplicates);

public enum GroupCreationSubdialog {
    None,
    SelectExistingPlayer,
    CreatePlayer,
    SelectGame
}

using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

/// <summary>
/// Owns create-group screen state and translates UI input into the create-group Application command.
/// </summary>
public partial class CreateGroupViewModel(CreateGroupUseCase createGroup, FindGroupDuplicatesQuery duplicatesQuery, ReuseGroupUseCase reuseGroup, IEnumerable<IGameSessionNavigationHandler> games, ILogger<CreateGroupViewModel> logger) : ObservableObject
{
    private bool _loaded;
    private string? _gameKey;
    private GroupSaveOutcome? _savedOutcome;
    private CreateGroupCommand? _pendingCommand;
    public ObservableCollection<GroupGameSelection> Games { get; } = [];
    public ObservableCollection<DuplicateGroupRow> Duplicates { get; } = [];
    public Func<Task>? ShowDuplicatesAsync { get; set; }
    public Func<GroupSaveOutcome, Task>? SavedAsync { get; set; }
    public void SetManagementOrigin() => _gameKey = null;
    public void SetGameContext(string gameKey) => _gameKey = gameKey;
    public void CancelDuplicates()
    {
        _pendingCommand = null;
        Duplicates.Clear();
    }
    public ObservableCollection<SelectablePlayer> AvailablePlayers { get; } = [];
    public ObservableCollection<GroupMemberDraft> Players { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedPlayerCommand))]
    public partial SelectablePlayer? SelectedExistingPlayer { get; set; }

    [ObservableProperty]
    public partial string NewPlayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewPlayerIsGuest { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedPlayerCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool IsIdle => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    public partial bool HasNoPlayers { get; private set; } = true;

    [ObservableProperty]
    public partial bool HasNoAvailablePlayers { get; private set; } = true;


    public bool HasAvailablePlayers => !HasNoAvailablePlayers;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || _loaded)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            IReadOnlyList<SelectableGroupGame> availableGames = await createGroup.GetSelectableGamesAsync(cancellationToken);
            string? selectedGameKey = _gameKey;
            Games.Clear();
            foreach (SelectableGroupGame game in availableGames)
                Games.Add(new(game.Id, games.FirstOrDefault(handler => handler.GameKey == game.Key)?.DisplayName ?? game.Key, game.Key == selectedGameKey));
            IReadOnlyList<SelectablePlayer> players =
                await createGroup.GetSelectablePlayersAsync(cancellationToken);

            AvailablePlayers.Clear();

            foreach (SelectablePlayer player in players)
            {
                if (Players.All(candidate => candidate.PlayerId != player.Id))
                {
                    AvailablePlayers.Add(player);
                }
            }

            SelectedExistingPlayer = null;
            UpdateCollectionStates();
            _loaded = true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load selectable players for group creation.");
            ErrorMessage = UserErrorMessage.Create(AppResources.LoadPlayersFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAddSelectedPlayer() =>
        !IsBusy && SelectedExistingPlayer is not null;

    [RelayCommand(CanExecute = nameof(CanAddSelectedPlayer))]
    private void AddSelectedPlayer()
    {
        SelectablePlayer? player = SelectedExistingPlayer;

        if (player is null || Players.Any(candidate => candidate.PlayerId == player.Id))
        {
            return;
        }

        Players.Add(new GroupMemberDraft(player.Id, player.Name, player.IsGuest));
        AvailablePlayers.Remove(player);
        SelectedExistingPlayer = null;
        ErrorMessage = null;
        PlayersChanged();
    }

    [RelayCommand]
    private void AddNewPlayer()
    {
        if (IsBusy)
        {
            return;
        }

        string name = NewPlayerName.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = AppResources.EnterPlayerName;
            return;
        }

        Players.Add(new GroupMemberDraft(null, name, NewPlayerIsGuest));
        NewPlayerName = string.Empty;
        NewPlayerIsGuest = false;
        ErrorMessage = null;
        PlayersChanged();
    }

    [RelayCommand]
    private void RemovePlayer(GroupMemberDraft? player)
    {
        if (IsBusy || player is null || !Players.Remove(player))
        {
            return;
        }

        if (player.PlayerId is Guid playerId)
        {
            AvailablePlayers.Add(new SelectablePlayer(
                playerId,
                player.Name,
                player.IsGuest));

            SortAvailablePlayers();
        }

        PlayersChanged();
    }

    [RelayCommand]
    private void MovePlayerUp(GroupMemberDraft? player)
    {
        if (IsBusy || player is null)
        {
            return;
        }

        int index = Players.IndexOf(player);
        if (index > 0)
        {
            Players.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MovePlayerDown(GroupMemberDraft? player)
    {
        if (IsBusy || player is null)
        {
            return;
        }

        int index = Players.IndexOf(player);
        if (index >= 0 && index < Players.Count - 1)
        {
            Players.Move(index, index + 1);
        }
    }

    private bool CanSave() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(GroupName)
        && Players.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (_savedOutcome is not null)
            {
                await CompleteSaveAsync(_savedOutcome.GroupId, _savedOutcome.FromDuplicates);
                return;
            }
            _pendingCommand = new CreateGroupCommand(GroupName,
                [.. Players.Select(player => new CreateGroupPlayerInput(player.PlayerId, player.Name, player.IsGuest))],
                Games.Where(game => game.IsSelected).Select(game => game.Id).ToArray());
            IReadOnlyList<GroupDuplicate> matches = await duplicatesQuery.FindDuplicatesAsync(_pendingCommand.Players);
            Duplicates.Clear();
            foreach (GroupDuplicate match in matches)
                Duplicates.Add(new(match.Id, match.Name, string.Join(" · ", match.PlayerNames), match.IsArchived));
            if (Duplicates.Count > 0)
            {
                if (ShowDuplicatesAsync is not null)
                    await ShowDuplicatesAsync();
            }
            else
            {
                Guid groupId = await createGroup.CreateGroupAsync(_pendingCommand);
                await CompleteSaveAsync(groupId, false);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create group.");
            ErrorMessage = UserErrorMessage.Create(_savedOutcome is null ? AppResources.SaveGroupFailed : AppResources.GroupNavigationFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReuseDuplicateAsync(DuplicateGroupRow? row)
    {
        if (IsBusy || _pendingCommand is null || row is null || !Duplicates.Contains(row))
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            if (_savedOutcome is not null)
            {
                await CompleteSaveAsync(_savedOutcome.GroupId, _savedOutcome.FromDuplicates);
                return;
            }
            Guid groupId = await reuseGroup.ReuseGroupAsync(new(row.Id,
                _pendingCommand.Players.Select(player => player.ExistingPlayerId!.Value).ToArray(), _pendingCommand.GameIds,
                row.IsArchived ? GroupReuseAction.Reactivate : GroupReuseAction.UseExisting));
            await CompleteSaveAsync(groupId, true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not reuse selected group.");
            ErrorMessage = UserErrorMessage.Create(_savedOutcome is null ? AppResources.SaveGroupFailed : AppResources.GroupNavigationFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateAdditionalGroupAsync()
    {
        if (IsBusy || _pendingCommand is null)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            if (_savedOutcome is not null)
            {
                await CompleteSaveAsync(_savedOutcome.GroupId, _savedOutcome.FromDuplicates);
                return;
            }
            Guid groupId = await createGroup.CreateGroupAsync(_pendingCommand);
            await CompleteSaveAsync(groupId, true);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create additional group.");
            ErrorMessage = UserErrorMessage.Create(_savedOutcome is null ? AppResources.SaveGroupFailed : AppResources.GroupNavigationFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CompleteSaveAsync(Guid groupId, bool fromDuplicates)
    {
        _savedOutcome ??= new(groupId, _gameKey, fromDuplicates);
        if (SavedAsync is not null)
            await SavedAsync(_savedOutcome);
    }

    private void PlayersChanged()
    {
        SaveCommand.NotifyCanExecuteChanged();
        UpdateCollectionStates();
    }

    private void UpdateCollectionStates()
    {
        HasNoPlayers = Players.Count == 0;
        HasNoAvailablePlayers = AvailablePlayers.Count == 0;
        OnPropertyChanged(nameof(HasAvailablePlayers));
    }

    private void SortAvailablePlayers()
    {
        IReadOnlyList<SelectablePlayer> ordered =
            [.. AvailablePlayers.OrderBy(player => player.Name, StringComparer.CurrentCultureIgnoreCase)];

        AvailablePlayers.Clear();
        foreach (SelectablePlayer player in ordered)
        {
            AvailablePlayers.Add(player);
        }
    }
}


public sealed record GroupSaveOutcome(Guid GroupId, string? GameKey, bool FromDuplicates);

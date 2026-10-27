using BoardgameTracker.App.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups.CreateGroup;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

/// <summary>
/// Owns create-group screen state and translates UI input into the create-group Application command.
/// </summary>
public partial class CreateGroupViewModel(CreateGroupUseCase createGroup, ILogger<CreateGroupViewModel> logger) : ObservableObject
{
    public ObservableCollection<SelectablePlayer> AvailablePlayers { get; } = [];
    public ObservableCollection<CreateGroupPlayerInput> Players { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTemporary { get; set; }

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
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    public partial bool HasNoPlayers { get; private set; } = true;

    [ObservableProperty]
    public partial bool HasNoAvailablePlayers { get; private set; } = true;

    public event EventHandler? Saved;

    public bool HasAvailablePlayers => !HasNoAvailablePlayers;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            IReadOnlyList<SelectablePlayer> players =
                await createGroup.GetSelectablePlayersAsync(cancellationToken);

            AvailablePlayers.Clear();

            foreach (SelectablePlayer player in players)
            {
                if (Players.All(candidate => candidate.ExistingPlayerId != player.Id))
                {
                    AvailablePlayers.Add(player);
                }
            }

            SelectedExistingPlayer = null;
            UpdateCollectionStates();
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

        if (player is null || Players.Any(candidate => candidate.ExistingPlayerId == player.Id))
        {
            return;
        }

        Players.Add(new CreateGroupPlayerInput(player.Id, player.Name, player.IsGuest));
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

        Players.Add(new CreateGroupPlayerInput(null, name, NewPlayerIsGuest));
        NewPlayerName = string.Empty;
        NewPlayerIsGuest = false;
        ErrorMessage = null;
        PlayersChanged();
    }

    [RelayCommand]
    private void RemovePlayer(CreateGroupPlayerInput? player)
    {
        if (player is null || !Players.Remove(player))
        {
            return;
        }

        if (player.ExistingPlayerId is Guid playerId)
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
    private void MovePlayerUp(CreateGroupPlayerInput? player)
    {
        if (player is null)
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
    private void MovePlayerDown(CreateGroupPlayerInput? player)
    {
        if (player is null)
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

            await createGroup.ExecuteAsync(
                new CreateGroupCommand(GroupName, IsTemporary, [.. Players]));

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create group.");
            ErrorMessage = UserErrorMessage.Create(AppResources.SaveGroupFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
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


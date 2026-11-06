using BoardgameTracker.Application.Settings;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

/// <summary>
/// Adapts the existing editor to game-neutral group metadata and order changes.
/// </summary>
public partial class EditGroupViewModel(EditGroupUseCase editGroup, IEnumerable<IGameSessionNavigationHandler> games, GameActivationUseCase activation, ILogger<EditGroupViewModel> logger) : ObservableObject
{
    public ObservableCollection<GroupGameSelection> Games { get; } = [];
    public IReadOnlyList<GroupGameSelection> SelectedGames => Games.Where(game => game.IsSelected && game.IsVisible).ToArray();
    public IReadOnlyList<GroupGameSelection> AvailableGames => Games.Where(game => !game.IsSelected && game.IsVisible).ToArray();
    public bool HasAvailableGames => AvailableGames.Count > 0;
    [ObservableProperty] public partial bool IsChoosingGame { get; set; }

    private void GamesChanged()
    {
        OnPropertyChanged(nameof(SelectedGames));
        OnPropertyChanged(nameof(AvailableGames));
        OnPropertyChanged(nameof(HasAvailableGames));
    }

    [RelayCommand]
    private void AddGame(GroupGameSelection? game)
    {
        if (IsBusy || game is null || !game.IsVisible || !Games.Contains(game)) return;
        game.IsSelected = true;
        IsChoosingGame = false;
    }

    [RelayCommand]
    private void RemoveGame(GroupGameSelection? game)
    {
        if (IsBusy || game is null || !Games.Contains(game) || !game.CanChange) return;
        game.IsSelected = false;
    }

    [ObservableProperty] public partial bool CanArchive { get; private set; }
    public bool ArchiveLocked => !CanArchive;
    public ObservableCollection<GroupMemberDraft> Players { get; } = [];

    [ObservableProperty]
    public partial Guid GroupId { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public event EventHandler? Saved;

    public async Task LoadAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy || groupId == Guid.Empty || GroupId == groupId)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            EditGroupDetails? details =
                await editGroup.GetAsync(groupId, cancellationToken);

            if (details is null)
            {
                ErrorMessage = AppResources.GroupNotFound;
                return;
            }

            HashSet<string> visibleGames = (await activation.GetGamesAsync(cancellationToken)).Where(game => game.IsEnabled).Select(game => game.GameKey).ToHashSet(StringComparer.Ordinal);
            Games.Clear();
            foreach (EditGroupGame game in details.Games)
                Games.Add(new(game.Id, games.FirstOrDefault(handler => handler.GameKey == game.Key)?.DisplayName ?? game.Key, game.IsAssigned, game.CanRemove, visibleGames.Contains(game.Key)));
            foreach (var game in Games) game.PropertyChanged += (_, _) => GamesChanged();
            GamesChanged();
            CanArchive = details.CanArchive;
            OnPropertyChanged(nameof(ArchiveLocked));
            GroupId = details.Id;
            GroupName = details.Name;
            IsArchived = details.IsArchived;

            Players.Clear();
            foreach (EditGroupPlayer player in details.Players.OrderBy(player => player.DefaultOrder))
            {
                Players.Add(new GroupMemberDraft(
                    player.PlayerId,
                    player.Name,
                    player.IsGuest));
            }

            GroupMemberDraft.UpdatePositions(Players);
            SaveCommand.NotifyCanExecuteChanged();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load group {GroupId}.", groupId);
            ErrorMessage = UserErrorMessage.Create(AppResources.LoadGroupFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
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
            GroupMemberDraft.UpdatePositions(Players);
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
            GroupMemberDraft.UpdatePositions(Players);
        }
    }

    private bool CanSave() =>
        !IsBusy
        && GroupId != Guid.Empty
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

            IReadOnlyList<Guid> orderedPlayerIds =
                [.. Players.Select(player => player.PlayerId!.Value)];

            await editGroup.UpdateGroupAsync(new EditGroupCommand(
                GroupId,
                GroupName,
                IsArchived,
                orderedPlayerIds,
                Games.Where(game => game.IsSelected).Select(game => game.Id).ToArray()));

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save group {GroupId}.", GroupId);
            ErrorMessage = UserErrorMessage.Create(AppResources.SaveGroupFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

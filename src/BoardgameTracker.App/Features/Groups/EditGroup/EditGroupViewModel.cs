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

public partial class EditGroupViewModel : ObservableObject {
    private readonly EditGroupUseCase _editGroupUseCase;
    private readonly GameActivationUseCase _gameActivationUseCase;
    private readonly INavigationService _navigationService;
    private readonly ILogger<EditGroupViewModel> _logger;

    public EditGroupViewModel(EditGroupUseCase editGroupUseCase, GameActivationUseCase gameActivationUseCase, INavigationService navigationService, ILogger<EditGroupViewModel> logger) {
        _editGroupUseCase = editGroupUseCase;
        _gameActivationUseCase = gameActivationUseCase;
        _navigationService = navigationService;
        _logger = logger;
        Players.CollectionChanged += (_, args) => {
            if (IsNotBusy && args.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Move)
                SaveCommand.NotifyCanExecuteChanged();
        };
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(GoBackCommand))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    public partial bool IsBusy { get; private set; }
    private bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Keeps saved inputs frozen if navigation fails after a successful write.
    /// Retrying departure must use the completed save rather than write again.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(CanEdit), nameof(SaveText))]
    public partial bool IsSaveCompleted { get; private set; }

    public bool CanEdit => IsNotBusy && !IsSaveCompleted;
    public bool CanSave => IsNotBusy && (IsSaveCompleted || (GroupId != Guid.Empty && !string.IsNullOrWhiteSpace(GroupName) && Players.Count > 0));

    [ObservableProperty]
    public partial bool IsChoosingGame { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial Guid GroupId { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsGroupArchived { get; set; }

    [ObservableProperty]
    public partial bool CanArchiveGroup { get; private set; }

    public ObservableCollection<GroupMemberDraft> Players { get; } = [];
    public ObservableCollection<GroupGameSelection> GameSelections { get; } = [];

    public IReadOnlyList<GroupGameSelection> SelectedVisibleGames => GameSelections.Where(game => game.IsSelected && game.IsVisible).ToArray();
    public IReadOnlyList<GroupGameSelection> GamesAvailableToAdd => GameSelections.Where(game => !game.IsSelected && game.IsVisible).ToArray();
    public bool HasGamesAvailableToAdd => GamesAvailableToAdd.Count > 0;

    #region Localized UI Text

    public string SaveText => IsSaveCompleted ? AppResources.Done : AppResources.Save;

    #endregion

    public async Task LoadAsync(Guid groupId, CancellationToken cancellationToken = default) {
        if (!IsNotBusy)
            return;
        // Returning from an overlay must not replace edits to an already loaded group.
        if (groupId == Guid.Empty || GroupId == groupId)
            return;

        try {
            IsBusy = true;
            ErrorMessage = null;

            EditGroupDetails? details = await _editGroupUseCase.GetAsync(groupId, cancellationToken);

            if (details is null) {
                ErrorMessage = AppResources.GroupNotFound;
                return;
            }

            IReadOnlyList<GameActivationState> activationStates = await _gameActivationUseCase.GetGamesAsync(cancellationToken);
            HashSet<string> enabledGameKeys = activationStates.Where(game => game.IsEnabled).Select(game => game.GameKey).ToHashSet(StringComparer.Ordinal);
            GameSelections.Clear();
            foreach (EditGroupGame game in details.Games)
                GameSelections.Add(new(game.Id, game.Name, game.IsAssigned, game.CanRemove, enabledGameKeys.Contains(game.Key)));
            RefreshGameSelectionBindings();
            CanArchiveGroup = details.CanArchive;
            GroupName = details.Name;
            IsGroupArchived = details.IsArchived;

            Players.Clear();
            foreach (EditGroupPlayer player in details.Players.OrderBy(player => player.DefaultOrder))
                Players.Add(new GroupMemberDraft(player.PlayerId, player.Name, player.IsGuest));

            GroupMemberDraft.UpdatePositions(Players);
            IsSaveCompleted = false;
            GroupId = details.Id;
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load group {GroupId}.", groupId);
            ErrorMessage = UserErrorMessage.Create(AppResources.LoadGroupFailed, exception);
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ChooseGame() {
        if (CanEdit)
            IsChoosingGame = true;
    }

    [RelayCommand]
    private void CloseGameSelection()
        => IsChoosingGame = false;

    [RelayCommand]
    private void AddGame(GroupGameSelection? game) {
        if (!CanEdit)
            return;
        if (game is null || game.IsSelected || !game.IsVisible || !GameSelections.Contains(game))
            return;

        game.IsSelected = true;
        IsChoosingGame = false;
        RefreshGameSelectionBindings();
    }

    [RelayCommand]
    private void RemoveGame(GroupGameSelection? game) {
        if (!CanEdit)
            return;
        if (game is null || !game.IsSelected || !GameSelections.Contains(game) || !game.CanChange)
            return;

        game.IsSelected = false;
        RefreshGameSelectionBindings();
    }

    private void RefreshGameSelectionBindings() {
        OnPropertyChanged(nameof(SelectedVisibleGames));
        OnPropertyChanged(nameof(GamesAvailableToAdd));
        OnPropertyChanged(nameof(HasGamesAvailableToAdd));
    }

    [RelayCommand]
    private void MovePlayerUp(GroupMemberDraft? player) {
        if (!CanEdit || player is null)
            return;

        int index = Players.IndexOf(player);
        if (index <= 0)
            return;

        Players.Move(index, index - 1);
        GroupMemberDraft.UpdatePositions(Players);
    }

    [RelayCommand]
    private void MovePlayerDown(GroupMemberDraft? player) {
        if (!CanEdit || player is null)
            return;

        int index = Players.IndexOf(player);
        if (index < 0 || index >= Players.Count - 1)
            return;

        Players.Move(index, index + 1);
        GroupMemberDraft.UpdatePositions(Players);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync() {
        if (!CanSave)
            return;

        try {
            IsBusy = true;
            ErrorMessage = null;

            if (!IsSaveCompleted) {
                IReadOnlyList<Guid> orderedPlayerIds = [.. Players.Select(player => player.PlayerId!.Value)];
                Guid[] selectedGameIds = GameSelections.Where(game => game.IsSelected).Select(game => game.Id).ToArray();
                EditGroupCommand command = new(GroupId, GroupName, IsGroupArchived, orderedPlayerIds, selectedGameIds);
                await _editGroupUseCase.UpdateGroupAsync(command);
                IsSaveCompleted = true;
            }

            await NavigateFromEditorAsync();
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not save group {GroupId}.", GroupId);
            ErrorMessage = UserErrorMessage.Create(AppResources.SaveGroupFailed, exception);
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task GoBackAsync() {
        if (!IsNotBusy)
            return;

        try {
            IsBusy = true;
            ErrorMessage = null;

            await NavigateFromEditorAsync();
        } finally {
            IsBusy = false;
        }
    }

    private async Task NavigateFromEditorAsync() {
        try {
            await _navigationService.GoBackAsync();
        } catch (Exception exception) when (IsSaveCompleted) {
            _logger.LogError(exception, "Could not leave group editor after saving group {GroupId}.", GroupId);
            ErrorMessage = AppResources.GroupEditorNavigationFailed;
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not leave group editor.");
            ErrorMessage = AppResources.NavigationFailed;
        }
    }
}

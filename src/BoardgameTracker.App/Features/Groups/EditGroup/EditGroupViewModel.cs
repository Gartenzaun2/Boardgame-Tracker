using System.Collections.ObjectModel;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.Groups;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

/// <summary>
/// 1. Load metadata, fixed membership and game-assignment locks once per group.
/// 2. Edit the name, archive state, member order and enabled game assignments in memory.
/// 3. Save through the Application use case, which rechecks session locks; retry departure
///    after navigation failure without repeating the completed write.
/// </summary>
public partial class EditGroupViewModel : ObservableObject {
    private readonly EditGroupUseCase _editGroupUseCase;
    private readonly GamesQuery _gamesQuery;
    private readonly INavigationService _navigationService;
    private readonly ILogger<EditGroupViewModel> _logger;

    public EditGroupViewModel(EditGroupUseCase editGroupUseCase, GamesQuery gamesQuery, INavigationService navigationService, ILogger<EditGroupViewModel> logger) {
        _editGroupUseCase = editGroupUseCase;
        _gamesQuery = gamesQuery;
        _navigationService = navigationService;
        _logger = logger;
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
    public bool CanSave => IsNotBusy && (IsSaveCompleted || (GroupId != Guid.Empty && !string.IsNullOrWhiteSpace(GroupName)));

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

    [ObservableProperty]
    public partial EditGroupSubdialog ActiveSubdialog { get; private set; }

    public ObservableCollection<GroupGameSelection> GameSelections { get; } = [];

    public IReadOnlyList<GroupGameSelection> SelectedEnabledGames => GameSelections.Where(game => game.IsSelected && game.IsEnabled).ToArray();
    public IReadOnlyList<GroupGameSelection> GamesAvailableToAdd => GameSelections.Where(game => !game.IsSelected && game.IsEnabled).ToArray();
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

            HashSet<string> enabledGameKeys = (await _gamesQuery.GetEnabledAsync(cancellationToken)).Select(game => game.Key).ToHashSet(StringComparer.Ordinal);
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

            GroupMemberDraft.RefreshPositionsAndMoveAvailability(Players);
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
            ActiveSubdialog = EditGroupSubdialog.SelectGame;
    }

    [RelayCommand]
    private void CloseGameSelection()
        => ActiveSubdialog = EditGroupSubdialog.None;

    [RelayCommand]
    private void AddGame(GroupGameSelection? game) {
        if (!CanEdit)
            return;
        if (game is null || game.IsSelected || !game.IsEnabled || !GameSelections.Contains(game))
            return;

        game.IsSelected = true;
        ActiveSubdialog = EditGroupSubdialog.None;
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
        OnPropertyChanged(nameof(SelectedEnabledGames));
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
        GroupMemberDraft.RefreshPositionsAndMoveAvailability(Players);
    }

    [RelayCommand]
    private void MovePlayerDown(GroupMemberDraft? player) {
        if (!CanEdit || player is null)
            return;

        int index = Players.IndexOf(player);
        if (index < 0 || index >= Players.Count - 1)
            return;

        Players.Move(index, index + 1);
        GroupMemberDraft.RefreshPositionsAndMoveAvailability(Players);
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

        if (ActiveSubdialog != EditGroupSubdialog.None) {
            CloseGameSelection();
            return;
        }

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

public enum EditGroupSubdialog {
    None,
    SelectGame
}

using BoardgameTracker.App.Features.Groups.Colors;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Presentation.PlayerColors;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

/// <summary>
/// Owns game-neutral group editing; registered game rules determine optional color choices.
/// </summary>
public partial class EditGroupViewModel(EditGroupUseCase editGroup, GroupColorSelectionViewModel colorSelection, IAppNavigationSettingsStore context, ILogger<EditGroupViewModel> logger) : ObservableObject
{
    public GroupColorSelectionViewModel ColorSelection { get; } = colorSelection;
    public ObservableCollection<GroupPlayerItemViewModel> Players { get; } = [];

    [ObservableProperty]
    public partial Guid GroupId { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTemporary { get; private set; }

    [ObservableProperty]
    public partial bool MakePermanent { get; set; }

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
        if (IsBusy || groupId == Guid.Empty)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            ColorSelection.Configure(context.Get().SelectedGameKey);
            EditGroupDetails? details =
                await editGroup.GetAsync(groupId, ColorSelection.GameKey, cancellationToken);

            if (details is null)
            {
                ErrorMessage = AppResources.GroupNotFound;
                return;
            }

            GroupId = details.Id;
            GroupName = details.Name;
            IsTemporary = details.IsTemporary;
            MakePermanent = false;
            IsArchived = details.IsArchived;

            Players.Clear();
            foreach (EditGroupPlayer player in details.Players.OrderBy(player => player.DefaultOrder))
            {
                Players.Add(new GroupPlayerItemViewModel(
                    player.PlayerId,
                    player.Name,
                    player.IsGuest,
                    player.DefaultColor));
            }

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
    private void MovePlayerUp(GroupPlayerItemViewModel? player)
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
    private void MovePlayerDown(GroupPlayerItemViewModel? player)
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

    [RelayCommand]
    private void OpenColorPicker(GroupPlayerItemViewModel? player)
    {
        if (!IsBusy && player is not null) ColorSelection.Open(player, [.. Players]);
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

            Dictionary<Guid, PlayerColor?> defaultColors = Players
                .ToDictionary(player => player.PlayerId!.Value, player => player.DefaultColor);

            await editGroup.UpdateGroupAsync(new EditGroupCommand(
                GroupId,
                GroupName,
                MakePermanent,
                IsArchived,
                orderedPlayerIds,
                ColorSelection.GameKey is null ? null : new GroupColorDefaults(ColorSelection.GameKey, defaultColors)));

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

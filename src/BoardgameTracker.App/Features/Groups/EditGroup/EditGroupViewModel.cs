using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Features.Games.ArkNova;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups.EditGroup;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

/// <summary>
/// Owns group-edit screen state and delegates loading/saving to the edit-group Application use case.
/// </summary>
public partial class EditGroupViewModel(EditGroupUseCase editGroup, ILogger<EditGroupViewModel> logger) : ObservableObject
{
    public ObservableCollection<EditGroupMemberItemViewModel> Members { get; } = [];

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

            EditGroupDetails? details =
                await editGroup.GetAsync(groupId, cancellationToken);

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

            Members.Clear();
            foreach (EditGroupMember member in details.Members.OrderBy(member => member.DefaultOrder))
            {
                Members.Add(new EditGroupMemberItemViewModel(
                    member.PersonId,
                    member.Name,
                    member.IsGuest,
                    member.ArkNovaDefaultColor));
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
    private void MoveMemberUp(EditGroupMemberItemViewModel? member)
    {
        if (member is null)
        {
            return;
        }

        int index = Members.IndexOf(member);
        if (index > 0)
        {
            Members.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveMemberDown(EditGroupMemberItemViewModel? member)
    {
        if (member is null)
        {
            return;
        }

        int index = Members.IndexOf(member);
        if (index >= 0 && index < Members.Count - 1)
        {
            Members.Move(index, index + 1);
        }
    }

    [RelayCommand]
    private void ToggleColorPicker(EditGroupMemberItemViewModel? member)
    {
        if (member is null)
        {
            return;
        }

        foreach (EditGroupMemberItemViewModel candidate in Members)
        {
            candidate.IsColorPickerOpen =
                ReferenceEquals(candidate, member) && !candidate.IsColorPickerOpen;
        }
    }

    [RelayCommand]
    private void SetBlue(EditGroupMemberItemViewModel? member) =>
        SetDefaultColor(member, ArkNovaPlayerColor.Blue);

    [RelayCommand]
    private void SetYellow(EditGroupMemberItemViewModel? member) =>
        SetDefaultColor(member, ArkNovaPlayerColor.Yellow);

    [RelayCommand]
    private void SetRed(EditGroupMemberItemViewModel? member) =>
        SetDefaultColor(member, ArkNovaPlayerColor.Red);

    [RelayCommand]
    private void SetBlack(EditGroupMemberItemViewModel? member) =>
        SetDefaultColor(member, ArkNovaPlayerColor.Black);

    private void SetDefaultColor(
        EditGroupMemberItemViewModel? member,
        ArkNovaPlayerColor color)
    {
        if (member is null)
        {
            return;
        }

        EditGroupMemberItemViewModel? currentOwner = Members
            .FirstOrDefault(candidate =>
                !ReferenceEquals(candidate, member)
                && candidate.DefaultColor == color);

        ArkNovaPlayerColor? previousColor = member.DefaultColor;
        member.DefaultColor = color;

        if (currentOwner is not null)
        {
            currentOwner.DefaultColor = previousColor;
        }

        member.IsColorPickerOpen = false;
    }

    private bool CanSave() =>
        !IsBusy
        && GroupId != Guid.Empty
        && !string.IsNullOrWhiteSpace(GroupName)
        && Members.Count > 0;

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

            IReadOnlyList<Guid> orderedPersonIds =
                [.. Members.Select(member => member.PersonId)];

            Dictionary<Guid, ArkNovaPlayerColor?> defaultColors = Members
                .ToDictionary(member => member.PersonId, member => member.DefaultColor);

            await editGroup.ExecuteAsync(new EditGroupCommand(
                GroupId,
                GroupName,
                MakePermanent,
                IsArchived,
                orderedPersonIds,
                defaultColors));

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


/// <summary>
/// Presentation model for one fixed group member while editing order and Ark Nova default color.
/// </summary>
public partial class EditGroupMemberItemViewModel(
    Guid personId,
    string name,
    bool isGuest,
    ArkNovaPlayerColor? defaultColor) : ObservableObject
{
    public Guid PersonId { get; } = personId;
    public string Name { get; } = name;
    public bool IsGuest { get; } = isGuest;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ColorBrush))]
    public partial ArkNovaPlayerColor? DefaultColor { get; set; } = defaultColor;

    [ObservableProperty]
    public partial bool IsColorPickerOpen { get; set; }

    public Microsoft.Maui.Graphics.Color ColorBrush => DefaultColor is ArkNovaPlayerColor color
        ? ArkNovaPlayerColorPresentation.GetTint(color, darkBlack: true)
        : Microsoft.Maui.Graphics.Color.FromArgb("#E7E7E7");
}

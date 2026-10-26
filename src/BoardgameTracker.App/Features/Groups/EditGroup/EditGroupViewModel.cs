using BoardgameTracker.App.Features.Groups;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups.EditGroup;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

public partial class EditGroupViewModel(EditGroupUseCase editGroup) : ObservableObject
{
    public ObservableCollection<GroupMemberItemViewModel> Members { get; } = [];

    [ObservableProperty]
    public partial Guid GroupId { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTemporary { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

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

            Members.Clear();
            foreach (EditGroupMember member in details.Members.OrderBy(member => member.DefaultOrder))
            {
                Members.Add(new GroupMemberItemViewModel(
                    member.PersonId,
                    member.Name,
                    member.IsGuest));
            }

            SaveCommand.NotifyCanExecuteChanged();
        }
        catch (Exception)
        {
            ErrorMessage = AppResources.LoadGroupFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void MoveMemberUp(GroupMemberItemViewModel? member)
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
    private void MoveMemberDown(GroupMemberItemViewModel? member)
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

    private bool CanSave() =>
        !IsBusy
        && GroupId != Guid.Empty
        && !string.IsNullOrWhiteSpace(GroupName)
        && Members.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Guid[] orderedPersonIds = Members
                .Select(member => member.PersonId
                    ?? throw new InvalidOperationException(
                        "An existing group member must have a person id."))
                .ToArray();

            await editGroup.ExecuteAsync(new EditGroupCommand(
                GroupId,
                GroupName,
                IsTemporary,
                orderedPersonIds));

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            ErrorMessage = AppResources.SaveGroupFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardgameTracker.App.Features.Groups;

public partial class CreateGroupViewModel(GroupStore groupStore) : ObservableObject
{
    public ObservableCollection<AvailablePersonItemViewModel> AvailablePersons { get; } = [];

    public ObservableCollection<GroupMemberItemViewModel> Members { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTemporary { get; set; }

    [ObservableProperty]
    public partial string NewPersonName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewPersonIsGuest { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

    public event EventHandler? Saved;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<PersonSummary> persons =
                await groupStore.GetActivePersonsAsync(cancellationToken);

            AvailablePersons.Clear();

            foreach (PersonSummary person in persons)
            {
                if (Members.All(member => member.PersonId != person.Id))
                {
                    AvailablePersons.Add(new AvailablePersonItemViewModel(
                        person.Id,
                        person.Name,
                        person.IsGuest));
                }
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = AppResources.LoadPersonsFailed;
        }
    }

    [RelayCommand]
    private void AddExistingPerson(AvailablePersonItemViewModel? person)
    {
        if (person is null || Members.Any(member => member.PersonId == person.Id))
        {
            return;
        }

        Members.Add(new GroupMemberItemViewModel(
            person.Id,
            person.Name,
            person.IsGuest));
        AvailablePersons.Remove(person);
        MembersChanged();
    }

    [RelayCommand]
    private void AddNewPerson()
    {
        string name = NewPersonName.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = AppResources.EnterPersonName;
            return;
        }

        if (Members.Any(member =>
                string.Equals(member.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            ErrorMessage = AppResources.PersonAlreadyInGroup;
            return;
        }

        Members.Add(new GroupMemberItemViewModel(
            null,
            name,
            NewPersonIsGuest));

        NewPersonName = string.Empty;
        NewPersonIsGuest = false;
        ErrorMessage = null;
        MembersChanged();
    }

    [RelayCommand]
    private void RemoveMember(GroupMemberItemViewModel? member)
    {
        if (member is null || !Members.Remove(member))
        {
            return;
        }

        if (member.PersonId is Guid personId)
        {
            AvailablePersons.Add(new AvailablePersonItemViewModel(
                personId,
                member.Name,
                member.IsGuest));
        }

        MembersChanged();
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
        && !string.IsNullOrWhiteSpace(GroupName)
        && Members.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            CreateGroupMember[] members = Members
                .Select(member => new CreateGroupMember(
                    member.PersonId,
                    member.Name,
                    member.IsGuest))
                .ToArray();

            await groupStore.CreateGroupAsync(
                new CreateGroupRequest(
                    GroupName,
                    IsTemporary,
                    members));

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

    private void MembersChanged()
    {
        SaveCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(Members));
    }
}

public sealed record AvailablePersonItemViewModel(
    Guid Id,
    string Name,
    bool IsGuest);

public sealed record GroupMemberItemViewModel(
    Guid? PersonId,
    string Name,
    bool IsGuest);

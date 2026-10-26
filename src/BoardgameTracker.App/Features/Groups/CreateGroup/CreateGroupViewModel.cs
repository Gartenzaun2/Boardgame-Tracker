using BoardgameTracker.App.Features.Groups;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups.CreateGroup;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

public partial class CreateGroupViewModel(CreateGroupUseCase createGroup) : ObservableObject
{
    public ObservableCollection<AvailablePersonItemViewModel> AvailablePersons { get; } = [];
    public ObservableCollection<GroupMemberItemViewModel> Members { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTemporary { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedPersonCommand))]
    public partial AvailablePersonItemViewModel? SelectedExistingPerson { get; set; }

    [ObservableProperty]
    public partial string NewPersonName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewPersonIsGuest { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedPersonCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

    [ObservableProperty]
    public partial bool HasNoMembers { get; private set; } = true;

    [ObservableProperty]
    public partial bool HasNoAvailablePersons { get; private set; } = true;

    public event EventHandler? Saved;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<SelectablePerson> people =
                await createGroup.GetSelectablePeopleAsync(cancellationToken);

            AvailablePersons.Clear();

            foreach (SelectablePerson person in people)
            {
                if (Members.All(member => member.PersonId != person.Id))
                {
                    AvailablePersons.Add(new AvailablePersonItemViewModel(
                        person.Id,
                        person.Name,
                        person.IsGuest));
                }
            }

            SelectedExistingPerson = null;
            UpdateCollectionStates();
        }
        catch (Exception)
        {
            ErrorMessage = AppResources.LoadPersonsFailed;
        }
    }

    private bool CanAddSelectedPerson() =>
        !IsBusy && SelectedExistingPerson is not null;

    [RelayCommand(CanExecute = nameof(CanAddSelectedPerson))]
    private void AddSelectedPerson()
    {
        AvailablePersonItemViewModel? person = SelectedExistingPerson;

        if (person is null || Members.Any(member => member.PersonId == person.Id))
        {
            return;
        }

        Members.Add(new GroupMemberItemViewModel(person.Id, person.Name, person.IsGuest));
        AvailablePersons.Remove(person);
        SelectedExistingPerson = null;
        ErrorMessage = null;
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

        Members.Add(new GroupMemberItemViewModel(null, name, NewPersonIsGuest));
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

            SortAvailablePersons();
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

            CreateGroupMemberInput[] members = Members
                .Select(member => new CreateGroupMemberInput(
                    member.PersonId,
                    member.Name,
                    member.IsGuest))
                .ToArray();

            await createGroup.ExecuteAsync(
                new CreateGroupCommand(GroupName, IsTemporary, members));

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
        UpdateCollectionStates();
    }

    private void UpdateCollectionStates()
    {
        HasNoMembers = Members.Count == 0;
        HasNoAvailablePersons = AvailablePersons.Count == 0;
    }

    private void SortAvailablePersons()
    {
        AvailablePersonItemViewModel[] ordered = AvailablePersons
            .OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        AvailablePersons.Clear();
        foreach (AvailablePersonItemViewModel person in ordered)
        {
            AvailablePersons.Add(person);
        }
    }
}

public sealed record AvailablePersonItemViewModel(Guid Id, string Name, bool IsGuest);

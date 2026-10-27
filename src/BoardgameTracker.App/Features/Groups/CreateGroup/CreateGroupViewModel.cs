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
    public ObservableCollection<SelectablePerson> AvailablePersons { get; } = [];
    public ObservableCollection<CreateGroupMemberInput> Members { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTemporary { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedPersonCommand))]
    public partial SelectablePerson? SelectedExistingPerson { get; set; }

    [ObservableProperty]
    public partial string NewPersonName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewPersonIsGuest { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSelectedPersonCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    public partial bool HasNoMembers { get; private set; } = true;

    [ObservableProperty]
    public partial bool HasNoAvailablePersons { get; private set; } = true;

    public event EventHandler? Saved;

    public bool HasAvailablePersons => !HasNoAvailablePersons;

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
            IReadOnlyList<SelectablePerson> people =
                await createGroup.GetSelectablePeopleAsync(cancellationToken);

            AvailablePersons.Clear();

            foreach (SelectablePerson person in people)
            {
                if (Members.All(member => member.ExistingPersonId != person.Id))
                {
                    AvailablePersons.Add(person);
                }
            }

            SelectedExistingPerson = null;
            UpdateCollectionStates();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load selectable people for group creation.");
            ErrorMessage = UserErrorMessage.Create(AppResources.LoadPersonsFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAddSelectedPerson() =>
        !IsBusy && SelectedExistingPerson is not null;

    [RelayCommand(CanExecute = nameof(CanAddSelectedPerson))]
    private void AddSelectedPerson()
    {
        SelectablePerson? person = SelectedExistingPerson;

        if (person is null || Members.Any(member => member.ExistingPersonId == person.Id))
        {
            return;
        }

        Members.Add(new CreateGroupMemberInput(person.Id, person.Name, person.IsGuest));
        AvailablePersons.Remove(person);
        SelectedExistingPerson = null;
        ErrorMessage = null;
        MembersChanged();
    }

    [RelayCommand]
    private void AddNewPerson()
    {
        if (IsBusy)
        {
            return;
        }

        string name = NewPersonName.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = AppResources.EnterPersonName;
            return;
        }

        Members.Add(new CreateGroupMemberInput(null, name, NewPersonIsGuest));
        NewPersonName = string.Empty;
        NewPersonIsGuest = false;
        ErrorMessage = null;
        MembersChanged();
    }

    [RelayCommand]
    private void RemoveMember(CreateGroupMemberInput? member)
    {
        if (member is null || !Members.Remove(member))
        {
            return;
        }

        if (member.ExistingPersonId is Guid personId)
        {
            AvailablePersons.Add(new SelectablePerson(
                personId,
                member.Name,
                member.IsGuest));

            SortAvailablePersons();
        }

        MembersChanged();
    }

    [RelayCommand]
    private void MoveMemberUp(CreateGroupMemberInput? member)
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
    private void MoveMemberDown(CreateGroupMemberInput? member)
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
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            await createGroup.ExecuteAsync(
                new CreateGroupCommand(GroupName, IsTemporary, [.. Members]));

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

    private void MembersChanged()
    {
        SaveCommand.NotifyCanExecuteChanged();
        UpdateCollectionStates();
    }

    private void UpdateCollectionStates()
    {
        HasNoMembers = Members.Count == 0;
        HasNoAvailablePersons = AvailablePersons.Count == 0;
        OnPropertyChanged(nameof(HasAvailablePersons));
    }

    private void SortAvailablePersons()
    {
        IReadOnlyList<SelectablePerson> ordered =
            [.. AvailablePersons.OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)];

        AvailablePersons.Clear();
        foreach (SelectablePerson person in ordered)
        {
            AvailablePersons.Add(person);
        }
    }
}


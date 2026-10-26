using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.Groups;

public partial class GroupListViewModel(GroupStore groupStore) : ObservableObject
{
    public ObservableCollection<GroupListItemViewModel> Groups { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

    public bool HasGroups => Groups.Count > 0;

    public bool HasNoGroups => !HasGroups && !IsBusy;

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

            IReadOnlyList<GroupSummary> groups =
                await groupStore.GetGroupsAsync(cancellationToken);

            Groups.Clear();

            foreach (GroupSummary group in groups)
            {
                Groups.Add(new GroupListItemViewModel(
                    group.Id,
                    group.Name,
                    group.MemberCount,
                    string.Join(" · ", group.MemberNames),
                    group.LastPlayedAt));
            }

            OnPropertyChanged(nameof(HasGroups));
            OnPropertyChanged(nameof(HasNoGroups));
        }
        catch (Exception)
        {
            ErrorMessage = AppResources.LoadGroupsFailed;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasGroups));
            OnPropertyChanged(nameof(HasNoGroups));
        }
    }
}

public sealed record GroupListItemViewModel(
    Guid Id,
    string Name,
    int MemberCount,
    string MemberNames,
    DateTimeOffset? LastPlayedAt);

using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups.ListGroups;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

public partial class GroupListViewModel(IListGroupsQuery listGroupsQuery) : ObservableObject
{
    public ObservableCollection<GroupListItemViewModel> Groups { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

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

            IReadOnlyList<GroupListItem> groups =
                await listGroupsQuery.ExecuteAsync(cancellationToken);

            Groups.Clear();

            foreach (GroupListItem group in groups)
            {
                Groups.Add(new GroupListItemViewModel(
                    group.Id,
                    group.Name,
                    group.MemberCount,
                    string.Join(" · ", group.MemberNames),
                    group.LastPlayedAt));
            }
        }
        catch (Exception)
        {
            ErrorMessage = AppResources.LoadGroupsFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record GroupListItemViewModel(
    Guid Id,
    string Name,
    int MemberCount,
    string MemberNames,
    DateTimeOffset? LastPlayedAt);

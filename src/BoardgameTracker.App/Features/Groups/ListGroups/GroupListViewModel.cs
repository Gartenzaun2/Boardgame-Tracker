using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Diagnostics;
using Microsoft.Extensions.Logging;
using BoardgameTracker.Application.Groups.ListGroups;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

public partial class GroupListViewModel(IListGroupsQuery listGroupsQuery, ILogger<GroupListViewModel> logger) : ObservableObject
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
                string lastPlayedText = group.LastPlayedAt is DateTimeOffset lastPlayed
                    ? string.Format(
                        CultureInfo.CurrentCulture,
                        AppResources.LastPlayedFormat,
                        lastPlayed.ToLocalTime().ToString("d", CultureInfo.CurrentCulture))
                    : AppResources.NeverPlayed;

                Groups.Add(new GroupListItemViewModel(
                    group.Id,
                    group.Name,
                    group.MemberCount,
                    string.Join(" · ", group.MemberNames),
                    lastPlayedText));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load groups.");
            ErrorMessage = UserErrorMessage.Create(AppResources.LoadGroupsFailed, exception);
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
    string LastPlayedText);

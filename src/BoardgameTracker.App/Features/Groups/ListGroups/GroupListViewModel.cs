using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Core.GameSessions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

/// <summary>
/// Owns group-overview screen state, filters and navigation re-entry protection.
/// </summary>
public partial class GroupListViewModel(
    IListGroupsQuery listGroupsQuery,
    ILogger<GroupListViewModel> logger) : ObservableObject
{
    private bool _reloadRequested;

    public ObservableCollection<GroupListItemViewModel> Groups { get; } = [];

    [ObservableProperty]
    public partial bool IncludeTemporary { get; set; }

    [ObservableProperty]
    public partial bool IncludeArchived { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial bool IsNavigating { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnIncludeTemporaryChanged(bool value) => _ = LoadAsync();
    partial void OnIncludeArchivedChanged(bool value) => _ = LoadAsync();

    public bool TryBeginNavigation()
    {
        if (IsNavigating)
        {
            return false;
        }

        IsNavigating = true;
        return true;
    }

    public void EndNavigation() => IsNavigating = false;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            _reloadRequested = true;
            return;
        }

        try
        {
            IsBusy = true;

            do
            {
                _reloadRequested = false;
                ErrorMessage = null;

                IReadOnlyList<GroupListItem> groups =
                    await listGroupsQuery.ExecuteAsync(
                        IncludeTemporary,
                        IncludeArchived,
                        cancellationToken);

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
                        group.IsTemporary,
                        group.IsArchived,
                        group.MemberCount,
                        string.Join(" · ", group.MemberNames),
                        lastPlayedText,
                        AppResources.ArkNova,
                        group.OpenSessionId,
                        group.OpenSessionStatus));
                }
            }
            while (_reloadRequested && !cancellationToken.IsCancellationRequested);
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

/// <summary>
/// Presentation model for one group card in the group overview.
/// </summary>
public sealed record GroupListItemViewModel(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    int MemberCount,
    string MemberNames,
    string LastPlayedText,
    string GameName,
    Guid? OpenSessionId,
    GameSessionStatus? OpenSessionStatus)
{
    public bool HasOpenSession => OpenSessionId is not null;
}

using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.App.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

/// <summary>
/// Owns the game-context group overview, filters and navigation re-entry protection.
/// </summary>
public partial class GroupListViewModel(
    ListGroupsQuery listGroupsQuery,
    IAppNavigationSettingsStore navigationSettingsStore,
    ILogger<GroupListViewModel> logger,
    IEnumerable<IGameSessionNavigationHandler> navigationHandlers) : ObservableObject
{
    private bool _reloadRequested;

    public ObservableCollection<GroupListItemViewModel> Groups { get; } = [];

    [ObservableProperty]
    public partial string SelectedGameName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedGameKey { get; private set; } = string.Empty;

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

                AppNavigationSettings navigation = navigationSettingsStore.Get();
                string? gameKey = navigation.SelectedGameKey ?? navigation.DefaultGameKey;
                if (string.IsNullOrWhiteSpace(gameKey))
                {
                    SelectedGameKey = string.Empty;
                    SelectedGameName = string.Empty;
                    Groups.Clear();
                    return;
                }

                if (navigation.SelectedGameKey != gameKey)
                {
                    navigationSettingsStore.Save(navigation with { SelectedGameKey = gameKey });
                }

                SelectedGameKey = gameKey;
                SelectedGameName = GetGameName(gameKey);

                IReadOnlyList<GroupListItem> groups =
                    await listGroupsQuery.ListGroupsAsync(
                        gameKey,
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
                        group.PlayerCount,
                        string.Join(" · ", group.PlayerNames),
                        lastPlayedText,
                        group.OpenGameSessionId,
                        group.OpenGameSessionStatus));
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

    private string GetGameName(string gameKey) => navigationHandlers.FirstOrDefault(handler => handler.GameKey == gameKey)?.DisplayName ?? gameKey;
}

/// <summary>
/// Presentation model for one group card in the selected-game overview.
/// </summary>
public sealed record GroupListItemViewModel(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    int PlayerCount,
    string PlayerNames,
    string LastPlayedText,
    Guid? OpenGameSessionId,
    GameSessionStatus? OpenGameSessionStatus)
{
    public bool HasOpenGameSession => OpenGameSessionId is not null;
}

using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Core.GameSessions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

public partial class GroupListViewModel(ListGroupsQuery query, AddGroupGameUseCase addAssignment, PlayOverviewQuery games,
    ILogger<GroupListViewModel> logger, IEnumerable<IGameSessionNavigationHandler> handlers) : ObservableObject
{
    public ObservableCollection<GroupListItemViewModel> Groups { get; } = [];
    public ObservableCollection<AvailablePlayGroup> AvailableGroups { get; } = [];
    [ObservableProperty] public partial string SelectedGameName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string BadgeText { get; private set; } = string.Empty;
    public string SelectedGameKey { get; private set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    public partial bool IsNavigating { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    public partial bool IsChoosingExisting { get; set; }
    [ObservableProperty] public partial bool CanCreate { get; private set; }
    [ObservableProperty] public partial bool CanChoose { get; private set; }
    [ObservableProperty] public partial string EmptyMessage { get; private set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsWorking => IsBusy || IsNavigating;
    public bool CanInteract => !IsBusy && !IsNavigating && !IsChoosingExisting;
    public bool IsEmpty => Groups.Count == 0;
    public void SetGameContext(string key) => SelectedGameKey = key;
    public bool TryBeginNavigation()
    {
        if (IsBusy || IsNavigating)
            return false;
        IsNavigating = true;
        return true;
    }
    public void EndNavigation() => IsNavigating = false;
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(SelectedGameKey))
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            GameDetails game = await games.GetGameAsync(SelectedGameKey, cancellationToken);
            SelectedGameName = game.Name;
            BadgeText = game.BadgeText;
            GroupPresence presence = await query.GetPresenceAsync(cancellationToken);
            CanCreate = presence.HasPlayers;
            EmptyMessage = !presence.HasPlayers ? AppResources.FirstPlayerHint : !presence.HasGroups ? AppResources.FirstGroupHint : AppResources.NoGroupsForGame;
            IReadOnlyList<GroupListItem> rows = await query.ListGroupsAsync(SelectedGameKey, cancellationToken);
            Groups.Clear();
            foreach (GroupListItem row in rows)
            {
                string lastPlayed = row.LastPlayedAt is DateTimeOffset date ? string.Format(CultureInfo.CurrentCulture,
                    AppResources.LastPlayedFormat, date.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)) : string.Empty;
                Groups.Add(new(row.Id, row.Name, string.Join(" · ", row.PlayerNames), lastPlayed, row.OpenGameSessionId, row.OpenGameSessionStatus));
            }
            AvailableGroups.Clear();
            foreach (ManagedGroup group in await query.GetAvailableGroupsAsync(SelectedGameKey, cancellationToken))
                AvailableGroups.Add(new(group.Id, group.Name, string.Join(" · ", group.PlayerNames)));
            CanChoose = presence.HasPlayers && AvailableGroups.Count > 0;
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load groups for game.");
            ErrorMessage = AppResources.LoadGroupsFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
    public async Task OpenGroupAsync(Guid groupId)
    {
        if (!TryBeginNavigation())
            return;
        ErrorMessage = null;
        try
        {
            GroupListItem group = (await query.ListGroupsAsync(SelectedGameKey)).Single(group => group.Id == groupId);
            await handlers.Single(handler => handler.CanHandle(SelectedGameKey)).OpenGroupAsync(SelectedGameKey, group.Id, group.OpenGameSessionId, group.OpenGameSessionStatus);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not open game group.");
            ErrorMessage = UserErrorMessage.Create(AppResources.OpenGroupFailed, exception);
        }
        finally
        {
            EndNavigation();
        }
    }
    public async Task SelectExistingAsync(AvailablePlayGroup group)
    {
        if (!AvailableGroups.Contains(group) || !TryBeginNavigation())
            return;
        ErrorMessage = null;
        try
        {
            Guid gameId = (await games.ListAsync()).Single(game => game.Key == SelectedGameKey).Id;
            await addAssignment.AddGroupGameAsync(group.Id, gameId);
            IsChoosingExisting = false;
            await handlers.Single(handler => handler.CanHandle(SelectedGameKey)).OpenGroupAsync(SelectedGameKey, group.Id, null, null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not assign and open game group.");
            ErrorMessage = UserErrorMessage.Create(AppResources.OpenGroupFailed, exception);
        }
        finally
        {
            EndNavigation();
        }
    }
}
public sealed record AvailablePlayGroup(Guid Id, string Name, string Members);
public sealed record GroupListItemViewModel(Guid Id, string Name, string PlayerNames, string LastPlayedText,
    Guid? OpenGameSessionId, GameSessionStatus? OpenGameSessionStatus)
{
    public bool HasOpenGameSession => OpenGameSessionId.HasValue;
    public bool HasLastPlayed => !string.IsNullOrWhiteSpace(LastPlayedText);
}

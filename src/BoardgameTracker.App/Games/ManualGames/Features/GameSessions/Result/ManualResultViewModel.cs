using System.Collections.ObjectModel;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.GameSessions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Result;

public sealed class ManualResultPlayerViewModel(Guid id, string name) : ManualParticipantViewModel(id, name)
{
    public int? Points { get; init; }
    public int? Placement { get; init; }
    public bool FullPlacements { get; init; }
    public bool HasPoints => Points.HasValue;
    public string PointsText => Points is int value ? string.Format(AppResources.PointsFormat, value) : string.Empty;
    public string PlacementText => FullPlacements && Placement.HasValue ? Placement + "." : Placement == 1 ? "🏆" : string.Empty;
    public bool HasPlacement => !string.IsNullOrEmpty(PlacementText);
}
public partial class ManualResultViewModel(ManualScoringQuery query, ILogger<ManualResultViewModel> logger) : ObservableObject
{
    public ObservableCollection<ManualResultPlayerViewModel> Players { get; } = [];
    [ObservableProperty] public partial string GameName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string BadgeText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Metadata { get; private set; } = string.Empty;
    [ObservableProperty] public partial string StartingOrderText { get; private set; } = string.Empty;
    public bool HasStartingOrder => !string.IsNullOrEmpty(StartingOrderText);
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty] public partial string? ErrorMessage { get; private set; }
    public async Task LoadAsync(Guid id, CancellationToken token = default)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            ManualScoringData data = await query.GetAsync(id, token) ?? throw new InvalidOperationException("Manual result not found.");
            if (data.Session.Status != GameSessionStatus.Completed)
                throw new InvalidOperationException("The session is not completed.");
            GameName = data.Session.GameName;
            BadgeText = data.Session.BadgeText;
            GroupName = data.Session.GroupName;
            TimeSpan duration = data.Session.EndedAt!.Value - data.Session.StartedAt;
            string elapsed = duration.TotalHours >= 1 ? string.Format(AppResources.DurationHoursFormat, (int)duration.TotalHours, duration.Minutes)
                : string.Format(AppResources.DurationMinutesFormat, (int)duration.TotalMinutes);
            Metadata = data.Session.StartedAt.ToLocalTime().ToString("g") + " · " + elapsed;
            StartingOrderText = data.Session.Players.All(player => player.Order.HasValue)
                ? string.Format(AppResources.StartingOrderFormat, string.Join(" → ", data.Session.Players.OrderBy(player => player.Order).Select(player => player.Name))) : string.Empty;
            OnPropertyChanged(nameof(HasStartingOrder));
            bool full = data.Values.All(player => player.Placement.HasValue)
                && (data.Session.ScoringRules.CapturePlacements || data.Values.Any(player => player.Placement > 1));
            Players.Clear();
            foreach (var value in data.Values.OrderBy(player => player.Placement ?? int.MaxValue)
                .ThenBy(player => data.Session.Players.Single(participant => participant.PlayerId == player.PlayerId).Order))
            {
                ManualRunningPlayer player = data.Session.Players.Single(player => player.PlayerId == value.PlayerId);
                Players.Add(new(player.PlayerId, player.Name) { Color = player.Color, Points = value.Points, Placement = value.Placement, FullPlacements = full });
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load manual result.");
            ErrorMessage = AppResources.ScoringLoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

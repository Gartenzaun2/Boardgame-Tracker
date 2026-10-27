using System.Collections.ObjectModel;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Features.Games.ArkNova;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Application.Games.ArkNova.Running;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.GameSessions.Running;

/// <summary>
/// Holds UI state for a persisted running Ark Nova session and invokes lifecycle
/// actions such as ending or discarding the session.
/// </summary>
public partial class ArkNovaRunningViewModel(
    IArkNovaRunningSessionQuery runningSessionQuery,
    RunningGameSessionUseCase runningSessionUseCase,
    ILogger<ArkNovaRunningViewModel> logger) : ObservableObject
{
    public ObservableCollection<ArkNovaRunningPlayerViewModel> Players { get; } = [];

    [ObservableProperty]
    public partial Guid SessionId { get; private set; }

    [ObservableProperty]
    public partial string GroupName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string DurationText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private DateTimeOffset _startedAt;

    public async Task LoadAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            ArkNovaRunningSessionData? data = await runningSessionQuery.ExecuteAsync(
                sessionId,
                cancellationToken);

            if (data is null)
            {
                ErrorMessage = AppResources.RunningSessionNotFound;
                return;
            }

            SessionId = data.SessionId;
            GroupName = data.GroupName;
            _startedAt = data.StartedAt;
            Players.Clear();

            foreach (ArkNovaRunningSessionPlayer player in data.Players)
            {
                Players.Add(new ArkNovaRunningPlayerViewModel(
                    player.Name,
                    player.Order,
                    player.Color,
                    player.ZooPlanNumber,
                    player.ZooPlanNameResourceKey,
                    player.ZooPlanIconSource));
            }

            RefreshDuration(DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load running Ark Nova session {SessionId}.", sessionId);
            ErrorMessage = UserErrorMessage.Create(
                AppResources.LoadRunningSessionFailed,
                exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void RefreshDuration(DateTimeOffset now)
    {
        if (_startedAt == default)
        {
            DurationText = string.Empty;
            return;
        }

        TimeSpan duration = now - _startedAt;

        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        int totalHours = (int)duration.TotalHours;
        DurationText = totalHours > 0
            ? string.Format(AppResources.DurationHoursFormat, totalHours, duration.Minutes)
            : string.Format(AppResources.DurationMinutesFormat, duration.Minutes);
    }

    public async Task<bool> BeginScoringAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || SessionId == Guid.Empty)
        {
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await runningSessionUseCase.BeginScoringAsync(
                SessionId,
                DateTimeOffset.UtcNow,
                cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not end Ark Nova session {SessionId}.", SessionId);
            ErrorMessage = UserErrorMessage.Create(
                AppResources.EndGameFailed,
                exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> DiscardAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || SessionId == Guid.Empty)
        {
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await runningSessionUseCase.DiscardAsync(SessionId, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discard Ark Nova session {SessionId}.", SessionId);
            ErrorMessage = UserErrorMessage.Create(
                AppResources.DiscardGameFailed,
                exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Presentation projection for one player on the running-session screen.
/// </summary>
public sealed class ArkNovaRunningPlayerViewModel(
    string name,
    int position,
    ArkNovaPlayerColor color,
    int zooPlanNumber,
    string zooPlanNameResourceKey,
    string zooPlanIconSource)
{
    public string Name { get; } = name;
    public int Position { get; } = position;
    public int ZooPlanNumber { get; } = zooPlanNumber;
    public string ZooPlanText { get; } = AppResources.GetByKey(zooPlanNameResourceKey);
    public string ZooPlanIconSource { get; } = zooPlanIconSource;

    public string ColorName => ArkNovaPlayerColorPresentation.GetName(color);

    public Microsoft.Maui.Graphics.Color ColorBrush =>
        ArkNovaPlayerColorPresentation.GetTint(color);
}

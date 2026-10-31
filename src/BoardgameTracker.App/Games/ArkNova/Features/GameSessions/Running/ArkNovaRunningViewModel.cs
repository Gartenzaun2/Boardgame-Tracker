using BoardgameTracker.App.Presentation.PlayerColors;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Games.ArkNova;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;

/// <summary>
/// Holds UI state for a persisted running Ark Nova GameSession and invokes lifecycle
/// actions such as ending or discarding the gameSession.
/// </summary>
public partial class ArkNovaRunningViewModel(
    ArkNovaRunningGameSessionQuery runningGameSessionQuery,
    RunningGameSessionUseCase runningGameSessionUseCase,
    ILogger<ArkNovaRunningViewModel> logger) : ObservableObject
{
    public ObservableCollection<ArkNovaRunningPlayerViewModel> Players { get; } = [];

    [ObservableProperty]
    public partial Guid GameSessionId { get; private set; }

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
        Guid gameSessionId,
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

            ArkNovaRunningGameSessionData? data = await runningGameSessionQuery.GetRunningGameSessionAsync(
                gameSessionId,
                cancellationToken);

            if (data is null)
            {
                ErrorMessage = AppResources.RunningGameSessionNotFound;
                return;
            }

            GameSessionId = data.GameSessionId;
            GroupName = data.GroupName;
            _startedAt = data.StartedAt;
            Players.Clear();

            foreach (ArkNovaRunningGameSessionPlayer player in data.Players)
            {
                Players.Add(new ArkNovaRunningPlayerViewModel(
                    player.Name,
                    player.Order,
                    player.Color,
                    player.ZooPlanNumber, player.ZooPlanName));
            }

            RefreshDuration(DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load running Ark Nova GameSession {GameSessionId}.", gameSessionId);
            ErrorMessage = UserErrorMessage.Create(
                AppResources.LoadRunningGameSessionFailed,
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
        if (IsBusy || GameSessionId == Guid.Empty)
        {
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await runningGameSessionUseCase.BeginScoringAsync(
                GameSessionId,
                DateTimeOffset.UtcNow,
                cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not end Ark Nova GameSession {GameSessionId}.", GameSessionId);
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
        if (IsBusy || GameSessionId == Guid.Empty)
        {
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await runningGameSessionUseCase.DiscardAsync(GameSessionId, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discard Ark Nova GameSession {GameSessionId}.", GameSessionId);
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
/// Presentation projection for one player on the running GameSession screen.
/// </summary>
public sealed class ArkNovaRunningPlayerViewModel
{
    private readonly PlayerColor _color;

    public ArkNovaRunningPlayerViewModel(
        string name,
        int position,
        PlayerColor color,
        int zooPlanNumber, string zooPlanName)
    {
        Name = name;
        Position = position;
        _color = color;
        ZooPlanNumber = zooPlanNumber;
        ZooPlanText = zooPlanName;
        ZooPlanIconSource = ArkNovaZooPlanPresentation.GetIconSource(zooPlanNumber);
    }

    public string Name { get; }
    public int Position { get; }
    public int ZooPlanNumber { get; }
    public string ZooPlanText { get; }
    public string ZooPlanIconSource { get; }

    public Microsoft.Maui.Graphics.Color PlayerTextColor => PlayerColorPresentation.GetText(_color);

    public string ColorName => PlayerColorPresentation.GetName(_color);

    public Microsoft.Maui.Graphics.Color ColorBrush =>
        PlayerColorPresentation.GetBackground(_color);
}

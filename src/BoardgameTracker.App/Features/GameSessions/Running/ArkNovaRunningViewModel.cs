using System.Collections.ObjectModel;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Application.Games.ArkNova.Running;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.GameSessions.Running;

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
                ErrorMessage = "Die laufende Partie wurde nicht gefunden.";
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
                    player.ZooPlanIconSource));
            }

            RefreshDuration(DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load running Ark Nova session {SessionId}.", sessionId);
            ErrorMessage = UserErrorMessage.Create(
                "Die laufende Partie konnte nicht geladen werden.",
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
            ? $"Dauer: {totalHours}:{duration.Minutes:00} h"
            : $"Dauer: {duration.Minutes} min";
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
                "Die Partie konnte nicht beendet werden.",
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
                "Die Partie konnte nicht verworfen werden.",
                exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed class ArkNovaRunningPlayerViewModel(
    string name,
    int position,
    ArkNovaPlayerColor color,
    int zooPlanNumber,
    string zooPlanIconSource)
{
    public string Name { get; } = name;
    public int Position { get; } = position;
    public int ZooPlanNumber { get; } = zooPlanNumber;
    public string ZooPlanIconSource { get; } = zooPlanIconSource;

    public string ColorName => color switch
    {
        ArkNovaPlayerColor.Blue => "Blau",
        ArkNovaPlayerColor.Yellow => "Gelb",
        ArkNovaPlayerColor.Red => "Rot",
        ArkNovaPlayerColor.Black => "Schwarz",
        _ => color.ToString()
    };

    public Microsoft.Maui.Graphics.Color ColorBrush => color switch
    {
        ArkNovaPlayerColor.Blue => Microsoft.Maui.Graphics.Color.FromArgb("#8BC4E8"),
        ArkNovaPlayerColor.Yellow => Microsoft.Maui.Graphics.Color.FromArgb("#F2D65C"),
        ArkNovaPlayerColor.Red => Microsoft.Maui.Graphics.Color.FromArgb("#E98A84"),
        ArkNovaPlayerColor.Black => Microsoft.Maui.Graphics.Color.FromArgb("#808080"),
        _ => Microsoft.Maui.Graphics.Colors.Transparent
    };
}

using System.Collections.ObjectModel;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.GameSessions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Running;

public partial class ManualRunningViewModel(ManualRunningGameSessionQuery query, ManualRunningGameSessionUseCase useCase,
    ILogger<ManualRunningViewModel> logger) : ObservableObject
{
    private ManualRunningData? _data;
    public Guid SessionId => _data?.SessionId ?? Guid.Empty;
    public ObservableCollection<ManualParticipantViewModel> Players { get; } = [];
    [ObservableProperty] public partial string GameName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string BadgeText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string DurationText { get; private set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(CanEnd))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(CanEnd))]
    public partial bool IsConfirming { get; set; }
    public bool CanInteract => !IsBusy && !IsConfirming;
    public bool CanEnd => CanInteract && _data is not null;
    public bool ShowEnd => _data is not null;
    public string EndText => IsScoring ? AppResources.ResumeScoring : AppResources.EndGame;
    public bool IsScoring => _data?.Status == GameSessionStatus.Scoring;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public async Task LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try { Apply(await query.GetAsync(id, cancellationToken) ?? throw new InvalidOperationException("Manual session not found.")); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load manual session.");
            ErrorMessage = AppResources.ManualSessionLoadFailed;
        }
        finally { IsBusy = false; }
    }
    private void Apply(ManualRunningData data)
    {
        if (data.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            throw new InvalidOperationException("The session is not open.");
        _data = data;
        GameName = data.GameName;
        BadgeText = data.BadgeText;
        GroupName = data.GroupName;
        Players.Clear();
        foreach (ManualRunningPlayer player in data.Players)
            Players.Add(new(player.PlayerId, player.Name) { Position = player.Order, Color = player.Color });
        OnPropertyChanged(nameof(ShowEnd));
        OnPropertyChanged(nameof(IsScoring));
        OnPropertyChanged(nameof(EndText));
        OnPropertyChanged(nameof(CanEnd));
        RefreshDuration(DateTimeOffset.UtcNow);
    }
    public void RefreshDuration(DateTimeOffset now)
    {
        if (_data is null)
            return;
        TimeSpan duration = (_data.EndedAt ?? now) - _data.StartedAt;
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;
        int hours = (int)duration.TotalHours;
        DurationText = hours > 0 ? string.Format(AppResources.DurationHoursFormat, hours, duration.Minutes)
            : string.Format(AppResources.DurationMinutesFormat, (int)duration.TotalMinutes);
    }
    public async Task<bool> BeginScoringAsync(CancellationToken cancellationToken = default)
    {
        if (!CanEnd || _data is null)
            return false;
        if (IsScoring)
            return true;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await useCase.BeginScoringAsync(SessionId, DateTimeOffset.UtcNow, cancellationToken);
            Apply(await query.GetAsync(SessionId, cancellationToken) ?? throw new InvalidOperationException("Manual session not found."));
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not end manual session.");
            ErrorMessage = AppResources.EndGameFailed;
            return false;
        }
        finally { IsBusy = false; }
    }
    public async Task<bool> DiscardAsync(bool confirmed, CancellationToken cancellationToken = default)
    {
        if (!confirmed || !CanInteract || _data is null)
            return false;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await useCase.DiscardAsync(SessionId, cancellationToken);
            _data = null;
            Players.Clear();
            OnPropertyChanged(nameof(ShowEnd));
            OnPropertyChanged(nameof(CanEnd));
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discard manual session.");
            ErrorMessage = AppResources.DiscardGameFailed;
            return false;
        }
        finally { IsBusy = false; }
    }
}

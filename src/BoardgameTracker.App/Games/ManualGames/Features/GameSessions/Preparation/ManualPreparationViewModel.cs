using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Preparation;

public partial class ManualPreparationViewModel(ManualPreparationQuery query, StartManualGameSessionUseCase start,
    ILogger<ManualPreparationViewModel> logger) : ObservableObject
{
    private ManualPreparationData? _data;
    private ManualParticipantViewModel? _colorTarget;
    private Guid? _startedSessionId;
    public ObservableCollection<ManualParticipantViewModel> Players { get; } = [];
    public ObservableCollection<ManualColorOption> ColorOptions { get; } = [];
    public string GameKey => _data?.GameKey ?? string.Empty;
    [ObservableProperty] public partial string GameName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string BadgeText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string LastSessionText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool CapturePlayerOrder { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    public partial bool IsChoosingColor { get; private set; }
    [ObservableProperty] public partial string ColorPickerTitle { get; private set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanInteract => !IsBusy && !IsChoosingColor;
    public bool CanStart => _data is not null && CanInteract && Players.Count == _data.Players.Count
        && (!_data.CapturePlayerColors || Players.All(player => player.Color is PlayerColor color && Enum.IsDefined(color))
            && Players.Select(player => player.Color).Distinct().Count() == Players.Count);

    public async Task LoadAsync(string gameKey, Guid groupId, CancellationToken cancellationToken = default)
    {
        if (_data is not null || IsBusy)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            ManualPreparationData data = await query.GetAsync(gameKey, groupId, cancellationToken);
            GameName = data.GameName;
            BadgeText = data.BadgeText;
            GroupName = data.GroupName;
            CapturePlayerOrder = data.CapturePlayerOrder;
            LastSessionText = FormatLastSession(data.LastSession);
            ManualPreparationPlayer[] ordered = data.CapturePlayerOrder ? data.Players.OrderBy(player => player.DefaultOrder).ToArray()
                : data.Players.OrderBy(player => player.Name, StringComparer.CurrentCulture).ToArray();
            Dictionary<Guid, PlayerColor> assigned = [];
            HashSet<PlayerColor> used = [];
            if (data.CapturePlayerColors)
            {
                // Reserve all existing valid defaults before filling gaps so later players retain their colors.
                foreach (ManualPreparationPlayer player in ordered)
                    if (player.DefaultColor is PlayerColor color && Enum.IsDefined(color) && used.Add(color))
                        assigned[player.PlayerId] = color;
                foreach (ManualPreparationPlayer player in ordered.Where(player => !assigned.ContainsKey(player.PlayerId)))
                {
                    PlayerColor color = Enum.GetValues<PlayerColor>().First(color => !used.Contains(color));
                    assigned[player.PlayerId] = color;
                    used.Add(color);
                }
            }
            Players.Clear();
            foreach (ManualPreparationPlayer player in ordered)
                Players.Add(new(player.PlayerId, player.Name)
                {
                    Color = data.CapturePlayerColors ? assigned[player.PlayerId] : null,
                    CanChooseColor = data.CapturePlayerColors
                });
            _data = data;
            UpdatePositions();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load manual session preparation.");
            ErrorMessage = AppResources.ManualPreparationInvalid;
        }
        finally { IsBusy = false; }
    }

    public void Move(ManualParticipantViewModel player, int direction)
    {
        if (!CanInteract || !CapturePlayerOrder || !Players.Contains(player))
            return;
        int source = Players.IndexOf(player);
        int target = source + direction;
        if (target >= 0 && target < Players.Count)
            ReplacePlayers(StartingPlayerRotation.MovePlayer(Players.ToArray(), source, target));
    }

    public void Rotate(int startIndex = 1)
    {
        if (CanInteract && CapturePlayerOrder && Players.Count > 1)
            ReplacePlayers(StartingPlayerRotation.RotateToStart(Players.ToArray(), startIndex));
    }
    public void RotateRandom(Random? random = null)
    {
        if (Players.Count > 1)
            Rotate((random ?? Random.Shared).Next(Players.Count));
    }
    private void ReplacePlayers(IReadOnlyList<ManualParticipantViewModel> players)
    {
        Players.Clear();
        foreach (ManualParticipantViewModel player in players)
            Players.Add(player);
        UpdatePositions();
    }
    private void UpdatePositions()
    {
        for (int index = 0; index < Players.Count; index++)
        {
            Players[index].Position = CapturePlayerOrder ? index + 1 : null;
            Players[index].CanMoveUp = CapturePlayerOrder && index > 0;
            Players[index].CanMoveDown = CapturePlayerOrder && index < Players.Count - 1;
        }
        OnPropertyChanged(nameof(CanStart));
    }

    public void OpenColorPicker(ManualParticipantViewModel player)
    {
        if (!CanInteract || !player.CanChooseColor || !Players.Contains(player))
            return;
        _colorTarget = player;
        ColorPickerTitle = string.Format(AppResources.SetColorForGameFormat, GameName) + " · " + player.Name;
        ColorOptions.Clear();
        foreach (PlayerColor color in Enum.GetValues<PlayerColor>())
            ColorOptions.Add(new(color, PlayerColorPresentation.GetName(color), PlayerColorPresentation.GetBackground(color),
                PlayerColorPresentation.GetText(color), color == player.Color));
        IsChoosingColor = true;
    }
    public void SelectColor(PlayerColor color)
    {
        if (_colorTarget is null || !IsChoosingColor || !Enum.IsDefined(color))
            return;
        ManualParticipantViewModel? owner = Players.FirstOrDefault(player => player != _colorTarget && player.Color == color);
        PlayerColor? previous = _colorTarget.Color;
        _colorTarget.Color = color;
        if (owner is not null)
            owner.Color = previous;
        CloseColorPicker();
        OnPropertyChanged(nameof(CanStart));
    }
    public void CloseColorPicker()
    {
        _colorTarget = null;
        IsChoosingColor = false;
    }

    public async Task<Guid?> StartAsync(CancellationToken cancellationToken = default)
    {
        if (!CanStart || _data is null)
            return null;
        if (_startedSessionId is not null)
            return _startedSessionId;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            _startedSessionId = await start.StartAsync(new(GameKey, _data.GroupId, DateTimeOffset.UtcNow,
                Players.Select(player => new ManualStartingPlayer(player.PlayerId, player.Position, player.Color)).ToArray()), cancellationToken);
            return _startedSessionId;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not start manual session.");
            ErrorMessage = AppResources.ManualPreparationInvalid;
            return null;
        }
        finally { IsBusy = false; }
    }

    public static string FormatLastSession(ManualLastSession? session)
    {
        if (session is null)
            return AppResources.NoPreviousGameSession;
        string date = session.StartedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        IReadOnlyList<ManualLastPlayer> players = session.Players;
        if (players.Count > 0 && players.All(player => player.Placement.HasValue))
            return date + " · " + string.Join(" · ", players.OrderBy(player => player.Placement).ThenBy(player => player.Name)
                .Select(player => player.Placement + ". " + player.Name));
        if (players.Any(player => player.Placement == 1))
            return date + " · 🏆 " + string.Join(" · ", players.Where(player => player.Placement == 1).Select(player => player.Name));
        if (players.Count > 0 && players.All(player => player.Points.HasValue))
            return date + " · " + string.Join(" · ", players.Select(player => player.Name + ": " + player.Points));
        return date;
    }
}

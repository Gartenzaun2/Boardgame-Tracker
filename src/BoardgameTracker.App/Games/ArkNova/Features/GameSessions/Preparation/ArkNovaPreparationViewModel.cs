using BoardgameTracker.App.Presentation.PlayerColors;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Games.ArkNova;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;

/// <summary>
/// Holds the non-persisted screen state for preparing an Ark Nova GameSession and
/// delegates persistence to the start-gameSession Application use case.
/// </summary>
public partial class ArkNovaPreparationViewModel(
    IArkNovaPreparationQuery preparationQuery,
    StartArkNovaGameSessionUseCase startGameSessionUseCase,
    ILogger<ArkNovaPreparationViewModel> logger) : ObservableObject
{
    private static readonly PlayerColor[] AllColors = [.. ArkNovaPlayerColors.Rules.AllowedColors];

    public ObservableCollection<ArkNovaPreparationPlayerViewModel> Players { get; } = [];

    [ObservableProperty]
    public partial Guid GroupId { get; private set; }

    [ObservableProperty]
    public partial string GroupName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string LastGameSessionText { get; private set; } = AppResources.NoPreviousGameSession;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial bool PlansDrawn { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool CanStart =>
        !IsBusy
        && Players.Count is >= ArkNovaGameSessionSetup.MinimumPlayerCount and <= ArkNovaGameSessionSetup.MaximumPlayerCount
        && Players.All(player => player.SelectedZooPlanNumber is not null)
        && Players.Select(player => player.Color).Distinct().Count() == Players.Count;

    public async Task LoadAsync(
        Guid groupId,
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

            ArkNovaPreparationData? data = await preparationQuery.GetPreparationAsync(
                groupId,
                cancellationToken);

            if (data is null)
            {
                ErrorMessage = AppResources.GroupNotFoundForPreparation;
                return;
            }

            GroupId = data.GroupId;
            GroupName = data.GroupName;
            LastGameSessionText = FormatLastGameSession(data.LastGameSession);
            Players.Clear();
            PlansDrawn = false;

            if (data.IsArchived)
            {
                ErrorMessage = AppResources.ArchivedGroupCannotStart;
                return;
            }

            if (data.Players.Count is < ArkNovaGameSessionSetup.MinimumPlayerCount or > ArkNovaGameSessionSetup.MaximumPlayerCount)
            {
                ErrorMessage = AppResources.ArkNovaPlayerCountError;
                return;
            }

            PlayerColor[] assignedColors = AssignColors(data.Players);

            for (int index = 0; index < data.Players.Count; index++)
            {
                ArkNovaPreparationPlayer player = data.Players[index];
                Players.Add(new ArkNovaPreparationPlayerViewModel(
                    player.PlayerId,
                    player.Name,
                    index + 1,
                    assignedColors[index]));
            }

            UpdatePositions();
            if (data.LastGameSession is not null)
            {
                Guid? startingPlayerId =
                    ArkNovaStartingPlayerRule.SelectFromPreviousPlacements(data.LastGameSession.Placements);

                if (startingPlayerId is Guid playerId)
                {
                    int startIndex = Players
                        .Select((player, index) => new { player.PlayerId, Index = index })
                        .Where(item => item.PlayerId == playerId)
                        .Select(item => item.Index)
                        .DefaultIfEmpty(-1)
                        .First();

                    if (startIndex > 0)
                    {
                        IReadOnlyList<ArkNovaPreparationPlayerViewModel> currentOrder = [.. Players];
                        IReadOnlyList<ArkNovaPreparationPlayerViewModel> rotated =
                            StartingPlayerRotation.RotateToStart(currentOrder, startIndex);
                        ReplacePlayers(rotated);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load Ark Nova preparation for group {GroupId}.", groupId);
            ErrorMessage = UserErrorMessage.Create(
                AppResources.LoadPreparationFailed,
                exception);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanStart));
        }
    }

    public void DrawZooPlans()
    {
        if (Players.Count == 0)
        {
            return;
        }

        IReadOnlyList<ArkNovaZooPlanDraw> draws = ArkNovaPreparationDraw.Draw(
            [.. Players.Select(player => player.PlayerId)],
            Random.Shared);

        foreach (ArkNovaZooPlanDraw draw in draws)
        {
            ArkNovaPreparationPlayerViewModel player = Players.Single(
                candidate => candidate.PlayerId == draw.PlayerId);
            player.SetZooPlans(draw.Option1, draw.Option2);
        }

        PlansDrawn = true;
        OnPropertyChanged(nameof(CanStart));
    }

    public void SelectZooPlan(ArkNovaPreparationPlayerViewModel player, int optionNumber)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.SelectZooPlan(optionNumber);
        OnPropertyChanged(nameof(CanStart));
    }

    public void RotateToRandomStartingPlayer()
    {
        if (Players.Count < 2)
        {
            return;
        }

        int startIndex = Random.Shared.Next(Players.Count);
        IReadOnlyList<ArkNovaPreparationPlayerViewModel> currentOrder = [.. Players];
        IReadOnlyList<ArkNovaPreparationPlayerViewModel> rotated =
            StartingPlayerRotation.RotateToStart(currentOrder, startIndex);

        ReplacePlayers(rotated);
    }

    public void RotateOrder()
    {
        if (Players.Count < 2)
        {
            return;
        }

        IReadOnlyList<ArkNovaPreparationPlayerViewModel> currentOrder = [.. Players];
        IReadOnlyList<ArkNovaPreparationPlayerViewModel> rotated =
            StartingPlayerRotation.RotateToStart(currentOrder, 1);

        ReplacePlayers(rotated);
    }

    public void ToggleColorPicker(ArkNovaPreparationPlayerViewModel player)
    {
        ArgumentNullException.ThrowIfNull(player);

        foreach (ArkNovaPreparationPlayerViewModel candidate in Players)
        {
            candidate.IsColorPickerOpen =
                ReferenceEquals(candidate, player) && !candidate.IsColorPickerOpen;
        }
    }

    public void SetColor(
        ArkNovaPreparationPlayerViewModel player,
        PlayerColor color)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.Color = color;
        player.IsColorPickerOpen = false;
        OnPropertyChanged(nameof(CanStart));
    }

    public async Task<Guid?> StartAsync(CancellationToken cancellationToken = default)
    {
        if (!CanStart || IsBusy)
        {
            return null;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            IReadOnlyList<ArkNovaGameSessionSetupPlayer> setupPlayers =
            [
                .. Players.Select(player => new ArkNovaGameSessionSetupPlayer(
                    player.PlayerId,
                    player.Color,
                    player.ZooPlanOption1!.Number,
                    player.ZooPlanOption2!.Number,
                    player.SelectedZooPlanNumber))
            ];

            StartArkNovaGameSessionCommand command = new(
                GroupId,
                DateTimeOffset.UtcNow,
                setupPlayers);

            return await startGameSessionUseCase.StartAsync(command, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not start Ark Nova GameSession for group {GroupId}.", GroupId);
            ErrorMessage = UserErrorMessage.Create(
                AppResources.StartGameFailed,
                exception);
            return null;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanStart));
        }
    }

    private static string FormatLastGameSession(ArkNovaLastGameSession? lastGameSession)
    {
        if (lastGameSession is null)
        {
            return AppResources.NoPreviousGameSession;
        }

        string header = lastGameSession.StartedAt.ToLocalTime().ToString("d");

        if (lastGameSession.Placements.Count == 0)
        {
            return header;
        }

        string placements = string.Join(
            " · ",
            lastGameSession.Placements
                .OrderBy(player => player.Placement)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(player => $"{player.Placement}. {player.Name}"));

        return $"{header}\n{placements}";
    }

    private static PlayerColor[] AssignColors(
        IReadOnlyList<ArkNovaPreparationPlayer> players)
    {
        PlayerColor[] result = new PlayerColor[players.Count];
        HashSet<PlayerColor> used = [];

        for (int index = 0; index < players.Count; index++)
        {
            PlayerColor? defaultColor = players[index].DefaultColor;

            if (defaultColor is not null && used.Add(defaultColor.Value))
            {
                result[index] = defaultColor.Value;
                continue;
            }

            PlayerColor fallback = AllColors.First(color => !used.Contains(color));
            used.Add(fallback);
            result[index] = fallback;
        }

        return result;
    }

    private void ReplacePlayers(IReadOnlyList<ArkNovaPreparationPlayerViewModel> orderedPlayers)
    {
        Players.Clear();

        foreach (ArkNovaPreparationPlayerViewModel player in orderedPlayers)
        {
            Players.Add(player);
        }

        UpdatePositions();
    }

    public void MovePlayer(ArkNovaPreparationPlayerViewModel player, int offset)
    {
        int sourceIndex = Players.IndexOf(player);
        int targetIndex = sourceIndex + offset;
        if (IsBusy || sourceIndex < 0 || offset is not (-1 or 1) || targetIndex < 0 || targetIndex >= Players.Count)
            return;

        ReplacePlayers(StartingPlayerRotation.MovePlayer([.. Players], sourceIndex, targetIndex));
    }

    private void UpdatePositions()
    {
        for (int index = 0; index < Players.Count; index++)
        {
            Players[index].Position = index + 1;
            Players[index].CanMoveUp = index > 0;
            Players[index].CanMoveDown = index < Players.Count - 1;
        }
    }
}

/// <summary>
/// Presentation model for one player tile during Ark Nova preparation. It contains
/// temporary choices that are persisted only after the GameSession starts.
/// </summary>
public partial class ArkNovaPreparationPlayerViewModel(
    Guid playerId,
    string name,
    int position,
    PlayerColor color) : ObservableObject
{
    public Guid PlayerId { get; } = playerId;
    public string Name { get; } = name;

    [ObservableProperty]
    public partial int Position { get; set; } = position;

    [ObservableProperty] public partial bool CanMoveUp { get; set; }
    [ObservableProperty] public partial bool CanMoveDown { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ColorName))]
    [NotifyPropertyChangedFor(nameof(ColorBrush))]
    [NotifyPropertyChangedFor(nameof(PlayerTextColor))]
    [NotifyPropertyChangedFor(nameof(CanChooseBlue))]
    [NotifyPropertyChangedFor(nameof(CanChooseYellow))]
    [NotifyPropertyChangedFor(nameof(CanChooseRed))]
    [NotifyPropertyChangedFor(nameof(CanChooseBlack))]
    public partial PlayerColor Color { get; set; } = color;

    [ObservableProperty]
    public partial bool IsColorPickerOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasZooPlans))]
    [NotifyPropertyChangedFor(nameof(ZooPlanOption1IconSource))]
    public partial ArkNovaZooPlanOption? ZooPlanOption1 { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasZooPlans))]
    [NotifyPropertyChangedFor(nameof(ZooPlanOption2IconSource))]
    public partial ArkNovaZooPlanOption? ZooPlanOption2 { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Option1Selected))]
    [NotifyPropertyChangedFor(nameof(Option2Selected))]
    [NotifyPropertyChangedFor(nameof(Option1StrokeThickness))]
    [NotifyPropertyChangedFor(nameof(Option2StrokeThickness))]
    public partial int? SelectedZooPlanNumber { get; private set; }

    public Microsoft.Maui.Graphics.Color PlayerTextColor => PlayerColorPresentation.GetText(Color);

    public string ColorName => PlayerColorPresentation.GetName(Color);

    public Microsoft.Maui.Graphics.Color ColorBrush =>
        PlayerColorPresentation.GetBackground(Color);

    public bool CanChooseBlue => Color != PlayerColor.Blue;
    public bool CanChooseYellow => Color != PlayerColor.Yellow;
    public bool CanChooseRed => Color != PlayerColor.Red;
    public bool CanChooseBlack => Color != PlayerColor.Black;

    public bool HasZooPlans => ZooPlanOption1 is not null && ZooPlanOption2 is not null;
    public string? ZooPlanOption1IconSource =>
        ZooPlanOption1 is null ? null : ArkNovaZooPlanPresentation.GetIconSource(ZooPlanOption1.Number);
    public string? ZooPlanOption2IconSource =>
        ZooPlanOption2 is null ? null : ArkNovaZooPlanPresentation.GetIconSource(ZooPlanOption2.Number);
    public bool Option1Selected =>
        SelectedZooPlanNumber.HasValue
        && ZooPlanOption1 is not null
        && ZooPlanOption1.Number == SelectedZooPlanNumber.Value;

    public bool Option2Selected =>
        SelectedZooPlanNumber.HasValue
        && ZooPlanOption2 is not null
        && ZooPlanOption2.Number == SelectedZooPlanNumber.Value;
    public double Option1StrokeThickness => Option1Selected ? 3 : 0;
    public double Option2StrokeThickness => Option2Selected ? 3 : 0;

    public void SetZooPlans(ArkNovaZooPlanOption option1, ArkNovaZooPlanOption option2)
    {
        ZooPlanOption1 = option1;
        ZooPlanOption2 = option2;
        SelectedZooPlanNumber = null;
    }

    public void SelectZooPlan(int optionNumber)
    {
        ArkNovaZooPlanOption? selected = optionNumber switch
        {
            1 => ZooPlanOption1,
            2 => ZooPlanOption2,
            _ => null
        };

        if (selected is null)
        {
            return;
        }

        SelectedZooPlanNumber = selected.Number;
    }
}

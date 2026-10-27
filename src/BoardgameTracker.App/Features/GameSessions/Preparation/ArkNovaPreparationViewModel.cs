using System.Collections.ObjectModel;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Features.Games.ArkNova;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.Preparation;
using BoardgameTracker.Application.Games.ArkNova.StartSession;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Setup;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.GameSessions.Preparation;

/// <summary>
/// Holds the non-persisted screen state for preparing an Ark Nova session and
/// delegates persistence to the start-session Application use case.
/// </summary>
public partial class ArkNovaPreparationViewModel(
    IArkNovaPreparationQuery preparationQuery,
    StartArkNovaSessionUseCase startSessionUseCase,
    ILogger<ArkNovaPreparationViewModel> logger) : ObservableObject
{
    private static readonly ArkNovaPlayerColor[] AllColors = Enum.GetValues<ArkNovaPlayerColor>();

    public ObservableCollection<ArkNovaPreparationPlayerViewModel> Players { get; } = [];

    [ObservableProperty]
    public partial Guid GroupId { get; private set; }

    [ObservableProperty]
    public partial string GroupName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string LastSessionText { get; private set; } = AppResources.NoPreviousSession;

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
        && Players.Count is >= ArkNovaSessionSetup.MinimumPlayerCount and <= ArkNovaSessionSetup.MaximumPlayerCount
        && Players.All(player => player.SelectedZooPlanId is not null)
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

            ArkNovaPreparationData? data = await preparationQuery.ExecuteAsync(
                groupId,
                cancellationToken);

            if (data is null)
            {
                ErrorMessage = AppResources.GroupNotFoundForPreparation;
                return;
            }

            GroupId = data.GroupId;
            GroupName = data.GroupName;
            LastSessionText = FormatLastSession(data.LastSession);
            Players.Clear();
            PlansDrawn = false;

            if (data.IsArchived)
            {
                ErrorMessage = AppResources.ArchivedGroupCannotStart;
                return;
            }

            if (data.Players.Count is < ArkNovaSessionSetup.MinimumPlayerCount or > ArkNovaSessionSetup.MaximumPlayerCount)
            {
                ErrorMessage = AppResources.ArkNovaPlayerCountError;
                return;
            }

            ArkNovaPlayerColor[] assignedColors = AssignColors(data.Players);

            for (int index = 0; index < data.Players.Count; index++)
            {
                ArkNovaPreparationPlayer player = data.Players[index];
                Players.Add(new ArkNovaPreparationPlayerViewModel(
                    player.PersonId,
                    player.Name,
                    index + 1,
                    assignedColors[index]));
            }

            if (data.LastSession is not null)
            {
                Guid? startingPlayerId =
                    ArkNovaStartingPlayerRule.SelectFromPreviousPlacements(data.LastSession.Placements);

                if (startingPlayerId is Guid personId)
                {
                    int startIndex = Players
                        .Select((player, index) => new { player.PersonId, Index = index })
                        .Where(item => item.PersonId == personId)
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
            [.. Players.Select(player => player.PersonId)],
            Random.Shared);

        foreach (ArkNovaZooPlanDraw draw in draws)
        {
            ArkNovaPreparationPlayerViewModel player = Players.Single(
                candidate => candidate.PersonId == draw.PersonId);
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
        ArkNovaPlayerColor color)
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

            IReadOnlyList<ArkNovaSessionSetupPlayer> setupPlayers =
            [
                .. Players.Select(player => new ArkNovaSessionSetupPlayer(
                    player.PersonId,
                    player.Color,
                    player.ZooPlanOption1!.Id,
                    player.ZooPlanOption2!.Id,
                    player.SelectedZooPlanId))
            ];

            StartArkNovaSessionCommand command = new(
                GroupId,
                DateTimeOffset.UtcNow,
                setupPlayers);

            return await startSessionUseCase.ExecuteAsync(command, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not start Ark Nova session for group {GroupId}.", GroupId);
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

    private static string FormatLastSession(ArkNovaLastSession? lastSession)
    {
        if (lastSession is null)
        {
            return AppResources.NoPreviousSession;
        }

        string header = string.Format(AppResources.PreviousSessionFormat, lastSession.StartedAt.ToLocalTime().ToString("d"));

        if (lastSession.Placements.Count == 0)
        {
            return header;
        }

        string placements = string.Join(
            " · ",
            lastSession.Placements
                .OrderBy(player => player.Placement)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(player => $"{player.Placement}. {player.Name}"));

        return $"{header}\n{placements}";
    }

    private static ArkNovaPlayerColor[] AssignColors(
        IReadOnlyList<ArkNovaPreparationPlayer> players)
    {
        ArkNovaPlayerColor[] result = new ArkNovaPlayerColor[players.Count];
        HashSet<ArkNovaPlayerColor> used = [];

        for (int index = 0; index < players.Count; index++)
        {
            ArkNovaPlayerColor? defaultColor = players[index].DefaultColor;

            if (defaultColor is not null && used.Add(defaultColor.Value))
            {
                result[index] = defaultColor.Value;
                continue;
            }

            ArkNovaPlayerColor fallback = AllColors.First(color => !used.Contains(color));
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

    private void UpdatePositions()
    {
        for (int index = 0; index < Players.Count; index++)
        {
            Players[index].Position = index + 1;
        }
    }
}

/// <summary>
/// Presentation model for one player tile during Ark Nova preparation. It contains
/// temporary choices that are persisted only after the session starts.
/// </summary>
public partial class ArkNovaPreparationPlayerViewModel(
    Guid personId,
    string name,
    int position,
    ArkNovaPlayerColor color) : ObservableObject
{
    public Guid PersonId { get; } = personId;
    public string Name { get; } = name;

    [ObservableProperty]
    public partial int Position { get; set; } = position;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ColorName))]
    [NotifyPropertyChangedFor(nameof(ColorBrush))]
    [NotifyPropertyChangedFor(nameof(CanChooseBlue))]
    [NotifyPropertyChangedFor(nameof(CanChooseYellow))]
    [NotifyPropertyChangedFor(nameof(CanChooseRed))]
    [NotifyPropertyChangedFor(nameof(CanChooseBlack))]
    public partial ArkNovaPlayerColor Color { get; set; } = color;

    [ObservableProperty]
    public partial bool IsColorPickerOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasZooPlans))]
    public partial ArkNovaZooPlanOption? ZooPlanOption1 { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasZooPlans))]
    public partial ArkNovaZooPlanOption? ZooPlanOption2 { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Option1Selected))]
    [NotifyPropertyChangedFor(nameof(Option2Selected))]
    [NotifyPropertyChangedFor(nameof(Option1Stroke))]
    [NotifyPropertyChangedFor(nameof(Option2Stroke))]
    [NotifyPropertyChangedFor(nameof(Option1StrokeThickness))]
    [NotifyPropertyChangedFor(nameof(Option2StrokeThickness))]
    public partial int? SelectedZooPlanId { get; private set; }

    public string ColorName => ArkNovaPlayerColorPresentation.GetName(Color);

    public Microsoft.Maui.Graphics.Color ColorBrush =>
        ArkNovaPlayerColorPresentation.GetTint(Color);

    public bool CanChooseBlue => Color != ArkNovaPlayerColor.Blue;
    public bool CanChooseYellow => Color != ArkNovaPlayerColor.Yellow;
    public bool CanChooseRed => Color != ArkNovaPlayerColor.Red;
    public bool CanChooseBlack => Color != ArkNovaPlayerColor.Black;

    public bool HasZooPlans => ZooPlanOption1 is not null && ZooPlanOption2 is not null;
    public bool Option1Selected =>
        SelectedZooPlanId.HasValue
        && ZooPlanOption1 is not null
        && ZooPlanOption1.Id == SelectedZooPlanId.Value;

    public bool Option2Selected =>
        SelectedZooPlanId.HasValue
        && ZooPlanOption2 is not null
        && ZooPlanOption2.Id == SelectedZooPlanId.Value;
    public Microsoft.Maui.Graphics.Color Option1Stroke =>
        Option1Selected ? Microsoft.Maui.Graphics.Colors.Green : Microsoft.Maui.Graphics.Colors.Transparent;

    public Microsoft.Maui.Graphics.Color Option2Stroke =>
        Option2Selected ? Microsoft.Maui.Graphics.Colors.Green : Microsoft.Maui.Graphics.Colors.Transparent;

    public double Option1StrokeThickness => Option1Selected ? 3 : 0;
    public double Option2StrokeThickness => Option2Selected ? 3 : 0;

    public void SetZooPlans(ArkNovaZooPlanOption option1, ArkNovaZooPlanOption option2)
    {
        ZooPlanOption1 = option1;
        ZooPlanOption2 = option2;
        SelectedZooPlanId = null;
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

        SelectedZooPlanId = selected.Id;
    }
}

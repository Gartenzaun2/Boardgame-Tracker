using System.Collections.ObjectModel;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.Application.Games.ArkNova.Preparation;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.GameSessions.Preparation;

public partial class ArkNovaPreparationViewModel(
    IArkNovaPreparationQuery preparationQuery,
    ILogger<ArkNovaPreparationViewModel> logger) : ObservableObject
{
    private static readonly ArkNovaPlayerColor[] AllColors = Enum.GetValues<ArkNovaPlayerColor>();

    public ObservableCollection<ArkNovaPreparationPlayerViewModel> Players { get; } = [];

    [ObservableProperty]
    public partial Guid GroupId { get; private set; }

    [ObservableProperty]
    public partial string GroupName { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string LastSessionText { get; private set; } = "Keine letzte Partie vorhanden.";

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial bool PlansDrawn { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

    public bool CanStart =>
        !IsBusy
        && Players.Count is >= 1 and <= 4
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
                ErrorMessage = "Die Gruppe wurde nicht gefunden.";
                return;
            }

            GroupId = data.GroupId;
            GroupName = data.GroupName;
            LastSessionText = data.LastSession is null
                ? "Keine letzte Partie vorhanden."
                : data.LastSession.WinnerName is null
                    ? $"Letzte Partie: {data.LastSession.StartedAt.ToLocalTime():d}"
                    : $"Letzte Partie: {data.LastSession.StartedAt.ToLocalTime():d} · Gewinner: {data.LastSession.WinnerName}";
            Players.Clear();
            PlansDrawn = false;

            if (data.IsArchived)
            {
                ErrorMessage = "Archivierte Gruppen können keine neue Partie starten.";
                return;
            }

            if (data.Players.Count is < 1 or > 4)
            {
                ErrorMessage = "Arche Nova unterstützt in dieser App Gruppen mit 1 bis 4 Personen.";
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

            if (data.LastSession?.LastPlacePersonId is Guid lastPlacePersonId)
            {
                int lastPlaceIndex = Players
                    .Select((player, index) => new { player.PersonId, Index = index })
                    .Where(item => item.PersonId == lastPlacePersonId)
                    .Select(item => item.Index)
                    .DefaultIfEmpty(-1)
                    .First();

                if (lastPlaceIndex > 0)
                {
                    IReadOnlyList<ArkNovaPreparationPlayerViewModel> rotated =
                        StartingPlayerRotation.RotateToStart(Players.ToArray(), lastPlaceIndex);
                    ReplacePlayers(rotated);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load Ark Nova preparation for group {GroupId}.", groupId);
            ErrorMessage = UserErrorMessage.Create(
                "Die Partievorbereitung konnte nicht geladen werden.",
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

        List<ArkNovaZooPlanOption> available = [.. ArkNovaZooPlanCatalog.All];
        Shuffle(available);

        for (int index = 0; index < Players.Count; index++)
        {
            int offset = index * 2;
            Players[index].SetZooPlans(available[offset], available[offset + 1]);
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
        IReadOnlyList<ArkNovaPreparationPlayerViewModel> rotated =
            StartingPlayerRotation.RotateToStart(Players.ToArray(), startIndex);

        ReplacePlayers(rotated);
    }

    public void RotateOrder()
    {
        if (Players.Count < 2)
        {
            return;
        }

        IReadOnlyList<ArkNovaPreparationPlayerViewModel> rotated =
            StartingPlayerRotation.RotateToStart(Players.ToArray(), 1);

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

    private static void Shuffle<T>(IList<T> items)
    {
        for (int index = items.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Shared.Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
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

    partial void OnColorChanged(ArkNovaPlayerColor value)
    {
        // The parent view model validates duplicate colors when starting.
    }

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

    public string ColorName => Color switch
    {
        ArkNovaPlayerColor.Blue => "Blau",
        ArkNovaPlayerColor.Yellow => "Gelb",
        ArkNovaPlayerColor.Red => "Rot",
        ArkNovaPlayerColor.Black => "Schwarz",
        _ => Color.ToString()
    };

    public Microsoft.Maui.Graphics.Color ColorBrush => Color switch
    {
        ArkNovaPlayerColor.Blue => Microsoft.Maui.Graphics.Color.FromArgb("#8BC4E8"),
        ArkNovaPlayerColor.Yellow => Microsoft.Maui.Graphics.Color.FromArgb("#F2D65C"),
        ArkNovaPlayerColor.Red => Microsoft.Maui.Graphics.Color.FromArgb("#E98A84"),
        ArkNovaPlayerColor.Black => Microsoft.Maui.Graphics.Color.FromArgb("#808080"),
        _ => Microsoft.Maui.Graphics.Colors.Transparent
    };

    public bool CanChooseBlue => Color != ArkNovaPlayerColor.Blue;
    public bool CanChooseYellow => Color != ArkNovaPlayerColor.Yellow;
    public bool CanChooseRed => Color != ArkNovaPlayerColor.Red;
    public bool CanChooseBlack => Color != ArkNovaPlayerColor.Black;

    public bool HasZooPlans => ZooPlanOption1 is not null && ZooPlanOption2 is not null;
    public bool Option1Selected => ZooPlanOption1?.Id == SelectedZooPlanId;
    public bool Option2Selected => ZooPlanOption2?.Id == SelectedZooPlanId;
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
        OnPropertyChanged(nameof(Option1Selected));
        OnPropertyChanged(nameof(Option2Selected));
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

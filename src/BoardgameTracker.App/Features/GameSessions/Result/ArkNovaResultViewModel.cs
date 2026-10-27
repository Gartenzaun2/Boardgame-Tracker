using System.Collections.ObjectModel;
using BoardgameTracker.App.Features.Games.ArkNova;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.GameSessions.Result;

public partial class ArkNovaResultViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    IArkNovaSettingsStore settingsStore) : ObservableObject
{
    public ObservableCollection<ArkNovaResultPlayerViewModel> Players { get; } = [];
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string DurationText { get; private set; } = string.Empty;

    public async Task LoadAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        ArkNovaScoringSessionData session = await scoringUseCase.LoadAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("The completed Ark Nova session does not exist.");
        ArkNovaScoringDisplayMode mode = settingsStore.Get().ScoringDisplayMode;
        GroupName = session.GroupName;
        DurationText = FormatDuration(session.StartedAt, session.EndedAt);
        Players.Clear();

        foreach (ArkNovaScoringPlayerData player in session.Players
                     .OrderBy(x => x.Placement ?? int.MaxValue)
                     .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            ArkNovaSpecialCard[] cards = session.SpecialCards
                .Where(x => x.PersonId == player.PersonId)
                .Select(x => x.SpecialCard)
                .ToArray();
            Players.Add(new ArkNovaResultPlayerViewModel(player, cards, mode));
        }
    }

    private static string FormatDuration(DateTimeOffset startedAt, DateTimeOffset? endedAt)
    {
        TimeSpan duration = (endedAt ?? DateTimeOffset.UtcNow) - startedAt;
        int totalMinutes = Math.Max(0, (int)duration.TotalMinutes);
        return totalMinutes >= 60
            ? string.Format(AppResources.DurationHoursFormat, totalMinutes / 60, totalMinutes % 60)
            : string.Format(AppResources.DurationMinutesFormat, totalMinutes);
    }

}

public sealed class ArkNovaResultPlayerViewModel
{
    public ArkNovaResultPlayerViewModel(
        ArkNovaScoringPlayerData player,
        IReadOnlyList<ArkNovaSpecialCard> cards,
        ArkNovaScoringDisplayMode mode)
    {
        Placement = player.Placement ?? 0;
        Name = player.Name;
        ColorText = ArkNovaPlayerColorPresentation.GetName(player.Color);
        Tint = ArkNovaPlayerColorPresentation.GetTint(player.Color);
        ZooPlanText = $"Zooplan {player.ZooPlanNumber}";
        RawValuesText = $"Attraktivität {player.Attractiveness} · Artenschutz {player.Conservation}"
            + (player.Reputation is int reputation ? $" · Ruf {reputation}" : " · Ruf nicht erfasst");
        Score = player.Attractiveness is int attractiveness && player.Conservation is int conservation
            ? mode == ArkNovaScoringDisplayMode.Old
                ? ArkNovaScoringRules.CalculateOldScore(attractiveness, conservation)
                : ArkNovaScoringRules.CalculateNewScore(attractiveness, conservation)
            : 0;
        SpecialCardsText = cards.Count == 0 ? AppResources.NoSpecialCards : string.Join(" · ", cards.Select(ArkNovaSpecialCardPresentation.GetName));
    }

    public int Placement { get; }
    public string PlacementText => $"{Placement}.";
    public string Name { get; }
    public string ColorText { get; }
    public Microsoft.Maui.Graphics.Color Tint { get; }
    public string ZooPlanText { get; }
    public string RawValuesText { get; }
    public int Score { get; }
    public string ScoreText => $"{Score} Punkte";
    public string SpecialCardsText { get; }
}

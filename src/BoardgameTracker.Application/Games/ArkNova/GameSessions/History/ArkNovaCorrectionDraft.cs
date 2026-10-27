using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.History;

// A view-owned, non-persistent edit buffer. No status transition or partial writes.
public sealed class ArkNovaCorrectionDraft
{
    internal ArkNovaCorrectionDraft(ArkNovaScoringGameSessionData data) { Original = data; Data = data; }
    public ArkNovaScoringGameSessionData Original { get; }
    public bool Saved { get; internal set; }
    public ArkNovaScoringGameSessionData Data { get; private set; }
    public void SetScore(Guid playerId, int? attractiveness, int? conservation, int? reputation)
    {
        ArkNovaScoringRules.ValidateScoreValues(attractiveness, conservation, reputation);
        if (!Data.Players.Any(p => p.PlayerId == playerId)) throw new ArgumentException("Unknown player.");
        Data = Data with
        {
            Players = Data.Players.Select(p => p.PlayerId == playerId
            ? p with { Attractiveness = attractiveness, Conservation = conservation, Reputation = reputation } : p).ToArray()
        };
    }
    public void SetCard(Guid playerId, ArkNovaSpecialCard card, bool assigned)
    {
        if (!Enum.IsDefined(card) || !Data.Players.Any(p => p.PlayerId == playerId)) throw new ArgumentException("Invalid card assignment.");
        var cards = Data.SpecialCards.Where(c => c.PlayerId != playerId || c.SpecialCard != card).ToList();
        if (assigned) cards.Add(new(playerId, card));
        Data = Data with { SpecialCards = cards };
    }
}

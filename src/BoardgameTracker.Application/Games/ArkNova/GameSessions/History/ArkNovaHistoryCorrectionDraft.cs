using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.History;

// A view-owned, non-persistent edit buffer. No status transition or partial writes.
public sealed class ArkNovaHistoryCorrectionDraft
{
    internal ArkNovaHistoryCorrectionDraft(ArkNovaGameSessionResultData gameSessionResult)
    {
        Original = gameSessionResult;
        Data = gameSessionResult;
    }
    public ArkNovaGameSessionResultData Original { get; }
    public bool Saved { get; internal set; }
    public ArkNovaGameSessionResultData Data { get; private set; }
    public void RefreshCardCatalog(IReadOnlyList<ArkNovaSpecialCardData> catalog) => Data = Data with { CardCatalog = catalog };

    public void SetScore(Guid playerId, int? attractiveness, int? conservation, int? reputation)
    {
        ArkNovaScoringRules.ValidateScoreValues(attractiveness, conservation, reputation);
        if (!Data.Players.Any(p => p.PlayerId == playerId))
            throw new ArgumentException("Unknown player.");
        Data = Data with
        {
            Players = Data.Players.Select(p => p.PlayerId == playerId
            ? p with { Attractiveness = attractiveness, Conservation = conservation, Reputation = reputation } : p).ToArray()
        };
    }
    public void SetCard(Guid playerId, Guid cardId, bool assigned)
    {
        if (cardId == Guid.Empty || !Data.Players.Any(p => p.PlayerId == playerId))
            throw new ArgumentException("Invalid card assignment.");
        bool exists = Data.SpecialCards.Any(c => c.PlayerId == playerId && c.SpecialCardId == cardId);
        if (assigned && !exists && !Data.CardCatalog.Any(c => c.Id == cardId && !c.IsArchived))
            throw new InvalidOperationException("Archived or unknown cards cannot be newly assigned.");
        List<ArkNovaSpecialCardAssignmentData> cards = Data.SpecialCards
            .Where(c => c.PlayerId != playerId || c.SpecialCardId != cardId).ToList();
        if (assigned)
            cards.Add(new(playerId, cardId));
        Data = Data with { SpecialCards = cards };
    }
}

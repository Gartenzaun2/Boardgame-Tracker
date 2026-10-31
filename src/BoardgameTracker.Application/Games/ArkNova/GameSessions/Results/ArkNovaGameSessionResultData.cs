using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;

// Live scoring and historical correction share the persisted result structure,
// not the rules governing when that structure may be changed.
public sealed record ArkNovaGameSessionResultData(
    Guid GameSessionId,
    string GroupName,
    GameSessionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<ArkNovaGameSessionResultPlayerData> Players,
    IReadOnlyList<ArkNovaSpecialCardAssignmentData> SpecialCards)
{
    public IReadOnlyDictionary<int, string> ZooPlanNames { get; init; } = new Dictionary<int, string>();
    public IReadOnlyList<ArkNovaSpecialCardData> CardCatalog { get; init; } = [];
}

public sealed record ArkNovaGameSessionResultPlayerData(
    Guid PlayerId,
    string Name,
    int Order,
    PlayerColor Color,
    int ZooPlanNumber,
    int? Attractiveness,
    int? Conservation,
    int? Reputation,
    int? Placement);

public sealed record ArkNovaSpecialCardAssignmentData(Guid PlayerId, Guid SpecialCardId);

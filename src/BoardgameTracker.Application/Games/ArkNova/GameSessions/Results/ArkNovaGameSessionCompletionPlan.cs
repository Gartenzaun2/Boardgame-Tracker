namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;

public sealed record ArkNovaGameSessionCompletionPlan(
    IReadOnlyDictionary<Guid, int> Scores,
    IReadOnlyDictionary<Guid, int> DefaultPlacements,
    bool HasTies);

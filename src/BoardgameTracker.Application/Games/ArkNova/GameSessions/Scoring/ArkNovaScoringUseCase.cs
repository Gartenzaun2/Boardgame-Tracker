using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

/// <summary>
/// Orchestrates scoring edits and completion while keeping scoring rules in the Core layer.
/// </summary>
public sealed class ArkNovaScoringUseCase(IArkNovaScoringStore store)
{
    public Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        return store.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
    }

    public async Task SavePlayerScoreAsync(
        Guid gameSessionId,
        Guid playerId,
        int? attractiveness,
        int? conservation,
        int? reputation,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        ArkNovaGameSessionPlayer? player = await store.FindScoringPlayerAsync(gameSessionId, playerId, cancellationToken);

        if (player is null)
        {
            throw new InvalidOperationException("The Ark Nova GameSession player does not exist.");
        }

        player.SetScoreValues(attractiveness, conservation, reputation);
        await store.SaveScoringPlayerAsync(player, cancellationToken);
    }

    public async Task AssignSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(gameSessionId, playerId, specialCardId);
        ArkNovaGameSessionResultData data = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        if (!data.Players.Any(p => p.PlayerId == playerId)
            || !data.CardCatalog.Any(c => c.Id == specialCardId && !c.IsArchived))
            throw new InvalidOperationException("Only active cards may be assigned to an existing player.");
        await store.AssignSpecialCardAsync(
            new ArkNovaGameSessionSpecialCardAssignment(gameSessionId, playerId, specialCardId),
            cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(gameSessionId, playerId, specialCardId);
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await store.RemoveSpecialCardAsync(gameSessionId, playerId, specialCardId, cancellationToken);
    }

    public async Task<ArkNovaGameSessionCompletionPlan> PrepareCompletionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        ArkNovaGameSessionResultData scoringData = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(scoringData);
        IReadOnlyDictionary<Guid, int> placements = ArkNovaPlacementRules.CreateDefaultPlacements(scores);

        return new ArkNovaGameSessionCompletionPlan(
            scores,
            placements,
            scores.GroupBy(x => x.Value).Any(group => group.Count() > 1));
    }

    public async Task CompleteAsync(
        Guid gameSessionId,
        IReadOnlyDictionary<Guid, int> placements,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placements);
        ArkNovaGameSessionResultData scoringData = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(scoringData);
        ArkNovaPlacementRules.ValidateResolution(scores, placements);

        GameSession? gameSession = await store.FindGameSessionAsync(gameSessionId, cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be completed.");
        }

        foreach (GameSessionPlayer player in gameSession.Players)
        {
            player.SetPlacement(placements[player.PlayerId]);
        }

        gameSession.Complete(completedAt);
        await store.SaveCompletedGameSessionAsync(gameSession, cancellationToken);
    }


    public async Task DiscardAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await store.DeleteScoringGameSessionAsync(gameSessionId, cancellationToken);
    }

    private async Task EnsureScoringGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken)
    {
        ArkNovaGameSessionResultData? gameSession = await store.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be changed here.");
        }
    }
    private async Task<ArkNovaGameSessionResultData> GetScoringGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArkNovaGameSessionResultData? gameSession = await store.LoadGameSessionResultAsync(gameSessionId, cancellationToken);

        if (gameSession is null)
        {
            throw new InvalidOperationException("The Ark Nova GameSession does not exist.");
        }

        if (gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be changed here.");
        }

        return gameSession;
    }

    private static Dictionary<Guid, int> CreateScores(ArkNovaGameSessionResultData scoringData)
    {
        if (scoringData.Players.Count == 0 || scoringData.Players.Any(x => x.Attractiveness is null || x.Conservation is null))
        {
            throw new InvalidOperationException("Attractiveness and conservation are required for every player.");
        }

        return scoringData.Players.ToDictionary(
            player => player.PlayerId,
            player => ArkNovaScoringRules.CalculateNewScore(
                player.Attractiveness!.Value,
                player.Conservation!.Value));
    }

    private static void ValidateSpecialCardArguments(Guid gameSessionId, Guid playerId, Guid specialCardId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(specialCardId, Guid.Empty);
    }
}

using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

/// <summary>
/// Orchestrates scoring edits and completion while keeping scoring rules in the Core layer.
/// </summary>
public sealed class ArkNovaScoringUseCase(IArkNovaScoringDataAccess dataAccess)
{
    public Task<ArkNovaScoringGameSessionData?> LoadAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        return dataAccess.LoadAsync(gameSessionId, cancellationToken);
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
        ArkNovaGameSessionPlayer? player = await dataAccess.FindPlayerAsync(gameSessionId, playerId, cancellationToken);

        if (player is null)
        {
            throw new InvalidOperationException("The Ark Nova GameSession player does not exist.");
        }

        player.SetScoreValues(attractiveness, conservation, reputation);
        await dataAccess.SavePlayerAsync(player, cancellationToken);
    }

    public async Task AssignSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        ArkNovaSpecialCard specialCard,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(gameSessionId, playerId, specialCard);
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await dataAccess.AddSpecialCardAsync(
            new ArkNovaGameSessionSpecialCardAssignment(gameSessionId, playerId, specialCard),
            cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        ArkNovaSpecialCard specialCard,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(gameSessionId, playerId, specialCard);
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await dataAccess.RemoveSpecialCardAsync(gameSessionId, playerId, specialCard, cancellationToken);
    }

    public async Task<ArkNovaCompletionPlan> PrepareCompletionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        ArkNovaScoringGameSessionData scoringData = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(scoringData);
        IReadOnlyDictionary<Guid, int> placements = ArkNovaPlacementRules.CreateDefaultPlacements(scores);

        return new ArkNovaCompletionPlan(
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
        ArkNovaScoringGameSessionData scoringData = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(scoringData);
        ArkNovaPlacementRules.ValidateResolution(scores, placements);

        GameSession? gameSession = await dataAccess.FindGameSessionAsync(gameSessionId, cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be completed.");
        }

        foreach (GameSessionPlayer player in gameSession.Players)
        {
            player.SetPlacement(placements[player.PlayerId]);
        }

        gameSession.Complete(completedAt);
        await dataAccess.SaveCompletedGameSessionAsync(gameSession, cancellationToken);
    }


    public async Task DiscardAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await dataAccess.DeleteGameSessionAsync(gameSessionId, cancellationToken);
    }

    private async Task EnsureScoringGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken)
    {
        ArkNovaScoringGameSessionData? gameSession = await dataAccess.LoadAsync(gameSessionId, cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be changed here.");
        }
    }
    private async Task<ArkNovaScoringGameSessionData> GetScoringGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArkNovaScoringGameSessionData? gameSession = await dataAccess.LoadAsync(gameSessionId, cancellationToken);

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

    private static Dictionary<Guid, int> CreateScores(ArkNovaScoringGameSessionData scoringData)
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

    private static void ValidateSpecialCardArguments(Guid gameSessionId, Guid playerId, ArkNovaSpecialCard specialCard)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        if (!Enum.IsDefined(specialCard))
        {
            throw new ArgumentOutOfRangeException(nameof(specialCard));
        }
    }
}

public sealed record ArkNovaCompletionPlan(
    IReadOnlyDictionary<Guid, int> Scores,
    IReadOnlyDictionary<Guid, int> DefaultPlacements,
    bool HasTies);

using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.Scoring;

/// <summary>
/// Orchestrates scoring edits and completion while keeping scoring rules in the Core layer.
/// </summary>
public sealed class ArkNovaScoringUseCase(IArkNovaScoringDataAccess dataAccess)
{
    public Task<ArkNovaScoringSessionData?> LoadAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        return dataAccess.LoadAsync(sessionId, cancellationToken);
    }

    public async Task SavePlayerScoreAsync(
        Guid sessionId,
        Guid personId,
        int? attractiveness,
        int? conservation,
        int? reputation,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);
        await EnsureScoringSessionAsync(sessionId, cancellationToken);
        ArkNovaSessionPlayer? player = await dataAccess.FindPlayerAsync(sessionId, personId, cancellationToken);

        if (player is null)
        {
            throw new InvalidOperationException("The Ark Nova session player does not exist.");
        }

        player.SetScoreValues(attractiveness, conservation, reputation);
        await dataAccess.SavePlayerAsync(player, cancellationToken);
    }

    public async Task AssignSpecialCardAsync(
        Guid sessionId,
        Guid personId,
        ArkNovaSpecialCard specialCard,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(sessionId, personId, specialCard);
        await EnsureScoringSessionAsync(sessionId, cancellationToken);
        await dataAccess.AddSpecialCardAsync(
            new ArkNovaSessionSpecialCardAssignment(sessionId, personId, specialCard),
            cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid sessionId,
        Guid personId,
        ArkNovaSpecialCard specialCard,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(sessionId, personId, specialCard);
        await EnsureScoringSessionAsync(sessionId, cancellationToken);
        await dataAccess.RemoveSpecialCardAsync(sessionId, personId, specialCard, cancellationToken);
    }

    public async Task<ArkNovaCompletionPlan> PrepareCompletionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ArkNovaScoringSessionData session = await GetScoringSessionAsync(sessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(session);
        IReadOnlyDictionary<Guid, int> placements = ArkNovaPlacementRules.CreateDefaultPlacements(scores);

        return new ArkNovaCompletionPlan(
            scores,
            placements,
            scores.GroupBy(x => x.Value).Any(group => group.Count() > 1));
    }

    public async Task CompleteAsync(
        Guid sessionId,
        IReadOnlyDictionary<Guid, int> placements,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placements);
        ArkNovaScoringSessionData session = await GetScoringSessionAsync(sessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(session);
        ArkNovaPlacementRules.ValidateResolution(scores, placements);

        GameSession? gameSession = await dataAccess.FindSessionAsync(sessionId, cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a session in scoring can be completed.");
        }

        foreach (GameSessionPlayer player in gameSession.Players)
        {
            player.SetPlacement(placements[player.PersonId]);
        }

        gameSession.Complete(completedAt);
        await dataAccess.SaveCompletedSessionAsync(gameSession, cancellationToken);
    }


    public async Task DiscardAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScoringSessionAsync(sessionId, cancellationToken);
        await dataAccess.DeleteSessionAsync(sessionId, cancellationToken);
    }

    private async Task EnsureScoringSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        ArkNovaScoringSessionData? session = await dataAccess.LoadAsync(sessionId, cancellationToken);
        if (session is null || session.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a session in scoring can be changed here.");
        }
    }
    private async Task<ArkNovaScoringSessionData> GetScoringSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        ArkNovaScoringSessionData? session = await dataAccess.LoadAsync(sessionId, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException("The Ark Nova session does not exist.");
        }

        if (session.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a session in scoring can be changed here.");
        }

        return session;
    }

    private static Dictionary<Guid, int> CreateScores(ArkNovaScoringSessionData session)
    {
        if (session.Players.Count == 0 || session.Players.Any(x => x.Attractiveness is null || x.Conservation is null))
        {
            throw new InvalidOperationException("Attractiveness and conservation are required for every player.");
        }

        return session.Players.ToDictionary(
            player => player.PersonId,
            player => ArkNovaScoringRules.CalculateNewScore(
                player.Attractiveness!.Value,
                player.Conservation!.Value));
    }

    private static void ValidateSpecialCardArguments(Guid sessionId, Guid personId, ArkNovaSpecialCard specialCard)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);
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

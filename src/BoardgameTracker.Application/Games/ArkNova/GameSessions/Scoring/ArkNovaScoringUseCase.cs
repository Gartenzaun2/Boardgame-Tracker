using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Games.ArkNova.PhotoDataset;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

/// <summary>Coordinates incremental scoring and completion; domain types protect scoring rules.</summary>
public sealed class ArkNovaScoringUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory, ArkNovaGameSessionResultQuery resultQuery,
    ArkNovaPhotoDataset? photoDataset = null)
{
    public Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        return resultQuery.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
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
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LoadScoringSessionAsync(dbContext, gameSessionId, cancellationToken);
        ArkNovaGameSessionPlayer? player = await dbContext.ArkNovaGameSessionPlayers
            .SingleOrDefaultAsync(player => player.GameSessionId == gameSessionId && player.PlayerId == playerId, cancellationToken);

        if (player is null)
        {
            throw new InvalidOperationException("The Ark Nova GameSession player does not exist.");
        }

        player.SetScoreValues(attractiveness, conservation, reputation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Applies only an explicitly confirmed photo draft; one SaveChanges commits all players atomically.</summary>
    public async Task SaveConfirmedPhotoScoresAsync(Guid gameSessionId, IReadOnlyList<ArkNovaConfirmedPhotoScore> scores,
        CancellationToken cancellationToken = default)
    {
        if (scores.Count == 0 || scores.Select(score => score.PlayerId).Distinct().Count() != scores.Count)
            throw new ArgumentException("Confirmed photo scores must identify distinct players.", nameof(scores));
        await using BoardgameTrackerDbContext dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LoadScoringSessionAsync(dbContext, gameSessionId, cancellationToken);
        Guid[] ids = scores.Select(score => score.PlayerId).ToArray();
        List<ArkNovaGameSessionPlayer> players = await dbContext.ArkNovaGameSessionPlayers
            .Where(player => player.GameSessionId == gameSessionId && ids.Contains(player.PlayerId)).ToListAsync(cancellationToken);
        if (players.Count != scores.Count)
            throw new InvalidOperationException("A confirmed photo player no longer belongs to this GameSession.");
        foreach (ArkNovaConfirmedPhotoScore score in scores)
            players.Single(player => player.PlayerId == score.PlayerId).SetScoreValues(score.Attractiveness, score.Conservation, score.Reputation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AssignSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(gameSessionId, playerId, specialCardId);
        await PersistSpecialCardAssignmentAsync(
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
        await DeleteSpecialCardAssignmentAsync(gameSessionId, playerId, specialCardId, cancellationToken);
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
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        // Scores, status and placements must describe the same protected database
        // state; a previously prepared UI plan is validated again here.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        GameSession gameSession = await LoadScoringSessionAsync(dbContext, gameSessionId, cancellationToken, includePlayers: true);
        var scoringData = await (from player in dbContext.ArkNovaGameSessionPlayers.AsNoTracking()
                                 join color in dbContext.GameSessionPlayerColors.AsNoTracking()
                                     on new { player.GameSessionId, player.PlayerId } equals new { color.GameSessionId, color.PlayerId }
                                 where player.GameSessionId == gameSessionId
                                 select new { player.PlayerId, color.Color, player.Attractiveness, player.Conservation, player.Reputation })
            .ToArrayAsync(cancellationToken);
        if (scoringData.Length == 0 || scoringData.Length != gameSession.Players.Count
            || scoringData.Any(player => player.Attractiveness is null || player.Conservation is null))
            throw new InvalidOperationException("Attractiveness and conservation are required for every player.");
        Dictionary<Guid, int> scores = scoringData.ToDictionary(player => player.PlayerId,
            player => ArkNovaScoringRules.CalculateNewScore(player.Attractiveness!.Value, player.Conservation!.Value));
        ArkNovaPlacementRules.ValidateResolution(scores, placements);

        foreach (GameSessionPlayer player in gameSession.Players)
        {
            player.SetPlacement(placements[player.PlayerId]);
        }

        gameSession.Complete(completedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        // Only the real completion commit freezes labels, never review confirmation or historical corrections.
        if (photoDataset is not null)
            await photoDataset.FreezeGroundTruthAsync(gameSessionId, scoringData.Select(player =>
                new ArkNovaDatasetScore(player.Color.ToString().ToLowerInvariant(), player.Attractiveness,
                    player.Conservation, player.Reputation)).ToArray());
    }

    public async Task DiscardAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await DeleteScoringGameSessionAsync(gameSessionId, cancellationToken);
    }

    private static async Task<GameSession> LoadScoringSessionAsync(BoardgameTrackerDbContext dbContext, Guid gameSessionId,
        CancellationToken cancellationToken, bool includePlayers = false)
    {
        IQueryable<GameSession> sessions = dbContext.GameSessions;
        if (includePlayers)
            sessions = sessions.Include(session => session.Players);
        GameSession? gameSession = await sessions.SingleOrDefaultAsync(session => session.Id == gameSessionId
            && dbContext.Games.Any(game => game.Id == session.GameId && game.Key == ArkNovaGame.Key), cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be changed here.");
        }
        return gameSession;
    }
    private async Task<ArkNovaGameSessionResultData> GetScoringGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArkNovaGameSessionResultData? gameSession = await resultQuery.LoadGameSessionResultAsync(gameSessionId, cancellationToken);

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

    private async Task PersistSpecialCardAssignmentAsync(
        ArkNovaGameSessionSpecialCardAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LoadScoringSessionAsync(dbContext, assignment.GameSessionId, cancellationToken);
        bool playerExists = await dbContext.ArkNovaGameSessionPlayers.AnyAsync(player => player.GameSessionId == assignment.GameSessionId
            && player.PlayerId == assignment.PlayerId && dbContext.GameSessionPlayerColors.Any(color => color.GameSessionId == player.GameSessionId
                && color.PlayerId == player.PlayerId), cancellationToken);
        if (!playerExists)
            throw new InvalidOperationException("Only active cards may be assigned to an existing player.");
        if (!await dbContext.ArkNovaSpecialCards.AnyAsync(c => c.Id == assignment.SpecialCardId && !c.IsArchived, cancellationToken))
            throw new InvalidOperationException("Only active special cards may be assigned.");
        bool exists = await dbContext.ArkNovaGameSessionSpecialCardAssignments.AnyAsync(
            x => x.GameSessionId == assignment.GameSessionId
                 && x.PlayerId == assignment.PlayerId
                 && x.SpecialCardId == assignment.SpecialCardId,
            cancellationToken);

        if (!exists)
        {
            dbContext.ArkNovaGameSessionSpecialCardAssignments.Add(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task DeleteSpecialCardAssignmentAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LoadScoringSessionAsync(dbContext, gameSessionId, cancellationToken);

        ArkNovaGameSessionSpecialCardAssignment? assignment = await dbContext.ArkNovaGameSessionSpecialCardAssignments
            .SingleOrDefaultAsync(
                x => x.GameSessionId == gameSessionId && x.PlayerId == playerId && x.SpecialCardId == specialCardId,
                cancellationToken);

        if (assignment is not null)
        {
            dbContext.ArkNovaGameSessionSpecialCardAssignments.Remove(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task DeleteScoringGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        GameSession gameSession = await LoadScoringSessionAsync(dbContext, gameSessionId, cancellationToken);
        dbContext.GameSessions.Remove(gameSession);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
public sealed record ArkNovaConfirmedPhotoScore(Guid PlayerId, int? Attractiveness, int? Conservation, int? Reputation);

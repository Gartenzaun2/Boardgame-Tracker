using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Application.Persistence;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;

/// <summary>Validates persisted group/reference data before creating the complete session snapshot.</summary>
public sealed class StartArkNovaGameSessionUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<Guid> StartAsync(
        StartArkNovaGameSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(command.Players);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        StartArkNovaGameSessionContext? context = await LoadContextAsync(
            dbContext,
            command.GroupId,
            cancellationToken);

        if (context is null)
        {
            throw new InvalidOperationException("The group does not exist.");
        }

        if (context.IsArchived)
        {
            throw new InvalidOperationException(
                "Archived groups cannot start a new gameSession.");
        }

        if (context.HasOpenGameSession)
        {
            throw new InvalidOperationException(
                "An unfinished game gameSession already exists.");
        }

        ArkNovaGameSessionSetup.Validate(command.Players);

        HashSet<Guid> groupPlayers = [.. context.GroupPlayerIds];
        HashSet<Guid> gameSessionPlayers = [.. command.Players.Select(player => player.PlayerId)];

        if (!groupPlayers.SetEquals(gameSessionPlayers))
        {
            throw new InvalidOperationException(
                "The gameSession players must match the fixed group membership.");
        }

        IReadOnlyList<int> requestedZooPlanIds =
        [
            .. command.Players
                .SelectMany(player => new[]
                {
                    player.ZooPlanOption1Number,
                    player.ZooPlanOption2Number
                })
                .Distinct()
        ];

        if (requestedZooPlanIds.Any(id => !context.AvailableZooPlanIds.Contains(id)))
        {
            throw new InvalidOperationException(
                "The gameSession contains an unknown Ark Nova zoo plan.");
        }

        IReadOnlyList<Guid> playerIds = [.. command.Players.Select(player => player.PlayerId)];
        GameSession gameSession = new(
            context.GameId,
            command.GroupId,
            command.StartedAt,
            playerIds);

        IReadOnlyCollection<ArkNovaGameSessionPlayer> arkNovaPlayers =
        [
            .. command.Players.Select(player => new ArkNovaGameSessionPlayer(
                gameSession.Id,
                player.PlayerId,
                player.Color,
                player.ZooPlanOption1Number,
                player.ZooPlanOption2Number,
                player.SelectedZooPlanNumber.GetValueOrDefault()))
        ];

        // One SaveChanges inserts the aggregate and game-specific snapshots atomically.

        await dbContext.GameSessions.AddAsync(gameSession, cancellationToken);
        await dbContext.ArkNovaGameSessionPlayers.AddRangeAsync(arkNovaPlayers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return gameSession.Id;
    }

    private static async Task<StartArkNovaGameSessionContext?> LoadContextAsync(
        BoardgameTrackerDbContext dbContext,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {

        var group = await dbContext.Groups
            .AsNoTracking()
            .Where(candidate => candidate.Id == groupId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.IsArchived
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            return null;
        }

        Guid? gameId = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Key == ArkNovaGame.Key)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (gameId is null)
        {
            throw new InvalidOperationException(
                "Ark Nova reference data is missing from the database.");
        }

        Guid[] groupPlayerIds = await dbContext.GroupPlayers
            .AsNoTracking()
            .Where(player => player.GroupId == groupId)
            .OrderBy(player => player.DefaultOrder)
            .Select(player => player.PlayerId)
            .ToArrayAsync(cancellationToken);

        int[] zooPlanNumbers = await dbContext.ArkNovaZooPlans
            .AsNoTracking()
            .Select(plan => plan.Number)
            .ToArrayAsync(cancellationToken);

        bool hasOpenGameSession = await dbContext.GameSessions
            .AsNoTracking()
            .AnyAsync(
                gameSession => gameSession.GameId == gameId.Value
                            && gameSession.GroupId == groupId
                            && (gameSession.Status == GameSessionStatus.Running
                             || gameSession.Status == GameSessionStatus.Scoring),
                cancellationToken);

        return new StartArkNovaGameSessionContext(
            gameId.Value,
            group.IsArchived,
            hasOpenGameSession,
            groupPlayerIds,
            zooPlanNumbers.ToHashSet());
    }
}

using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Application.Persistence;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions;

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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

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

        // Temporary ranges free both unique indexes before applying swaps. These direct
        // writes and all snapshots share one transaction; no temporary value can escape it.
        await SaveDefaultsAsync(dbContext, command, cancellationToken);

        await dbContext.GameSessions.AddAsync(gameSession, cancellationToken);
        await dbContext.ArkNovaGameSessionPlayers.AddRangeAsync(arkNovaPlayers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return gameSession.Id;
    }

    private static async Task SaveDefaultsAsync(
        BoardgameTrackerDbContext dbContext,
        StartArkNovaGameSessionCommand command,
        CancellationToken cancellationToken)
    {
        await dbContext.GroupPlayers.Where(player => player.GroupId == command.GroupId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                player => player.DefaultOrder, player => player.DefaultOrder + 1000), cancellationToken);

        Guid[] existingColorPlayerIds = await dbContext.ArkNovaGroupPlayers.AsNoTracking()
            .Where(player => player.GroupId == command.GroupId)
            .Select(player => player.PlayerId).ToArrayAsync(cancellationToken);
        // Color has no nullable representation. Reserve temporary database-only values
        // to support even a full four-color permutation without deleting default records.
        await dbContext.ArkNovaGroupPlayers.Where(player => player.GroupId == command.GroupId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                player => player.DefaultColor,
                player => (PlayerColor)((int)player.DefaultColor + 1000)), cancellationToken);

        for (int index = 0; index < command.Players.Count; index++)
        {
            ArkNovaGameSessionSetupPlayer player = command.Players[index];
            int order = index + 1;
            await dbContext.GroupPlayers
                .Where(candidate => candidate.GroupId == command.GroupId && candidate.PlayerId == player.PlayerId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.DefaultOrder, order), cancellationToken);

            if (existingColorPlayerIds.Contains(player.PlayerId))
            {
                await dbContext.ArkNovaGroupPlayers
                    .Where(candidate => candidate.GroupId == command.GroupId && candidate.PlayerId == player.PlayerId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.DefaultColor, player.Color), cancellationToken);
            }
            else
            {
                dbContext.ArkNovaGroupPlayers.Add(new ArkNovaGroupPlayer(command.GroupId, player.PlayerId, player.Color));
            }
        }
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

/// <summary>
/// Describes the complete user request to turn an already prepared Ark Nova setup
/// into a persisted running game gameSession. The player setup objects are Core types
/// because they represent the same domain setup state that Core validates; no
/// second Application DTO is needed for identical data.
/// </summary>
public sealed record StartArkNovaGameSessionCommand(
    Guid GroupId,
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaGameSessionSetupPlayer> Players);

/// <summary>
/// Minimal persisted context required by the start use case before it may create a
/// gameSession. This is a use-case read model, not a domain entity and not a database
/// model.
/// </summary>
public sealed record StartArkNovaGameSessionContext(
    Guid GameId,
    bool IsArchived,
    bool HasOpenGameSession,
    IReadOnlyList<Guid> GroupPlayerIds,
    IReadOnlySet<int> AvailableZooPlanIds);

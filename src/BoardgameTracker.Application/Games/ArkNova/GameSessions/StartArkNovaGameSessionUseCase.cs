using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Core.Groups;
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
                .Select(player => player.SelectedZooPlanNumber!.Value)
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
                player.SelectedZooPlanNumber.GetValueOrDefault()))
        ];

        await SaveDefaultsAsync(dbContext, command, gameSession, cancellationToken);
        dbContext.GameSessionPlayerColors.AddRange(GameSessionPlayerColor.Create(gameSession,
            command.Players.ToDictionary(player => player.PlayerId, player => player.Color)));

        await dbContext.GameSessions.AddAsync(gameSession, cancellationToken);
        await dbContext.ArkNovaGameSessionPlayers.AddRangeAsync(arkNovaPlayers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return gameSession.Id;
    }

    private static async Task SaveDefaultsAsync(
        BoardgameTrackerDbContext dbContext,
        StartArkNovaGameSessionCommand command,
        GameSession gameSession,
        CancellationToken cancellationToken)
    {
        Group group = await dbContext.Groups.AsNoTracking().Include(group => group.Players)
            .SingleAsync(group => group.Id == command.GroupId, cancellationToken);
        group.ApplyStartingOrder(gameSession);
        if (gameSession.HasStartingOrder)
        {
            // Free the unique order index before applying a permutation.
            await dbContext.GroupPlayers.Where(player => player.GroupId == group.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    player => player.DefaultOrder, player => player.DefaultOrder + 1000), cancellationToken);
            foreach (GroupPlayer player in group.Players)
                await dbContext.GroupPlayers.Where(candidate => candidate.GroupId == group.Id && candidate.PlayerId == player.PlayerId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.DefaultOrder, player.DefaultOrder), cancellationToken);
        }

        // Replacing the optional rows permits full color swaps without invalid temporary colors.
        // Deletion and insertion are protected by the same transaction as the session snapshots.
        IReadOnlyList<GroupGamePlayerColor> colors = GroupGamePlayerColor.Create(group, gameSession.GameId,
            command.Players.ToDictionary(player => player.PlayerId, player => player.Color));
        await dbContext.GroupGamePlayerColors.Where(color => color.GroupId == group.Id && color.GameId == gameSession.GameId)
            .ExecuteDeleteAsync(cancellationToken);
        dbContext.GroupGamePlayerColors.AddRange(colors);
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

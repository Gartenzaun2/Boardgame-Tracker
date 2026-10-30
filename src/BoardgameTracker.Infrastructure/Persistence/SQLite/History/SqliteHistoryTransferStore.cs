using System.Text.Json;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.History;

public sealed class SqliteHistoryTransferStore(IDbContextFactory<BoardgameTrackerDbContext> factory,
    IEnumerable<IGameTransferPersistence> modules) : IHistoryTransferStore
{
    public async Task<TransferSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await ReadSnapshotAsync(db, cancellationToken);
    }
    private async Task<TransferSnapshot> ReadSnapshotAsync(BoardgameTrackerDbContext db, CancellationToken cancellationToken)
    {
        LocalPlayer[] players = await db.Players.AsNoTracking().OrderBy(player => player.Id)
            .Select(player => new LocalPlayer(player.Id, player.Name)).ToArrayAsync(cancellationToken);
        var groups = await db.Groups.AsNoTracking().OrderBy(group => group.Id)
            .Select(group => new { group.Id, group.Name }).ToArrayAsync(cancellationToken);
        var groupPlayers = await db.GroupPlayers.AsNoTracking().OrderBy(player => player.PlayerId)
            .Select(player => new { player.GroupId, player.PlayerId }).ToArrayAsync(cancellationToken);
        Guid[] ids = await db.GameSessions.AsNoTracking().OrderBy(gameSession => gameSession.Id).Select(gameSession => gameSession.Id).ToArrayAsync(cancellationToken);
        Dictionary<string, JsonElement> moduleState = [];
        foreach (IGameTransferPersistence module in modules.OrderBy(module => module.GameKey, StringComparer.Ordinal))
            moduleState[module.GameKey] = await module.ReadSnapshotAsync(db, cancellationToken);
        return new(players, groups.Select(group => new LocalGroup(group.Id, group.Name,
            groupPlayers.Where(player => player.GroupId == group.Id).Select(player => player.PlayerId).ToArray())).ToArray(), ids, moduleState);
    }
    public async Task<BgtFile> ExportAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Guid[] ids = gameSessionIds.Distinct().ToArray();
        var gameSessions = await db.GameSessions.AsNoTracking().Where(gameSession => ids.Contains(gameSession.Id) && gameSession.Status == GameSessionStatus.Completed)
            .Select(gameSession => new { gameSession.Id, gameSession.GameId, gameSession.GroupId, gameSession.StartedAt, gameSession.EndedAt, gameSession.ModifiedAt }).ToArrayAsync(cancellationToken);
        if (gameSessions.Length != ids.Length)
            throw new InvalidOperationException("HistoryChanged");
        Guid[] groupIds = gameSessions.Select(gameSession => gameSession.GroupId).Distinct().ToArray();
        Guid[] gameIds = gameSessions.Select(gameSession => gameSession.GameId).Distinct().ToArray();
        var groups = await db.Groups.AsNoTracking().Where(group => groupIds.Contains(group.Id))
            .Select(group => new { group.Id, group.Name, group.IsTemporary, group.IsArchived }).ToArrayAsync(cancellationToken);
        var groupPlayers = await db.GroupPlayers.AsNoTracking().Where(player => groupIds.Contains(player.GroupId))
            .Select(player => new { player.GroupId, player.PlayerId, player.DefaultOrder }).ToArrayAsync(cancellationToken);
        Guid[] playerIds = groupPlayers.Select(player => player.PlayerId).Distinct().ToArray();
        BgtPlayer[] players = await db.Players.AsNoTracking().Where(player => playerIds.Contains(player.Id))
            .Select(player => new BgtPlayer(player.Id, player.Name, player.IsGuest, player.IsArchived, player.CreatedAt)).ToArrayAsync(cancellationToken);
        var gameData = await db.Games.AsNoTracking().Where(game => gameIds.Contains(game.Id)).Select(game => new { game.Id, game.Key }).ToArrayAsync(cancellationToken);
        var gameSessionPlayers = await db.GameSessionPlayers.AsNoTracking().Where(player => ids.Contains(player.GameSessionId))
            .Select(player => new { player.GameSessionId, player.PlayerId, player.Order, player.Placement }).ToArrayAsync(cancellationToken);
        List<BgtGame> games = [];
        List<BgtGameSession> output = [];
        foreach (var game in gameData)
        {
            IGameTransferPersistence module = modules.Single(module => module.GameKey == game.Key);
            Guid[] selected = gameSessions.Where(gameSession => gameSession.GameId == game.Id).Select(gameSession => gameSession.Id).ToArray();
            games.Add(new(game.Id, game.Key, module.SchemaVersion, await module.ExportReferencesAsync(db, selected, cancellationToken)));
            IReadOnlyDictionary<Guid, System.Text.Json.JsonElement> details = await module.ExportGameSessionsAsync(db, selected, cancellationToken);
            foreach (var gameSession in gameSessions.Where(gameSession => gameSession.GameId == game.Id))
                output.Add(new(gameSession.Id, gameSession.GameId, gameSession.GroupId, gameSession.StartedAt, gameSession.EndedAt!.Value, gameSession.ModifiedAt,
                    gameSessionPlayers.Where(player => player.GameSessionId == gameSession.Id).Select(player => new BgtGameSessionPlayer(player.PlayerId, player.Order, player.Placement!.Value)).ToArray(),
                    details[gameSession.Id]));
        }
        await transaction.CommitAsync(cancellationToken);
        return new(1, appVersion, DateTimeOffset.UtcNow, games.ToArray(), players,
            groups.Select(group => new BgtGroup(group.Id, group.Name, group.IsTemporary, group.IsArchived,
                groupPlayers.Where(player => player.GroupId == group.Id).Select(player => new BgtGroupPlayer(player.PlayerId, player.DefaultOrder)).ToArray())).ToArray(), output.ToArray());
    }
    public async Task<TransferPreview> PreviewAsync(ImportPlan plan, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        int games = 0;
        int existing = 0;
        int added = 0;
        foreach (BgtGame game in plan.Source.Games.Where(game => plan.GameSessions.Any(gameSession => gameSession.GameId == game.Id)))
        {
            if (!await db.Games.AnyAsync(candidate => candidate.Key == game.Key, cancellationToken))
                throw new InvalidDataException("UpdateRequired");
            (int found, int created) = await modules.Single(module => module.GameKey == game.Key).PreviewAsync(db, game,
                plan.GameSessions.Where(gameSession => gameSession.GameId == game.Id).ToArray(), cancellationToken);
            games++;
            existing += found;
            added += created;
        }
        return new(games, existing, added);
    }
    public async Task CommitAsync(ImportPlan plan, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        TransferSnapshot current = await ReadSnapshotAsync(db, cancellationToken);
        if (JsonSerializer.Serialize(current) != JsonSerializer.Serialize(plan.Snapshot))
            throw new InvalidOperationException("ImportStateChanged");
        plan = ImportPlanner.Analyze(plan.Source, current, plan.Resolutions);
        if (!plan.CanCommit)
            throw new InvalidOperationException("UnresolvedImport");
        Dictionary<Guid, Guid> playerMap = plan.Players.ToDictionary(player => player.Imported.Id, player => player.LocalId);
        Dictionary<Guid, Guid> groupMap = plan.Groups.ToDictionary(group => group.Imported.Id, group => group.LocalId);
        foreach (PlannedPlayer planned in plan.Players.Where(player => player.IsNew))
        {
            BgtPlayer input = planned.Imported;
            Player player = new(input.Name, input.IsGuest, input.Id, input.CreatedAt);
            player.SetArchived(input.IsArchived);
            db.Players.Add(player);
        }
        foreach (PlannedGroup planned in plan.Groups.Where(group => group.IsNew))
        {
            Group group = new(planned.Name, planned.Imported.IsTemporary,
                planned.Imported.GroupPlayers.OrderBy(player => player.DefaultOrder).Select(player => playerMap[player.PlayerId]).ToArray(), planned.LocalId);
            group.SetArchived(planned.Imported.IsArchived);
            db.Groups.Add(group);
        }
        Guid[] neededGames = plan.GameSessions.Select(gameSession => gameSession.GameId).Distinct().ToArray();
        foreach (BgtGame game in plan.Source.Games.Where(game => neededGames.Contains(game.Id)))
        {
            Guid localId = await db.Games.Where(candidate => candidate.Key == game.Key).Select(candidate => candidate.Id).SingleAsync(cancellationToken);
            foreach (BgtGameSession input in plan.GameSessions.Where(gameSession => gameSession.GameId == game.Id))
            {
                GameSession gameSession = new(localId, groupMap[input.GroupId], input.StartedAt,
                    input.Players.OrderBy(player => player.Order).Select(player => playerMap[player.PlayerId]).ToArray(), input.Id);
                gameSession.BeginScoring(input.EndedAt);
                foreach (BgtGameSessionPlayer player in input.Players)
                    gameSession.Players.Single(candidate => candidate.PlayerId == playerMap[player.PlayerId]).SetPlacement(player.Placement);
                gameSession.Complete(input.ModifiedAt);
                db.GameSessions.Add(gameSession);
            }
            await modules.Single(module => module.GameKey == game.Key).ImportAsync(db, game,
                plan.GameSessions.Where(gameSession => gameSession.GameId == game.Id).ToArray(), playerMap, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

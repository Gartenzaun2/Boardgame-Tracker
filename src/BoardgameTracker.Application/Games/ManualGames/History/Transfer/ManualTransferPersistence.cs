using System.Text.Json;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames.History.Transfer;

public sealed class ManualTransferPersistence : IGameTransferPersistence
{
    public string GameKey => ManualTransferSchema.ModuleKey;
    public bool CanHandle(string key) => ManualGame.MatchesKey(key);
    public int SchemaVersion => 1;
    private sealed record LocalManualGame(Guid Id, string Key, ManualTransferGame Data);
    private static Task<LocalManualGame[]> ReadGamesAsync(BoardgameTrackerDbContext db, CancellationToken token) =>
        (from game in db.Games.AsNoTracking()
         join manual in db.ManualGames.AsNoTracking() on game.Id equals manual.GameId
         orderby game.Id
         select new LocalManualGame(game.Id, game.Key, new(game.Name, game.BadgeText, game.MinPlayers, game.MaxPlayers,
             manual.CapturePoints, manual.AllowNegativePoints, manual.CaptureWinner, manual.CapturePlacements, manual.AllowTies,
             manual.ResultFromPoints, manual.PointsDirection, manual.CapturePlayerOrder, manual.CapturePlayerColors))).ToArrayAsync(token);

    public async Task<JsonElement> ReadSnapshotAsync(BoardgameTrackerDbContext db, CancellationToken cancellationToken) =>
        JsonSerializer.SerializeToElement(await ReadGamesAsync(db, cancellationToken), BgtFormat.JsonOptions);

    public (PlannedGame[] Games, GameConflict[] Conflicts) PlanGames(BgtGame[] games, TransferSnapshot snapshot, GameResolution[] resolutions)
    {
        LocalManualGame[] local = snapshot.ModuleState?.TryGetValue(GameKey, out JsonElement state) == true
            ? ManualTransferSchema.Read<LocalManualGame[]>(state) : [];
        List<PlannedGame> planned = [];
        List<GameConflict> conflicts = [];
        Guid[] targets = resolutions.Where(decision => decision.LocalId.HasValue).Select(decision => decision.LocalId!.Value).ToArray();
        if (targets.Distinct().Count() != targets.Length)
            throw new InvalidDataException("GameMappingConflict");
        HashSet<Guid> used = targets.ToHashSet();
        HashSet<string> names = new(local.Select(game => game.Data.Name.Trim()), StringComparer.OrdinalIgnoreCase);
        foreach (BgtGame game in games.OrderBy(game => game.Id))
        {
            ManualTransferGame source = ManualTransferSchema.Read<ManualTransferGame>(game.Data);
            LocalManualGame[] candidates = local.Where(candidate => string.Equals(candidate.Data.Name.Trim(), source.Name.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
            GameResolution? decision = resolutions.SingleOrDefault(candidate => candidate.SourceId == game.Id);
            if (decision?.LocalId is Guid localId)
            {
                LocalManualGame match = candidates.SingleOrDefault(candidate => candidate.Id == localId)
                    ?? throw new InvalidDataException("InvalidGameResolution");
                planned.Add(new(game.Id, localId, match.Data.Name, source.Name));
            }
            else if (decision is null && candidates.Length > 0)
                conflicts.Add(new(game.Id, source.Name, candidates.Where(candidate => !used.Contains(candidate.Id)).Select(candidate => new GameCandidate(candidate.Id, candidate.Data.Name)).ToArray()));
            else
            {
                string name = source.Name.Trim();
                int suffix = 1;
                while (names.Contains(name))
                {
                    string tail = "-Importiert" + suffix++;
                    name = source.Name.Trim()[..Math.Min(source.Name.Trim().Length, 100 - tail.Length)] + tail;
                }
                names.Add(name);
                planned.Add(new(game.Id, null, name, source.Name));
            }
        }
        return (planned.ToArray(), conflicts.ToArray());
    }

    public async Task<Game> ResolveGameAsync(BoardgameTrackerDbContext db, BgtGame game, PlannedGame? planned, CancellationToken cancellationToken)
    {
        if (planned is null)
            throw new InvalidDataException("UnresolvedGame");
        if (planned.LocalId is Guid id)
            return await db.Games.SingleAsync(candidate => candidate.Id == id, cancellationToken);
        ManualTransferGame data = ManualTransferSchema.Read<ManualTransferGame>(game.Data);
        Game created = new(ManualGame.CreateKey(), planned.Name, data.BadgeText, data.MinPlayers, data.MaxPlayers);
        db.AddRange(created, new ManualGame(created, data.Options));
        return created;
    }

    public async Task<JsonElement> ExportReferencesAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken)
    {
        Guid id = await db.GameSessions.Where(session => gameSessionIds.Contains(session.Id)).Select(session => session.GameId).Distinct().SingleAsync(cancellationToken);
        LocalManualGame game = (await ReadGamesAsync(db, cancellationToken)).Single(game => game.Id == id);
        return JsonSerializer.SerializeToElement(game.Data, BgtFormat.JsonOptions);
    }

    public async Task<IReadOnlyDictionary<Guid, JsonElement>> ExportGameSessionsAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken)
    {
        var sessions = await db.ManualGameSessions.AsNoTracking().Where(session => gameSessionIds.Contains(session.GameSessionId)).ToArrayAsync(cancellationToken);
        var players = await (from participant in db.GameSessionPlayers.AsNoTracking()
            join points in db.ManualGameSessionPlayers.AsNoTracking() on new { participant.GameSessionId, participant.PlayerId } equals new { points.GameSessionId, points.PlayerId } into pointRows
            from points in pointRows.DefaultIfEmpty()
            join color in db.GameSessionPlayerColors.AsNoTracking() on new { participant.GameSessionId, participant.PlayerId } equals new { color.GameSessionId, color.PlayerId } into colorRows
            from color in colorRows.DefaultIfEmpty()
            where gameSessionIds.Contains(participant.GameSessionId)
            select new { participant.GameSessionId, participant.PlayerId, participant.Placement, Points = points == null ? (int?)null : points.Points, Color = color == null ? (PlayerColor?)null : color.Color }).ToArrayAsync(cancellationToken);
        return sessions.ToDictionary(session => session.GameSessionId, session =>
        {
            var rows = players.Where(player => player.GameSessionId == session.GameSessionId).ToArray();
            ManualHistoricalResult form = ManualHistoricalResult.FromValues(rows.Select(player => new ManualScoreInput(player.PlayerId, player.Points, player.Placement)).ToArray(), session.ScoringRulesSnapshot);
            return JsonSerializer.SerializeToElement(new ManualTransferSession(ManualTransferSchema.Kind(form.Placements), rows.Select(player => new ManualTransferPlayer(player.PlayerId, player.Points, player.Color)).ToArray()), BgtFormat.JsonOptions);
        });
    }

    public Task<(int Existing, int New)> PreviewAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions, CancellationToken cancellationToken) => Task.FromResult((0, 0));

    public Task ImportAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions, IReadOnlyDictionary<Guid, Guid> players, CancellationToken cancellationToken)
    {
        foreach (BgtGameSession session in gameSessions)
        {
            ManualTransferSession data = ManualTransferSchema.Read<ManualTransferSession>(session.Data);
            db.ManualGameSessions.Add(ManualGameSession.FromImportedResult(session.Id, ManualTransferSchema.ImportedSnapshot(data, session.Players)));
            foreach (ManualTransferPlayer player in data.Players)
            {
                if (player.Points.HasValue)
                    db.ManualGameSessionPlayers.Add(new(session.Id, players[player.PlayerId], player.Points));
                if (player.Color is PlayerColor color)
                    db.GameSessionPlayerColors.Add(new(session.Id, players[player.PlayerId], color));
            }
        }
        return Task.CompletedTask;
    }
}

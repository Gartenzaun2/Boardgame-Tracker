using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames.GameSessions;

public sealed record ManualScoringData(ManualRunningData Session, IReadOnlyList<ManualScoreInput> Values);

public sealed class ManualScoringQuery(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<ManualScoringData?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        ManualRunningData? session = await ManualRunningGameSessionQuery.ReadAsync(db, sessionId, cancellationToken);
        if (session is null)
            return null;
        var values = await (from participant in db.GameSessionPlayers.AsNoTracking()
                            join detail in db.ManualGameSessionPlayers.AsNoTracking()
                                on new { participant.GameSessionId, participant.PlayerId } equals new { detail.GameSessionId, detail.PlayerId } into details
                            from detail in details.DefaultIfEmpty()
                            where participant.GameSessionId == sessionId
                            select new ManualScoreInput(participant.PlayerId, detail == null ? (int?)null : detail.Points, participant.Placement))
            .ToArrayAsync(cancellationToken);
        return new(session, session.Players.Select(player => values.Single(value => value.PlayerId == player.PlayerId)).ToArray());
    }
}

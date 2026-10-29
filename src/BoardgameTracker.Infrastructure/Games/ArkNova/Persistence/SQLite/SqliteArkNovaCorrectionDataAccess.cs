using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using Microsoft.EntityFrameworkCore;
namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;

public sealed class SqliteArkNovaCorrectionDataAccess(IDbContextFactory<BoardgameTrackerDbContext> factory) : IArkNovaCorrectionDataAccess
{
    public async Task SaveAsync(ArkNovaScoringGameSessionData data, IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.GameSessions.Include(s => s.Players).SingleAsync(s => s.Id == data.GameSessionId, cancellationToken);
        if (session.Status != GameSessionStatus.Completed || !await db.Games.AnyAsync(g => g.Id == session.GameId && g.Key == ArkNovaGame.Key, cancellationToken))
            throw new InvalidOperationException("Only completed Ark Nova sessions can be corrected.");
        if (!session.Players.Select(p => p.PlayerId).ToHashSet().SetEquals(data.Players.Select(p => p.PlayerId)))
            throw new InvalidOperationException("Participants cannot change.");
        var players = await db.ArkNovaGameSessionPlayers.Where(p => p.GameSessionId == session.Id).ToListAsync(cancellationToken);
        foreach (var player in players)
        {
            var values = data.Players.Single(p => p.PlayerId == player.PlayerId);
            player.SetScoreValues(values.Attractiveness, values.Conservation, values.Reputation);
            session.Players.Single(p => p.PlayerId == player.PlayerId).SetPlacement(placements[player.PlayerId]);
        }
        ArkNovaGameSessionSpecialCardAssignment[] existingCards = await db.ArkNovaGameSessionSpecialCardAssignments.AsNoTracking()
            .Where(c => c.GameSessionId == session.Id).ToArrayAsync(cancellationToken);
        foreach (ArkNovaSpecialCardAssignmentData assignment in data.SpecialCards)
        {
            bool active = await db.ArkNovaSpecialCards.AnyAsync(c => c.Id == assignment.SpecialCardId && !c.IsArchived, cancellationToken);
            bool retained = existingCards.Any(c => c.PlayerId == assignment.PlayerId && c.SpecialCardId == assignment.SpecialCardId);
            if (!active && !retained)
                throw new InvalidOperationException("Archived cards cannot be newly assigned.");
        }
        await db.ArkNovaGameSessionSpecialCardAssignments.Where(c => c.GameSessionId == session.Id).ExecuteDeleteAsync(cancellationToken);
        db.ArkNovaGameSessionSpecialCardAssignments.AddRange(data.SpecialCards.Select(c => new ArkNovaGameSessionSpecialCardAssignment(session.Id, c.PlayerId, c.SpecialCardId)));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    public async Task DeleteAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var session = await db.GameSessions.SingleAsync(s => s.Id == gameSessionId, cancellationToken);
        if (session.Status != GameSessionStatus.Completed || !await db.Games.AnyAsync(g => g.Id == session.GameId && g.Key == ArkNovaGame.Key, cancellationToken)) throw new InvalidOperationException("Only completed sessions can be deleted here.");
        db.GameSessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
    }
}

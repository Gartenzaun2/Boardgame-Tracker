using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames.GameSessions;

public sealed class ManualHistoryCorrectionUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task SaveCorrectionAsync(Guid gameSessionId, IReadOnlyList<ManualScoreInput> values, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var context = await ManualScoringUseCase.LoadAsync(db, gameSessionId, cancellationToken);
        string key = await db.Games.Where(game => game.Id == context.Session.GameId).Select(game => game.Key).SingleAsync(cancellationToken);
        if (context.Session.Status != GameSessionStatus.Completed || !ManualGame.MatchesKey(key))
            throw new InvalidOperationException("Only completed ManualGames sessions can be corrected.");
        ManualHistoricalResult form = ManualHistoricalResult.FromValues(ManualScoringUseCase.ReadValues(context), context.Rules);
        if (values.Count != context.Session.Players.Count
            || !values.Select(player => player.PlayerId).ToHashSet().SetEquals(context.Session.Players.Select(player => player.PlayerId)))
            throw new ArgumentException("Participants cannot change.", nameof(values));
        form.ValidateCorrection(values, context.Rules);
        foreach (ManualScoreInput value in values)
        {
            if (form.HasPoints)
                context.Points[value.PlayerId].SetPoints(value.Points);
            if (form.Placements != ManualHistoricalPlacements.None)
                context.Session.Players.Single(player => player.PlayerId == value.PlayerId).SetPlacement(value.Placement);
        }
        context.Session.RecordHistoricalCorrection(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

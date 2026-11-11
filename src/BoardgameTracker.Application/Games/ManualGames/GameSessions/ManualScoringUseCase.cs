using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames.GameSessions;

public sealed class ManualScoringUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task SaveDraftAsync(Guid id, IReadOnlyList<ManualScoreInput> values, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var context = await LoadAsync(db, id, cancellationToken);
        if (context.Session.Status != GameSessionStatus.Scoring)
            throw new InvalidOperationException("Only scoring sessions accept draft changes.");
        ApplyDraft(db, context, values);
        context.Session.RecordScoringUpdate(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    internal sealed record Context(GameSession Session, ScoringRulesSnapshot Rules, Dictionary<Guid, ManualGameSessionPlayer> Points);
    internal static async Task<Context> LoadAsync(BoardgameTrackerDbContext db, Guid id, CancellationToken token)
    {
        ManualGameSession detail = await db.ManualGameSessions.SingleOrDefaultAsync(session => session.GameSessionId == id, token)
            ?? throw new InvalidOperationException("Manual session not found.");
        GameSession session = await db.GameSessions.Include(session => session.Players).SingleAsync(session => session.Id == id, token);
        Dictionary<Guid, ManualGameSessionPlayer> points = await db.ManualGameSessionPlayers.Where(player => player.GameSessionId == id)
            .ToDictionaryAsync(player => player.PlayerId, token);
        return new(session, detail.ScoringRulesSnapshot, points);
    }
    internal static void ApplyDraft(BoardgameTrackerDbContext db, Context context, IReadOnlyList<ManualScoreInput> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Count != context.Session.Players.Count
            || !input.Select(player => player.PlayerId).ToHashSet().SetEquals(context.Session.Players.Select(player => player.PlayerId)))
            throw new ArgumentException("The draft must contain exactly the persisted participants.", nameof(input));
        bool changedPoints = input.Any(player => player.Points != context.Points.GetValueOrDefault(player.PlayerId)?.Points);
        ManualScoreInput[] values = context.Rules.ResultFromPoints && changedPoints
            ? input.Select(player => player with { Placement = null }).ToArray() : input.ToArray();
        ManualScoringRules.ValidateDraft(context.Rules, values);
        foreach (ManualScoreInput value in values)
        {
            context.Session.Players.Single(player => player.PlayerId == value.PlayerId).SetPlacement(value.Placement);
            if (context.Points.TryGetValue(value.PlayerId, out ManualGameSessionPlayer? points))
                points.SetPoints(value.Points);
            else if (value.Points.HasValue)
            {
                points = new(context.Session.Id, value.PlayerId, value.Points);
                db.ManualGameSessionPlayers.Add(points);
                context.Points.Add(value.PlayerId, points);
            }
        }
    }
    internal static ManualScoreInput[] ReadValues(Context context) => context.Session.Players.Select(player =>
        new ManualScoreInput(player.PlayerId, context.Points.GetValueOrDefault(player.PlayerId)?.Points, player.Placement)).ToArray();
}

public sealed class CompleteManualGameSessionUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task CompleteAsync(Guid id, IReadOnlyList<ManualScoreInput>? values = null, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var context = await ManualScoringUseCase.LoadAsync(db, id, cancellationToken);
        // Retried completion never writes over the completed historical result.
        if (context.Session.Status == GameSessionStatus.Completed)
            return;
        if (context.Session.Status != GameSessionStatus.Scoring)
            throw new InvalidOperationException("Only scoring sessions can be completed.");
        if (values is not null)
            ManualScoringUseCase.ApplyDraft(db, context, values);
        IReadOnlyDictionary<Guid, int?> placements = ManualScoringRules.ResolveFinal(context.Rules, ManualScoringUseCase.ReadValues(context));
        foreach (GameSessionPlayer player in context.Session.Players)
            player.SetPlacement(placements[player.PlayerId]);
        context.Session.Complete(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

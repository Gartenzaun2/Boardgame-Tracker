using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames.GameSessions;

public sealed class StartManualGameSessionUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<Guid> StartAsync(StartManualGameSessionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Players);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ManualPreparationContext context = await ManualPreparationQuery.LoadContextAsync(db, command.GameKey, command.GroupId, cancellationToken);
        ManualGame config = context.Configuration;
        Guid[] ids = command.Players.Select(player => player.PlayerId).ToArray();
        if (ids.Length != context.Group.Players.Count || ids.Distinct().Count() != ids.Length
            || !ids.ToHashSet().SetEquals(context.Group.Players.Select(player => player.PlayerId)))
            throw new InvalidOperationException("Session participants must exactly match the complete group membership.");
        GameSession.ValidateStartingOrder(command.Players.Select(player => player.Order).ToArray());
        if (command.Players.Any(player => player.Order.HasValue != config.CapturePlayerOrder))
            throw new InvalidOperationException("Starting order no longer matches the saved configuration.");
        if (command.Players.Any(player => player.Color.HasValue != config.CapturePlayerColors))
            throw new InvalidOperationException("Player colors no longer match the saved configuration.");
        Guid[] orderedIds = config.CapturePlayerOrder
            ? command.Players.OrderBy(player => player.Order).Select(player => player.PlayerId).ToArray() : ids;
        GameSession session = new(context.Game.Id, context.Group.Id, command.StartedAt, orderedIds, recordStartingOrder: config.CapturePlayerOrder);
        db.GameSessions.Add(session);
        db.ManualGameSessions.Add(new(session.Id, config));
        if (config.CapturePlayerOrder)
        {
            context.Group.ApplyStartingOrder(session);
            // Free the unique order index before applying a permutation, within the same transaction.
            await db.GroupPlayers.Where(player => player.GroupId == context.Group.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(player => player.DefaultOrder, player => player.DefaultOrder + 1000), cancellationToken);
            foreach (GroupPlayer player in context.Group.Players)
                await db.GroupPlayers.Where(candidate => candidate.GroupId == context.Group.Id && candidate.PlayerId == player.PlayerId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.DefaultOrder, player.DefaultOrder), cancellationToken);
        }
        if (config.CapturePlayerColors)
        {
            Dictionary<Guid, PlayerColor> colors = command.Players.ToDictionary(player => player.PlayerId, player => player.Color!.Value);
            db.GameSessionPlayerColors.AddRange(GameSessionPlayerColor.Create(session, colors));
            IReadOnlyList<GroupGamePlayerColor> defaults = GroupGamePlayerColor.Create(context.Group, context.Game.Id, colors);
            await db.GroupGamePlayerColors.Where(color => color.GroupId == context.Group.Id && color.GameId == context.Game.Id)
                .ExecuteDeleteAsync(cancellationToken);
            db.GroupGamePlayerColors.AddRange(defaults);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session.Id;
    }
}

public sealed record StartManualGameSessionCommand(string GameKey, Guid GroupId, DateTimeOffset StartedAt, IReadOnlyList<ManualStartingPlayer> Players);
public sealed record ManualStartingPlayer(Guid PlayerId, int? Order, PlayerColor? Color);

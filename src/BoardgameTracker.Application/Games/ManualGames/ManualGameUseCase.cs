using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames;

public sealed class ManualGameUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory, IGameActivationStore visibility)
{
    public async Task<Guid> SaveAsync(ManualGameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.BadgeText);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        string name = command.Name.Trim();
        if (await db.Games.AnyAsync(game => game.Id != command.GameId && game.Name == name
            && db.ManualGames.Any(manual => manual.GameId == game.Id), cancellationToken))
            throw new ManualGameValidationException(ManualGameError.DuplicateName);

        if (command.GameId is Guid id)
        {
            ManualGame manual = await db.ManualGames.SingleOrDefaultAsync(game => game.GameId == id, cancellationToken)
                ?? throw new ManualGameValidationException(ManualGameError.NotFound);
            Game game = await db.Games.SingleAsync(game => game.Id == id, cancellationToken);
            if (command.MaxPlayers != game.MaxPlayers)
                throw new ManualGameValidationException(ManualGameError.MaximumLocked);
            if (await db.GroupGames.AnyAsync(assignment => assignment.GameId == id
                && db.GroupPlayers.Count(player => player.GroupId == assignment.GroupId) < command.MinPlayers, cancellationToken))
                throw new ManualGameValidationException(ManualGameError.AssignedGroups);
            manual.UpdateMetadata(game, name, command.BadgeText, command.MinPlayers, command.MaxPlayers);
            manual.SetConfiguration(command.Options);
        }
        else
        {
            Game game = new(ManualGame.CreateKey(), name, command.BadgeText, command.MinPlayers, command.MaxPlayers);
            ManualGame manual = new(game, command.Options);
            db.AddRange(game, manual);
            command = command with { GameId = game.Id };
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return command.GameId!.Value;
    }

    public async Task<ManualGameDetails?> GetAsync(string gameKey, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await (from manual in db.ManualGames.AsNoTracking()
                      join game in db.Games.AsNoTracking() on manual.GameId equals game.Id
                      where game.Key == gameKey
                      select new ManualGameDetails(game.Id, game.Key, game.Name, game.BadgeText, game.MinPlayers, game.MaxPlayers,
                          new ManualGameOptions(manual.CapturePoints, manual.AllowNegativePoints, manual.CaptureWinner,
                              manual.CapturePlacements, manual.AllowTies, manual.ResultFromPoints, manual.PointsDirection,
                              manual.CapturePlayerOrder, manual.CapturePlayerColors),
                          db.GameSessions.Any(session => session.GameId == game.Id))).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await db.ManualGames.AnyAsync(game => game.GameId == gameId, cancellationToken))
            throw new ManualGameValidationException(ManualGameError.NotFound);
        Game game = await db.Games.SingleAsync(game => game.Id == gameId, cancellationToken);
        if (await db.GameSessions.AnyAsync(session => session.GameId == gameId, cancellationToken))
            throw new ManualGameValidationException(ManualGameError.HasSessions);
        await db.GroupGamePlayerColors.Where(color => color.GameId == gameId).ExecuteDeleteAsync(cancellationToken);
        await db.GroupGames.Where(assignment => assignment.GameId == gameId).ExecuteDeleteAsync(cancellationToken);
        db.Games.Remove(game);
        await db.SaveChangesAsync(cancellationToken);
        bool wasEnabled = visibility.IsEnabled(game.Key);
        // Host preferences cannot enlist in SQLite. Restore visibility on normal write/commit failures.
        try
        {
            visibility.SetEnabled(game.Key, true);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            try { visibility.SetEnabled(game.Key, wasEnabled); }
            catch (Exception restoreFailure)
            {
                throw new AggregateException("Deletion failed and visibility could not be restored.", exception, restoreFailure);
            }
            throw;
        }
    }
}

public sealed record ManualGameCommand(Guid? GameId, string Name, string BadgeText, int MinPlayers, int MaxPlayers, ManualGameOptions Options);
public sealed record ManualGameDetails(Guid Id, string Key, string Name, string BadgeText, int MinPlayers, int MaxPlayers,
    ManualGameOptions Options, bool HasSessions);
public enum ManualGameError { DuplicateName, MaximumLocked, AssignedGroups, HasSessions, NotFound }
public sealed class ManualGameValidationException(ManualGameError error) : InvalidOperationException(error.ToString())
{
    public ManualGameError Error { get; } = error;
}

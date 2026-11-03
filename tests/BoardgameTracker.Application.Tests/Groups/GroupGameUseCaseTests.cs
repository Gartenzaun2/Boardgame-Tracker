using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Groups;

public sealed class GroupGameUseCaseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Add_IsIdempotentAndSupportsMultipleGames()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Game other = new("other");
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            seed.Games.Add(other);
            await seed.SaveChangesAsync(Token);
        }
        AddGroupGameUseCase useCase = new(database);
        await useCase.AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        await useCase.AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        await useCase.AddGroupGameAsync(database.Group.Id, other.Id, Token);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Equal(2, await verify.GroupGames.CountAsync(Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Add_RejectsMissingReferencesWithoutChanges(bool missingGroup)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AddGroupGameUseCase useCase = new(database);
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.AddGroupGameAsync(
            missingGroup ? Guid.NewGuid() : database.Group.Id,
            missingGroup ? database.GameId : Guid.NewGuid(), Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData(GameSessionStatus.Running)]
    [InlineData(GameSessionStatus.Scoring)]
    [InlineData(GameSessionStatus.Completed)]
    public async Task Remove_ProtectsOpenSessionsAndPreservesHistory(GameSessionStatus status)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await new AddGroupGameUseCase(database).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        GameSession session = await AddSessionAsync(database, database.GameId, status);
        RemoveGroupGameUseCase useCase = new(database);
        if (status == GameSessionStatus.Completed)
            await useCase.RemoveGroupGameAsync(database.Group.Id, database.GameId, Token);
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.RemoveGroupGameAsync(database.Group.Id, database.GameId, Token));

        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Equal(status != GameSessionStatus.Completed, await verify.GroupGames.AnyAsync(Token));
        GameSession persisted = await verify.GameSessions.Include(candidate => candidate.Players).SingleAsync(Token);
        Assert.Equal(session.Id, persisted.Id);
        Assert.Equal(session.GameId, persisted.GameId);
        Assert.Equal(session.GroupId, persisted.GroupId);
        Assert.Equal(session.Status, persisted.Status);
        Assert.Equal(session.StartedAt, persisted.StartedAt);
        Assert.Equal(session.EndedAt, persisted.EndedAt);
        Assert.Equal(session.ModifiedAt, persisted.ModifiedAt);
        Assert.Equal(session.Players.OrderBy(player => player.Order).Select(player => (player.PlayerId, player.Order, player.Placement)),
            persisted.Players.OrderBy(player => player.Order).Select(player => (player.PlayerId, player.Order, player.Placement)));
    }

    [Fact]
    public async Task Remove_UsesExactPairAndMissingAssignmentDoesNotChangeOtherData()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Game other = new("other");
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            seed.Games.Add(other);
            seed.GroupGames.Add(new(database.Group.Id, other.Id));
            await seed.SaveChangesAsync(Token);
        }
        await database.AddSessionAsync();
        RemoveGroupGameUseCase useCase = new(database);
        await useCase.RemoveGroupGameAsync(database.Group.Id, Guid.NewGuid(), Token);
        await using (BoardgameTrackerDbContext verify = database.CreateDbContext())
            Assert.Single(await verify.GroupGames.ToArrayAsync(Token));
        await useCase.RemoveGroupGameAsync(database.Group.Id, other.Id, Token);
        await using BoardgameTrackerDbContext final = database.CreateDbContext();
        Assert.Empty(await final.GroupGames.ToArrayAsync(Token));
        Assert.Single(await final.GameSessions.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Persistence_RejectsDuplicateCompositeKey()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await new AddGroupGameUseCase(database).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        db.GroupGames.Add(new(database.Group.Id, database.GameId));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Persistence_EnforcesForeignKeys(bool missingGroup)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        db.GroupGames.Add(new(missingGroup ? Guid.NewGuid() : database.Group.Id,
            missingGroup ? database.GameId : Guid.NewGuid()));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Token));
    }

    [Theory]
    [InlineData(GameSessionStatus.Running)]
    [InlineData(GameSessionStatus.Scoring)]
    [InlineData(GameSessionStatus.Completed)]
    public async Task Archive_ChecksOtherGamesAtSaveAndKeepsChangesAtomic(GameSessionStatus status)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        EditGroupUseCase useCase = new(database, new([]), []);
        // Simulate an editor loaded before a session was started in another game.
        EditGroupDetails details = (await useCase.GetAsync(database.Group.Id, cancellationToken: Token))!;
        Game other = new("other");
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            seed.Games.Add(other);
            seed.GroupGames.Add(new(database.Group.Id, other.Id));
            await seed.SaveChangesAsync(Token);
        }
        await AddSessionAsync(database, other.Id, status);
        EditGroupCommand command = new(details.Id, "Changed", true, [database.Second.Id, database.First.Id]);
        if (status == GameSessionStatus.Completed)
            await useCase.UpdateGroupAsync(command, Token);
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.UpdateGroupAsync(command, Token));

        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Group group = await verify.Groups.Include(candidate => candidate.Players).SingleAsync(Token);
        bool allowed = status == GameSessionStatus.Completed;
        Assert.Equal(allowed ? "Changed" : details.Name, group.Name);
        Assert.Equal(allowed, group.IsArchived);
        Assert.Equal(allowed ? 2 : 1, group.Players.Single(player => player.PlayerId == database.First.Id).DefaultOrder);
        Assert.Equal(allowed ? 1 : 2, group.Players.Single(player => player.PlayerId == database.Second.Id).DefaultOrder);
    }

    private static async Task<GameSession> AddSessionAsync(SliceDatabase database, Guid gameId, GameSessionStatus status)
    {
        GameSession session = new(gameId, database.Group.Id, DateTimeOffset.UtcNow.AddHours(-2), [database.First.Id, database.Second.Id]);
        if (status != GameSessionStatus.Running)
            session.BeginScoring(session.StartedAt.AddHours(1));
        if (status == GameSessionStatus.Completed)
            session.Complete(session.StartedAt.AddHours(1));
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(Token);
        return session;
    }
}

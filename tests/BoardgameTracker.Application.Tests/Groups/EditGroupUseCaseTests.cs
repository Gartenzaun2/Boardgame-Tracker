using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Application.Tests.Groups;

public sealed class EditGroupUseCaseTests
{
    [Fact]
    public async Task UpdateGroupAsync_ChangesNameOrderAndArchiveStateWithoutChangingPlayership()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Alt", false, [first, second]);
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            foreach (var member in group.Players)
                seed.Players.Add(new Player(member.PlayerId.ToString(), false, member.PlayerId));
            seed.Groups.Add(group);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        EditGroupUseCase useCase = new(database, new([]), []);

        await useCase.UpdateGroupAsync(new EditGroupCommand(
            group.Id, "Neu", false, true, [second, first]), TestContext.Current.CancellationToken);

        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        group = await db.Groups.Include(g => g.Players).SingleAsync(g => g.Id == group.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Neu", group.Name);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.False((await verify.Groups.SingleAsync(g => g.Id == group.Id, TestContext.Current.CancellationToken)).IsTemporary);
        Assert.True(group.IsArchived);
        Assert.Equal(2, group.Players.Count);
        Assert.Equal(1, group.Players.Single(player => player.PlayerId == second).DefaultOrder);
        Assert.Equal(2, group.Players.Single(player => player.PlayerId == first).DefaultOrder);
    }

    [Fact]
    public async Task UpdateGroupAsync_CanMakeTemporaryGroupPermanent()
    {
        Group group = new("Einmalig", true, [Guid.NewGuid()]);
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            foreach (var member in group.Players)
                seed.Players.Add(new Player(member.PlayerId.ToString(), false, member.PlayerId));
            seed.Groups.Add(group);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        EditGroupUseCase useCase = new(database, new([]), []);

        await useCase.UpdateGroupAsync(new EditGroupCommand(
            group.Id, group.Name, true, false, [group.Players.Single().PlayerId]), TestContext.Current.CancellationToken);

        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.False((await verify.Groups.SingleAsync(g => g.Id == group.Id, TestContext.Current.CancellationToken)).IsTemporary);
    }

    [Fact]
    public async Task UpdateGroupAsync_DoesNotMakePermanentGroupTemporary()
    {
        Group group = new("Stammgruppe", false, [Guid.NewGuid()]);
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            foreach (var member in group.Players)
                seed.Players.Add(new Player(member.PlayerId.ToString(), false, member.PlayerId));
            seed.Groups.Add(group);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        EditGroupUseCase useCase = new(database, new([]), []);

        await useCase.UpdateGroupAsync(new EditGroupCommand(
            group.Id, group.Name, false, false, [group.Players.Single().PlayerId]), TestContext.Current.CancellationToken);

        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.False((await verify.Groups.SingleAsync(g => g.Id == group.Id, TestContext.Current.CancellationToken)).IsTemporary);
    }

    [Fact]
    public async Task UpdateGroupAsync_RejectsOrderThatChangesPlayership()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            foreach (var member in group.Players)
                seed.Players.Add(new Player(member.PlayerId.ToString(), false, member.PlayerId));
            seed.Groups.Add(group);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        EditGroupUseCase useCase = new(database, new([]), []);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.UpdateGroupAsync(new EditGroupCommand(
                group.Id, group.Name, false, false, [first, Guid.NewGuid()]), TestContext.Current.CancellationToken));
    }
}

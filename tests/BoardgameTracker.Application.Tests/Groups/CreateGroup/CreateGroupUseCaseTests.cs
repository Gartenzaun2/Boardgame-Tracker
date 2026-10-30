using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Tests.Groups.CreateGroup;

public sealed class CreateGroupUseCaseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateGroupAsync_CreatesGroupWithExistingAndNewPlayersInRequestedOrder()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Guid existingPlayerId = database.First.Id;

        CreateGroupUseCase useCase = new(database, new([]), []);

        Guid groupId = await useCase.CreateGroupAsync(new CreateGroupCommand(
            "Freitagabend",
            false,
            [
                new CreateGroupPlayerInput(existingPlayerId, "Anna", false),
                new CreateGroupPlayerInput(null, "Gast", true)
            ]), Token);

        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        Group savedGroup = await db.Groups.Include(group => group.Players).SingleAsync(group => group.Id == groupId, Token);
        Player guest = await db.Players.SingleAsync(player => player.IsGuest, Token);
        Assert.NotEqual(Guid.Empty, groupId);
        Assert.True(guest.IsGuest);
        Assert.Equal(3, await db.Players.CountAsync(Token));
        Assert.Collection(
            savedGroup.Players.OrderBy(player => player.DefaultOrder),
            first => Assert.Equal(existingPlayerId, first.PlayerId),
            second => Assert.Equal(guest.Id, second.PlayerId));
    }

    [Fact]
    public async Task CreateGroupAsync_RejectsInactiveOrMissingExistingPlayer()
    {
        Guid playerId = Guid.NewGuid();
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        CreateGroupUseCase useCase = new(database, new([]), []);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.CreateGroupAsync(new CreateGroupCommand(
                "Freitagabend",
                false,
                [new CreateGroupPlayerInput(playerId, "Anna", false)]), Token));
    }
}

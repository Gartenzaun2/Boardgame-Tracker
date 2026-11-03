using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class ArkNovaPreparationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static ArkNovaPreparationViewModel Create(SliceDatabase db) =>
        new(new ArkNovaPreparationQuery(db), new StartArkNovaGameSessionUseCase(db), NullLogger<ArkNovaPreparationViewModel>.Instance);

    [Fact]
    public async Task Load_UsesGlobalOrderAndDefaultsDespitePreviousLastPlaceAndSessionColors()
    {
        await using var database = await SliceDatabase.CreateAsync();
        var command = new StartArkNovaGameSessionCommand(database.Group.Id, DateTimeOffset.UtcNow.AddHours(-2),
            [new(database.First.Id, PlayerColor.Red, 1, 2, 1), new(database.Second.Id, PlayerColor.Black, 3, 4, 3)]);
        await new StartArkNovaGameSessionUseCase(database).StartAsync(command, Token);
        await using (var db = database.CreateDbContext())
        {
            var session = await db.GameSessions.Include(session => session.Players).SingleAsync(Token);
            session.Players.Single(player => player.PlayerId == database.First.Id).SetPlacement(1);
            session.Players.Single(player => player.PlayerId == database.Second.Id).SetPlacement(2);
            session.BeginScoring(DateTimeOffset.UtcNow.AddHours(-1));
            session.Complete(DateTimeOffset.UtcNow);
            var colors = await db.ArkNovaGroupPlayers.ToArrayAsync(Token);
            colors.Single(player => player.PlayerId == database.First.Id).SetDefaultColor(PlayerColor.Yellow);
            db.ArkNovaGroupPlayers.Remove(colors.Single(player => player.PlayerId == database.Second.Id));
            await db.SaveChangesAsync(Token);
        }
        var vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        Assert.False(vm.HasError);
        Assert.Equal(new[] { database.First.Id, database.Second.Id }, vm.Players.Select(player => player.PlayerId));
        Assert.Equal(new[] { PlayerColor.Yellow, PlayerColor.Blue }, vm.Players.Select(player => player.Color));
        Assert.Contains(database.Second.Name, vm.LastGameSessionText);
        Assert.False(vm.CanStart);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Load_FillsMissingOrInvalidColorWithoutTakingLaterValidDefault(bool invalid)
    {
        await using var database = await SliceDatabase.CreateAsync();
        await using (var db = database.CreateDbContext())
        {
            db.ArkNovaGroupPlayers.Add(new(database.Group.Id, database.Second.Id, PlayerColor.Blue));
            if (invalid)
            {
                db.ArkNovaGroupPlayers.Add(new(database.Group.Id, database.First.Id, PlayerColor.Red));
                await db.SaveChangesAsync(Token);
                await db.Database.ExecuteSqlRawAsync("UPDATE ArkNovaGroupPlayers SET DefaultColor = 5 WHERE PlayerId = {0}", [database.First.Id], Token);
            }
            else
                await db.SaveChangesAsync(Token);
        }
        var vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        Assert.False(vm.HasError);
        Assert.Equal(new[] { PlayerColor.Yellow, PlayerColor.Blue }, vm.Players.Select(player => player.Color));
    }

    [Fact]
    public async Task Draft_ValidatesPlansColorsAndPaletteAndCancelWritesNothing()
    {
        await using var database = await SliceDatabase.CreateAsync();
        var vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        vm.RotateOrder();
        vm.DrawZooPlans();
        Assert.False(vm.CanStart);
        vm.SelectZooPlan(vm.Players[0], 1);
        Assert.False(vm.CanStart);
        vm.SelectZooPlan(vm.Players[1], 2);
        Assert.True(vm.CanStart);
        vm.SetColor(vm.Players[0], vm.Players[1].Color);
        Assert.False(vm.CanStart);
        Assert.Null(await vm.StartAsync(Token));
        vm.SetColor(vm.Players[0], PlayerColor.Green);
        Assert.False(vm.CanStart);
        Assert.Null(await vm.StartAsync(Token));
        vm.SetColor(vm.Players[0], PlayerColor.Black);
        Assert.True(vm.CanStart);
        Assert.False(vm.Players[0].CanChooseBlack);
        Assert.True(vm.Players[0].CanChooseBlue);
        Assert.True(vm.Players[0].CanChooseYellow);
        Assert.True(vm.Players[0].CanChooseRed);

        // Leaving the draft and loading a fresh one never calls the start use case.
        var reopened = Create(database);
        await reopened.LoadAsync(database.Group.Id, Token);
        Assert.Equal(new[] { database.First.Id, database.Second.Id }, reopened.Players.Select(player => player.PlayerId));
        Assert.Equal(new[] { PlayerColor.Blue, PlayerColor.Yellow }, reopened.Players.Select(player => player.Color));
        Assert.All(reopened.Players, player => Assert.False(player.HasZooPlans));
        await using var db = database.CreateDbContext();
        Assert.Empty(await db.ArkNovaGroupPlayers.ToArrayAsync(Token));
        Assert.Empty(await db.GameSessions.ToArrayAsync(Token));
        Assert.Equal(new[] { database.First.Id, database.Second.Id }, await db.GroupPlayers.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId).ToArrayAsync(Token));
    }

    [Fact]
    public async Task ManualOrderActions_UpdatePositionsAndRandomStartKeepsCyclicOrder()
    {
        await using var database = await SliceDatabase.CreateAsync();
        var vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        vm.Players.Add(new(Guid.NewGuid(), "Clara", 3, PlayerColor.Red));
        vm.Players.Add(new(Guid.NewGuid(), "David", 4, PlayerColor.Black));
        var original = vm.Players.ToArray();
        vm.RotateOrder();
        Assert.Equal(new[] { original[1], original[2], original[3], original[0] }, vm.Players);
        vm.MovePlayer(original[0], -1);
        Assert.Equal(new[] { original[1], original[2], original[0], original[3] }, vm.Players);
        vm.MovePlayer(original[1], 1);
        var beforeRandom = vm.Players.ToArray();
        vm.RotateToRandomStartingPlayer();
        int start = Array.IndexOf(beforeRandom, vm.Players[0]);
        Assert.Equal(Enumerable.Range(0, 4).Select(index => beforeRandom[(start + index) % 4]), vm.Players);
        Assert.Equal(new[] { 1, 2, 3, 4 }, vm.Players.Select(player => player.Position));
        Assert.False(vm.Players[0].CanMoveUp);
        Assert.False(vm.Players[^1].CanMoveDown);
        Assert.All(vm.Players.Skip(1), player => Assert.True(player.CanMoveUp));
        Assert.All(vm.Players.Take(3), player => Assert.True(player.CanMoveDown));
    }

    [Fact]
    public async Task SuccessfulDraftStart_NextPreparationUsesActualOrderAndColors()
    {
        await using var database = await SliceDatabase.CreateAsync();
        var vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        vm.RotateOrder();
        vm.SetColor(vm.Players[0], PlayerColor.Red);
        vm.SetColor(vm.Players[1], PlayerColor.Black);
        vm.DrawZooPlans();
        foreach (var player in vm.Players)
            vm.SelectZooPlan(player, 1);
        Assert.NotNull(await vm.StartAsync(Token));
        Assert.False(vm.HasError);
        var next = Create(database);
        await next.LoadAsync(database.Group.Id, Token);
        Assert.Equal(vm.Players.Select(player => player.PlayerId), next.Players.Select(player => player.PlayerId));
        Assert.Equal(vm.Players.Select(player => player.Color), next.Players.Select(player => player.Color));
        Assert.False(next.PlansDrawn);
    }
}

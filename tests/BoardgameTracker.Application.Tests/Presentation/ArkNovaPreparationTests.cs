using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class ArkNovaPreparationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static ArkNovaPreparationViewModel Create(SliceDatabase db) =>
        new(new ArkNovaPreparationQuery(db, TestGameContent.Create()), new StartArkNovaGameSessionUseCase(db), NullLogger<ArkNovaPreparationViewModel>.Instance);

    [Fact]
    public async Task Load_UsesGlobalOrderAndDefaultsDespitePreviousLastPlaceAndSessionColors()
    {
        await using var database = await SliceDatabase.CreateAsync();
        var command = new StartArkNovaGameSessionCommand(database.Group.Id, DateTimeOffset.UtcNow.AddHours(-2),
            [new(database.First.Id, PlayerColor.Red, 1), new(database.Second.Id, PlayerColor.Black, 3)]);
        await new StartArkNovaGameSessionUseCase(database).StartAsync(command, Token);
        await using (var db = database.CreateDbContext())
        {
            var session = await db.GameSessions.Include(session => session.Players).SingleAsync(Token);
            session.Players.Single(player => player.PlayerId == database.First.Id).SetPlacement(1);
            session.Players.Single(player => player.PlayerId == database.Second.Id).SetPlacement(2);
            session.BeginScoring(DateTimeOffset.UtcNow.AddHours(-1));
            session.Complete(DateTimeOffset.UtcNow);
            var colors = await db.GroupGamePlayerColors.ToArrayAsync(Token);
            colors.Single(player => player.PlayerId == database.First.Id).SetDefaultColor(PlayerColor.Yellow);
            db.GroupGamePlayerColors.Remove(colors.Single(player => player.PlayerId == database.Second.Id));
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
            db.GroupGamePlayerColors.Add(new(database.Group.Id, database.GameId, database.Second.Id, PlayerColor.Blue));
            if (invalid)
            {
                db.GroupGamePlayerColors.Add(new(database.Group.Id, database.GameId, database.First.Id, PlayerColor.Red));
                await db.SaveChangesAsync(Token);
                await db.Database.ExecuteSqlRawAsync("UPDATE GroupGamePlayerColors SET DefaultColor = 5 WHERE PlayerId = {0}", [database.First.Id], Token);
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
        Assert.Empty(await db.GroupGamePlayerColors.ToArrayAsync(Token));
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
    private static void Choose(ArkNovaPreparationViewModel vm, int playerIndex, int number)
    {
        vm.OpenZooPlanPicker(vm.Players[playerIndex]);
        vm.SelectManualZooPlan(vm.ZooPlanOptions.Single(option => option.Number == number));
    }

    [Theory]
    [InlineData(1, "zoo_plan_01.png", "1")]
    [InlineData(15, "ark_zoo_plan_turnier.png", "Turnier")]
    [InlineData(16, "ark_zoo_plan_0.png", "0")]
    [InlineData(17, "ark_zoo_plan_a.png", "A")]
    public async Task ManualChoice_ReplacesOnlyTargetAndMarksBothAssignments(int number, string icon, string label)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        ArkNovaPreparationViewModel vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        Assert.All(vm.Players, player =>
        {
            Assert.Null(player.SelectedZooPlanNumber);
            Assert.True(player.CanChooseManualZooPlan);
        });
        vm.OpenZooPlanPicker(vm.Players[0]);
        Assert.Equal(Enumerable.Range(1, 17), vm.ZooPlanOptions.Select(option => option.Number));
        Assert.False(vm.CanInteract);
        vm.CloseZooPlanPicker();
        Assert.Null(vm.Players[0].SelectedZooPlanNumber);
        Choose(vm, 0, number);
        Assert.False(vm.IsZooPlanPickerVisible);
        Assert.False(vm.CanStart);
        Assert.Equal(icon, vm.Players[0].ManualZooPlanIconSource);
        Assert.Equal(label, vm.Players[0].ManualZooPlanLabel);
        Assert.True(vm.Players[0].HasManualZooPlan);
        Assert.False(vm.Players[0].CanChooseManualZooPlan);
        vm.OpenZooPlanPicker(vm.Players[1]);
        ArkNovaManualZooPlanOptionViewModel option = vm.ZooPlanOptions.Single(option => option.Number == number);
        Assert.True(option.IsAssignedToOtherPlayer);
        Assert.False(option.IsSelected);
        vm.SelectManualZooPlan(option);
        Assert.True(vm.CanStart);
        vm.OpenZooPlanPicker(vm.Players[0]);
        option = vm.ZooPlanOptions.Single(option => option.Number == number);
        Assert.True(option.IsAssignedToOtherPlayer);
        Assert.True(option.IsSelected);
        Assert.Contains(BoardgameTracker.App.Resources.Localization.AppResources.ZooPlanAssignedToOtherPlayer, option.Description);
        vm.SelectManualZooPlan(vm.ZooPlanOptions.Single(option => option.Number == 2));
        Assert.Equal(2, vm.Players[0].SelectedZooPlanNumber);
        Assert.Equal(number, vm.Players[1].SelectedZooPlanNumber);
        Assert.True(vm.CanStart);
        await using var db = database.CreateDbContext();
        Assert.Empty(await db.ArkNovaGameSessionPlayers.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    public async Task ManualDuplicatePlans_StartAndReopenRunningSessionWithSelectedPlanOnly(int number)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        ArkNovaPreparationViewModel vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        Choose(vm, 0, number);
        Choose(vm, 1, number);
        Guid? id = await vm.StartAsync(Token);
        Assert.NotNull(id);
        Assert.False(vm.HasError);
        var running = await new ArkNovaRunningGameSessionQuery(database, TestGameContent.Create())
            .GetRunningGameSessionAsync(id.Value, Token);
        Assert.All(running!.Players, player => Assert.Equal(number, player.ZooPlanNumber));
        Assert.All(running.Players, player => Assert.DoesNotContain("[missing:", player.ZooPlanName));
        await using var db = database.CreateDbContext();
        Assert.All(await db.ArkNovaGameSessionPlayers.ToArrayAsync(Token), player => Assert.Equal(number, player.SelectedZooPlanNumber));
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ArkNovaGameSessionPlayers') WHERE name IN ('ZooPlanOption1Number', 'ZooPlanOption2Number')";
        Assert.Equal(0L, await command.ExecuteScalarAsync(Token));
    }

    [Fact]
    public async Task Draw_ReplacesManualChoicesAndLocksManualSelectionUntilNewDraft()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        ArkNovaPreparationViewModel vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        Choose(vm, 0, 17);
        Choose(vm, 1, 17);
        vm.OpenZooPlanPicker(vm.Players[0]);
        ArkNovaManualZooPlanOptionViewModel stale = vm.ZooPlanOptions[0];
        vm.DrawZooPlans();
        Assert.True(vm.PlansDrawn);
        Assert.False(vm.IsZooPlanPickerVisible);
        Assert.False(vm.CanStart);
        Assert.All(vm.Players, player =>
        {
            Assert.Null(player.SelectedZooPlanNumber);
            Assert.True(player.HasZooPlans);
            Assert.False(player.CanChooseManualZooPlan);
            Assert.False(player.HasManualZooPlan);
            Assert.InRange(player.ZooPlanOption1!.Number, 1, 14);
            Assert.InRange(player.ZooPlanOption2!.Number, 1, 14);
        });
        Assert.Equal(4, vm.Players.SelectMany(player => new[] { player.ZooPlanOption1!.Number, player.ZooPlanOption2!.Number }).Distinct().Count());
        vm.OpenZooPlanPicker(vm.Players[0]);
        Assert.False(vm.IsZooPlanPickerVisible);
        vm.SelectManualZooPlan(stale);
        Assert.Null(vm.Players[0].SelectedZooPlanNumber);
        vm.SelectZooPlan(vm.Players[0], 3);
        Assert.Null(vm.Players[0].SelectedZooPlanNumber);
        foreach (ArkNovaPreparationPlayerViewModel player in vm.Players)
            vm.SelectZooPlan(player, 1);
        Assert.True(vm.CanStart);
        vm.DrawZooPlans();
        Assert.All(vm.Players, player => Assert.Null(player.SelectedZooPlanNumber));
        Assert.False(vm.CanStart);
        await vm.LoadAsync(database.Group.Id, Token);
        Assert.False(vm.PlansDrawn);
        Assert.All(vm.Players, player => Assert.True(player.CanChooseManualZooPlan));
        Choose(vm, 0, 15);
        Assert.Equal(15, vm.Players[0].SelectedZooPlanNumber);
    }

    [Fact]
    public async Task InitialDraw_NotifiesBindingsToHideManualButtons()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        ArkNovaPreparationViewModel vm = Create(database);
        await vm.LoadAsync(database.Group.Id, Token);
        Dictionary<Guid, bool> visibleButtons = vm.Players.ToDictionary(player => player.PlayerId, player => player.CanChooseManualZooPlan);
        foreach (ArkNovaPreparationPlayerViewModel player in vm.Players)
        {
            player.PropertyChanged += (_, change) =>
            {
                if (change.PropertyName == nameof(player.CanChooseManualZooPlan))
                    visibleButtons[player.PlayerId] = player.CanChooseManualZooPlan;
            };
        }
        vm.DrawZooPlans();
        Assert.All(visibleButtons.Values, visible => Assert.False(visible));
    }

}

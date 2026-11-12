using BoardgameTracker.App;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Games.ManualGames;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Running;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Presentation;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Games.ManualGames;

public sealed class ManualSessionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed record Fixture(SliceDatabase Database, Guid GameId, string Key, bool Order, bool Colors) : IAsyncDisposable
    {
        public Guid GroupId => Database.Group.Id;
        public ManualPreparationQuery Query => new(Database);
        public StartManualGameSessionUseCase Start => new(Database);
        public ManualRunningGameSessionQuery Running => new(Database);
        public ManualPreparationViewModel ViewModel() => new(Query, Start, NullLogger<ManualPreparationViewModel>.Instance);
        public static async Task<Fixture> CreateAsync(bool order = true, bool colors = true)
        {
            SliceDatabase db = await SliceDatabase.CreateAsync();
            Guid id = await new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(
                new(null, "Manual", "TAG", 1, 12, new(true, true, true, true, true, true, PointsDirection.Lowest, order, colors)), Token);
            await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, id, Token);
            await using BoardgameTrackerDbContext context = db.CreateDbContext();
            string key = (await context.Games.SingleAsync(game => game.Id == id, Token)).Key;
            return new(db, id, key, order, colors);
        }
        public async Task<StartManualGameSessionCommand> CommandAsync()
        {
            ManualPreparationData data = await Query.GetAsync(Key, GroupId, Token);
            return new(Key, GroupId, DateTimeOffset.UtcNow.AddMinutes(-70), data.Players.Select((player, index) =>
                new ManualStartingPlayer(player.PlayerId, Order ? index + 1 : null, Colors ? (PlayerColor)(index + 1) : null)).ToArray());
        }
        public ValueTask DisposeAsync() => Database.DisposeAsync();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Start_AtomicallyPersistsOptionalSnapshotsAndOnlyUpdatesCapturedDefaults(bool order, bool colors)
    {
        await using Fixture fixture = await Fixture.CreateAsync(order, colors);
        SliceDatabase database = fixture.Database;
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            seed.GroupGamePlayerColors.AddRange(new(fixture.GroupId, fixture.GameId, database.First.Id, PlayerColor.Red),
                new(fixture.GroupId, fixture.GameId, database.Second.Id, PlayerColor.Black),
                new(fixture.GroupId, database.GameId, database.First.Id, PlayerColor.Green));
            await seed.SaveChangesAsync(Token);
        }
        StartManualGameSessionCommand command = await fixture.CommandAsync();
        command = command with { Players = command.Players.Reverse().Select((player, index) => player with
            { Order = order ? index + 1 : null }).ToArray() };
        Guid id = await fixture.Start.StartAsync(command, Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Start.StartAsync(command, Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        GameSession session = await verify.GameSessions.Include(session => session.Players).SingleAsync(Token);
        Assert.Equal(GameSessionStatus.Running, session.Status);
        Assert.Equal(id, session.Id);
        Assert.Equal(order, session.HasStartingOrder);
        Assert.Equal(order ? command.Players.Select(player => player.PlayerId) : new[] { database.First.Id, database.Second.Id },
            order ? session.Players.OrderBy(player => player.Order).Select(player => player.PlayerId)
                : await verify.GroupPlayers.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId).ToArrayAsync(Token));
        if (!order)
            Assert.All(session.Players, player => Assert.Null(player.Order));
        ManualGameSession detail = await verify.ManualGameSessions.SingleAsync(Token);
        Assert.Equal(id, detail.GameSessionId);
        Assert.Equal(new ScoringRulesSnapshot(true, true, true, true, true, true, PointsDirection.Lowest), detail.ScoringRulesSnapshot);
        Assert.Equal(colors ? 2 : 0, await verify.GameSessionPlayerColors.CountAsync(Token));
        var defaults = await verify.GroupGamePlayerColors.Where(color => color.GameId == fixture.GameId).ToDictionaryAsync(color => color.PlayerId, color => color.DefaultColor, Token);
        Assert.Equal(colors ? command.Players.Single(player => player.PlayerId == database.First.Id).Color : PlayerColor.Red, defaults[database.First.Id]);
        Assert.Equal(PlayerColor.Green, (await verify.GroupGamePlayerColors.SingleAsync(color => color.GameId == database.GameId, Token)).DefaultColor);
        (await verify.ManualGames.SingleAsync(Token)).SetConfiguration(new());
        await verify.SaveChangesAsync(Token);
        ManualRunningData resumed = (await fixture.Running.GetAsync(id, Token))!;
        Assert.Equal(detail.ScoringRulesSnapshot, resumed.ScoringRules);
        Assert.Equal(colors, resumed.Players.All(player => player.Color.HasValue));
        Assert.Equal(order, resumed.Players.All(player => player.Order.HasValue));
    }

    [Theory]
    [InlineData("missing-game")]
    [InlineData("wrong-module")]
    [InlineData("missing-group")]
    [InlineData("archived")]
    [InlineData("unassigned")]
    [InlineData("below-minimum")]
    [InlineData("above-maximum")]
    [InlineData("running")]
    [InlineData("scoring")]
    public async Task PreparationAndStart_RecheckPersistedPreconditions(string invalid)
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        StartManualGameSessionCommand command = await fixture.CommandAsync();
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            if (invalid == "missing-game") command = command with { GameKey = ManualGame.CreateKey() };
            if (invalid == "wrong-module") command = command with { GameKey = "ark-nova" };
            if (invalid == "missing-group") command = command with { GroupId = Guid.NewGuid() };
            if (invalid == "archived") (await db.Groups.SingleAsync(Token)).SetArchived(true);
            if (invalid == "unassigned") await db.GroupGames.ExecuteDeleteAsync(Token);
            if (invalid is "below-minimum" or "above-maximum")
            {
                var game = await db.Games.SingleAsync(game => game.Id == fixture.GameId, Token);
                game.SetMetadata(game.Name, game.BadgeText, invalid == "below-minimum" ? 3 : 1, invalid == "above-maximum" ? 1 : 12);
            }
            if (invalid is "running" or "scoring")
            {
                GameSession session = new(fixture.GameId, fixture.GroupId, DateTimeOffset.UtcNow, [fixture.Database.First.Id, fixture.Database.Second.Id]);
                if (invalid == "scoring") session.BeginScoring(session.StartedAt);
                db.GameSessions.Add(session);
            }
            await db.SaveChangesAsync(Token);
        }
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => fixture.Query.GetAsync(command.GameKey, command.GroupId, Token));
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => fixture.Start.StartAsync(command, Token));
        await using BoardgameTrackerDbContext verify = fixture.Database.CreateDbContext();
        Assert.Empty(await verify.ManualGameSessions.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessionPlayerColors.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData("membership")]
    [InlineData("duplicate-player")]
    [InlineData("mixed-order")]
    [InlineData("duplicate-order")]
    [InlineData("duplicate-color")]
    [InlineData("invalid-color")]
    [InlineData("changed-order-setting")]
    [InlineData("changed-color-setting")]
    public async Task InvalidDraft_StartWritesNoSessionOrDefaults(string invalid)
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        StartManualGameSessionCommand command = await fixture.CommandAsync();
        ManualStartingPlayer first = command.Players[0];
        command = invalid switch
        {
            "membership" => command with { Players = [first with { PlayerId = Guid.NewGuid() }, command.Players[1]] },
            "duplicate-player" => command with { Players = [first, first] },
            "mixed-order" => command with { Players = [first with { Order = null }, command.Players[1]] },
            "duplicate-order" => command with { Players = [first, command.Players[1] with { Order = 1 }] },
            "duplicate-color" => command with { Players = [first, command.Players[1] with { Color = first.Color }] },
            "invalid-color" => command with { Players = [first with { Color = (PlayerColor)99 }, command.Players[1]] },
            _ => command
        };
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            if (invalid.StartsWith("changed-", StringComparison.Ordinal))
                (await db.ManualGames.SingleAsync(Token)).SetConfiguration(new(CapturePlayerOrder: invalid != "changed-order-setting",
                    CapturePlayerColors: invalid != "changed-color-setting"));
            await db.SaveChangesAsync(Token);
        }
        await Assert.ThrowsAnyAsync<Exception>(() => fixture.Start.StartAsync(command, Token));
        await using BoardgameTrackerDbContext verify = fixture.Database.CreateDbContext();
        Assert.Empty(await verify.GameSessions.ToArrayAsync(Token));
        Assert.Empty(await verify.ManualGameSessions.ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGamePlayerColors.ToArrayAsync(Token));
        Assert.Equal(new[] { fixture.Database.First.Id, fixture.Database.Second.Id },
            await verify.GroupPlayers.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId).ToArrayAsync(Token));
    }

    [Fact]
    public async Task StartFailure_RollsBackOrderColorDefaultsAndEverySnapshot()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            db.GroupGamePlayerColors.Add(new(fixture.GroupId, fixture.GameId, fixture.Database.First.Id, PlayerColor.Gray));
            await db.SaveChangesAsync(Token);
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectManualSession BEFORE INSERT ON ManualGameSessions BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        }
        StartManualGameSessionCommand command = await fixture.CommandAsync();
        command = command with { Players = command.Players.Reverse().Select((player, index) => player with { Order = index + 1 }).ToArray() };
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Start.StartAsync(command, Token));
        await using BoardgameTrackerDbContext verify = fixture.Database.CreateDbContext();
        Assert.Empty(await verify.GameSessions.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessionPlayers.ToArrayAsync(Token));
        Assert.Empty(await verify.ManualGameSessions.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessionPlayerColors.ToArrayAsync(Token));
        Assert.Equal(PlayerColor.Gray, (await verify.GroupGamePlayerColors.SingleAsync(Token)).DefaultColor);
        Assert.Equal(fixture.Database.First.Id, (await verify.GroupPlayers.SingleAsync(player => player.DefaultOrder == 1, Token)).PlayerId);
    }

    [Fact]
    public async Task Draft_UsesDefaultsFillsColorsSwapsRotatesAndDoesNotPersistBeforeStart()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            db.GroupGamePlayerColors.Add(new(fixture.GroupId, fixture.GameId, fixture.Database.Second.Id, PlayerColor.Blue));
            await db.SaveChangesAsync(Token);
        }
        ManualPreparationViewModel draft = fixture.ViewModel();
        await draft.LoadAsync(fixture.Key, fixture.GroupId, Token);
        Assert.Equal("TAG", draft.BadgeText);
        Assert.Equal(new PlayerColor?[] { PlayerColor.Yellow, PlayerColor.Blue }, draft.Players.Select(player => player.Color));
        Assert.False(draft.Players[0].CanMoveUp);
        Assert.False(draft.Players[^1].CanMoveDown);
        Guid[] original = draft.Players.Select(player => player.PlayerId).ToArray();
        draft.Rotate();
        Assert.Equal(original.Reverse(), draft.Players.Select(player => player.PlayerId));
        draft.Move(draft.Players[0], 1);
        Assert.Equal(original, draft.Players.Select(player => player.PlayerId));
        draft.RotateRandom(new Random(123));
        Assert.Equal(StartingPlayerRotation.RotateToStart(original, new Random(123).Next(2)), draft.Players.Select(player => player.PlayerId));
        var target = draft.Players[0];
        PlayerColor? previous = target.Color;
        var owner = draft.Players[1];
        draft.OpenColorPicker(target);
        Assert.Equal(12, draft.ColorOptions.Count);
        Assert.False(draft.CanStart);
        draft.SelectColor(owner.Color!.Value);
        Assert.Equal(previous, owner.Color);
        Assert.True(draft.CanStart);
        await using BoardgameTrackerDbContext verify = fixture.Database.CreateDbContext();
        Assert.Empty(await verify.GameSessions.ToArrayAsync(Token));
        Assert.Equal(PlayerColor.Blue, (await verify.GroupGamePlayerColors.SingleAsync(Token)).DefaultColor);
        Assert.Equal(fixture.Database.First.Id, (await verify.GroupPlayers.SingleAsync(player => player.DefaultOrder == 1, Token)).PlayerId);
    }

    [Fact]
    public async Task UnorderedColorlessDraft_IsAlphabeticalAndRepeatStartReturnsTheSameSession()
    {
        await using Fixture fixture = await Fixture.CreateAsync(false, false);
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            await db.GroupPlayers.ExecuteUpdateAsync(setters => setters.SetProperty(player => player.DefaultOrder, player => player.DefaultOrder + 1000), Token);
            await db.GroupPlayers.Where(player => player.PlayerId == fixture.Database.Second.Id).ExecuteUpdateAsync(setters => setters.SetProperty(player => player.DefaultOrder, 1), Token);
            await db.GroupPlayers.Where(player => player.PlayerId == fixture.Database.First.Id).ExecuteUpdateAsync(setters => setters.SetProperty(player => player.DefaultOrder, 2), Token);
        }
        ManualPreparationViewModel draft = fixture.ViewModel();
        await draft.LoadAsync(fixture.Key, fixture.GroupId, Token);
        Assert.Equal(new[] { "Anna", "Ben" }, draft.Players.Select(player => player.Name));
        Assert.All(draft.Players, player => { Assert.Null(player.Color); Assert.Null(player.Position); Assert.False(player.CanChooseColor); });
        draft.Rotate();
        draft.Move(draft.Players[0], 1);
        draft.OpenColorPicker(draft.Players[0]);
        Assert.False(draft.IsChoosingColor);
        Assert.Equal("Anna", draft.Players[0].Name);
        Guid? first = await draft.StartAsync(Token);
        Assert.NotNull(first);
        Assert.Equal(first, await draft.StartAsync(Token));
        await using BoardgameTrackerDbContext verify = fixture.Database.CreateDbContext();
        Assert.Single(await verify.GameSessions.ToArrayAsync(Token));
        Assert.Equal(fixture.Database.Second.Id, (await verify.GroupPlayers.SingleAsync(player => player.DefaultOrder == 1, Token)).PlayerId);
    }

    [Fact]
    public async Task TwelvePlayers_ReceiveTwelveDistinctColorsAndCompleteOrder()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        Guid group = await new CreateGroupUseCase(fixture.Database).CreateGroupAsync(new("Twelve",
            Enumerable.Range(1, 12).Select(index => new CreateGroupPlayerInput(null, "P" + index, false)).ToArray(), [fixture.GameId]), Token);
        ManualPreparationViewModel draft = fixture.ViewModel();
        await draft.LoadAsync(fixture.Key, group, Token);
        Assert.Equal(Enum.GetValues<PlayerColor>(), draft.Players.Select(player => player.Color!.Value));
        Guid? id = await draft.StartAsync(Token);
        Assert.NotNull(id);
        ManualRunningData data = (await fixture.Running.GetAsync(id.Value, Token))!;
        Assert.Equal(Enumerable.Range(1, 12).Select(index => (int?)index), data.Players.Select(player => player.Order));
        Assert.Equal(12, data.Players.Select(player => player.Color).Distinct().Count());
    }

    [Theory]
    [InlineData("places")]
    [InlineData("winners")]
    [InlineData("points")]
    [InlineData("partial-points")]
    [InlineData("date")]
    public async Task LastCompletedSession_UsesHistoricalResultFormRegardlessOfCurrentRules(string result)
    {
        await using Fixture fixture = await Fixture.CreateAsync(false, false);
        DateTimeOffset date = DateTimeOffset.UtcNow.AddDays(-1);
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            ManualGame config = await db.ManualGames.SingleAsync(Token);
            config.SetConfiguration(new());
            GameSession session = new(fixture.GameId, fixture.GroupId, date, [fixture.Database.First.Id, fixture.Database.Second.Id]);
            session.BeginScoring(date.AddHours(1));
            session.Complete(date.AddHours(2));
            if (result is "places" or "winners") session.Players.First().SetPlacement(1);
            if (result == "places") session.Players.Last().SetPlacement(2);
            db.AddRange(session, new ManualGameSession(session.Id, config));
            if (result is "places" or "winners" or "points" or "partial-points")
                db.ManualGameSessionPlayers.AddRange(new(session.Id, fixture.Database.First.Id, 0),
                    new(session.Id, fixture.Database.Second.Id, result == "partial-points" ? null : -2));
            await db.SaveChangesAsync(Token);
        }
        ManualPreparationData data = await fixture.Query.GetAsync(fixture.Key, fixture.GroupId, Token);
        Assert.Equal(date, data.LastSession!.StartedAt);
        string text = ManualPreparationViewModel.FormatLastSession(data.LastSession);
        if (result == "places") { Assert.Contains("1. Anna", text); Assert.Contains("2. Ben", text); }
        Assert.Equal(result == "winners", ManualPreparationViewModel.HasWinnerResult(data.LastSession));
        if (result == "winners") { Assert.Contains("Anna", text); Assert.DoesNotContain("Ben", text); }
        if (result == "points") { Assert.Contains("Anna: 0", text); Assert.Contains("Ben: -2", text); }
        if (result is "partial-points" or "date") { Assert.DoesNotContain("Anna", text); Assert.DoesNotContain("Ben", text); }
        Assert.Equal(AppResources.NoPreviousGameSession, ManualPreparationViewModel.FormatLastSession(null));
    }

    [Fact]
    public async Task Running_EndsInScoringResumesWithoutChangesAndDiscardsOnlyAfterConfirmation()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        Guid id = await fixture.Start.StartAsync(await fixture.CommandAsync(), Token);
        ManualRunningViewModel running = new(fixture.Running, new(fixture.Database), NullLogger<ManualRunningViewModel>.Instance);
        await running.LoadAsync(id, Token);
        ManualRunningData original = (await fixture.Running.GetAsync(id, Token))!;
        running.RefreshDuration(original.StartedAt.AddMinutes(90));
        Assert.Equal(string.Format(AppResources.DurationHoursFormat, 1, 30), running.DurationText);
        Assert.False(await running.DiscardAsync(false, Token));
        Assert.NotNull(await fixture.Running.GetAsync(id, Token));
        Assert.True(await running.BeginScoringAsync(Token));
        Assert.True(running.IsScoring);
        Assert.False(running.ShowEnd);
        ManualRunningData scoring = (await fixture.Running.GetAsync(id, Token))!;
        Assert.Equal(GameSessionStatus.Scoring, scoring.Status);
        Assert.NotNull(scoring.EndedAt);
        await new ManualRunningGameSessionUseCase(fixture.Database).BeginScoringAsync(id, scoring.EndedAt.Value.AddHours(1), Token);
        Assert.Equal(scoring.EndedAt, (await fixture.Running.GetAsync(id, Token))!.EndedAt);
        await running.LoadAsync(id, Token);
        string frozen = running.DurationText;
        running.RefreshDuration(DateTimeOffset.UtcNow.AddHours(1));
        Assert.Equal(frozen, running.DurationText);
        Assert.Equal(original.ScoringRules, scoring.ScoringRules);
        Assert.Equal(original.Players, scoring.Players);
        await using (BoardgameTrackerDbContext db = fixture.Database.CreateDbContext())
        {
            db.ManualGameSessionPlayers.Add(new(id, fixture.Database.First.Id));
            await db.SaveChangesAsync(Token);
        }
        Assert.True(await running.DiscardAsync(true, Token));
        Assert.Null(await fixture.Running.GetAsync(id, Token));
        Assert.Null(await fixture.Running.FindOpenAsync(fixture.Key, fixture.GroupId, Token));
        await using BoardgameTrackerDbContext verify = fixture.Database.CreateDbContext();
        Assert.Empty(await verify.GameSessionPlayers.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessionPlayerColors.ToArrayAsync(Token));
        Assert.Empty(await verify.ManualGameSessions.ToArrayAsync(Token));
        Assert.Empty(await verify.ManualGameSessionPlayers.ToArrayAsync(Token));
        Assert.Equal(2, await verify.GroupGamePlayerColors.CountAsync(Token));
        Assert.Single(await verify.ManualGames.ToArrayAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
    }

    [Fact]
    public async Task OneHandler_ResolvesDynamicKeysAndSharedGroupsAndResumesRunningAndScoring()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        ManualGameSessionNavigationHandler handler = new(fixture.Running, new(fixture.Database), new(fixture.Database));
        Assert.True(handler.CanHandle(fixture.Key));
        Assert.True(handler.CanHandle(ManualGame.CreateKey()));
        Assert.False(handler.CanHandle("ark-nova"));
        MainSectionNavigationTests.SettingsStore settings = new();
        AppShell shell = MainSectionNavigationTests.CreateShell(settings, fixture.Database);
        Shell.Current = shell;
        GroupListViewModel groups = new(new(fixture.Database), new(fixture.Database), new(fixture.Database, new TestGameActivationStore()),
            NullLogger<GroupListViewModel>.Instance, [handler]);
        groups.SetGameContext(fixture.Key);
        await groups.LoadAsync(Token);
        Assert.Equal("TAG", groups.BadgeText);
        await groups.OpenGroupAsync(fixture.GroupId);
        Assert.Equal("ManualPreparationPage", shell.Calls.Last().Route);
        Assert.Equal(fixture.Key, shell.Calls.Last().Parameters![NavigationParameters.GameKey]);
        Guid id = await fixture.Start.StartAsync(await fixture.CommandAsync(), Token);
        await handler.OpenGroupAsync(fixture.Key, fixture.GroupId, null, null);
        Assert.Equal("ManualRunningPage", shell.Calls.Last().Route);
        Assert.Equal(id, shell.Calls.Last().Parameters![NavigationParameters.GameSessionId]);
        await new ManualRunningGameSessionUseCase(fixture.Database).BeginScoringAsync(id, DateTimeOffset.UtcNow, Token);
        OpenGameSession open = (await new OpenGameSessionQuery(fixture.Database).GetAsync(id, Token))!;
        await handler.ResumeAsync(shell, open);
        Assert.EndsWith("/ManualPointsPage", shell.Calls.Last().Route);
        Assert.Equal(id, settings.Value.LastGameSessionId);
        Assert.Equal(fixture.Key, settings.Value.SelectedGameKey);
        GamesViewModel games = new(new(fixture.Database, new TestGameActivationStore()), [handler], NullLogger<GamesViewModel>.Instance);
        await games.LoadAsync(Token);
        Assert.True(games.Games.Single(game => game.Key == fixture.Key).CanOpen);
        Assert.True(games.Games.First().HasOpenSession);
        Assert.Empty(await new BoardgameTracker.Application.History.CompletedGameSessionsQuery(fixture.Database).ListAsync(cancellationToken: Token));
    }
}

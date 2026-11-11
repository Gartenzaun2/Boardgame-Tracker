using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Games.ManualGames;

public sealed class ManualGameUseCaseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static ManualGameCommand Command(string name = "Manual", int min = 1, int max = 12) =>
        new(null, name, " Tag ", min, max, new(true, true, true, true, true, true, PointsDirection.Lowest, true, true));

    [Fact]
    public async Task CreateAndEdit_PersistFullConfigurationAndStableIdentity()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameActivationStore store = new();
        ManualGameUseCase useCase = new(db, store);
        Guid id = await useCase.SaveAsync(Command("  Manual  "), Token);
        string key;
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
        {
            Game game = await verify.Games.SingleAsync(game => game.Id == id, Token);
            key = game.Key;
            Assert.True(ManualGame.MatchesKey(key));
            Assert.Equal("Manual", game.Name);
            Assert.Equal("Tag", game.BadgeText);
            ManualGame manual = await verify.ManualGames.SingleAsync(Token);
            Assert.Equal(id, manual.GameId);
            Assert.True(manual.CapturePoints && manual.AllowNegativePoints && manual.CaptureWinner && manual.CapturePlacements);
            Assert.True(manual.AllowTies && manual.ResultFromPoints && manual.CapturePlayerOrder && manual.CapturePlayerColors);
            Assert.Equal(PointsDirection.Lowest, manual.PointsDirection);
        }
        Assert.True(store.IsEnabled(key));
        await useCase.SaveAsync(Command("Renamed", 2) with { GameId = id, Options = new() }, Token);
        ManualGameDetails changed = (await useCase.GetAsync(key, Token))!;
        Assert.Equal(id, changed.Id);
        Assert.Equal(key, changed.Key);
        Assert.Equal("Renamed", changed.Name);
        Assert.Equal(2, changed.MinPlayers);
        Assert.Equal(12, changed.MaxPlayers);
        Assert.False(changed.Options.CapturePoints || changed.Options.CaptureWinner);
    }

    [Theory]
    [InlineData("", "Tag", 1, 4)]
    [InlineData("Name", " ", 1, 4)]
    [InlineData("Name", "Tag", 0, 4)]
    [InlineData("Name", "Tag", 5, 4)]
    [InlineData("Name", "Tag", 1, 13)]
    public async Task InvalidInput_LeavesNoPartialGame(string name, string badge, int min, int max)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new ManualGameUseCase(db, new TestGameActivationStore())
            .SaveAsync(Command(name, min, max) with { BadgeText = badge }, Token));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));
        Assert.Single(await verify.Games.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Names_AreUniqueAcrossHiddenManualGamesButMayMatchIntegratedGames()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameActivationStore store = new();
        ManualGameUseCase useCase = new(db, store);
        string integratedName;
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
            integratedName = (await verify.Games.SingleAsync(Token)).Name;
        Guid id = await useCase.SaveAsync(Command(integratedName), Token);
        string key;
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
            key = (await verify.Games.SingleAsync(game => game.Id == id, Token)).Key;
        store.SetEnabled(key, false);
        ManualGameValidationException duplicate = await Assert.ThrowsAsync<ManualGameValidationException>(() =>
            useCase.SaveAsync(Command(" " + integratedName.ToUpperInvariant() + " "), Token));
        Assert.Equal(ManualGameError.DuplicateName, duplicate.Error);
        await using BoardgameTrackerDbContext direct = db.CreateDbContext();
        Game duplicateGame = new(ManualGame.CreateKey(), integratedName.ToLowerInvariant(), "D", 1, 4);
        direct.AddRange(duplicateGame, new ManualGame(duplicateGame, new()));
        await Assert.ThrowsAsync<DbUpdateException>(() => direct.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task DatabaseFailure_RollsBackBothGameAndConfiguration()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
            await seed.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectManual BEFORE INSERT ON ManualGames BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(Command(), Token));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Single(await verify.Games.ToArrayAsync(Token));
        Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Limits_KeepMaximumFixedAndProtectActualAssignedMembership()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        ManualGameUseCase useCase = new(db, new TestGameActivationStore());
        Guid id = await useCase.SaveAsync(Command(max: 4), Token);
        await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, id, Token);
        ManualGameValidationException locked = await Assert.ThrowsAsync<ManualGameValidationException>(() =>
            useCase.SaveAsync(Command(max: 5) with { GameId = id }, Token));
        Assert.Equal(ManualGameError.MaximumLocked, locked.Error);
        ManualGameValidationException assigned = await Assert.ThrowsAsync<ManualGameValidationException>(() =>
            useCase.SaveAsync(Command(min: 3, max: 4) with { GameId = id }, Token));
        Assert.Equal(ManualGameError.AssignedGroups, assigned.Error);
        await useCase.SaveAsync(Command(min: 2, max: 4) with { GameId = id }, Token);
        await new RemoveGroupGameUseCase(db).RemoveGroupGameAsync(db.Group.Id, id, Token);
        await useCase.SaveAsync(Command(min: 3, max: 4) with { GameId = id }, Token);
        await Assert.ThrowsAsync<GroupPlayerMinimumNotMetException>(() => new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, id, Token));
    }

    [Fact]
    public async Task Visibility_ReactivationAndDisabledPlayEntryUseSharedMetadata()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameActivationStore store = new();
        Guid id = await new ManualGameUseCase(db, store).SaveAsync(Command(), Token);
        GameActivationUseCase activation = new(db, store);
        GameActivationState state = (await activation.GetGamesAsync(Token)).Single(game => game.Name == "Manual");
        PlayOverviewQuery query = new(db, store);
        GamesViewModel viewModel = new(query, [], NullLogger<GamesViewModel>.Instance);
        await viewModel.LoadAsync(Token);
        Assert.False(viewModel.Games.Single(game => game.Key == state.GameKey).CanOpen);
        await activation.SetEnabledAsync(state.GameKey, false, Token);
        Assert.DoesNotContain(await query.ListAsync(Token), game => game.Id == id);
        Assert.False((await activation.GetGamesAsync(Token)).Single(game => game.GameKey == state.GameKey).IsEnabled);
        await activation.SetEnabledAsync(state.GameKey, true, Token);
        Assert.Contains(await query.ListAsync(Token), game => game.Id == id);
    }

    [Theory]
    [InlineData(GameSessionStatus.Running)]
    [InlineData(GameSessionStatus.Scoring)]
    [InlineData(GameSessionStatus.Completed)]
    public async Task Delete_RechecksEverySessionStatusAndLeavesAllData(GameSessionStatus status)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameActivationStore store = new();
        ManualGameUseCase useCase = new(db, store);
        Guid id = await useCase.SaveAsync(Command(), Token);
        string key;
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            key = (await seed.Games.SingleAsync(game => game.Id == id, Token)).Key;
            GameSession session = new(id, db.Group.Id, DateTimeOffset.UtcNow, [db.First.Id, db.Second.Id]);
            if (status != GameSessionStatus.Running)
                session.BeginScoring(session.StartedAt);
            if (status == GameSessionStatus.Completed)
                session.Complete(session.StartedAt);
            seed.GameSessions.Add(session);
            await seed.SaveChangesAsync(Token);
        }
        Assert.True((await useCase.GetAsync(key, Token))!.HasSessions);
        ManualGameValidationException blocked = await Assert.ThrowsAsync<ManualGameValidationException>(() => useCase.DeleteAsync(id, Token));
        Assert.Equal(ManualGameError.HasSessions, blocked.Error);
        GameActivationUseCase activation = new(db, store);
        if (status == GameSessionStatus.Completed)
            await activation.SetEnabledAsync(key, false, Token);
        else
            await Assert.ThrowsAsync<GameActivationBlockedException>(() => activation.SetEnabledAsync(key, false, Token));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Single(await verify.ManualGames.ToArrayAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
    }

    [Fact]
    public async Task Editor_CancelCreatesNothingAndDependentOptionsSaveNormalized()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameActivationStore store = new();
        ManualGameEditorViewModel canceled = new(new(db, store), new(db, store), NullLogger<ManualGameEditorViewModel>.Instance);
        await canceled.LoadAsync(null, Token);
        canceled.GameName = "Discarded";
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
            Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));
        ManualGameEditorViewModel editor = new(new(db, store), new(db, store), NullLogger<ManualGameEditorViewModel>.Instance);
        await editor.LoadAsync(null, Token);
        editor.GameName = "Editor";
        editor.BadgeText = "E";
        editor.AllowNegativePoints = editor.CapturePlacements = editor.AllowTies = true;
        editor.ResultMode = 1;
        Assert.False(editor.CanDeriveResult);
        Assert.True(await editor.SaveAsync(Token));
        await using BoardgameTrackerDbContext saved = db.CreateDbContext();
        ManualGame config = await saved.ManualGames.SingleAsync(Token);
        Assert.False(config.AllowNegativePoints || config.CapturePlacements || config.AllowTies || config.ResultFromPoints);
        Assert.Null(config.PointsDirection);
    }

    [Fact]
    public async Task Delete_RemovesOnlyThisGamesConfigurationAssignmentsDefaultsAndPreference()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameActivationStore store = new();
        ManualGameUseCase useCase = new(db, store);
        Guid id = await useCase.SaveAsync(Command(), Token);
        await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, id, Token);
        await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, db.GameId, Token);
        string key;
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            key = (await seed.Games.SingleAsync(game => game.Id == id, Token)).Key;
            seed.GroupGamePlayerColors.AddRange(new(db.Group.Id, id, db.First.Id, PlayerColor.Blue),
                new(db.Group.Id, db.GameId, db.First.Id, PlayerColor.Blue));
            await seed.SaveChangesAsync(Token);
        }
        store.SetEnabled(key, false);
        Assert.False((await useCase.GetAsync(key, Token))!.HasSessions);
        await useCase.DeleteAsync(id, Token);
        Assert.True(store.IsEnabled(key));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));
        Assert.Equal(db.GameId, (await verify.Games.SingleAsync(Token)).Id);
        Assert.Equal(db.GameId, (await verify.GroupGames.SingleAsync(Token)).GameId);
        Assert.Equal(db.GameId, (await verify.GroupGamePlayerColors.SingleAsync(Token)).GameId);
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        await Assert.ThrowsAsync<ManualGameValidationException>(() => useCase.DeleteAsync(db.GameId, Token));
    }

    [Theory]
    [InlineData("database")]
    [InlineData("commit")]
    public async Task DeleteFailure_RollsBackConfigurationAssignmentsAndDefaults(string failure)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        FailingVisibility store = new();
        ManualGameUseCase useCase = new(db, store);
        Guid id = await useCase.SaveAsync(Command(), Token);
        await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, id, Token);
        string key;
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            key = (await seed.Games.SingleAsync(game => game.Id == id, Token)).Key;
            seed.GroupGamePlayerColors.Add(new(db.Group.Id, id, db.First.Id, PlayerColor.Blue));
            await seed.SaveChangesAsync(Token);
            if (failure == "database")
                await seed.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectDelete BEFORE DELETE ON Games BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
            else
            {
                // A deferred foreign key fails at the outer transaction commit, after SaveChanges succeeds.
                await seed.Database.ExecuteSqlRawAsync("CREATE TABLE CommitGuard (GameId TEXT NOT NULL REFERENCES Games(Id) DEFERRABLE INITIALLY DEFERRED);", Token);
                await seed.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectCommit AFTER DELETE ON Games BEGIN INSERT INTO CommitGuard(GameId) VALUES (OLD.Id); END;", Token);
            }
        }
        store.SetEnabled(key, false);
        await Assert.ThrowsAnyAsync<Exception>(() => useCase.DeleteAsync(id, Token));
        Assert.False(store.IsEnabled(key));
        Assert.Equal(0, store.RemovalAttempts);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Single(await verify.ManualGames.ToArrayAsync(Token));
        Assert.Single(await verify.GroupGames.Where(assignment => assignment.GameId == id).ToArrayAsync(Token));
        Assert.Single(await verify.GroupGamePlayerColors.Where(color => color.GameId == id).ToArrayAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreferenceCleanup_RunsAfterCommitAndCannotUndoDeletion(bool cleanupFails)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        FailingVisibility store = new();
        ManualGameUseCase useCase = new(db, store);
        Guid id = await useCase.SaveAsync(Command(), Token);
        await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, id, Token);
        string key;
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            key = (await seed.Games.SingleAsync(game => game.Id == id, Token)).Key;
            seed.GroupGamePlayerColors.Add(new(db.Group.Id, id, db.First.Id, PlayerColor.Blue));
            await seed.SaveChangesAsync(Token);
        }
        store.SetEnabled(key, false);
        store.BeforeRemoval = () =>
        {
            using BoardgameTrackerDbContext committed = db.CreateDbContext();
            // Starting another transaction on the shared connection proves the delete transaction ended.
            using var probeTransaction = committed.Database.BeginTransaction();
            Assert.False(committed.Games.Any(game => game.Id == id));
            Assert.Empty(committed.ManualGames);
            Assert.Empty(committed.GroupGames.Where(assignment => assignment.GameId == id));
            Assert.Empty(committed.GroupGamePlayerColors.Where(color => color.GameId == id));
        };
        store.FailNextRemoval = cleanupFails;
        if (cleanupFails)
        {
            ManualGamePreferenceCleanupException failure = await Assert.ThrowsAsync<ManualGamePreferenceCleanupException>(() => useCase.DeleteAsync(id, Token));
            Assert.Equal(id, failure.GameId);
            Assert.IsType<IOException>(failure.InnerException);
        }
        else
        {
            await useCase.DeleteAsync(id, Token);
        }
        Assert.Equal(1, store.RemovalAttempts);
        Assert.Equal(!cleanupFails, store.IsEnabled(key));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.False(await verify.Games.AnyAsync(game => game.Id == id, Token));
        Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGames.Where(assignment => assignment.GameId == id).ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGamePlayerColors.Where(color => color.GameId == id).ToArrayAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
    }

    [Fact]
    public async Task Editor_PreferenceFailureReportsWarningAndTreatsDeletionAsSuccessful()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        FailingVisibility store = new();
        ManualGameUseCase useCase = new(db, store);
        Guid id = await useCase.SaveAsync(Command(), Token);
        string key;
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
            key = (await seed.Games.SingleAsync(game => game.Id == id, Token)).Key;
        store.SetEnabled(key, false);
        ManualGameEditorViewModel editor = new(useCase, new(db, store), NullLogger<ManualGameEditorViewModel>.Instance);
        await editor.LoadAsync(key, Token);
        store.FailNextRemoval = true;
        Assert.True(await editor.DeleteAsync(Token));
        Assert.False(editor.HasError);
        Assert.False(editor.IsBusy);
        Assert.Equal(BoardgameTracker.App.Resources.Localization.AppResources.ManualPreferenceCleanupFailed, editor.DeletionWarning);
        Assert.False(store.IsEnabled(key));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.False(await verify.Games.AnyAsync(game => game.Id == id, Token));
    }

    private sealed class FailingVisibility : IGameActivationStore
    {
        private readonly TestGameActivationStore _store = new();
        public bool FailNextRemoval { get; set; }
        public int RemovalAttempts { get; private set; }
        public Action? BeforeRemoval { get; set; }
        public bool IsEnabled(string key) => _store.IsEnabled(key);
        public void SetEnabled(string key, bool enabled)
        {
            if (enabled)
            {
                RemovalAttempts++;
                BeforeRemoval?.Invoke();
            }
            if (enabled && FailNextRemoval)
            {
                FailNextRemoval = false;
                throw new IOException("Test preference failure.");
            }
            _store.SetEnabled(key, enabled);
        }
    }
}

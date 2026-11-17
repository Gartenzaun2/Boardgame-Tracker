using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class ManualEditorInitializationTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FailedInitialQuery_BlocksAllMutationsButAllowsBackAndRetry(int failingQuery) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameEnabledStore store = new();
        ManualGameUseCase useCase = new(db, store);
        Guid id = await useCase.SaveAsync(new(null, "Existing", "E", 2, 4,
            new(true, true, true, true, true, true, PointsDirection.Lowest, true, true)), Token);
        string key;
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
            key = (await seed.Games.SingleAsync(game => game.Id == id, Token)).Key;
        FailingQueryFactory factory = new(db, failingQuery);
        EditorNavigationFake navigation = new();
        ManualGameEditorViewModel editor = new(new(factory, store), new(factory, store), new(factory, store), navigation, NullLogger<ManualGameEditorViewModel>.Instance);
        List<string?> changes = [];
        editor.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        await editor.LoadAsync(key, Token);
        Assert.False(editor.IsBusy);
        Assert.False(editor.IsEditing);
        Assert.False(editor.CanEdit);
        Assert.False(editor.CanDelete);
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.True(editor.GoBackCommand.CanExecute(null));
        Assert.Equal(AppResources.ManualLoadFailed, editor.ErrorMessage);
        Assert.Equal(string.Empty, editor.GameName);
        int queryCalls = factory.Calls;
        editor.GameName = "Must not persist";
        editor.BadgeText = "M";
        Assert.False(await editor.SaveAsync(Token));
        await editor.SetEnabledAsync(false, Token);
        Assert.False(await editor.RefreshDeleteEligibilityAsync(Token));
        Assert.False(await editor.DeleteAsync(Token));
        Assert.Equal(queryCalls, factory.Calls);
        Assert.Equal(AppResources.ManualLoadFailed, editor.ErrorMessage);
        Assert.True(store.IsEnabled(key));
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext()) {
            Assert.Equal("Existing", (await verify.Games.SingleAsync(game => game.Id == id, Token)).Name);
            Assert.Single(await verify.ManualGames.ToArrayAsync(Token));
        }
        await editor.GoBackCommand.ExecuteAsync(null);
        Assert.Single(navigation.Calls);

        changes.Clear();
        await editor.LoadAsync(key, Token);
        Assert.Null(editor.ErrorMessage);
        Assert.True(editor.IsEditing);
        Assert.True(editor.CanEdit);
        Assert.True(editor.CanDelete);
        Assert.True(editor.SaveCommand.CanExecute(null));
        Assert.Equal("Existing", editor.GameName);
        Assert.Equal("2", editor.MinimumPlayers);
        Assert.Equal("4", editor.MaximumPlayers);
        Assert.Equal(2, editor.ResultMode);
        Assert.True(editor.CapturePlayerOrder);
        Assert.True(editor.CapturePlayerColors);
        Assert.Contains(nameof(editor.CanEdit), changes);
        Assert.Contains(nameof(editor.CanDelete), changes);
        editor.GameName = "Kept draft";
        queryCalls = factory.Calls;
        await editor.LoadAsync(key, Token);
        Assert.Equal(queryCalls, factory.Calls);
        Assert.Equal("Kept draft", editor.GameName);
        Assert.True(await editor.RefreshDeleteEligibilityAsync(Token));
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task NewDraft_IsEditableOnlyAfterInitializationAndStillSaves() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameEnabledStore store = new();
        ManualGameEditorViewModel editor = new(new(db, store), new(db, store), new(db, store), new EditorNavigationFake(), NullLogger<ManualGameEditorViewModel>.Instance);
        Assert.False(editor.CanEdit);
        Assert.False(editor.SaveCommand.CanExecute(null));
        Assert.True(editor.GoBackCommand.CanExecute(null));
        await editor.LoadAsync(null, Token);
        Assert.True(editor.CanEdit);
        Assert.True(editor.SaveCommand.CanExecute(null));
        editor.GameName = "New game";
        editor.BadgeText = "N";
        Assert.True(await editor.SaveAsync(Token));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Single(await verify.ManualGames.ToArrayAsync(Token));
    }

    private sealed class FailingQueryFactory(IDbContextFactory<BoardgameTrackerDbContext> inner, int failingQuery) : IDbContextFactory<BoardgameTrackerDbContext> {
        public int Calls { get; private set; }
        public BoardgameTrackerDbContext CreateDbContext() => inner.CreateDbContext();
        public Task<BoardgameTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) {
            Calls++;
            return Calls == failingQuery
                ? Task.FromException<BoardgameTrackerDbContext>(new InvalidOperationException("Query unavailable"))
                : inner.CreateDbContextAsync(cancellationToken);
        }
    }
}

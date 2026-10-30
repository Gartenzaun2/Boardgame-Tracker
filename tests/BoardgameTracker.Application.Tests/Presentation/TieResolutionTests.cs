using BoardgameTracker.Infrastructure.Persistence.SQLite.History;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Seeding;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class TieResolutionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PlacementGroups_MoveOnlySelectedPlayer_KeepEmptyTargetsAndPlayerColors_AfterReopening()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.Equal([1, 2, 3, 4], viewModel.PlacementGroups.Select(group => group.Placement));
        Assert.Equal(2, viewModel.PlacementGroups[1].Players.Count);
        Assert.Empty(viewModel.PlacementGroups[2].Players);

        viewModel.SelectPlacement(database.Players[1].Id, 3);
        Assert.Equal(database.Players[2].Id, Assert.Single(viewModel.PlacementGroups[1].Players).PlayerId);
        ArkNovaPlacementPlayerViewModel moved = Assert.Single(viewModel.PlacementGroups[2].Players);
        Assert.Equal(database.Players[1].Id, moved.PlayerId);
        Assert.Equal(PlayerColor.Yellow, moved.Color);
        Assert.Equal([2, 3], moved.PlacementOptions.Select(option => option.Placement));
        viewModel.SelectPlacement(database.Players[2].Id, 3);
        Assert.Empty(viewModel.PlacementGroups[1].Players);
        Assert.Equal(2, viewModel.PlacementGroups[2].Players.Count);
        Assert.True(viewModel.CanFinish);
        await viewModel.CompleteAsync(Token);

        ArkNovaTieResolutionViewModel reopened = database.CreateViewModel();
        await reopened.LoadAsync(database.Session.Id, Token);
        Assert.Empty(reopened.PlacementGroups[1].Players);
        Assert.Equal(2, reopened.PlacementGroups[2].Players.Count);
        Assert.Equal([PlayerColor.Yellow, PlayerColor.Red], reopened.PlacementGroups[2].Players.OrderBy(player => player.Name).Select(player => player.Color!.Value));
        Assert.All(reopened.PlacementGroups[2].Players, player => Assert.True(player.HasColor));
        Assert.Empty(Assert.Single(reopened.PlacementGroups[0].Players).PlacementOptions);
        Assert.Empty(Assert.Single(reopened.PlacementGroups[3].Players).PlacementOptions);
    }

    [Fact]
    public void PlayerWithoutColor_LeavesNeutralThemeStyleInEffect()
    {
        ArkNovaPlacementPlayerViewModel player = new(Guid.NewGuid(), "Player", 100, 1, [], null);
        Assert.False(player.HasColor);
        Assert.Null(player.ColorStyle);
    }

    [Fact]
    public async Task SharedPlaces_AreValidOnLoad_CanBeRestoredAndPersistInHistory()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.Equal(new[] { 1, 2, 2, 4 }, viewModel.Players.Select(player => player.Placement));
        Assert.True(viewModel.CanFinish);
        Assert.Null(viewModel.ValidationMessage);

        viewModel.SelectPlacement(database.Players[1].Id, 3);
        Assert.True(viewModel.CanFinish);
        viewModel.SelectPlacement(database.Players[2].Id, 3);
        Assert.True(viewModel.CanFinish);
        Assert.Null(viewModel.ValidationMessage);
        viewModel.SelectPlacement(database.Players[1].Id, 2);
        viewModel.SelectPlacement(database.Players[2].Id, 2);
        Assert.True(viewModel.CanFinish);
        Assert.Null(viewModel.ValidationMessage);

        await viewModel.CompleteAsync(Token);
        ArkNovaGameSessionResultData saved = (await database.Scoring.LoadGameSessionResultAsync(database.Session.Id, Token))!;
        Assert.Equal(new int?[] { 1, 2, 2, 4 }, saved.Players.OrderBy(player => player.Order).Select(player => player.Placement));
        var history = await new SqliteCompletedGameSessionsQuery(database).ListAsync(cancellationToken: Token);
        Assert.Equal(new[] { 1, 2, 2, 4 }, Assert.Single(history).Players.Select(player => player.Placement));
    }

    [Fact]
    public async Task TwoBlocks_PersistIndependentSelections_AndKeepChoicesOnRepeatedLoad()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync([100, 100, 80, 80]);
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.All(viewModel.Players.Take(2), player => Assert.Equal([1, 2], player.PlacementOptions.Select(option => option.Placement)));
        Assert.All(viewModel.Players.Skip(2), player => Assert.Equal([3, 4], player.PlacementOptions.Select(option => option.Placement)));

        viewModel.SelectPlacement(database.Players[0].Id, 2);
        Assert.Equal(new[] { 2, 1, 3, 3 }, viewModel.Players.Select(player => player.Placement));
        Assert.Equal(2, viewModel.PlacementGroups[2].Players.Count);
        viewModel.SelectPlacement(database.Players[2].Id, 4);
        viewModel.SelectPlacement(database.Players[0].Id, 4); // Foreign block is rejected.
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.Equal(new[] { 2, 1, 4, 3 }, viewModel.Players.Select(player => player.Placement));
        Assert.Equal(database.Players[1].Id, Assert.Single(viewModel.PlacementGroups[0].Players).PlayerId);
        Assert.Equal(database.Players[3].Id, Assert.Single(viewModel.PlacementGroups[2].Players).PlayerId);

        await viewModel.CompleteAsync(Token);
        ArkNovaGameSessionResultData reopened = (await database.Scoring.LoadGameSessionResultAsync(database.Session.Id, Token))!;
        Assert.Equal(new int?[] { 2, 1, 4, 3 }, reopened.Players.OrderBy(player => player.Order).Select(player => player.Placement));
        var history = await new SqliteCompletedGameSessionsQuery(database).ListAsync(cancellationToken: Token);
        Assert.Equal(new[] { database.Players[1].Id, database.Players[0].Id, database.Players[3].Id, database.Players[2].Id },
            Assert.Single(history).Players.Select(player => player.PlayerId));
    }

    [Fact]
    public async Task Completion_RejectsParallelCallWhilePersistenceIsPending()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        GatedCompletionStore data = new(database.Scoring);
        ArkNovaTieResolutionViewModel viewModel = new(new(data), database.Correction);
        await viewModel.LoadAsync(database.Session.Id, Token);
        Task completion = viewModel.CompleteAsync(Token);
        await data.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        try
        {
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.CanFinish);
            await viewModel.CompleteAsync(Token);
            Assert.Equal(1, data.SaveCount);
        }
        finally
        {
            data.Release.TrySetResult();
        }
        await completion;
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.CanFinish);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task DirectPlaceSelection_ResolvesOnlyTiedPlayers_AndPersistsInHistory(int playerIndex)
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel viewModel = database.CreateViewModel();
        await viewModel.LoadAsync(database.Session.Id, Token);
        Assert.True(viewModel.CanFinish);
        Assert.Empty(viewModel.Players.Single(player => player.PlayerId == database.Players[0].Id).PlacementOptions);
        Assert.Empty(viewModel.Players.Single(player => player.PlayerId == database.Players[3].Id).PlacementOptions);
        foreach (ArkNovaPlacementPlayerViewModel player in viewModel.Players.Skip(1).Take(2))
        {
            Assert.Equal([2, 3], player.PlacementOptions.Select(option => option.Placement));
            Assert.Equal(player.Placement, Assert.Single(player.PlacementOptions, option => option.IsSelected).Placement);
        }

        int[] expected = [1, playerIndex == 1 ? 3 : 2, playerIndex == 2 ? 3 : 2, 4];
        viewModel.SelectPlacement(database.Players[playerIndex].Id, 3);
        Assert.True(viewModel.CanFinish);
        viewModel.SelectPlacement(database.Players[2].Id, 1);
        viewModel.SelectPlacement(database.Players[2].Id, 4);
        viewModel.SelectPlacement(database.Players[0].Id, 2);
        Assert.Equal(expected, viewModel.Players.Select(player => player.Placement));

        await viewModel.CompleteAsync(Token);
        await viewModel.CompleteAsync(Token); // Repeated completion cannot write again.
        Assert.False(viewModel.CanFinish);
        ArkNovaGameSessionResultData saved = (await database.Scoring.LoadGameSessionResultAsync(database.Session.Id, Token))!;
        Assert.Equal(GameSessionStatus.Completed, saved.Status);
        Assert.Equal(expected.Select(placement => (int?)placement), saved.Players.OrderBy(p => p.Order).Select(p => p.Placement));
        Assert.Equal(new int?[] { 100, 90, 90, 80 }, saved.Players.OrderBy(p => p.Order).Select(p => p.Attractiveness));
        var history = await new SqliteCompletedGameSessionsQuery(database).ListAsync(cancellationToken: Token);
        Assert.Equal(database.Players[3 - playerIndex].Id, Assert.Single(history).Players[1].PlayerId);
    }

    [Fact]
    public async Task HistoricalTie_DirectPlacesPersistCorrectionAndKeepUnrelatedPlacement()
    {
        await using TieDatabase database = await TieDatabase.CreateAsync();
        ArkNovaTieResolutionViewModel initial = database.CreateViewModel();
        await initial.LoadAsync(database.Session.Id, Token);
        await initial.CompleteAsync(Token);

        ArkNovaHistoryCorrectionDraft draft = await database.Correction.LoadCorrectionDraftAsync(database.Session.Id, Token);
        draft.SetScore(database.Players[0].Id, 90, 7, null);
        ArkNovaTieResolutionViewModel correction = database.CreateViewModel();
        correction.Draft = draft;
        await correction.LoadAsync(database.Session.Id, Token);
        correction.SelectPlacement(database.Players[2].Id, 2);
        correction.SelectPlacement(database.Players[1].Id, 3);
        Assert.True(correction.CanFinish);
        await correction.CompleteAsync(Token);

        ArkNovaHistoryCorrectionDraft reopened = await database.Correction.LoadCorrectionDraftAsync(database.Session.Id, Token);
        Assert.True(draft.Saved);
        Assert.Equal(GameSessionStatus.Completed, reopened.Data.Status);
        Assert.Equal(new int?[] { 1, 2, 3, 4 }, reopened.Data.Players.Select(p => p.Placement));
        Assert.Equal(database.Players[2].Id, reopened.Data.Players[1].PlayerId);
        Assert.Equal(database.Players[3].Id, reopened.Data.Players[3].PlayerId);
    }

    private sealed class GatedCompletionStore(IArkNovaScoringStore inner) : IArkNovaScoringStore
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int SaveCount { get; private set; }
        public Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => inner.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
        public Task<GameSession?> FindGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => inner.FindGameSessionAsync(gameSessionId, cancellationToken);
        public async Task SaveCompletedGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            await inner.SaveCompletedGameSessionAsync(gameSession, cancellationToken);
        }
        public Task<ArkNovaGameSessionPlayer?> FindScoringPlayerAsync(Guid gameSessionId, Guid playerId, CancellationToken cancellationToken = default) => inner.FindScoringPlayerAsync(gameSessionId, playerId, cancellationToken);
        public Task SaveScoringPlayerAsync(ArkNovaGameSessionPlayer player, CancellationToken cancellationToken = default) => inner.SaveScoringPlayerAsync(player, cancellationToken);
        public Task AssignSpecialCardAsync(ArkNovaGameSessionSpecialCardAssignment assignment, CancellationToken cancellationToken = default) => inner.AssignSpecialCardAsync(assignment, cancellationToken);
        public Task RemoveSpecialCardAsync(Guid gameSessionId, Guid playerId, Guid specialCardId, CancellationToken cancellationToken = default) => inner.RemoveSpecialCardAsync(gameSessionId, playerId, specialCardId, cancellationToken);
        public Task DeleteScoringGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => inner.DeleteScoringGameSessionAsync(gameSessionId, cancellationToken);
    }

    private sealed class TieDatabase : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public Player[] Players { get; } = [new("Anna"), new("Bernd"), new("Clara"), new("Dirk")];
        public GameSession Session { get; private set; } = null!;
        public SqliteArkNovaGameSessionResultStore Scoring => new(this);
        public ArkNovaHistoryCorrectionUseCase Correction => new(new SqliteArkNovaGameSessionResultStore(this));
        public ArkNovaTieResolutionViewModel CreateViewModel() => new(new(Scoring), Correction);
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseSqlite(_connection).Options);

        public static async Task<TieDatabase> CreateAsync(int[]? scores = null)
        {
            TieDatabase database = new();
            await database._connection.OpenAsync(Token);
            await new DatabaseInitializer(database, [new ArkNovaReferenceDataSeeder()]).InitializeAsync(Token);
            await using BoardgameTrackerDbContext context = database.CreateDbContext();
            Guid gameId = await context.Games.Select(game => game.Id).SingleAsync(Token);
            Group group = new("Group", false, database.Players.Select(player => player.Id).ToArray());
            database.Session = new(gameId, group.Id, DateTimeOffset.UtcNow.AddHours(-2), database.Players.Select(player => player.Id).ToArray());
            database.Session.BeginScoring(DateTimeOffset.UtcNow.AddHours(-1));
            context.AddRange(database.Players);
            context.AddRange(group, database.Session);
            scores ??= [100, 90, 90, 80];
            for (int index = 0; index < database.Players.Length; index++)
            {
                ArkNovaGameSessionPlayer player = new(database.Session.Id, database.Players[index].Id, (PlayerColor)(index + 1), index * 2 + 1, index * 2 + 2, index * 2 + 1);
                player.SetScoreValues(scores[index], 7, null);
                context.Add(player);
            }
            await context.SaveChangesAsync(Token);
            return database;
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

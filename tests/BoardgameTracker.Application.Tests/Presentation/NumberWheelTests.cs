using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using Microsoft.Extensions.Logging.Abstractions;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class NumberWheelTests
{
    [Theory]
    [InlineData(0, 113, 57)]
    [InlineData(0, 41, 21)]
    [InlineData(1, 15, 8)]
    public void EmptyWheel_DisplaysMiddleWithoutCreatingCapturedValue(int minimum, int maximum, int expected)
    {
        int? captured = null;
        Assert.Equal(expected, new NumberWheelRange(minimum, maximum).DisplayedValue(captured));
        Assert.Null(captured);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(72)]
    [InlineData(113)]
    public void CapturedValue_IsDisplayedIncludingBothLimits(int value) =>
        Assert.Equal(value, new NumberWheelRange(0, 113).DisplayedValue(value));

    [Theory]
    [InlineData(-1)]
    [InlineData(114)]
    public void OutsideRange_IsRejected(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new NumberWheelRange(0, 113).DisplayedValue(value));

    [Fact]
    public void EmptyInput_OnlyExplicitConfirmationAdoptsRequiredSuggestions()
    {
        ArkNovaPlayerScoringViewModel viewModel = CreateViewModel();
        Assert.Null(viewModel.AttractivenessValue);
        Assert.Null(viewModel.ConservationValue);
        Assert.Null(viewModel.ReputationValue);
        Assert.True(viewModel.IsAttractivenessSuggested);
        Assert.True(viewModel.IsValid);
        Assert.Null(viewModel.AttractivenessValue);

        viewModel.ConfirmSuggestedValues();

        Assert.Equal(57, viewModel.AttractivenessValue);
        Assert.Equal(21, viewModel.ConservationValue);
        Assert.Null(viewModel.ReputationValue);
        Assert.False(viewModel.IsAttractivenessSuggested);
    }

    [Fact]
    public void Confirmation_PreservesEnteredValuesAndZero()
    {
        ArkNovaPlayerScoringViewModel viewModel = CreateViewModel();
        viewModel.AttractivenessValue = 0;
        viewModel.ConservationValue = 41;
        viewModel.ConfirmSuggestedValues();
        Assert.Equal(0, viewModel.AttractivenessValue);
        Assert.Equal(41, viewModel.ConservationValue);
    }

    [Fact]
    public void ReputationSlider_StartsAtMiddleWithoutCapturingAndUsesWholeNumbers()
    {
        ArkNovaPlayerScoringViewModel viewModel = CreateViewModel();
        Assert.Equal(8, viewModel.ReputationSliderValue);
        Assert.Null(viewModel.ReputationValue);
        viewModel.ReputationSliderValue = 11.6;
        Assert.Equal(12, viewModel.ReputationValue);
        Assert.Equal("12", viewModel.ReputationText);
        viewModel.ReputationText = string.Empty;
        Assert.Null(viewModel.ReputationValue);
        Assert.Equal(8, viewModel.ReputationSliderValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReputationVisibility_FollowsGameSettings(bool captureReputation)
    {
        await using ScoringDatabase data = await ScoringDatabase.CreateAsync(null, null, null);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data, new(data)), new SettingsStore(captureReputation), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
        await viewModel.LoadAsync(data.GameSessionId, data.PlayerId, TestContext.Current.CancellationToken);
        Assert.Equal(captureReputation, viewModel.IsReputationVisible);
        Assert.Null(viewModel.ReputationValue);
        Assert.Equal(0, data.SaveCount);
    }

    [Theory]
    [InlineData("114")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void InvalidDirectInput_IsRejectedWithoutSendingInvalidValueToNativeWheel(string input)
    {
        ArkNovaPlayerScoringViewModel viewModel = CreateViewModel();
        viewModel.AttractivenessText = input;
        Assert.False(viewModel.IsValid);
        Assert.Null(viewModel.AttractivenessValue);
        Assert.NotNull(viewModel.ValidationMessage);
        viewModel.ConfirmSuggestedValues();
        Assert.Equal(input, viewModel.AttractivenessText);
    }

    [Fact]
    public void WheelAndKeyboard_UseTheSameCapturedValue()
    {
        ArkNovaPlayerScoringViewModel viewModel = CreateViewModel();
        viewModel.AttractivenessValue = 72;
        Assert.Equal("72", viewModel.AttractivenessText);
        viewModel.AttractivenessText = "86";
        Assert.Equal(86, viewModel.AttractivenessValue);
    }

    [Fact]
    public async Task OpeningEmptyPlayer_DoesNotSaveDisplayedSuggestions()
    {
        await using ScoringDatabase data = await ScoringDatabase.CreateAsync(null, null, null);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data, new(data)), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
        await viewModel.LoadAsync(data.GameSessionId, data.PlayerId, TestContext.Current.CancellationToken);
        Assert.Equal("57", viewModel.AttractivenessSuggestion);
        Assert.Equal("21", viewModel.ConservationSuggestion);
        Assert.Null(viewModel.AttractivenessValue);
        Assert.Null(viewModel.ConservationValue);
        Assert.Null(viewModel.ReputationValue);
        Assert.NotEqual("–", viewModel.ScoreText);
        await Task.Delay(600, TestContext.Current.CancellationToken);
        Assert.Equal(0, data.SaveCount);

        await viewModel.FlushAsync(TestContext.Current.CancellationToken);
        Assert.Null(data.Player.Attractiveness);
        Assert.Null(data.Player.Conservation);
        Assert.Null(data.Player.Reputation);

        viewModel.ConfirmSuggestedValues();
        await viewModel.FlushAsync(TestContext.Current.CancellationToken);
        Assert.Equal(57, data.Player.Attractiveness);
        Assert.Equal(21, data.Player.Conservation);
        Assert.Null(data.Player.Reputation);
    }

    [Fact]
    public async Task OpeningSavedPlayer_SelectsStoredValuesAndConfirmationPreservesThem()
    {
        await using ScoringDatabase data = await ScoringDatabase.CreateAsync(73, 19, 11);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data, new(data)), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
        await viewModel.LoadAsync(data.GameSessionId, data.PlayerId, TestContext.Current.CancellationToken);
        Assert.Equal(73, viewModel.AttractivenessValue);
        Assert.Equal(19, viewModel.ConservationValue);
        Assert.Equal(11, viewModel.ReputationValue);
        Assert.False(viewModel.IsAttractivenessSuggested);
        Assert.Equal(0, data.SaveCount);
        viewModel.ConfirmSuggestedValues();
        Assert.Equal(73, viewModel.AttractivenessValue);
        Assert.Equal(19, viewModel.ConservationValue);
    }

    [Fact]
    public async Task FinalFlush_WaitsForActiveAutosave_ThenSavesLatestInput()
    {
        await using ScoringDatabase data = await ScoringDatabase.CreateAsync(70, 20, null, blockFirstSave: true);
        ArkNovaPlayerScoringViewModel viewModel = new(new(data, new(data)), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
        CancellationToken token = TestContext.Current.CancellationToken;
        await viewModel.LoadAsync(data.GameSessionId, data.PlayerId, token);
        viewModel.AttractivenessValue = 80;
        await data.FirstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), token);

        viewModel.AttractivenessValue = 90;
        Task flush = viewModel.FlushAsync(token);
        try
        {
            Assert.True(viewModel.IsBusy);
            Assert.False(flush.IsCompleted);
            Assert.Equal(1, data.SaveCount);
        }
        finally
        {
            data.ReleaseFirstSave.TrySetResult();
        }
        await flush;

        Assert.Equal(1, data.MaxConcurrentSaves);
        Assert.Equal(2, data.SaveCount);
        Assert.Equal(90, data.Player.Attractiveness);
        Assert.False(viewModel.IsBusy);
        Assert.False(data.ActiveSaveWasCancelled);
    }

    [Fact]
    public async Task FailedFinalFlush_PropagatesFailureAndReleasesSaveGateForRetry()
    {
        await using ScoringDatabase data = await ScoringDatabase.CreateAsync(70, 20, null, failNextSave: true);
        ArkNovaPlayerScoringViewModel viewModel = new(new(data, new(data)), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
        CancellationToken token = TestContext.Current.CancellationToken;
        await viewModel.LoadAsync(data.GameSessionId, data.PlayerId, token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.FlushAsync(token));
        Assert.False(viewModel.IsBusy);
        await viewModel.FlushAsync(token);
        Assert.Equal(2, data.SaveCount);
    }

    private sealed class ScoringDatabase : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly SaveProbe _probe = new();
        public Guid GameSessionId { get; private set; }
        public Guid PlayerId { get; private set; }
        public int SaveCount => _probe.SaveCount;
        public int MaxConcurrentSaves => _probe.MaxConcurrentSaves;
        public bool ActiveSaveWasCancelled => _probe.ActiveSaveWasCancelled;
        public TaskCompletionSource FirstSaveStarted => _probe.FirstSaveStarted;
        public TaskCompletionSource ReleaseFirstSave => _probe.ReleaseFirstSave;
        public ArkNovaGameSessionPlayer Player
        {
            get
            {
                using BoardgameTrackerDbContext db = CreateDbContext();
                return db.ArkNovaGameSessionPlayers.Single();
            }
        }
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>()
            .UseTestSqlite(_connection).AddInterceptors(_probe).Options);

        public static async Task<ScoringDatabase> CreateAsync(int? attractiveness, int? conservation, int? reputation,
            bool blockFirstSave = false, bool failNextSave = false)
        {
            ScoringDatabase database = new();
            CancellationToken token = TestContext.Current.CancellationToken;
            await database._connection.OpenAsync(token);
            await using BoardgameTrackerDbContext db = new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>()
                .UseTestSqlite(database._connection).Options);
            await db.Database.EnsureCreatedAsync(token);
            BoardgameTracker.Core.Players.Player player = new("Anna");
            BoardgameTracker.Core.Groups.Group group = new("Group", false, [player.Id]);
            BoardgameTracker.Core.Games.Game game = new(ArkNovaGame.Key);
            GameSession session = new(game.Id, group.Id, DateTimeOffset.UtcNow.AddHours(-2), [player.Id]);
            session.BeginScoring(DateTimeOffset.UtcNow.AddHours(-1));
            database.GameSessionId = session.Id;
            database.PlayerId = player.Id;
            ArkNovaGameSessionPlayer values = new(session.Id, player.Id, PlayerColor.Blue, 1, 2, 1);
            values.SetScoreValues(attractiveness, conservation, reputation);
            db.AddRange(player, group, game, session, values, new ArkNovaZooPlan(1), new ArkNovaZooPlan(2));
            await db.SaveChangesAsync(token);
            database._probe.BlockFirstSave = blockFirstSave;
            database._probe.FailNextSave = failNextSave;
            return database;
        }
        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    // These input-only tests never load or save, so no database is opened.
    private sealed class UnopenedContextFactory : IDbContextFactory<BoardgameTrackerDbContext>
    {
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().Options);
    }
    private static ArkNovaPlayerScoringViewModel CreateViewModel()
    {
        UnopenedContextFactory factory = new();
        return new(new(factory, new(factory)), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
    }

    private sealed class SettingsStore(bool captureReputation = false) : IArkNovaSettingsStore
    {
        public ArkNovaSettings Get() => ArkNovaSettings.Default with { CaptureReputation = captureReputation };
        public void Save(ArkNovaSettings settings) { }
    }
}

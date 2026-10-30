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
        ScoringStore data = new(null, null, null);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data), new SettingsStore(captureReputation), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
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
        ScoringStore data = new(null, null, null);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
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
        ScoringStore data = new(73, 19, 11);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
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
        ScoringStore data = new(70, 20, null) { BlockFirstSave = true };
        ArkNovaPlayerScoringViewModel viewModel = new(new(data), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
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
        ScoringStore data = new(70, 20, null) { FailNextSave = true };
        ArkNovaPlayerScoringViewModel viewModel = new(new(data), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);
        CancellationToken token = TestContext.Current.CancellationToken;
        await viewModel.LoadAsync(data.GameSessionId, data.PlayerId, token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.FlushAsync(token));
        Assert.False(viewModel.IsBusy);
        await viewModel.FlushAsync(token);
        Assert.Equal(2, data.SaveCount);
    }

    private sealed class ScoringStore : IArkNovaScoringStore
    {
        public Guid GameSessionId { get; } = Guid.NewGuid();
        public Guid PlayerId { get; } = Guid.NewGuid();
        public ArkNovaGameSessionPlayer Player { get; }
        public int SaveCount { get; private set; }
        public bool BlockFirstSave { get; init; }
        public bool FailNextSave { get; set; }
        public TaskCompletionSource FirstSaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirstSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _activeSaves;
        public int MaxConcurrentSaves { get; private set; }
        public bool ActiveSaveWasCancelled { get; private set; }

        public ScoringStore(int? attractiveness, int? conservation, int? reputation)
        {
            Player = new(GameSessionId, PlayerId, PlayerColor.Blue, 1, 2, 1);
            Player.SetScoreValues(attractiveness, conservation, reputation);
        }

        public Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArkNovaGameSessionResultData?>(new(GameSessionId, "Group", GameSessionStatus.Scoring,
                DateTimeOffset.UtcNow, null,
                [new(PlayerId, "Anna", 1, PlayerColor.Blue, 1, Player.Attractiveness, Player.Conservation, Player.Reputation, null)], []));
        public Task<ArkNovaGameSessionPlayer?> FindScoringPlayerAsync(Guid gameSessionId, Guid playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArkNovaGameSessionPlayer?>(Player);
        public async Task SaveScoringPlayerAsync(ArkNovaGameSessionPlayer player, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            _activeSaves++;
            MaxConcurrentSaves = Math.Max(MaxConcurrentSaves, _activeSaves);
            try
            {
                if (BlockFirstSave && SaveCount == 1)
                {
                    FirstSaveStarted.TrySetResult();
                    await ReleaseFirstSave.Task.WaitAsync(TestContext.Current.CancellationToken);
                    ActiveSaveWasCancelled = cancellationToken.IsCancellationRequested;
                }
                if (FailNextSave)
                {
                    FailNextSave = false;
                    throw new InvalidOperationException("Simulated write failure.");
                }
            }
            finally
            {
                _activeSaves--;
            }
        }
        public Task<GameSession?> FindGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveCompletedGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AssignSpecialCardAsync(ArkNovaGameSessionSpecialCardAssignment assignment, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveSpecialCardAsync(Guid gameSessionId, Guid playerId, Guid specialCardId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteScoringGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static ArkNovaPlayerScoringViewModel CreateViewModel() => new(new ArkNovaScoringUseCase(new ScoringStore(null, null, null)), new SettingsStore(), NullLogger<ArkNovaPlayerScoringViewModel>.Instance);

    private sealed class SettingsStore(bool captureReputation = false) : IArkNovaSettingsStore
    {
        public ArkNovaSettings Get() => ArkNovaSettings.Default with { CaptureReputation = captureReputation };
        public void Save(ArkNovaSettings settings) { }
    }
}

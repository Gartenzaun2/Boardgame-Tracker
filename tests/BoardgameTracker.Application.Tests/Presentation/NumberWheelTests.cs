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
        ScoringDataAccess data = new(null, null, null);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data), new SettingsStore(captureReputation));
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
        ScoringDataAccess data = new(null, null, null);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data), new SettingsStore());
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
        ScoringDataAccess data = new(73, 19, 11);
        ArkNovaPlayerScoringViewModel viewModel = new(new ArkNovaScoringUseCase(data), new SettingsStore());
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

    private sealed class ScoringDataAccess : IArkNovaScoringDataAccess
    {
        public Guid GameSessionId { get; } = Guid.NewGuid();
        public Guid PlayerId { get; } = Guid.NewGuid();
        public ArkNovaGameSessionPlayer Player { get; }
        public int SaveCount { get; private set; }

        public ScoringDataAccess(int? attractiveness, int? conservation, int? reputation)
        {
            Player = new(GameSessionId, PlayerId, PlayerColor.Blue, 1, 2, 1);
            Player.SetScoreValues(attractiveness, conservation, reputation);
        }

        public Task<ArkNovaScoringGameSessionData?> LoadAsync(Guid gameSessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArkNovaScoringGameSessionData?>(new(GameSessionId, "Group", GameSessionStatus.Scoring,
                DateTimeOffset.UtcNow, null,
                [new(PlayerId, "Anna", 1, PlayerColor.Blue, 1, Player.Attractiveness, Player.Conservation, Player.Reputation, null)], []));
        public Task<ArkNovaGameSessionPlayer?> FindPlayerAsync(Guid gameSessionId, Guid playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArkNovaGameSessionPlayer?>(Player);
        public Task SavePlayerAsync(ArkNovaGameSessionPlayer player, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
        public Task<GameSession?> FindGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveCompletedGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddSpecialCardAsync(ArkNovaGameSessionSpecialCardAssignment assignment, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveSpecialCardAsync(Guid gameSessionId, Guid playerId, Guid specialCardId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static ArkNovaPlayerScoringViewModel CreateViewModel() => new(new ArkNovaScoringUseCase(new ScoringDataAccess(null, null, null)), new SettingsStore());

    private sealed class SettingsStore(bool captureReputation = false) : IArkNovaSettingsStore
    {
        public ArkNovaSettings Get() => ArkNovaSettings.Default with { CaptureReputation = captureReputation };
        public void Save(ArkNovaSettings settings) { }
    }
}

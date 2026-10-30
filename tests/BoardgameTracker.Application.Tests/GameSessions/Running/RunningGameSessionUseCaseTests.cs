using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.Tests.GameSessions.Running;

public sealed class RunningGameSessionUseCaseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BeginScoringAsync_TransitionsAndSavesGameSession()
    {
        DateTimeOffset startedAt = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);
        GameSession gameSession = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);
        FakeRunningGameSessionStore dataAccess = new(gameSession);
        RunningGameSessionUseCase useCase = new(dataAccess);
        DateTimeOffset endedAt = startedAt.AddHours(2);

        await useCase.BeginScoringAsync(gameSession.Id, endedAt, Token);

        Assert.Same(gameSession, dataAccess.SavedGameSession);
        Assert.Equal(GameSessionStatus.Scoring, gameSession.Status);
        Assert.Equal(endedAt, gameSession.EndedAt);
    }

    [Fact]
    public async Task DiscardAsync_DeletesRunningGameSession()
    {
        GameSession gameSession = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [Guid.NewGuid()]);
        FakeRunningGameSessionStore dataAccess = new(gameSession);
        RunningGameSessionUseCase useCase = new(dataAccess);

        await useCase.DiscardAsync(gameSession.Id, Token);

        Assert.Same(gameSession, dataAccess.DeletedGameSession);
    }

    [Fact]
    public async Task BeginScoringAsync_WhenGameSessionIsNotRunning_Throws()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        GameSession gameSession = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);
        gameSession.BeginScoring(startedAt.AddMinutes(1));
        FakeRunningGameSessionStore dataAccess = new(gameSession);
        RunningGameSessionUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.BeginScoringAsync(gameSession.Id, startedAt.AddMinutes(2), Token));
    }

    private sealed class FakeRunningGameSessionStore(GameSession? gameSession)
        : IRunningGameSessionStore
    {
        public GameSession? SavedGameSession { get; private set; }
        public GameSession? DeletedGameSession { get; private set; }

        public Task<GameSession?> FindAsync(
            Guid gameSessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(gameSession?.Id == gameSessionId ? gameSession : null);

        public Task SaveAsync(
            GameSession gameSessionToSave,
            CancellationToken cancellationToken = default)
        {
            SavedGameSession = gameSessionToSave;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            GameSession gameSessionToDelete,
            CancellationToken cancellationToken = default)
        {
            DeletedGameSession = gameSessionToDelete;
            return Task.CompletedTask;
        }
    }
}

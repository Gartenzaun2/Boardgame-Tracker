using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.Tests.GameSessions.Running;

public sealed class RunningGameSessionUseCaseTests
{
    [Fact]
    public async Task BeginScoringAsync_TransitionsAndSavesSession()
    {
        DateTimeOffset startedAt = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);
        GameSession session = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);
        FakeDataAccess dataAccess = new(session);
        RunningGameSessionUseCase useCase = new(dataAccess);
        DateTimeOffset endedAt = startedAt.AddHours(2);

        await useCase.BeginScoringAsync(session.Id, endedAt);

        Assert.Same(session, dataAccess.SavedSession);
        Assert.Equal(GameSessionStatus.Scoring, session.Status);
        Assert.Equal(endedAt, session.EndedAt);
    }

    [Fact]
    public async Task DiscardAsync_DeletesRunningSession()
    {
        GameSession session = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [Guid.NewGuid()]);
        FakeDataAccess dataAccess = new(session);
        RunningGameSessionUseCase useCase = new(dataAccess);

        await useCase.DiscardAsync(session.Id);

        Assert.Same(session, dataAccess.DeletedSession);
    }

    [Fact]
    public async Task BeginScoringAsync_WhenSessionIsNotRunning_Throws()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        GameSession session = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);
        session.BeginScoring(startedAt.AddMinutes(1));
        FakeDataAccess dataAccess = new(session);
        RunningGameSessionUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.BeginScoringAsync(session.Id, startedAt.AddMinutes(2)));
    }

    private sealed class FakeDataAccess(GameSession? session)
        : IRunningGameSessionDataAccess
    {
        public GameSession? SavedSession { get; private set; }
        public GameSession? DeletedSession { get; private set; }

        public Task<GameSession?> FindAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(session?.Id == sessionId ? session : null);

        public Task SaveAsync(
            GameSession sessionToSave,
            CancellationToken cancellationToken = default)
        {
            SavedSession = sessionToSave;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            GameSession sessionToDelete,
            CancellationToken cancellationToken = default)
        {
            DeletedSession = sessionToDelete;
            return Task.CompletedTask;
        }
    }
}

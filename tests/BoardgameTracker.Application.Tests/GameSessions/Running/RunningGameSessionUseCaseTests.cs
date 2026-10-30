using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
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
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameSession gameSession = await database.AddSessionAsync();

        RunningGameSessionUseCase useCase = new(database);
        DateTimeOffset endedAt = gameSession.StartedAt.AddHours(2);

        await useCase.BeginScoringAsync(gameSession.Id, endedAt, Token);

        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        gameSession = await db.GameSessions.SingleAsync(Token);
        Assert.Equal(GameSessionStatus.Scoring, gameSession.Status);
        Assert.Equal(endedAt, gameSession.EndedAt);
    }

    [Fact]
    public async Task DiscardAsync_DeletesRunningGameSession()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameSession gameSession = await database.AddSessionAsync();

        RunningGameSessionUseCase useCase = new(database);

        await useCase.DiscardAsync(gameSession.Id, Token);

        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        Assert.Empty(await db.GameSessions.ToArrayAsync(Token));
        Assert.Empty(await db.GameSessionPlayers.ToArrayAsync(Token));
    }

    [Fact]
    public async Task BeginScoringAsync_WhenGameSessionIsNotRunning_Throws()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameSession gameSession = await database.AddSessionAsync();
        await new RunningGameSessionUseCase(database).BeginScoringAsync(gameSession.Id, startedAt.AddMinutes(1), Token);

        RunningGameSessionUseCase useCase = new(database);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.BeginScoringAsync(gameSession.Id, startedAt.AddMinutes(2), Token));
    }
}

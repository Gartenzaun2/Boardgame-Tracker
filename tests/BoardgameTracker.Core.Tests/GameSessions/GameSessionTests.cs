using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Core.Tests.GameSessions;

public sealed class GameSessionTests
{
    [Fact]
    public void Complete_RequiresScoringState()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        GameSession gameSession = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);

        Assert.Throws<InvalidOperationException>(() => gameSession.Complete(startedAt.AddMinutes(1)));
    }

    [Fact]
    public void Complete_BeforeScoringEnd_Throws()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        GameSession gameSession = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);
        gameSession.BeginScoring(startedAt.AddMinutes(30));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            gameSession.Complete(startedAt.AddMinutes(29)));
    }

    [Fact]
    public void BeginScoring_ThenComplete_UsesValidTransitions()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        GameSession gameSession = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);

        gameSession.BeginScoring(startedAt.AddMinutes(30));
        gameSession.Complete(startedAt.AddMinutes(35));

        Assert.Equal(GameSessionStatus.Completed, gameSession.Status);
        Assert.Equal(startedAt.AddMinutes(30), gameSession.EndedAt);
    }
}

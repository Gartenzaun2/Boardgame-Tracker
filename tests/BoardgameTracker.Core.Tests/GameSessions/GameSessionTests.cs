using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Core.Tests.GameSessions;

public sealed class GameSessionTests
{
    [Fact]
    public void Complete_RequiresScoringState()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        GameSession session = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);

        Assert.Throws<InvalidOperationException>(() => session.Complete(startedAt.AddMinutes(1)));
    }

    [Fact]
    public void BeginScoring_ThenComplete_UsesValidTransitions()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        GameSession session = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            startedAt,
            [Guid.NewGuid()]);

        session.BeginScoring(startedAt.AddMinutes(30));
        session.Complete(startedAt.AddMinutes(35));

        Assert.Equal(GameSessionStatus.Completed, session.Status);
        Assert.Equal(startedAt.AddMinutes(30), session.EndedAt);
    }
}

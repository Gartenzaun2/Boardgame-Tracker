using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Core.Tests.Games.ArkNova;

public sealed class StartingPlayerRotationTests
{
    [Fact]
    public void RotateToStart_PreservesCyclicOrder()
    {
        string[] players = ["Anna", "Ben", "Chris", "David"];

        IReadOnlyList<string> result =
            StartingPlayerRotation.RotateToStart(players, 2);

        Assert.Equal(["Chris", "David", "Anna", "Ben"], result);
    }

    [Fact]
    public void RotateToStart_WithFirstPlayer_ReturnsSameOrder()
    {
        string[] players = ["Anna", "Ben", "Chris", "David"];

        IReadOnlyList<string> result =
            StartingPlayerRotation.RotateToStart(players, 0);

        Assert.Equal(players, result);
    }
}

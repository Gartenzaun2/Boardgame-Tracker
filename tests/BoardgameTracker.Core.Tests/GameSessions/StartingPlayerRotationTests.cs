using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Core.Tests.GameSessions;

public sealed class StartingPlayerRotationTests
{
    [Theory]
    [InlineData(1, 0, "Ben,Anna,Chris")]
    [InlineData(1, 2, "Anna,Chris,Ben")]
    public void MovePlayer_ChangesOnlySessionOrder(int source, int target, string expected)
    {
        string[] groupOrder = ["Anna", "Ben", "Chris"];
        IReadOnlyList<string> sessionOrder = StartingPlayerRotation.MovePlayer(groupOrder, source, target);
        Assert.Equal(expected.Split(','), sessionOrder);
        Assert.Equal(["Anna", "Ben", "Chris"], groupOrder);
        Assert.Equal(["Anna", "Ben", "Chris"], StartingPlayerRotation.MovePlayer(sessionOrder, target, source));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(2, 3)]
    public void MovePlayer_RejectsOutsidePositions(int source, int target) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => StartingPlayerRotation.MovePlayer([1, 2, 3], source, target));

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

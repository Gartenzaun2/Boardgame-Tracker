using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Tests.GameSessions;

public sealed class OptionalSnapshotsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StartingOrder_UpdatesGroupDefaultOnlyWhenRecorded(bool recordStartingOrder)
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Group", [first, second]);
        GameSession session = new(Guid.NewGuid(), group.Id, DateTimeOffset.UtcNow,
            [second, first], recordStartingOrder: recordStartingOrder);
        GameSession.ValidateStartingOrder(session.Players.Select(player => player.Order).ToArray());
        group.ApplyStartingOrder(session);
        Assert.Equal(recordStartingOrder, session.HasStartingOrder);
        Assert.Equal(recordStartingOrder ? new[] { second, first } : new[] { first, second },
            group.Players.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId));
        Assert.Equal(recordStartingOrder ? new int?[] { 1, 2 } : new int?[] { null, null },
            session.Players.Select(player => player.Order));
    }

    [Fact]
    public void StartingOrder_RejectsMixedDuplicateGappedAndNonpositiveValues()
    {
        int?[][] invalid = [[], [null, 1], [1, 1], [1, 3], [0, 1], [-1, 2]];
        foreach (int?[] orders in invalid)
            Assert.Throws<ArgumentException>(() => GameSession.ValidateStartingOrder(orders));
        GameSession.ValidateStartingOrder([null, null]);
        GameSession.ValidateStartingOrder([2, 1]);
    }

    [Fact]
    public void ColorFactories_RejectNonmembersDuplicatesAndUnknownColors()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Group", [first, second]);
        Guid gameId = Guid.NewGuid();
        GameSession session = new(gameId, group.Id, DateTimeOffset.UtcNow, [first, second]);
        Assert.Throws<ArgumentException>(() => GroupGamePlayerColor.Create(group, gameId,
            new Dictionary<Guid, PlayerColor> { [Guid.NewGuid()] = PlayerColor.Blue }));
        Assert.Throws<ArgumentException>(() => GameSessionPlayerColor.Create(session,
            new Dictionary<Guid, PlayerColor> { [Guid.NewGuid()] = PlayerColor.Blue }));
        Dictionary<Guid, PlayerColor> duplicate = new() { [first] = PlayerColor.Blue, [second] = PlayerColor.Blue };
        Assert.Throws<ArgumentException>(() => GroupGamePlayerColor.Create(group, gameId, duplicate));
        Assert.Throws<ArgumentException>(() => GameSessionPlayerColor.Create(session, duplicate));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GroupGamePlayerColor(group.Id, gameId, first, (PlayerColor)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSessionPlayerColor(session.Id, first, (PlayerColor)13));
        Assert.Empty(GroupGamePlayerColor.Create(group, gameId, new Dictionary<Guid, PlayerColor>()));
        Assert.Empty(GameSessionPlayerColor.Create(session, new Dictionary<Guid, PlayerColor>()));
    }

    [Fact]
    public void Colors_KeepAllTwelveNumericIdentitiesAndHistoricalValues()
    {
        PlayerColor[] colors = [PlayerColor.Blue, PlayerColor.Yellow, PlayerColor.Red, PlayerColor.Black,
            PlayerColor.Green, PlayerColor.White, PlayerColor.Orange, PlayerColor.Brown,
            PlayerColor.Purple, PlayerColor.Pink, PlayerColor.Turquoise, PlayerColor.Gray];
        Assert.Equal(Enumerable.Range(1, 12), colors.Select(color => (int)color));
        foreach (PlayerColor color in colors)
        {
            Guid playerId = Guid.NewGuid();
            GroupGamePlayerColor defaults = new(Guid.NewGuid(), Guid.NewGuid(), playerId, color);
            GameSessionPlayerColor historical = new(Guid.NewGuid(), playerId, color);
            defaults.SetDefaultColor(color == PlayerColor.Blue ? PlayerColor.Red : PlayerColor.Blue);
            Assert.Equal(color, historical.Color);
        }
    }
}

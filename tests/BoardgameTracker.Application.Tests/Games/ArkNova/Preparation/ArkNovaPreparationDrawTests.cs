using BoardgameTracker.Application.Games.ArkNova.Preparation;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.Preparation;

public sealed class ArkNovaPreparationDrawTests
{
    [Fact]
    public void Draw_AssignsTwoUniquePlansToEveryPlayer()
    {
        Guid[] players = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        IReadOnlyList<ArkNovaZooPlanDraw> result = ArkNovaPreparationDraw.Draw(
            players,
            new Random(12345));

        Assert.Equal(4, result.Count);
        Assert.Equal(players, result.Select(draw => draw.PersonId));

        int[] offeredPlans =
        [
            .. result.SelectMany(draw => new[] { draw.Option1.Number, draw.Option2.Number })
        ];

        Assert.Equal(8, offeredPlans.Distinct().Count());
    }
}

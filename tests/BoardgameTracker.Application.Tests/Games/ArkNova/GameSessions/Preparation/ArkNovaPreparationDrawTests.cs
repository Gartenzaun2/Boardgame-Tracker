using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.Preparation;

public sealed class ArkNovaPreparationDrawTests
{
    [Fact]
    public void Preparation_UsesRegisteredPlayerLimit()
    {
        int maximumPlayers = TestGameLimits.Catalog.GetMaximumPlayers(ArkNovaGame.Key);
        Assert.Same(ArkNovaGame.PlayerLimit, TestGameLimits.Catalog.GetLimit(ArkNovaGame.Key));
        Guid[] players = [.. Enumerable.Range(0, maximumPlayers).Select(_ => Guid.NewGuid())];
        IReadOnlyList<ArkNovaZooPlanDraw> draws = ArkNovaPreparationDraw.Draw(players, new Random(12345));
        IReadOnlyList<PlayerColor> colors = ArkNovaPlayerColors.Rules.AllowedColors;
        ArkNovaGameSessionSetupPlayer[] setupPlayers = [.. draws.Select((draw, index) =>
            new ArkNovaGameSessionSetupPlayer(
                draw.PlayerId, colors[index], draw.Option1.Number, draw.Option2.Number, draw.Option1.Number))];

        ArkNovaGameSessionSetup.Validate(setupPlayers);

        Assert.Throws<ArgumentException>(() => ArkNovaGameSessionSetup.Validate(
            [.. setupPlayers, new(Guid.NewGuid(), PlayerColor.Blue, 9, 10, 9)]));
        Assert.Throws<ArgumentException>(() => ArkNovaPreparationDraw.Draw(
            [.. players, Guid.NewGuid()], new Random(12345)));
    }

    [Fact]
    public void Draw_AssignsTwoUniquePlansToEveryPlayer()
    {
        Guid[] players = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

        IReadOnlyList<ArkNovaZooPlanDraw> result = ArkNovaPreparationDraw.Draw(
            players,
            new Random(12345));

        Assert.Equal(4, result.Count);
        Assert.Equal(players, result.Select(draw => draw.PlayerId));

        int[] offeredPlans =
        [
            .. result.SelectMany(draw => new[] { draw.Option1.Number, draw.Option2.Number })
        ];

        Assert.Equal(8, offeredPlans.Distinct().Count());
    }
}

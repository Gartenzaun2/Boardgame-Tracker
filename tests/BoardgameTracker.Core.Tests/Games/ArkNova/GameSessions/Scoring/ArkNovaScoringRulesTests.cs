using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.GameSessions.Scoring;

public sealed class ArkNovaScoringRulesTests
{
    // Independent reference: Feuerland rules V2.0, board ranges on pp. 4-5 and scoring on p. 19.
    // For each range, the addend is 100 minus its lowest appeal value (e.g. 100 - 76 = 24 at CP 16).
    // https://www.feuerland-spiele.de/fileadmin/game/Arche_Nova/AN_Rules_DE_V2_0_Web.pdf
    // The initial -14 and the +100 shift are also stated in the translated designer announcement:
    // https://www.boardgame.fr/2023/03/31/ark-nova-2-nouvelles-regles-sur-les-prochains-prints-comptage-des-points-version-solo/
    [Theory]
    [InlineData(0, -14)]
    [InlineData(1, -12)]
    [InlineData(2, -10)]
    [InlineData(3, -8)]
    [InlineData(4, -6)]
    [InlineData(5, -4)]
    [InlineData(6, -2)]
    [InlineData(7, 0)]
    [InlineData(8, 2)]
    [InlineData(9, 4)]
    [InlineData(10, 6)]
    [InlineData(11, 9)]
    [InlineData(12, 12)]
    [InlineData(13, 15)]
    [InlineData(14, 18)]
    [InlineData(15, 21)]
    [InlineData(16, 24)]
    [InlineData(17, 27)]
    [InlineData(18, 30)]
    [InlineData(19, 33)]
    [InlineData(20, 36)]
    [InlineData(21, 39)]
    [InlineData(22, 42)]
    [InlineData(23, 45)]
    [InlineData(24, 48)]
    [InlineData(25, 51)]
    [InlineData(26, 54)]
    [InlineData(27, 57)]
    [InlineData(28, 60)]
    [InlineData(29, 63)]
    [InlineData(30, 66)]
    [InlineData(31, 69)]
    [InlineData(32, 72)]
    [InlineData(33, 75)]
    [InlineData(34, 78)]
    [InlineData(35, 81)]
    [InlineData(36, 84)]
    [InlineData(37, 87)]
    [InlineData(38, 90)]
    [InlineData(39, 93)]
    [InlineData(40, 96)]
    [InlineData(41, 99)]
    public void GetConservationAddend_ReturnsOfficialLookupValue(int conservation, int expected)
    {
        Assert.Equal(expected, ArkNovaScoringRules.GetConservationAddend(conservation));
        Assert.Equal(80 + expected, ArkNovaScoringRules.CalculateNewScore(80, conservation));
        Assert.Equal(expected - 20, ArkNovaScoringRules.CalculateOldScore(80, conservation));
    }

    [Fact]
    public void OldScore_IsExactlyOneHundredBelowNewScore()
    {
        int newScore = ArkNovaScoringRules.CalculateNewScore(67, 24);
        int oldScore = ArkNovaScoringRules.CalculateOldScore(67, 24);

        Assert.Equal(115, newScore);
        Assert.Equal(15, oldScore);
    }

    [Theory]
    [InlineData(-1, 0, null)]
    [InlineData(114, 0, null)]
    [InlineData(0, -1, null)]
    [InlineData(0, 42, null)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 16)]
    public void ValidateScoreValues_RejectsOutOfRangeValues(int? attractiveness, int? conservation, int? reputation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArkNovaScoringRules.ValidateScoreValues(attractiveness, conservation, reputation));
    }
}

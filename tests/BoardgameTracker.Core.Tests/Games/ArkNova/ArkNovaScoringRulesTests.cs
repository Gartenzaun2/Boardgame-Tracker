using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Core.Tests.Games.ArkNova;

public sealed class ArkNovaScoringRulesTests
{
    [Theory]
    [InlineData(0, -14)]
    [InlineData(7, 0)]
    [InlineData(16, 24)]
    [InlineData(18, 30)]
    [InlineData(24, 48)]
    [InlineData(41, 99)]
    public void GetConservationAddend_ReturnsOfficialLookupValue(int conservation, int expected)
    {
        Assert.Equal(expected, ArkNovaScoringRules.GetConservationAddend(conservation));
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

using BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

namespace BoardgameTracker.Application.Tests.Games.ArkNova;

public sealed class ArkNovaMaskFractionTests
{
    [Fact]
    public void ValidMaskFractionsArePreservedExactly()
    {
        foreach (double fraction in new[] { 0, .001, .15, .999999999999, 1 })
            Assert.Equal(fraction, ArkNovaMaskFraction.ClampRoundoff(fraction));
    }

    [Fact]
    public void FullMaskReciprocalRoundoffIsBounded()
    {
        Assert.Equal(1, ArkNovaMaskFraction.ClampRoundoff(1.0000000000000002));
        Assert.Equal(0, ArkNovaMaskFraction.ClampRoundoff(-double.Epsilon));
    }

    [Fact]
    public void RealInvalidEvidenceStillFails()
    {
        foreach (double fraction in new[] { -.001, 1.0001, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => ArkNovaMaskFraction.ClampRoundoff(fraction));
    }
}

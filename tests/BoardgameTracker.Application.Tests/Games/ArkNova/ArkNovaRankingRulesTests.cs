using BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;

namespace BoardgameTracker.Application.Tests.Games.ArkNova;

public sealed class ArkNovaRankingRulesTests
{
    [Fact]
    public void WeakReplacementOfExcludedZeroAbstainsInsteadOfSelectingOne()
    {
        RankingObservation[] fields = [Field(0, 0.2, 0.2), Field(1, 0.103, 0.062), Field(20, 0.065, 0.076)];
        RankingResult result = ArkNovaRankingRules.Rank(fields, RankingVariant.A_NoZero);
        Assert.Null(result.Value);
        Assert.DoesNotContain(result.Fields, field => field.Observation.Value == 0);
        Assert.Equal("weak_zero_replacement", result.Abstention);
    }

    [Fact]
    public void StrongUnsupportedZeroDoesNotSnapToOtherFieldAndRemainsValidManually()
    {
        RankingObservation[] fields = [Field(0, 0.6, 0.6), Field(1, 0.2, 0.2), Field(20, 0.1, 0.1)];
        RankingResult result = ArkNovaRankingRules.Rank(fields, RankingVariant.A_NoZero);
        Assert.Null(result.Value);
        Assert.Equal("unsupported_zero_occupancy", result.Abstention);
        Assert.Equal(-14, ArkNovaScoringRules.CalculateNewScore(0, 0));
    }

    [Fact]
    public void DifferenceWithoutColorDoesNotBecomeAPlayerScore()
    {
        RankingResult result = ArkNovaRankingRules.Rank([Field(1, 0, 0), Field(20, 0, 0)], RankingVariant.A_NoZero);
        Assert.Null(result.Value);
    }

    [Fact]
    public void CoreRankingRetainsInformationLostBySaturation()
    {
        RankingObservation[] fields = [Field(0, 0.198, 0.174, 0.9), Field(20, 0.186, 0.205, 0.57)];
        Assert.Equal(0, ArkNovaRankingRules.Rank(fields, RankingVariant.Baseline).Value);
        Assert.Equal(20, ArkNovaRankingRules.Rank(fields, RankingVariant.C_CoreRanking).Value);
    }

    [Fact]
    public void RelativeBlackEvidenceDoesNotChangeOtherColors()
    {
        RankingObservation[] fields = [Field(1, 0.2, 0.2) with { Color = "blue" }, Field(20, 0.1, 0.1) with { Color = "blue" }];
        Assert.Equal(ArkNovaRankingRules.Rank(fields, RankingVariant.Baseline).Value,
            ArkNovaRankingRules.Rank(fields, RankingVariant.B_RelativeBlack).Value);
    }

    private static RankingObservation Field(int value, double full, double core, double difference = 0.5) =>
        new("black", "conservation", value, full, core, difference, difference, full, 1, 1, 0.3, 0.4, 0.3);
}

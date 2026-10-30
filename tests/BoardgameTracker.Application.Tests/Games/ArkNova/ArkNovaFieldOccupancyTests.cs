using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

namespace BoardgameTracker.Application.Tests.Games.ArkNova;

public sealed class ArkNovaFieldOccupancyTests
{
    [Fact]
    public void SaturatedColorEvidenceStillRequiresMarginForHighConfidence()
    {
        double score = ArkNovaFieldOccupancy.CalculateScore(0.5, 0.5, 1, 1);
        Assert.Equal(1, score, 10);
        Assert.Equal(ArkNovaRecognitionConfidence.Uncertain, ArkNovaFieldOccupancy.ClassifyConfidence(score, 0.01, 0.5));
        Assert.Equal(ArkNovaRecognitionConfidence.High, ArkNovaFieldOccupancy.ClassifyConfidence(score, 0.2, 0.5));
    }

    [Fact]
    public void ReferenceDifferenceAloneDoesNotIdentifyPlayerColor()
    {
        double score = ArkNovaFieldOccupancy.CalculateScore(0, 0, 1, 1);
        Assert.Equal(ArkNovaRecognitionConfidence.Uncertain, ArkNovaFieldOccupancy.ClassifyConfidence(score, 0.2, 0));
    }

    [Theory]
    [InlineData(0.7758, 0.371, 0.069, 0.8201)]
    [InlineData(0.7925, 0.382, 0.0803, 0.7946)]
    [InlineData(0.142, 0.2985, 0.2741, -0.7399)]
    public void RecordedThreeDimensionalConflictsAreFlagged(double margin, double predicted, double adjacent, double alignment)
    {
        Assert.True(ArkNovaFieldOccupancy.HasConservationNeighborConflict(ArkNovaRecognitionConfidence.High, margin, predicted, adjacent, alignment));
        Assert.False(ArkNovaFieldOccupancy.HasConservationNeighborConflict(ArkNovaRecognitionConfidence.Medium, margin, predicted, adjacent, alignment));
    }

    [Fact]
    public void IsolatedEvidenceDoesNotCauseConflict()
    {
        Assert.False(ArkNovaFieldOccupancy.HasConservationNeighborConflict(ArkNovaRecognitionConfidence.High, 0.8, 0.4, 0.01, 1));
        Assert.False(ArkNovaFieldOccupancy.HasConservationNeighborConflict(ArkNovaRecognitionConfidence.High, 0.8, 0, 0, 0));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void InvalidEvidenceIsRejected(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ArkNovaFieldOccupancy.CalculateScore(fraction, 0, 0, 0));
    }
}

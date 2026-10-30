using BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

namespace BoardgameTracker.Application.Tests.Games.ArkNova;

public sealed class ArkNovaConfidencePolicyTests
{
    [Fact]
    public void FieldVisibilityAblationRecoversVisibleCandidateOnIncompleteTrack()
    {
        FieldPrediction candidate = Strong(57) with { Confidence = "Uncertain", TrackVisibility = 0.95, Warning = "incomplete_track" };
        Dictionary<string, FieldPrediction> tracks = new() { ["attractiveness"] = candidate };
        Assert.Equal("Uncertain", ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.A_Baseline)["attractiveness"].Confidence);
        Assert.Equal("High", ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.B_FieldVisibility)["attractiveness"].Confidence);
        tracks["attractiveness"] = candidate with { FieldVisibility = 0.9 };
        Assert.Equal("Uncertain", ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.B_FieldVisibility)["attractiveness"].Confidence);
    }

    [Theory]
    [InlineData(0.07, "High")]
    [InlineData(0.35, "Uncertain")]
    public void NeighborAblationUsesBalancedEvidenceAndNeverRewritesValue(double neighbor, string expectedConfidence)
    {
        FieldPrediction candidate = Strong(17) with { GrownFraction = 0.4, Neighbors = [new(16, neighbor, 1)] };
        Dictionary<string, FieldPrediction> tracks = new() { ["conservation"] = candidate };
        FieldPrediction result = ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.C_NeighborBalance)["conservation"];
        Assert.Equal(17, result.Value);
        Assert.Equal(expectedConfidence, result.Confidence);
    }

    [Fact]
    public void SingleStrongBackgroundPatchDoesNotEstablishPlayerColor()
    {
        Dictionary<string, FieldPrediction> tracks = new()
        {
            ["attractiveness"] = Strong(0),
            ["conservation"] = Strong(0) with { CoreColor = 0.02 },
            ["reputation"] = Strong(1) with { CoreColor = 0 }
        };
        Dictionary<string, FieldPrediction> results = ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.E_Guarded);
        Assert.All(results.Values, result =>
        {
            Assert.Null(result.Value);
            Assert.Equal("Uncertain", result.Confidence);
            Assert.Equal("insufficient_color_support", result.Warning);
        });
        // Presence abstention must not erase the raw observation used for diagnostics.
        Assert.Equal(0, tracks["attractiveness"].Value);
    }

    [Fact]
    public void GuardedPolicyCanRecoverSaturatedEvidenceButRejectsAmbiguousCore()
    {
        Dictionary<string, FieldPrediction> tracks = SupportedTracks();
        tracks["attractiveness"] = Strong(57) with { Confidence = "Uncertain", OriginalConfidence = "Uncertain", Margin = 0.01 };
        FieldPrediction recovered = ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.E_Guarded)["attractiveness"];
        Assert.Equal(57, recovered.Value);
        Assert.Equal("Medium", recovered.Confidence);
        tracks["attractiveness"] = tracks["attractiveness"] with { RunnerUpCoreColor = 0.55 };
        Assert.Equal("Uncertain", ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.E_Guarded)["attractiveness"].Confidence);
    }

    [Fact]
    public void UnvalidatedConservationRetainsCandidateButNeverAutoAccepts()
    {
        Dictionary<string, FieldPrediction> tracks = SupportedTracks();
        FieldPrediction result = ArkNovaConfidencePolicy.Evaluate(tracks, ConfidenceVariant.E_Guarded)["conservation"];
        Assert.Equal(tracks["conservation"].Value, result.Value);
        Assert.Equal("Uncertain", result.Confidence);
        Assert.Equal("unvalidated_conservation_evidence", result.Warning);
    }

    private static Dictionary<string, FieldPrediction> SupportedTracks() => new()
    {
        ["attractiveness"] = Strong(57), ["conservation"] = Strong(24), ["reputation"] = Strong(11)
    };

    private static FieldPrediction Strong(int value) => new(value, "High", "High", null, null,
        0.95, 0.4, 0.5, 0.6, 1, 1, 0.7, [], value + 1, 0.55, 0.1, 0.2);
}

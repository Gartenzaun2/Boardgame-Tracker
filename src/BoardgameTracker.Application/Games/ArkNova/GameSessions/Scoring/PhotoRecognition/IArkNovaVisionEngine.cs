namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;

/// <summary>Platform CV boundary. Input is encoded JPEG/PNG; EXIF orientation is normalized on decode.</summary>
public interface IArkNovaVisionEngine
{
    Task<ArkNovaVisionEvidence> AnalyzeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default);
}

public sealed record ArkNovaVisionEvidence(ArkNovaVisionRegistration Registration, IReadOnlyList<RankingObservation> Fields);
public sealed record ArkNovaVisionRegistration(bool Success, int Matches, int Inliers, double? ReprojectionP95,
    double MinimumTrackVisibility, double InlierSpread, IReadOnlyList<double> Homography);

/// <summary>Owns the frozen managed ranking and confidence policy. Never persists a proposal.</summary>
public sealed class ArkNovaPhotoRecognition(IArkNovaVisionEngine engine)
{
    public async Task<ArkNovaPhotoRecognitionResult> AnalyzeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
    {
        ArkNovaVisionEvidence evidence = await engine.AnalyzeAsync(image, cancellationToken);
        if (!evidence.Registration.Success)
            return new(evidence.Registration, new Dictionary<string, ColorScores>());
        Dictionary<string, ColorScores> raw = ArkNovaRankingRules.Predict(evidence.Fields, ArkNovaRankingRules.Selected);
        Dictionary<string, ColorScores> evaluated = raw.ToDictionary(pair => pair.Key,
            pair => new ColorScores(ArkNovaConfidencePolicy.Evaluate(pair.Value.Tracks, ArkNovaConfidencePolicy.Selected)));
        return new(evidence.Registration, evaluated);
    }
}

public sealed record ArkNovaPhotoRecognitionResult(ArkNovaVisionRegistration Registration, IReadOnlyDictionary<string, ColorScores> Colors);

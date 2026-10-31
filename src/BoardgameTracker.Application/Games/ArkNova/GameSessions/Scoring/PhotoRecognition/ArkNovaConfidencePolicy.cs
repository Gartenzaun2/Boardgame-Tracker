namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;

public enum ConfidenceVariant { A_Baseline, B_FieldVisibility, C_NeighborBalance, D_Combined, E_Guarded }

/// <summary>
/// Small, named ablations. Never reads ground truth or changes a candidate to
/// another field. The guarded policy can abstain when color presence is unsupported.
/// </summary>
public static class ArkNovaConfidencePolicy
{
    public const ConfidenceVariant Selected = ConfidenceVariant.E_Guarded;

    public static Dictionary<string, FieldPrediction> Evaluate(Dictionary<string, FieldPrediction> tracks, ConfidenceVariant variant)
    {
        if (variant == ConfidenceVariant.A_Baseline)
            return tracks;
        bool localVisibility = variant is ConfidenceVariant.B_FieldVisibility or ConfidenceVariant.D_Combined or ConfidenceVariant.E_Guarded;
        bool balancedNeighbor = variant is ConfidenceVariant.C_NeighborBalance or ConfidenceVariant.D_Combined or ConfidenceVariant.E_Guarded;
        // A player has independent physical markers on the three tracks. A single
        // background-colored patch is insufficient evidence that the color is present.
        bool colorSupported = tracks.Values.Count(prediction => prediction.Value.HasValue && prediction.CoreColor >= 0.15) >= 2;
        Dictionary<string, FieldPrediction> result = [];
        foreach ((string track, FieldPrediction candidate) in tracks)
        {
            string confidence = candidate.OriginalConfidence;
            string? warning = confidence == "Uncertain" ? "low_score_or_margin" : null;
            int? alternative = null;
            foreach (NeighborEvidence neighbor in candidate.Neighbors.OrderByDescending(neighbor => neighbor.GrownFraction))
            {
                double largest = Math.Max(candidate.GrownFraction, neighbor.GrownFraction);
                double balance = largest > 0 ? Math.Min(candidate.GrownFraction, neighbor.GrownFraction) / largest : 0;
                bool conflict = balancedNeighbor ? neighbor.GrownFraction >= 0.15 && balance >= 0.75 : neighbor.GrownFraction >= 0.06;
                if (track == "conservation" && confidence == "High" && conflict)
                {
                    confidence = "Uncertain";
                    warning = balancedNeighbor ? "balanced_neighbor_conflict" : "conservative_neighbor_evidence";
                    alternative = neighbor.Value;
                    break;
                }
            }
            if (candidate.FieldVisibility < 0.97 || (!localVisibility && candidate.TrackVisibility < 0.99))
            {
                confidence = "Uncertain";
                warning = candidate.FieldVisibility < 0.97 ? "insufficient_field_visibility" : "incomplete_track";
            }
            FieldPrediction evaluated = candidate with { Confidence = confidence, Warning = warning, Alternative = alternative };
            if (variant == ConfidenceVariant.E_Guarded)
            {
                if (!colorSupported)
                    evaluated = evaluated with { Value = null, Confidence = "Uncertain", Warning = "insufficient_color_support" };
                else if (track == "conservation")
                    // Calibration shows high-confidence errors with no neighboring
                    // color evidence. These observations are not separable by B/C.
                    evaluated = evaluated with { Confidence = "Uncertain", Warning = "unvalidated_conservation_evidence" };
                else
                {
                    bool strongCore = candidate.CoreColor >= 0.30 && candidate.FullColor >= 0.20;
                    bool dominantCore = candidate.CoreColor >= 1.5 * candidate.RunnerUpCoreColor;
                    bool supportedReputation = track == "reputation" && candidate.CoreColor >= 0.15
                        && candidate.FullColor >= 0.12 && candidate.Margin >= 0.12;
                    bool accept = candidate.Value.HasValue && candidate.FieldVisibility >= 0.97
                        && dominantCore && (strongCore || supportedReputation) && candidate.Score >= 0.58;
                    if (!accept)
                        evaluated = evaluated with { Confidence = "Uncertain", Warning = "weak_or_ambiguous_core" };
                    else
                        // Saturation clips both top scores near 1. Recover a Medium
                        // only when the unclipped core evidence still dominates.
                        evaluated = evaluated with { Confidence = candidate.OriginalConfidence == "High" ? "High" : "Medium", Warning = null };
                }
            }
            result[track] = evaluated;
        }
        return result;
    }
}

public sealed record FieldPrediction(int? Value, string Confidence, string OriginalConfidence, int? Alternative,
    string? Warning, double Score, double Margin, double FullColor, double CoreColor,
    double TrackVisibility, double FieldVisibility, double GrownFraction, List<NeighborEvidence> Neighbors,
    int RunnerUpValue, double RunnerUpScore, double RunnerUpFullColor, double RunnerUpCoreColor);
public sealed record NeighborEvidence(int Value, double GrownFraction, double FieldVisibility);

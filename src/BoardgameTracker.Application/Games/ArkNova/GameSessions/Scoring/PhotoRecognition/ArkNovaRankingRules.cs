using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;

[Flags]
public enum RankingVariant { Baseline = 0, A_NoZero = 1, B_RelativeBlack = 2, C_CoreRanking = 4 }

public static class ArkNovaRankingRules
{
    public const RankingVariant Selected = RankingVariant.A_NoZero;

    public static RankingResult Rank(IEnumerable<RankingObservation> observations, RankingVariant variant)
    {
        bool excludeZero = variant.HasFlag(RankingVariant.A_NoZero);
        RankedField[] original = observations.Where(field => field.Visibility >= 0.97).Select(field =>
        {
            bool relative = variant.HasFlag(RankingVariant.B_RelativeBlack) && field.Color == "black";
            double full = relative ? field.RelativeFull : field.FullColor;
            double core = relative ? field.RelativeCore : field.CoreColor;
            return new RankedField(field, full, core, relative ? field.RelativeGrown : field.Grown,
                ArkNovaFieldOccupancy.CalculateScore(full, core, field.FullDifference, field.CoreDifference));
        }).OrderByDescending(field => field.Score).ToArray();
        RankedField[] candidates = original.Where(field => !excludeZero || field.Observation.Value != 0).ToArray();
        if (candidates.Length < 2)
            return new(null, "insufficient_visible_candidates", candidates, 0);
        // The clipped color terms are identical for fully saturated fields. In
        // that tier, retain actual core coverage as the primary discriminator.
        bool coreTier = variant.HasFlag(RankingVariant.C_CoreRanking) && Saturated(candidates[0]);
        if (coreTier)
            candidates = candidates.OrderByDescending(field => Saturated(field))
                .ThenByDescending(field => Saturated(field) ? field.CoreColor : field.Score)
                .ThenByDescending(field => field.Score).ToArray();
        RankedField best = candidates[0];
        double gap = coreTier && Saturated(candidates[1])
            ? best.CoreColor - candidates[1].CoreColor : best.Score - candidates[1].Score;
        string? abstention = null;
        if (best.FullColor == 0 && best.CoreColor == 0)
            abstention = "no_color_evidence";
        if (excludeZero)
        {
            // Removing an unsupported area must not turn weak background into a
            // score. Replacements of a zero winner require stronger core support.
            if (best.Score < 0.38 || best.FullColor < 0.06 || best.CoreColor < 0.06)
                abstention = "insufficient_field_evidence";
            else if (original[0].Observation.Value == 0 && best.CoreColor < 0.10)
                abstention = "weak_zero_replacement";
            RankedField? zero = original.FirstOrDefault(field => field.Observation.Value == 0);
            if (zero is not null && zero.FullColor >= 0.30 && zero.CoreColor >= 0.30 && zero.Score >= best.Score)
                abstention = "unsupported_zero_occupancy";
        }
        return new(abstention is null ? best.Observation.Value : null, abstention, candidates, gap);
    }

    public static Dictionary<string, ColorScores> Predict(IEnumerable<RankingObservation> observations, RankingVariant variant)
    {
        Dictionary<string, ColorScores> colors = [];
        foreach (IGrouping<string, RankingObservation> color in observations.GroupBy(field => field.Color))
        {
            Dictionary<string, FieldPrediction> tracks = [];
            foreach (IGrouping<string, RankingObservation> track in color.GroupBy(field => field.Track))
            {
                RankingResult ranking = Rank(track, variant);
                if (ranking.Fields.Length < 2)
                    continue;
                RankedField best = ranking.Fields[0];
                RankedField runner = ranking.Fields[1];
                double margin = Math.Clamp(best.Score - runner.Score, 0, 1);
                string original = ArkNovaFieldOccupancy.ClassifyConfidence(best.Score, margin, Math.Max(best.FullColor, best.CoreColor)).ToString();
                string confidence = original;
                int? alternative = null;
                string? warning = ranking.Abstention;
                List<NeighborEvidence> neighbors = track.Where(field => Math.Abs(field.Value - best.Observation.Value) == 1
                        && (!variant.HasFlag(RankingVariant.A_NoZero) || field.Value != 0))
                    .Select(field => new NeighborEvidence(field.Value,
                        variant.HasFlag(RankingVariant.B_RelativeBlack) && field.Color == "black" ? field.RelativeGrown : field.Grown, field.Visibility)).ToList();
                foreach (NeighborEvidence neighbor in neighbors)
                {
                    if (track.Key == "conservation" && original == "High" && neighbor.GrownFraction >= 0.06)
                    {
                        confidence = "Uncertain";
                        alternative = neighbor.Value;
                        warning ??= "conservative_neighbor_evidence";
                    }
                }
                if (best.Observation.TrackVisibility < 0.99)
                {
                    confidence = "Uncertain";
                    warning ??= "incomplete_track";
                }
                if (ranking.Value is null)
                    confidence = "Uncertain";
                tracks[track.Key] = new(ranking.Value, confidence, original, alternative, warning, best.Score, margin,
                    best.FullColor, best.CoreColor, best.Observation.TrackVisibility, best.Observation.Visibility, best.Grown,
                    neighbors, runner.Observation.Value, runner.Score, runner.FullColor, runner.CoreColor);
            }
            colors[color.Key] = new(tracks);
        }
        return colors;
    }

    private static bool Saturated(RankedField field) => field.FullColor >= 0.18 && field.CoreColor >= 0.16;
}

public sealed record RankingObservation(string Color, string Track, int Value, double FullColor, double CoreColor,
    double FullDifference, double CoreDifference, double Grown, double Visibility, double TrackVisibility,
    double RelativeFull, double RelativeCore, double RelativeGrown);
public sealed record RankedField(RankingObservation Observation, double FullColor, double CoreColor, double Grown, double Score);
public sealed record RankingResult(int? Value, string? Abstention, RankedField[] Fields, double RankingGap);
public sealed record ColorScores(Dictionary<string, FieldPrediction> Tracks);

using System.Text.Json;
using OpenCvSharp;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal sealed class ArkNovaRankingCapture(string? diagnosticDirectory = null, RankingVariant diagnosticVariant = RankingVariant.Baseline)
{
    public List<RankingObservation> Observations { get; } = [];
    public Action<ArkNovaBlackDiagnostics.Frame>? FrameObserver { get; init; }

    public void Capture(ArkNovaBlackDiagnostics.Frame frame, CaptureField[] fields, Mat[] masks, Mat[] relaxed,
        Dictionary<string, double> trackVisibility)
    {
        FrameObserver?.Invoke(frame);
        using Mat seed = Mat.Zeros(frame.Strict.Size(), MatType.CV_8UC1);
        using Mat allowed = Mat.Zeros(frame.Strict.Size(), MatType.CV_8UC1);
        using Mat currentA = SmoothChannel(frame.Lab, 1);
        using Mat currentB = SmoothChannel(frame.Lab, 2);
        using Mat referenceA = SmoothChannel(frame.ReferenceLab, 1);
        using Mat referenceB = SmoothChannel(frame.ReferenceLab, 2);
        int height = seed.Rows;
        int width = seed.Cols;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (frame.Validity.At<byte>(y + frame.Origin.Y, x + frame.Origin.X) == 0)
                    continue;
                Vec3b pixel = frame.Lab.At<Vec3b>(y, x);
                Vec3b reference = frame.ReferenceLab.At<Vec3b>(y, x);
                double lightDelta = pixel.Item0 - reference.Item0 - frame.CurrentSmooth.At<byte>(y, x) + frame.ReferenceSmooth.At<byte>(y, x);
                double correctedA = pixel.Item1 - currentA.At<byte>(y, x) + referenceA.At<byte>(y, x);
                double correctedB = pixel.Item2 - currentB.At<byte>(y, x) + referenceB.At<byte>(y, x);
                double chroma = Math.Sqrt(Math.Pow(correctedA - 128, 2) + Math.Pow(correctedB - 128, 2));
                double referenceChroma = Math.Sqrt(Math.Pow(reference.Item1 - 128, 2) + Math.Pow(reference.Item2 - 128, 2));
                // Reference-dark print is not new black wood. Neutralization of
                // a light/chromatic surface supports added wood, without a V cap.
                if (reference.Item0 < 100 || frame.Difference.At<byte>(y, x) == 0)
                    continue;
                if (lightDelta < -18 && referenceChroma - chroma >= 8)
                    seed.Set(y, x, (byte)255);
                if (lightDelta < -10 && referenceChroma - chroma >= 4)
                    allowed.Set(y, x, (byte)255);
            }
        using Mat relative = ArkNovaPhotoAnalyzer.GrowMask(seed, allowed);
        string[] colors = ["red", "blue", "yellow", "black"];
        for (int index = 0; index < colors.Length; index++)
        {
            using Mat grown = ArkNovaPhotoAnalyzer.GrowMask(masks[index], relaxed[index]);
            foreach (CaptureField field in fields)
            {
                double full = Fraction(masks[index], field, frame.Origin);
                double core = Fraction(masks[index], field, frame.Origin, true);
                double grownFraction = Fraction(grown, field, frame.Origin);
                Observations.Add(new(colors[index], field.Track, field.Value, full, core,
                    Fraction(frame.Difference, field, frame.Origin), Fraction(frame.Difference, field, frame.Origin, true),
                    grownFraction, Fraction(frame.Validity, field, default), trackVisibility[field.Track],
                    index == 3 ? Fraction(relative, field, frame.Origin) : full,
                    index == 3 ? Fraction(relative, field, frame.Origin, true) : core,
                    index == 3 ? Fraction(relative, field, frame.Origin) : grownFraction));
            }
        }
        if (diagnosticDirectory is not null)
            WriteImages(frame, fields, relative);
    }

    private void WriteImages(ArkNovaBlackDiagnostics.Frame frame, CaptureField[] fields, Mat relative)
    {
        Directory.CreateDirectory(diagnosticDirectory!);
        bool useRelative = diagnosticVariant.HasFlag(RankingVariant.B_RelativeBlack);
        RankingObservation[] conservation = Observations.Where(field => field.Color == "black" && field.Track == "conservation").ToArray();
        RankingResult before = ArkNovaRankingRules.Rank(conservation, RankingVariant.Baseline);
        RankingResult after = ArkNovaRankingRules.Rank(conservation, diagnosticVariant);
        File.WriteAllText(Path.Combine(diagnosticDirectory!, "ranking.json"), JsonSerializer.Serialize(new
        {
            variant = diagnosticVariant.ToString(), before, after,
            rank20 = Array.FindIndex(after.Fields, field => field.Observation.Value == 20) + 1,
            allFields = conservation
        }, ArkNovaConfidenceBenchmark.JsonOptions));
        foreach (int value in new[] { 0, 20 })
        {
            CaptureField field = fields.Single(field => field.Track == "conservation" && field.Value == value);
            int left = Math.Max(frame.Origin.X, field.Bounds.X - 24);
            int top = Math.Max(frame.Origin.Y, field.Bounds.Y - 45);
            Rect patch = new(left, top, Math.Min(frame.Origin.X + frame.Strict.Cols, field.Bounds.Right + 24) - left,
                Math.Min(frame.Origin.Y + frame.Strict.Rows, field.Bounds.Bottom + 24) - top);
            using Mat canvas = new(patch.Height + 24, patch.Width * 4, MatType.CV_8UC3, Scalar.Black);
            Mat[] panels = [frame.Warped, frame.Strict, relative, useRelative ? relative : frame.Strict];
            string[] labels = ["photo", "old black", "B experiment", "selected black"];
            for (int column = 0; column < 4; column++)
            {
                Rect sourceRect = column == 0 ? patch : new Rect(patch.X - frame.Origin.X, patch.Y - frame.Origin.Y, patch.Width, patch.Height);
                using Mat source = new(panels[column], sourceRect);
                using Mat color = new();
                if (column == 0)
                    source.CopyTo(color);
                else
                    Cv2.CvtColor(source, color, ColorConversionCodes.GRAY2BGR);
                ArkNovaBlackDiagnostics.DrawField(color, new(field.Value, field.Bounds, field.Mask, field.Core), patch.Location);
                using Mat destination = new(canvas, new Rect(column * patch.Width, 24, patch.Width, patch.Height));
                color.CopyTo(destination);
                Cv2.PutText(canvas, labels[column], new Point(column * patch.Width + 2, 16), HersheyFonts.HersheySimplex, 0.36, Scalar.White);
            }
            using Mat large = new();
            Cv2.Resize(canvas, large, new Size(), 3, 3, InterpolationFlags.Nearest);
            File.WriteAllBytes(Path.Combine(diagnosticDirectory!, $"field-{value}-before-after.png"), large.ImEncode(".png"));
        }
        using Mat winners = frame.Warped.Clone();
        int rank = 0;
        foreach (RankedField ranked in after.Fields.Take(3))
        {
            rank++;
            CaptureField field = fields.Single(field => field.Track == "conservation" && field.Value == ranked.Observation.Value);
            ArkNovaBlackDiagnostics.DrawField(winners, new(field.Value, field.Bounds, field.Mask, field.Core), default);
            Cv2.PutText(winners, $"rank {rank}", new Point(field.Bounds.Left, field.Bounds.Top - 10), HersheyFonts.HersheySimplex,
                0.45, rank == 1 ? Scalar.Red : Scalar.Yellow, 1);
        }
        string text = $"selected={after.Value?.ToString() ?? "null"}; " + string.Join(" / ", after.Fields.Take(3).Select(field => $"{field.Observation.Value}:{field.Score:F3}"));
        Cv2.PutText(winners, text, new Point(20, 40), HersheyFonts.HersheySimplex, 0.75, Scalar.Red, 2);
        File.WriteAllBytes(Path.Combine(diagnosticDirectory!, "winner-top3.png"), winners.ImEncode(".png"));
    }

    private static double Fraction(Mat source, CaptureField field, Point origin, bool core = false)
    {
        using Mat region = new(source, new Rect(field.Bounds.X - origin.X, field.Bounds.Y - origin.Y, field.Bounds.Width, field.Bounds.Height));
        return ArkNovaMaskFraction.ClampRoundoff(Cv2.Mean(region, core ? field.Core : field.Mask).Val0 / 255);
    }

    private static Mat SmoothChannel(Mat image, int channel)
    {
        using Mat extracted = new();
        Mat smoothed = new();
        Cv2.ExtractChannel(image, extracted, channel);
        Cv2.GaussianBlur(extracted, smoothed, new Size(0, 0), 25);
        return smoothed;
    }

    internal sealed record CaptureField(string Track, int Value, Rect Bounds, Mat Mask, Mat Core);
}

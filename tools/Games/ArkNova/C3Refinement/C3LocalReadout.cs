using OpenCvSharp;
using static BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.ClassicDetectors;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>V4 Sparse evidence with narrow field-normal and localized-color changes.</summary>
internal static class C3LocalReadout
{
    internal static readonly string[] Colors = ["red", "blue", "yellow", "black"];

    public static double[][] Intensities(Mat color, ClassicDetectors geometry)
    {
        using Mat gray = ClassicDetectors.Gray(color);
        return geometry.Regions.Select(region =>
        {
            using Mat patch = new(gray, region.Patch);
            using Mat small = new();
            Cv2.Resize(patch, small, new Size(16, 16), 0, 0, InterpolationFlags.Area);
            return Enumerable.Range(0, 256).Select(pixel => small.At<byte>(pixel / 16, pixel % 16) / 255.0).ToArray();
        }).ToArray();
    }

    public static Measurement Measure(float[] sparse, Region region, Limits limits, double visibility, byte[][]? colors = null)
    {
        using Mat small = new(16, 16, MatType.CV_32FC1);
        small.SetArray(sparse);
        Mat heat = new();
        Cv2.Resize(small, heat, region.Patch.Size, 0, 0, InterpolationFlags.Linear);
        using Mat threshold = new();
        Cv2.Threshold(heat, threshold, limits.Pixel, 255, ThresholdTypes.Binary);
        threshold.ConvertTo(threshold, MatType.CV_8UC1);
        using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        Cv2.MorphologyEx(threshold, threshold, MorphTypes.Open, kernel);
        using Mat labels = new();
        using Mat stats = new();
        using Mat centers = new();
        int count = Cv2.ConnectedComponentsWithStats(threshold, labels, stats, centers);
        int[] contact = new int[count];
        int[] interior = new int[count];
        int width = region.Patch.Width;
        int height = region.Patch.Height;
        int area = Cv2.CountNonZero(region.Mask);
        int coreArea = Cv2.CountNonZero(region.Core);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int label = labels.At<int>(y, x);
                if (label == 0)
                    continue;
                contact[label] += region.Mask.At<byte>(y, x) != 0 ? 1 : 0;
                interior[label] += region.Core.At<byte>(y, x) != 0 ? 1 : 0;
            }
        bool[] keep = Enumerable.Range(0, count).Select(label => label > 0 && stats.At<int>(label, (int)ConnectedComponentsTypes.Area) >= 8
            && contact[label] >= .03 * area && interior[label] >= .01 * coreArea).ToArray();
        Mat gate = Mat.Zeros(region.Patch.Size, MatType.CV_8UC1);
        int total = 0;
        int core = 0;
        int[] strict = new int[4];
        int[] colored = new int[4];
        int[] coloredCore = new int[4];
        int[] coloredOutside = new int[4];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int label = labels.At<int>(y, x);
                if (!keep[label])
                    continue;
                int pixel = (region.Patch.Y + y) * 2000 + region.Patch.X + x;
                bool inside = region.Mask.At<byte>(y, x) != 0;
                bool inCore = region.Core.At<byte>(y, x) != 0;
                if (inside)
                {
                    gate.Set(y, x, (byte)255);
                    total++;
                    core += inCore ? 1 : 0;
                }
                if (colors is null)
                    continue;
                for (int color = 0; color < 4; color++)
                {
                    bool seed = colors[color][pixel] != 0;
                    bool supported = colors[color == 3 ? 4 : color][pixel] != 0;
                    if (inside && seed)
                        strict[color]++;
                    if (inside && supported)
                    {
                        colored[color]++;
                        coloredCore[color] += inCore ? 1 : 0;
                    }
                    if (!inside && supported)
                        coloredOutside[color]++;
                }
            }
        double score = .5 * total / area + .5 * core / coreArea;
        bool candidate = region.Value != 0 && visibility >= .97 && score > limits.Occupancy;
        ColorEvidence[] evidence = Enumerable.Range(0, 4).Select(index =>
        {
            double density = total > 0 ? (double)colored[index] / total : 0;
            double coreFraction = (double)coloredCore[index] / coreArea;
            bool supported = candidate && colored[index] >= 6 && coloredCore[index] >= 2 && strict[index] >= (index == 3 ? 3 : 6) && density >= .10;
            bool strong = supported && score >= (region.Track == "attractiveness" ? 2 : 1.5) * limits.Occupancy
                && density >= .20 && coreFraction >= .06;
            bool veryStrong = strong && score >= 2 * limits.Occupancy && density >= .35 && coreFraction >= .12;
            return new ColorEvidence(Colors[index], strict[index], colored[index], coloredCore[index], coloredOutside[index],
                density, coreFraction, supported, strong, veryStrong);
        }).ToArray();
        return new(new(region.Track, region.Value, score, limits.Pixel, limits.Occupancy, candidate, visibility, total, core, evidence), heat, gate);
    }

    public static Dictionary<string, Prediction> Predict(Field[] fields, RankingObservation[] original, bool local)
    {
        Dictionary<string, Prediction> predictions = [];
        foreach (string color in Colors)
            foreach (string track in new[] { "attractiveness", "conservation", "reputation" })
            {
                List<Hypothesis> hypotheses = [];
                foreach (Field field in fields.Where(field => field.Track == track && field.Candidate))
                {
                    ColorEvidence support = field.Colors.Single(evidence => evidence.Color == color);
                    if (local)
                    {
                        if (support.Supported)
                            hypotheses.Add(new(field.Value, field.Score, support.CoreFraction, support.Density, support.Strong, support.VeryStrong));
                    }
                    else
                    {
                        RankingObservation[] colors = original.Where(observation => observation.Track == track && observation.Value == field.Value)
                            .OrderByDescending(observation => observation.CoreColor).ToArray();
                        if (colors.Length >= 2 && colors[0].Color == color && colors[0].CoreColor >= .06
                            && colors[0].CoreColor >= 1.5 * Math.Max(.001, colors[1].CoreColor))
                            hypotheses.Add(new(field.Value, field.Score, support.CoreFraction, support.Density, support.Strong, support.VeryStrong));
                    }
                }
                Hypothesis[] ranked = local ? hypotheses.OrderByDescending(value => value.CoreFraction).ThenByDescending(value => value.Occupancy).ToArray()
                    : hypotheses.OrderByDescending(value => value.Occupancy).ToArray();
                double gap = ranked.Length > 0 ? ranked[0].CoreFraction - (ranked.Length > 1 ? ranked[1].CoreFraction : 0) : 0;
                predictions[color + "/" + track] = new(ranked.FirstOrDefault()?.Value, ranked.Take(3).Select(item => item.Value).ToArray(),
                    ranked.FirstOrDefault()?.Strong ?? false, ranked.FirstOrDefault()?.VeryStrong ?? false, gap, ranked.Take(3).ToArray());
            }
        return predictions;
    }

    public static int? Fuse(string rule, string track, FieldPrediction? a, FieldPrediction? confidence, Prediction c3, bool colorSupportedElsewhere)
    {
        bool aSafe = confidence?.Value.HasValue == true && confidence.Confidence is "High" or "Medium";
        if (aSafe)
            return c3.Strong && c3.Value != confidence!.Value ? null : confidence!.Value;
        if (!c3.Strong || c3.Value is null)
            return null;
        bool agree = a?.Value == c3.Value;
        if (rule == "F1")
            return agree ? c3.Value : null;
        if (track == "attractiveness")
            return agree && c3.VeryStrong && c3.Gap >= .03 && colorSupportedElsewhere ? c3.Value : null;
        if (track == "conservation")
        {
            bool aStrong = a?.Value.HasValue == true && a.Score >= .80 && a.Margin >= .20 && a.CoreColor >= .20;
            return c3.Gap >= .03 && !(aStrong && !agree) ? c3.Value : null;
        }
        return agree || (a?.Value is null && c3.VeryStrong && c3.Gap >= .03 && colorSupportedElsewhere) ? c3.Value : null;
    }

    public static void Export(string path, Mat color, Region region, Measurement result, string label)
    {
        using Mat patch = new(color, region.Patch);
        using Mat photo = patch.Clone();
        using Mat mask = region.Mask.Clone();
        Cv2.FindContours(mask, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Cv2.DrawContours(photo, contours, -1, Scalar.Yellow, 1);
        using Mat heat8 = new();
        using Mat heat = new();
        result.Heat.ConvertTo(heat8, MatType.CV_8UC1, 255 * 3);
        Cv2.ApplyColorMap(heat8, heat, ColormapTypes.Turbo);
        using Mat gated = new();
        Cv2.CvtColor(result.Gate, gated, ColorConversionCodes.GRAY2BGR);
        using Mat row = new();
        Cv2.HConcat([photo, heat, gated], row);
        using Mat large = new();
        Cv2.Resize(row, large, new Size(), 3, 3, InterpolationFlags.Nearest);
        using Mat canvas = new(large.Rows + 28, large.Cols, MatType.CV_8UC3, Scalar.Black);
        using Mat destination = new(canvas, new Rect(0, 28, large.Width, large.Height));
        large.CopyTo(destination);
        Cv2.PutText(canvas, label, new Point(3, 18), HersheyFonts.HersheySimplex, .4, Scalar.White);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, canvas.ImEncode(".png"));
    }

    internal sealed record ColorEvidence(string Color, int Seeds, int Contact, int Core, int Outside, double Density, double CoreFraction,
        bool Supported, bool Strong, bool VeryStrong);
    internal sealed record Field(string Track, int Value, double Score, double PixelThreshold, double OccupancyThreshold,
        bool Candidate, double Visibility, int Contact, int Core, ColorEvidence[] Colors);
    internal sealed record Hypothesis(int Value, double Occupancy, double CoreFraction, double Density, bool Strong, bool VeryStrong);
    internal sealed record Prediction(int? Value, int[] Top3, bool Strong, bool VeryStrong, double Gap, Hypothesis[] Hypotheses);
    internal sealed record Measurement(Field Field, Mat Heat, Mat Gate) : IDisposable
    {
        public void Dispose() { Heat.Dispose(); Gate.Dispose(); }
    }
}

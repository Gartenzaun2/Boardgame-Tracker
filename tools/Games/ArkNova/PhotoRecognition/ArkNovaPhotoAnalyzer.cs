using System.Diagnostics;
using System.Text.Json;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>
/// Windows-only development spike. Owns native resources; no CV dependency is
/// referenced by the app, Application, or Core. Not safe for concurrent calls.
/// </summary>
internal sealed class ArkNovaPhotoAnalyzer : IDisposable
{
    private const int RegistrationEdge = 1600;
    private const double AnalysisScale = 0.5;
    private readonly SIFT _features = SIFT.Create(6000);
    private readonly Mat _reference;
    private readonly Mat _referenceDescriptors = new();
    private readonly KeyPoint[] _referencePoints;
    private readonly double _referenceRegistrationScale;
    private readonly List<Field> _fields = [];
    private readonly Rect _scoringBounds;
    public double PreparationMilliseconds { get; }

    public ArkNovaPhotoAnalyzer(string assetDirectory)
    {
        Stopwatch timer = Stopwatch.StartNew();
        using JsonDocument geometry = JsonDocument.Parse(File.ReadAllText(Path.Combine(assetDirectory, "scoring_regions_v2.json")));
        using Mat master = ReadImage(Path.Combine(assetDirectory, "board_reference_master_clean.png"));
        JsonElement reference = geometry.RootElement.GetProperty("reference");
        if (master.Width != reference.GetProperty("width_px").GetInt32()
            || master.Height != reference.GetProperty("height_px").GetInt32())
            throw new InvalidDataException("Reference image and polygon coordinate system do not match.");
        _referenceRegistrationScale = RegistrationEdge / (double)Math.Max(master.Width, master.Height);
        using Mat registration = Resize(master, _referenceRegistrationScale);
        using Mat gray = new();
        Cv2.CvtColor(registration, gray, ColorConversionCodes.BGR2GRAY);
        _features.DetectAndCompute(gray, null, out _referencePoints, _referenceDescriptors);
        _reference = Resize(master, AnalysisScale);
        foreach (JsonProperty track in geometry.RootElement.GetProperty("tracks").EnumerateObject())
        {
            foreach (JsonElement field in track.Value.GetProperty("fields").EnumerateArray())
            {
                Point[] polygon = field.GetProperty("polygon").EnumerateArray()
                    .Select(point => new Point((int)Math.Round(point[0].GetDouble() * AnalysisScale),
                        (int)Math.Round(point[1].GetDouble() * AnalysisScale))).ToArray();
                Rect bounds = Cv2.BoundingRect(polygon);
                Point[] local = polygon.Select(point => new Point(point.X - bounds.X, point.Y - bounds.Y)).ToArray();
                Mat mask = Mat.Zeros(bounds.Height, bounds.Width, MatType.CV_8UC1);
                Cv2.FillPoly(mask, [local], Scalar.White);
                Mat core = new();
                int erosion = Math.Max(1, (int)(Math.Min(bounds.Width, bounds.Height) * 0.12));
                using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(erosion * 2 + 1, erosion * 2 + 1));
                // Explicit zero border is essential: a rectangular polygon fills its bounding box.
                Cv2.Erode(mask, core, kernel, borderType: BorderTypes.Constant, borderValue: Scalar.Black);
                _fields.Add(new Field(track.Name, field.GetProperty("value").GetInt32(), bounds, mask, core));
            }
        }
        int left = _fields.Min(field => field.Bounds.Left);
        int top = _fields.Min(field => field.Bounds.Top);
        _scoringBounds = new Rect(left, top, _fields.Max(field => field.Bounds.Right) - left,
            _fields.Max(field => field.Bounds.Bottom) - top);
        PreparationMilliseconds = timer.Elapsed.TotalMilliseconds;
    }

    public RecognitionResult Analyze(string imagePath, ArkNovaBlackDiagnostics? diagnostics = null, ArkNovaRankingCapture? rankingCapture = null)
    {
        Stopwatch timer = Stopwatch.StartNew();
        using Mat image = ReadImage(imagePath); // OpenCV normalizes EXIF orientation by default.
        double decode = timer.Elapsed.TotalMilliseconds;
        double scale = Math.Min(1, RegistrationEdge / (double)Math.Max(image.Width, image.Height));
        using Mat small = Resize(image, scale);
        using Mat gray = new();
        Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        using Mat descriptors = new();
        _features.DetectAndCompute(gray, null, out KeyPoint[] points, descriptors);
        if (descriptors.Empty())
            return Failed("No image features.", timer, decode);
        using BFMatcher matcher = new(NormTypes.L2);
        DMatch[] matches = matcher.KnnMatch(descriptors, _referenceDescriptors, 2)
            .Where(pair => pair.Length == 2 && pair[0].Distance < 0.72 * pair[1].Distance)
            .Select(pair => pair[0]).ToArray();
        if (matches.Length < 24)
            return Failed("Too few reference matches.", timer, decode, matches.Length);
        Point2d[] source = matches.Select(match => new Point2d(points[match.QueryIdx].Pt.X, points[match.QueryIdx].Pt.Y)).ToArray();
        Point2d[] target = matches.Select(match => new Point2d(_referencePoints[match.TrainIdx].Pt.X, _referencePoints[match.TrainIdx].Pt.Y)).ToArray();
        using Mat inlierMask = new();
        using Mat homography = Cv2.FindHomography(InputArray.Create(source), InputArray.Create(target),
            HomographyMethods.Ransac, 3, inlierMask);
        if (homography.Empty() || !Cv2.CheckRange(homography))
            return Failed("No finite homography.", timer, decode, matches.Length);
        int inliers = Cv2.CountNonZero(inlierMask);
        Point2d[] projected = Cv2.PerspectiveTransform(source, homography);
        double[] errors = Enumerable.Range(0, matches.Length).Where(index => inlierMask.At<byte>(index) != 0)
            .Select(index => projected[index].DistanceTo(target[index])).Order().ToArray();
        double p95 = errors.Length > 0 ? errors[(int)((errors.Length - 1) * 0.95)] : double.MaxValue;
        // Work in analysis coordinates from here onward, keeping input at registration resolution.
        using Mat transform = homography.Clone();
        for (int row = 0; row < 2; row++)
            for (int column = 0; column < 3; column++)
                transform.Set(row, column, transform.At<double>(row, column) * AnalysisScale / _referenceRegistrationScale);
        Point2f[] corners = [new(0, 0), new(small.Width, 0), new(small.Width, small.Height), new(0, small.Height)];
        Point2f[] footprint = Cv2.PerspectiveTransform(corners, transform);
        double footprintArea = Math.Abs(Cv2.ContourArea(footprint));
        using Mat validitySource = new(small.Size(), MatType.CV_8UC1, Scalar.White);
        using Mat validity = new();
        Cv2.WarpPerspective(validitySource, validity, transform, _reference.Size(), InterpolationFlags.Nearest);
        Dictionary<string, double> trackVisibility = _fields.GroupBy(field => field.Track)
            .ToDictionary(group => group.Key, group => group.Average(field => Fraction(validity, field, false)));
        double visibility = trackVisibility.Values.Min();
        // Inliers must cover a substantial part of the scoring area, not one small patch.
        Point2f[] inlierTargets = Enumerable.Range(0, matches.Length).Where(index => inlierMask.At<byte>(index) != 0)
            .Select(index => new Point2f((float)(target[index].X * AnalysisScale / _referenceRegistrationScale),
                (float)(target[index].Y * AnalysisScale / _referenceRegistrationScale))).ToArray();
        double scoringArea = _scoringBounds.Width * _scoringBounds.Height;
        double spread = inlierTargets.Length >= 3 ? Math.Abs(Cv2.ContourArea(Cv2.ConvexHull(inlierTargets))) / scoringArea : 0;
        bool success = inliers >= 20 && (double)inliers / matches.Length >= 0.35 && p95 <= 3
            && footprint.All(point => float.IsFinite(point.X) && float.IsFinite(point.Y))
            && Cv2.IsContourConvex(footprint) && footprintArea >= scoringArea * 0.8
            && visibility >= 0.75 && spread >= 0.25;
        RegistrationDiagnostics registration = new(success, matches.Length, inliers, p95, visibility, spread,
            success ? null : "Registration quality or track visibility is insufficient.");
        double registered = timer.Elapsed.TotalMilliseconds;
        if (!success)
            return new(registration, [], new(decode, registered - decode, 0, timer.Elapsed.TotalMilliseconds));
        using Mat warped = new();
        Cv2.WarpPerspective(small, warped, transform, _reference.Size());
        diagnostics?.RecordRegistration(registration, projected, target, inlierMask, AnalysisScale / _referenceRegistrationScale);
        Dictionary<string, ColorScores> colors = AnalyzeFields(warped, validity, trackVisibility, diagnostics, rankingCapture);
        return new(registration, colors, new(decode, registered - decode,
            timer.Elapsed.TotalMilliseconds - registered, timer.Elapsed.TotalMilliseconds));
    }

    private Dictionary<string, ColorScores> AnalyzeFields(Mat warped, Mat validity, Dictionary<string, double> trackVisibility,
        ArkNovaBlackDiagnostics? diagnostics, ArkNovaRankingCapture? rankingCapture)
    {
        using Mat currentRegion = new(warped, _scoringBounds);
        using Mat referenceRegion = new(_reference, _scoringBounds);
        using Mat visibleRegion = new(validity, _scoringBounds);
        using Mat hsv = new();
        using Mat lab = new();
        using Mat referenceLab = new();
        Cv2.CvtColor(currentRegion, hsv, ColorConversionCodes.BGR2HSV);
        Cv2.CvtColor(currentRegion, lab, ColorConversionCodes.BGR2Lab);
        Cv2.CvtColor(referenceRegion, referenceLab, ColorConversionCodes.BGR2Lab);
        // Low-frequency correction estimates illumination, preserving local marker differences.
        using Mat currentLight = new();
        using Mat referenceLight = new();
        using Mat currentSmooth = new();
        using Mat referenceSmooth = new();
        Cv2.ExtractChannel(lab, currentLight, 0);
        Cv2.ExtractChannel(referenceLab, referenceLight, 0);
        Cv2.GaussianBlur(currentLight, currentSmooth, new Size(0, 0), 25);
        Cv2.GaussianBlur(referenceLight, referenceSmooth, new Size(0, 0), 25);
        using Mat difference = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat red = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat blue = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat yellow = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat black = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat relaxedRed = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat relaxedBlue = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat relaxedYellow = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        using Mat relaxedBlack = Mat.Zeros(hsv.Size(), MatType.CV_8UC1);
        Mat[] masks = [red, blue, yellow, black];
        Mat[] relaxed = [relaxedRed, relaxedBlue, relaxedYellow, relaxedBlack];
        int height = hsv.Height;
        int width = hsv.Width;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visibleRegion.At<byte>(y, x) == 0)
                    continue;
                Vec3b color = hsv.At<Vec3b>(y, x);
                Vec3b pixel = lab.At<Vec3b>(y, x);
                Vec3b empty = referenceLab.At<Vec3b>(y, x);
                double lightDelta = pixel.Item0 - empty.Item0 - currentSmooth.At<byte>(y, x) + referenceSmooth.At<byte>(y, x);
                double distance = Math.Sqrt(lightDelta * lightDelta
                    + Math.Pow(pixel.Item1 - empty.Item1, 2) + Math.Pow(pixel.Item2 - empty.Item2, 2));
                bool changed = distance > 24;
                if (changed)
                    difference.Set(y, x, (byte)255);
                int hue = color.Item0;
                int saturation = color.Item1;
                int value = color.Item2;
                bool[] strict = [(hue < 12 || hue > 170) && saturation > 95 && value > 55,
                    hue >= 80 && hue <= 135 && saturation > 70 && value > 45,
                    hue >= 18 && hue <= 40 && saturation > 95 && value > 90,
                    value < 85 && lightDelta < -18];
                bool[] loose = [(hue < 18 || hue > 165) && saturation > 30 && value > 25,
                    hue >= 75 && hue <= 140 && saturation > 30 && value > 25,
                    hue >= 15 && hue <= 45 && saturation > 40 && value > 45,
                    value < 110 && lightDelta < -10];
                for (int index = 0; index < 4; index++)
                {
                    if (strict[index] && changed)
                        masks[index].Set(y, x, (byte)255);
                    if (loose[index] && changed)
                        relaxed[index].Set(y, x, (byte)255);
                }
            }
        }
        rankingCapture?.Capture(new(warped, _reference, hsv, lab, referenceLab, currentSmooth, referenceSmooth,
            difference, black, relaxedBlack, black, validity, _scoringBounds.Location),
            _fields.Select(field => new ArkNovaRankingCapture.CaptureField(field.Track, field.Value, field.Bounds, field.Mask, field.Core)).ToArray(),
            masks, relaxed, trackVisibility);
        Dictionary<string, ColorScores> results = [];
        string[] names = ["red", "blue", "yellow", "black"];
        for (int color = 0; color < names.Length; color++)
        {
            using Mat grown = GrowMask(masks[color], relaxed[color]);
            if (color == 3 && diagnostics is not null)
                diagnostics.Capture(new(warped, _reference, hsv, lab, referenceLab, currentSmooth, referenceSmooth,
                    difference, masks[color], relaxed[color], grown, validity, _scoringBounds.Location),
                    _fields.Where(field => field.Track == "conservation").Select(field =>
                        new ArkNovaBlackDiagnostics.DiagnosticField(field.Value, field.Bounds, field.Mask, field.Core)).ToArray());
            Dictionary<string, FieldPrediction> tracks = [];
            foreach (string track in new[] { "attractiveness", "conservation", "reputation" })
            {
                List<FieldEvidence> evidence = _fields.Where(field => field.Track == track && Fraction(validity, field, false) >= 0.97).Select(field =>
                {
                    double full = Fraction(masks[color], field, false, _scoringBounds.Location);
                    double core = Fraction(masks[color], field, true, _scoringBounds.Location);
                    double diff = Fraction(difference, field, false, _scoringBounds.Location);
                    double coreDiff = Fraction(difference, field, true, _scoringBounds.Location);
                    return new FieldEvidence(field, ArkNovaFieldOccupancy.CalculateScore(full, core, diff, coreDiff), full, core);
                }).OrderByDescending(item => item.Score).ToList();
                FieldEvidence best = evidence[0];
                double margin = Math.Max(0, best.Score - evidence[1].Score);
                ArkNovaRecognitionConfidence original = ArkNovaFieldOccupancy.ClassifyConfidence(best.Score, margin, Math.Max(best.FullColor, best.CoreColor));
                ArkNovaRecognitionConfidence confidence = original;
                int? alternative = null;
                string? warning = null;
                double grownFraction = Fraction(grown, best.Field, false, _scoringBounds.Location);
                List<NeighborEvidence> neighbors = [];
                foreach (Field neighbor in _fields.Where(field => field.Track == track && Math.Abs(field.Value - best.Field.Value) == 1))
                {
                    double neighborFraction = Fraction(grown, neighbor, false, _scoringBounds.Location);
                    neighbors.Add(new(neighbor.Value, neighborFraction, Fraction(validity, neighbor, false)));
                    // The original Python growth/alignment code was not supplied. Until
                    // independently calibrated, any appreciable adjacent grown evidence
                    // conservatively vetoes High. Zero alignment disables the uncalibrated
                    // directional rule; a homography is not a camera pose estimate.
                    bool balancedConflict = ArkNovaFieldOccupancy.HasConservationNeighborConflict(original, margin, grownFraction, neighborFraction, 0);
                    if (track == "conservation" && (balancedConflict
                        || (original == ArkNovaRecognitionConfidence.High && neighborFraction >= 0.06)))
                    {
                        confidence = ArkNovaRecognitionConfidence.Uncertain;
                        alternative = neighbor.Value;
                        warning = balancedConflict ? "balanced_neighbor_conflict" : "conservative_neighbor_evidence";
                    }
                }
                // No evidence is not a valid zero score.
                bool visible = Fraction(validity, best.Field, false) >= 0.97;
                if (!visible || trackVisibility[track] < 0.99)
                {
                    confidence = ArkNovaRecognitionConfidence.Uncertain;
                    warning ??= visible ? "incomplete_track" : "insufficient_field_visibility";
                }
                tracks[track] = new(visible && (best.FullColor > 0 || best.CoreColor > 0) ? best.Field.Value : null,
                    confidence.ToString(), original.ToString(), alternative, warning, best.Score, margin, best.FullColor, best.CoreColor,
                    trackVisibility[track], Fraction(validity, best.Field, false), grownFraction, neighbors,
                    evidence[1].Field.Value, evidence[1].Score, evidence[1].FullColor, evidence[1].CoreColor);
            }
            results[names[color]] = new(tracks);
        }
        return results;
    }

    internal static Mat GrowMask(Mat seed, Mat allowed)
    {
        Mat grown = seed.Clone();
        using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        using Mat next = new();
        // Bounded reference-guided expansion; this is evidence, not object reconstruction.
        for (int iteration = 0; iteration < 12; iteration++)
        {
            Cv2.Dilate(grown, next, kernel);
            Cv2.BitwiseAnd(next, allowed, next);
            Cv2.BitwiseOr(next, seed, next);
            if (Cv2.CountNonZero(next) == Cv2.CountNonZero(grown))
                break;
            next.CopyTo(grown);
        }
        return grown;
    }

    private static double Fraction(Mat image, Field field, bool core, Point origin = default)
    {
        using Mat region = new(image, new Rect(field.Bounds.X - origin.X, field.Bounds.Y - origin.Y, field.Bounds.Width, field.Bounds.Height));
        Mat mask = core ? field.Core : field.Mask;
        return Cv2.Mean(region, mask).Val0 / 255;
    }

    private static Mat Resize(Mat image, double scale)
    {
        Mat resized = new();
        Cv2.Resize(image, resized, new Size(), scale, scale, InterpolationFlags.Area);
        return resized;
    }

    private static Mat ReadImage(string path)
    {
        // ImDecode supports Unicode file names on Windows.
        Mat image = Cv2.ImDecode(File.ReadAllBytes(path), ImreadModes.Color);
        if (image.Empty())
        {
            image.Dispose();
            throw new InvalidDataException($"Cannot decode image: {path}");
        }
        return image;
    }

    private static RecognitionResult Failed(string reason, Stopwatch timer, double decode, int matches = 0) =>
        new(new(false, matches, 0, null, 0, 0, reason), [], new(decode, timer.Elapsed.TotalMilliseconds - decode, 0, timer.Elapsed.TotalMilliseconds));

    public void Dispose()
    {
        _features.Dispose();
        _reference.Dispose();
        _referenceDescriptors.Dispose();
        foreach (Field field in _fields)
        {
            field.Mask.Dispose();
            field.Core.Dispose();
        }
    }

    private sealed record Field(string Track, int Value, Rect Bounds, Mat Mask, Mat Core);
    private sealed record FieldEvidence(Field Field, double Score, double FullColor, double CoreColor);
}

internal sealed record RegistrationDiagnostics(bool Success, int Matches, int Inliers, double? ReprojectionP95,
    double MinimumTrackVisibility, double InlierSpread, string? Failure);
internal sealed record StageTimings(double DecodeMilliseconds, double RegistrationMilliseconds, double OccupancyMilliseconds, double TotalMilliseconds);
internal sealed record RecognitionResult(RegistrationDiagnostics Registration, Dictionary<string, ColorScores> Colors, StageTimings Timings);

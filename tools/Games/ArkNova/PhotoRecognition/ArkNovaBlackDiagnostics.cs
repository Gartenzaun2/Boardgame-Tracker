using System.Text.Json;
using System.Globalization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using OpenCvSharp;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>
/// Read-only observer of the existing pipeline. All display operations use clones;
/// raw Mats are borrowed only for the duration of Capture. No score is fed back.
/// </summary>
internal sealed class ArkNovaBlackDiagnostics(string outputDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly List<Residual> _residuals = [];
    private RegistrationDiagnostics? _registration;
    public List<FieldMeasurement> Measurements { get; } = [];

    public static int Run(string assets, string dataset, string output)
    {
        Directory.CreateDirectory(output);
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        List<object> images = [];
        List<string> table = ["| Foto | Feld | FullColor | CoreColor | FullDiff | CoreDiff | Score | Grown | Sichtbar | Polygon px | Core px |", "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|"];
        foreach (int number in new[] { 4265, 4266, 4267, 4268 })
        {
            string name = $"IMG_{number}";
            string directory = Path.Combine(output, name);
            Directory.CreateDirectory(directory);
            ArkNovaBlackDiagnostics observer = new(directory);
            RecognitionResult result = analyzer.Analyze(Path.Combine(dataset, "images", name + ".jpg"), observer);
            if (!result.Registration.Success || observer.Measurements.Count == 0)
                throw new InvalidDataException($"Diagnosis requires successful registration: {name}");
            FieldMeasurement twenty = observer.Measurements.Single(field => field.Value == 20);
            FieldMeasurement twentyOne = observer.Measurements.Single(field => field.Value == 21);
            object contrasts = new
            {
                score20Minus21 = twenty.Score - twentyOne.Score,
                fullColor20Minus21 = twenty.FullColor - twentyOne.FullColor,
                coreColor20Minus21 = twenty.CoreColor - twentyOne.CoreColor,
                fullDifference20Minus21 = twenty.FullDifference - twentyOne.FullDifference,
                coreDifference20Minus21 = twenty.CoreDifference - twentyOne.CoreDifference,
                differenceContribution20Minus21 = 0.10 * (twenty.FullDifference + twenty.CoreDifference - twentyOne.FullDifference - twentyOne.CoreDifference)
            };
            object report = new { image = name, result.Registration, rawBlack = result.Colors["black"], observer.Measurements, contrasts };
            WriteJson(Path.Combine(directory, "measurements.json"), report);
            images.Add(report);
            foreach (FieldMeasurement field in observer.Measurements)
                table.Add(FormattableString.Invariant($"| {name} | {field.Value} | {field.FullColor:F5} | {field.CoreColor:F5} | {field.FullDifference:F5} | {field.CoreDifference:F5} | {field.Score:F5} | {field.GrownFraction:F5} | {field.FieldVisibility:F5} | {field.PolygonPixelArea} | {field.CorePixelArea} |"));
            Console.WriteLine($"{name}: raw winner={result.Colors["black"].Tracks["conservation"].Value}; score20={twenty.Score:F5}; score21={twentyOne.Score:F5}");
        }
        WriteJson(Path.Combine(output, "summary.json"), new { coordinateSystem = "Analysis image 2000x1400; multiply coordinates and residuals by 2 for master pixels. Areas are raster mask pixels.", images });
        File.WriteAllLines(Path.Combine(output, "measurements.md"), table);
        return 0;
    }

    public void RecordRegistration(RegistrationDiagnostics registration, Point2d[] projected, Point2d[] target, Mat inliers, double scale)
    {
        _registration = registration;
        for (int index = 0; index < target.Length; index++)
        {
            if (inliers.At<byte>(index) != 0)
                _residuals.Add(new(target[index].X * scale, target[index].Y * scale,
                    (projected[index].X - target[index].X) * scale, (projected[index].Y - target[index].Y) * scale));
        }
    }

    public void Capture(Frame frame, DiagnosticField[] fields)
    {
        DiagnosticField[] focus = fields.Where(field => field.Value is 19 or 20 or 21).OrderBy(field => field.Value).ToArray();
        int x = focus.Min(field => field.Bounds.Left) - 24;
        int y = focus.Min(field => field.Bounds.Top) - 65;
        Rect region = new(x, y, focus.Max(field => field.Bounds.Right) - x + 24, focus.Max(field => field.Bounds.Bottom) - y + 38);
        using Mat fullOverlay = frame.Warped.Clone();
        foreach (DiagnosticField field in focus)
            DrawField(fullOverlay, field, default);
        Save("01-registered-full.png", fullOverlay);
        SavePatch("01-registered-19-21.png", frame.Warped, region, focus);
        SaveMask("02-strict-black.png", frame.Strict, frame.Origin, region, focus);
        SaveMask("03-relaxed-black.png", frame.Relaxed, frame.Origin, region, focus);
        SaveMask("04-grown-black.png", frame.Grown, frame.Origin, region, focus);
        using Mat continuous = Mat.Zeros(frame.Strict.Size(), MatType.CV_32FC1);
        using Mat lightDelta = Mat.Zeros(frame.Strict.Size(), MatType.CV_32FC1);
        int height = continuous.Rows;
        int width = continuous.Cols;
        for (int row = 0; row < height; row++)
            for (int column = 0; column < width; column++)
            {
                Pixel pixel = ReadPixel(frame, column, row);
                continuous.Set(row, column, (float)pixel.ReferenceDistance);
                lightDelta.Set(row, column, (float)pixel.LightDelta);
            }
        using Mat differenceDisplay = new();
        // Fixed display range across all images. This does not change the >24 mask.
        continuous.ConvertTo(differenceDisplay, MatType.CV_8UC1, 255.0 / 100);
        using Mat heat = new();
        Cv2.ApplyColorMap(differenceDisplay, heat, ColormapTypes.Turbo);
        SaveRegion("05-reference-distance-heat.png", heat, frame.Origin, region, focus);
        SaveMask("05-reference-difference-used-binary.png", frame.Difference, frame.Origin, region, focus);
        using Mat lightDisplay = new();
        lightDelta.ConvertTo(lightDisplay, MatType.CV_8UC1, 1, 128);
        SaveMask("05-light-delta.png", lightDisplay, frame.Origin, region, focus);
        // Float TIFF retains continuous values, unlike the display heatmap.
        using Mat distancePatch = new(continuous, Relative(region, frame.Origin));
        Save("05-reference-distance-float.tiff", distancePatch);

        foreach (DiagnosticField field in fields)
        {
            double full = Fraction(frame.Strict, field, frame.Origin, false);
            double core = Fraction(frame.Strict, field, frame.Origin, true);
            double fullDiff = Fraction(frame.Difference, field, frame.Origin, false);
            double coreDiff = Fraction(frame.Difference, field, frame.Origin, true);
            if (field.Value is 0 or 19 or 20 or 21)
            {
                double fullTerm = 0.46 * Math.Min(full / 0.18, 1);
                double coreTerm = 0.34 * Math.Min(core / 0.16, 1);
                Measurements.Add(new(field.Value, full, core, fullDiff, coreDiff,
                    ArkNovaFieldOccupancy.CalculateScore(full, core, fullDiff, coreDiff),
                    Fraction(frame.Grown, field, frame.Origin, false), Fraction(frame.Validity, field, default, false),
                    Cv2.CountNonZero(field.Mask), Cv2.CountNonZero(field.Core), fullTerm, coreTerm, 0.1 * fullDiff, 0.1 * coreDiff,
                    Fraction(frame.Relaxed, field, frame.Origin, false)));
                CapturePixels(frame, field);
                CaptureComparison(frame, field, continuous);
                Rect neighborhood = Expanded(field.Bounds, 45);
                Residual[] nearby = _residuals.Where(residual => neighborhood.Contains(new Point((int)residual.X, (int)residual.Y))).ToArray();
                WriteJson(Path.Combine(outputDirectory, $"field-{field.Value}-local-registration.json"), new
                {
                    neighborhood, count = nearby.Length,
                    dx = Distribution(nearby.Select(residual => residual.Dx)), dy = Distribution(nearby.Select(residual => residual.Dy)),
                    magnitude = Distribution(nearby.Select(residual => Math.Sqrt(residual.Dx * residual.Dx + residual.Dy * residual.Dy))),
                    residuals = nearby
                });
            }
        }
        WriteJson(Path.Combine(outputDirectory, "all-conservation-fields.json"), fields.Select(field => new
        {
            field.Value, fullColor = Fraction(frame.Strict, field, frame.Origin, false), coreColor = Fraction(frame.Strict, field, frame.Origin, true),
            score = ArkNovaFieldOccupancy.CalculateScore(Fraction(frame.Strict, field, frame.Origin, false), Fraction(frame.Strict, field, frame.Origin, true),
                Fraction(frame.Difference, field, frame.Origin, false), Fraction(frame.Difference, field, frame.Origin, true))
        }).OrderByDescending(field => field.score));

        Residual[] local = _residuals.Where(residual => region.Contains(new Point((int)residual.X, (int)residual.Y))).ToArray();
        WriteJson(Path.Combine(outputDirectory, "local-registration.json"), new
        {
            _registration, region, units = "analysis pixels; master pixels = 2x", count = local.Length,
            dx = Distribution(local.Select(residual => residual.Dx)), dy = Distribution(local.Select(residual => residual.Dy)),
            magnitude = Distribution(local.Select(residual => Math.Sqrt(residual.Dx * residual.Dx + residual.Dy * residual.Dy))),
            residuals = local
        });
        using Mat residualImage = frame.Warped.Clone();
        foreach (Residual residual in local)
        {
            Point start = new((int)Math.Round(residual.X), (int)Math.Round(residual.Y));
            Point end = new((int)Math.Round(residual.X + 10 * residual.Dx), (int)Math.Round(residual.Y + 10 * residual.Dy));
            Cv2.ArrowedLine(residualImage, start, end, Scalar.Red, 1, tipLength: 0.25);
        }
        SavePatch("07-local-inlier-residuals-10x.png", residualImage, region, focus);
        CaptureManualSample(frame, focus);
    }

    private void CaptureManualSample(Frame frame, DiagnosticField[] fields)
    {
        string annotationPath = Path.Combine(outputDirectory, "..", "manual-stone-samples.json");
        if (!File.Exists(annotationPath))
            return;
        using JsonDocument annotations = JsonDocument.Parse(File.ReadAllText(annotationPath));
        string image = Path.GetFileName(outputDirectory);
        JsonElement rectangle = annotations.RootElement.GetProperty("images").GetProperty(image);
        Rect sample = new(rectangle[0].GetInt32(), rectangle[1].GetInt32(), rectangle[2].GetInt32(), rectangle[3].GetInt32());
        List<Pixel> pixels = [];
        int strict = 0;
        int relaxed = 0;
        int grown = 0;
        int passesV = 0;
        int passesLight = 0;
        int passesDifference = 0;
        for (int y = sample.Top; y < sample.Bottom; y++)
            for (int x = sample.Left; x < sample.Right; x++)
            {
                Pixel pixel = ReadPixel(frame, x - frame.Origin.X, y - frame.Origin.Y);
                pixels.Add(pixel);
                if (pixel.V < 85)
                    passesV++;
                if (pixel.LightDelta < -18)
                    passesLight++;
                if (pixel.ReferenceDistance > 24)
                    passesDifference++;
                if (frame.Strict.At<byte>(y - frame.Origin.Y, x - frame.Origin.X) != 0)
                    strict++;
                if (frame.Relaxed.At<byte>(y - frame.Origin.Y, x - frame.Origin.X) != 0)
                    relaxed++;
                if (frame.Grown.At<byte>(y - frame.Origin.Y, x - frame.Origin.X) != 0)
                    grown++;
            }
        WriteJson(Path.Combine(outputDirectory, "manual-stone-sample.json"), new
        {
            sample, note = "Manually selected interior sample for diagnosis only, not whole-object segmentation or ground truth.",
            count = pixels.Count, strict, relaxed, grown, passesV, passesLight, passesDifference,
            hsvV = Distribution(pixels.Select(pixel => pixel.V)), lightDelta = Distribution(pixels.Select(pixel => pixel.LightDelta)),
            referenceDistance = Distribution(pixels.Select(pixel => pixel.ReferenceDistance)), pixels
        });
        using Mat overlay = frame.Warped.Clone();
        Cv2.Rectangle(overlay, sample, new Scalar(0, 165, 255), 1);
        SavePatch("08-manual-stone-interior-sample.png", overlay, Expanded(fields.Single(field => field.Value == 20).Bounds, 30), fields.Where(field => field.Value == 20).ToArray());
    }

    private void CapturePixels(Frame frame, DiagnosticField field)
    {
        List<Pixel> all = [];
        List<Pixel> strict = [];
        int dark = 0;
        int lightRejected = 0;
        int differenceRejected = 0;
        for (int row = 0; row < field.Bounds.Height; row++)
            for (int column = 0; column < field.Bounds.Width; column++)
            {
                if (field.Mask.At<byte>(row, column) == 0)
                    continue;
                int x = field.Bounds.X + column - frame.Origin.X;
                int y = field.Bounds.Y + row - frame.Origin.Y;
                Pixel pixel = ReadPixel(frame, x, y);
                all.Add(pixel);
                if (pixel.V < 85)
                {
                    dark++;
                    if (pixel.LightDelta >= -18)
                        lightRejected++;
                    if (pixel.ReferenceDistance <= 24)
                        differenceRejected++;
                }
                if (frame.Strict.At<byte>(y, x) != 0)
                    strict.Add(pixel);
            }
        object Stats(List<Pixel> pixels) => new
        {
            count = pixels.Count,
            hsv = new { h = Distribution(pixels.Select(pixel => pixel.H)), s = Distribution(pixels.Select(pixel => pixel.S)), v = Distribution(pixels.Select(pixel => pixel.V)) },
            lab = new { l = Distribution(pixels.Select(pixel => pixel.L)), a = Distribution(pixels.Select(pixel => pixel.A)), b = Distribution(pixels.Select(pixel => pixel.B)) },
            lightDelta = Distribution(pixels.Select(pixel => pixel.LightDelta)), rawLightDelta = Distribution(pixels.Select(pixel => pixel.RawLightDelta)),
            referenceDistance = Distribution(pixels.Select(pixel => pixel.ReferenceDistance)),
            x = Distribution(pixels.Select(pixel => (pixel.X - field.Bounds.X) / (double)field.Bounds.Width)),
            y = Distribution(pixels.Select(pixel => (pixel.Y - field.Bounds.Y) / (double)field.Bounds.Height))
        };
        WriteJson(Path.Combine(outputDirectory, $"field-{field.Value}-pixels.json"), new
        {
            field.Value, field.Bounds, all = Stats(all), strict = Stats(strict), darkPixelsVBelow85 = dark,
            darkRejectedByLightDelta = lightRejected, darkRejectedByDifference = differenceRejected,
            note = "Counts overlap. OpenCV HSV H:0..179, S/V:0..255; encoded 8-bit LAB. No semantic pixel classifier.",
            strictPixels = strict
        });
        using Mat overlay = frame.Warped.Clone();
        foreach (Pixel pixel in strict)
            overlay.Set(pixel.Y, pixel.X, new Vec3b(0, 0, 255));
        Rect patch = Expanded(field.Bounds, 14);
        SavePatch($"field-{field.Value}-strict-pixels-on-photo.png", overlay, patch, [field]);
    }

    private void CaptureComparison(Frame frame, DiagnosticField field, Mat continuous)
    {
        Rect patch = Expanded(field.Bounds, 16);
        int left = Math.Max(patch.Left, frame.Origin.X);
        int topEdge = Math.Max(patch.Top, frame.Origin.Y);
        patch = new Rect(left, topEdge, Math.Min(patch.Right, frame.Origin.X + continuous.Cols) - left,
            Math.Min(patch.Bottom, frame.Origin.Y + continuous.Rows) - topEdge);
        using Mat canvas = new(patch.Height * 2 + 44, patch.Width * 3, MatType.CV_8UC3, Scalar.Black);
        using Mat heat = new();
        using Mat intensity = new();
        continuous.ConvertTo(intensity, MatType.CV_8UC1, 255.0 / 100);
        Cv2.ApplyColorMap(intensity, heat, ColormapTypes.Turbo);
        Mat[] images = [frame.Reference, frame.Warped, heat];
        string[] labels = ["reference", "photo", "distance 0..100"];
        for (int column = 0; column < 3; column++)
        {
            using Mat source = new(images[column], column == 2 ? Relative(patch, frame.Origin) : patch);
            using Mat top = new(canvas, new Rect(column * patch.Width, 22, patch.Width, patch.Height));
            source.CopyTo(top);
            using Mat mask = Mat.Zeros(patch.Size, MatType.CV_8UC1);
            using Mat target = new(mask, new Rect(field.Bounds.X - patch.X, field.Bounds.Y - patch.Y, field.Bounds.Width, field.Bounds.Height));
            field.Core.CopyTo(target);
            using Mat coreOnly = Mat.Zeros(source.Size(), MatType.CV_8UC3);
            source.CopyTo(coreOnly, mask);
            using Mat bottom = new(canvas, new Rect(column * patch.Width, patch.Height + 44, patch.Width, patch.Height));
            coreOnly.CopyTo(bottom);
            Cv2.PutText(canvas, labels[column], new Point(column * patch.Width + 3, 15), HersheyFonts.HersheySimplex, 0.36, Scalar.White, 1);
            Cv2.PutText(canvas, "core only", new Point(column * patch.Width + 3, patch.Height + 38), HersheyFonts.HersheySimplex, 0.36, Scalar.White, 1);
        }
        SaveEnlarged($"06-field-{field.Value}-reference-photo-difference-core.png", canvas);
    }

    private static Pixel ReadPixel(Frame frame, int x, int y)
    {
        Vec3b hsv = frame.Hsv.At<Vec3b>(y, x);
        Vec3b lab = frame.Lab.At<Vec3b>(y, x);
        Vec3b reference = frame.ReferenceLab.At<Vec3b>(y, x);
        double rawLight = lab.Item0 - reference.Item0;
        double light = rawLight - frame.CurrentSmooth.At<byte>(y, x) + frame.ReferenceSmooth.At<byte>(y, x);
        double distance = Math.Sqrt(light * light + Math.Pow(lab.Item1 - reference.Item1, 2) + Math.Pow(lab.Item2 - reference.Item2, 2));
        return new(x + frame.Origin.X, y + frame.Origin.Y, hsv.Item0, hsv.Item1, hsv.Item2,
            lab.Item0, lab.Item1, lab.Item2, light, rawLight, distance);
    }

    private static object? Distribution(IEnumerable<double> source)
    {
        double[] values = source.Order().ToArray();
        if (values.Length == 0)
            return null;
        double P(double fraction) => values[(int)Math.Round((values.Length - 1) * fraction)];
        return new { min = values[0], p10 = P(0.1), median = P(0.5), p90 = P(0.9), max = values[^1], mean = values.Average() };
    }

    private static double Fraction(Mat source, DiagnosticField field, Point origin, bool core)
    {
        using Mat patch = new(source, Relative(field.Bounds, origin));
        return Cv2.Mean(patch, core ? field.Core : field.Mask).Val0 / 255;
    }

    private static Rect Relative(Rect rectangle, Point origin) => new(rectangle.X - origin.X, rectangle.Y - origin.Y, rectangle.Width, rectangle.Height);
    private static Rect Expanded(Rect rectangle, int margin) => new(rectangle.X - margin, rectangle.Y - margin, rectangle.Width + margin * 2, rectangle.Height + margin * 2);

    internal static void DrawField(Mat image, DiagnosticField field, Point origin)
    {
        using Mat maskCopy = field.Mask.Clone();
        Cv2.FindContours(maskCopy, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Scalar color = field.Value switch { 20 => new Scalar(0, 255, 0), 21 => new Scalar(255, 0, 255), _ => new Scalar(255, 255, 0) };
        foreach (Point[] contour in contours)
            Cv2.Polylines(image, [contour.Select(point => new Point(point.X + field.Bounds.X - origin.X, point.Y + field.Bounds.Y - origin.Y)).ToArray()], true, color, 1);
        Cv2.PutText(image, field.Value.ToString(CultureInfo.InvariantCulture), new Point(field.Bounds.X - origin.X + 3, field.Bounds.Bottom - origin.Y - 3),
            HersheyFonts.HersheySimplex, 0.5, color, 1);
    }

    private void SavePatch(string name, Mat image, Rect region, DiagnosticField[] fields)
    {
        using Mat source = new(image, region);
        using Mat patch = source.Clone();
        foreach (DiagnosticField field in fields)
            DrawField(patch, field, region.Location);
        SaveEnlarged(name, patch);
    }

    private void SaveRegion(string name, Mat image, Point origin, Rect region, DiagnosticField[] fields)
    {
        using Mat source = new(image, Relative(region, origin));
        using Mat patch = source.Clone();
        foreach (DiagnosticField field in fields)
            DrawField(patch, field, region.Location);
        SaveEnlarged(name, patch);
    }

    private void SaveMask(string name, Mat mask, Point origin, Rect region, DiagnosticField[] fields)
    {
        using Mat color = new();
        Cv2.CvtColor(mask, color, ColorConversionCodes.GRAY2BGR);
        SaveRegion(name, color, origin, region, fields);
    }

    private void SaveEnlarged(string name, Mat image)
    {
        using Mat large = new();
        Cv2.Resize(image, large, new Size(), 3, 3, InterpolationFlags.Nearest);
        Save(name, large);
    }

    private void Save(string name, Mat image) => File.WriteAllBytes(Path.Combine(outputDirectory, name), image.ImEncode(Path.GetExtension(name)));
    private static void WriteJson(string path, object data) => File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));

    internal sealed record Frame(Mat Warped, Mat Reference, Mat Hsv, Mat Lab, Mat ReferenceLab, Mat CurrentSmooth, Mat ReferenceSmooth,
        Mat Difference, Mat Strict, Mat Relaxed, Mat Grown, Mat Validity, Point Origin);
    internal sealed record DiagnosticField(int Value, Rect Bounds, Mat Mask, Mat Core);
    public sealed record FieldMeasurement(int Value, double FullColor, double CoreColor, double FullDifference, double CoreDifference,
        double Score, double GrownFraction, double FieldVisibility, int PolygonPixelArea, int CorePixelArea,
        double FullColorContribution, double CoreColorContribution, double FullDifferenceContribution, double CoreDifferenceContribution,
        double RelaxedFraction);
    private sealed record Residual(double X, double Y, double Dx, double Dy);
    private sealed record Pixel(int X, int Y, double H, double S, double V, double L, double A, double B,
        double LightDelta, double RawLightDelta, double ReferenceDistance);
}

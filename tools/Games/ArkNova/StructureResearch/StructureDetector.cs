using System.Text.Json;
using OpenCvSharp;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>
/// Color-free normal-reference experiment. All geometry and registration come
/// from A. Context can explain a component but cannot contribute contact pixels.
/// </summary>
internal sealed class StructureDetector : IDisposable
{
    public List<ScoreRegion> Regions { get; } = [];
    public List<NormalFrame> Normals { get; } = [];
    public const string Pooled = "B1_pool";
    public const string ViewMatched = "B1_view";
    public const string Padim = "B3_diagonal_normal";

    public StructureDetector(string assets)
    {
        using JsonDocument geometry = JsonDocument.Parse(File.ReadAllText(Path.Combine(assets, "scoring_regions_v2.json")));
        foreach (JsonProperty track in geometry.RootElement.GetProperty("tracks").EnumerateObject())
            foreach (JsonElement field in track.Value.GetProperty("fields").EnumerateArray())
            {
                Point[] polygon = field.GetProperty("polygon").EnumerateArray().Select(point =>
                    new Point((int)Math.Round(point[0].GetDouble() * .5), (int)Math.Round(point[1].GetDouble() * .5))).ToArray();
                Rect bounds = Cv2.BoundingRect(polygon);
                int margin = Math.Min(12, (int)Math.Ceiling(Math.Min(bounds.Width, bounds.Height) * .2));
                Rect patch = new(Math.Max(0, bounds.X - margin), Math.Max(0, bounds.Y - margin), 0, 0);
                patch.Width = Math.Min(2000, bounds.Right + margin) - patch.X;
                patch.Height = Math.Min(1400, bounds.Bottom + margin) - patch.Y;
                Mat mask = Mat.Zeros(patch.Height, patch.Width, MatType.CV_8UC1);
                Cv2.FillPoly(mask, [polygon.Select(p => new Point(p.X - patch.X, p.Y - patch.Y)).ToArray()], Scalar.White);
                Mat core = new();
                int erosion = Math.Max(1, (int)(Math.Min(bounds.Width, bounds.Height) * .12));
                using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(erosion * 2 + 1, erosion * 2 + 1));
                Cv2.Erode(mask, core, kernel, borderType: BorderTypes.Constant, borderValue: Scalar.Black);
                Regions.Add(new(track.Name, field.GetProperty("value").GetInt32(), patch, mask, core));
            }
    }

    public FeaturePatch[] Extract(Mat warped, Mat validity)
    {
        using Mat gray8 = new();
        using Mat gray = new();
        using Mat smooth = new();
        using Mat normalized = new();
        using Mat dx = new();
        using Mat dy = new();
        Cv2.CvtColor(warped, gray8, ColorConversionCodes.BGR2GRAY);
        gray8.ConvertTo(gray, MatType.CV_32FC1, 1.0 / 255);
        Cv2.GaussianBlur(gray, gray, new Size(3, 3), 1);
        Cv2.GaussianBlur(gray, smooth, new Size(), 16);
        Cv2.Subtract(gray, smooth, normalized);
        normalized.ConvertTo(normalized, MatType.CV_32FC1, 1 / .15);
        Cv2.Sobel(normalized, dx, MatType.CV_32F, 1, 0, 3, 1.0 / 8);
        Cv2.Sobel(normalized, dy, MatType.CV_32F, 0, 1, 3, 1.0 / 8);
        return Regions.Select(region =>
        {
            float[] light = new float[region.Mask.Rows * region.Mask.Cols];
            float[] x = new float[light.Length];
            float[] y = new float[light.Length];
            int visible = 0;
            for (int row = 0; row < region.Patch.Height; row++)
                for (int col = 0; col < region.Patch.Width; col++)
                {
                    int index = row * region.Patch.Width + col;
                    int px = col + region.Patch.X;
                    int py = row + region.Patch.Y;
                    light[index] = normalized.At<float>(py, px);
                    x[index] = dx.At<float>(py, px);
                    y[index] = dy.At<float>(py, px);
                    if (region.Mask.At<byte>(row, col) != 0 && validity.At<byte>(py, px) != 0)
                        visible++;
                }
            return new FeaturePatch(light, x, y, (double)visible / Cv2.CountNonZero(region.Mask));
        }).ToArray();
    }

    public ChangeMaps Compare(FeaturePatch current, int index, string method, string view, string? exclude = null)
    {
        NormalFrame[] pool = Normals.Where(normal => normal.Name != exclude && normal.Features[index].Visibility >= .97).ToArray();
        if (method == ViewMatched && pool.Any(normal => normal.View == view))
            pool = pool.Where(normal => normal.View == view).ToArray();
        ScoreRegion region = Regions[index];
        Mat residual = Mat.Zeros(region.Mask.Size(), MatType.CV_32FC1);
        Mat gradient = Mat.Zeros(region.Mask.Size(), MatType.CV_32FC1);
        if (pool.Length == 0)
            return new(residual, gradient);
        for (int pixel = 0; pixel < current.Light.Length; pixel++)
        {
            double light = double.PositiveInfinity;
            double edge = double.PositiveInfinity;
            if (method == Padim)
            {
                // PaDiM-inspired diagonal Gaussian, not pretrained deep PaDiM.
                // Variance floors regularize ten normals; no covariance inversion.
                double distance = 0;
                for (int channel = 0; channel < 3; channel++)
                {
                    double sum = 0;
                    double squared = 0;
                    foreach (NormalFrame normal in pool)
                    {
                        double value = normal.Features[index].Channel(channel)[pixel];
                        sum += value;
                        squared += value * value;
                    }
                    double mean = sum / pool.Length;
                    double variance = Math.Max(0, (squared - sum * mean) / Math.Max(1, pool.Length - 1));
                    double floor = channel == 0 ? .04 : .01;
                    distance += Math.Pow(current.Channel(channel)[pixel] - mean, 2) / (variance + floor);
                }
                light = Math.Sqrt(distance / 3);
                edge = light;
            }
            else
                foreach (NormalFrame normal in pool)
                {
                    FeaturePatch reference = normal.Features[index];
                    light = Math.Min(light, Math.Abs(current.Light[pixel] - reference.Light[pixel]));
                    edge = Math.Min(edge, Math.Sqrt(Math.Pow(current.Dx[pixel] - reference.Dx[pixel], 2)
                        + Math.Pow(current.Dy[pixel] - reference.Dy[pixel], 2)));
                }
            residual.Set(pixel / region.Patch.Width, pixel % region.Patch.Width, (float)light);
            gradient.Set(pixel / region.Patch.Width, pixel % region.Patch.Width, (float)edge);
        }
        return new(residual, gradient);
    }

    public static ContactResult Measure(Mat binary, Mat polygon, Mat core)
    {
        using Mat labels = new();
        using Mat stats = new();
        using Mat centers = new();
        int count = Cv2.ConnectedComponentsWithStats(binary, labels, stats, centers);
        int[] contact = new int[count];
        int[] interior = new int[count];
        int rows = binary.Rows;
        int columns = binary.Cols;
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int label = labels.At<int>(row, col);
                if (label == 0)
                    continue;
                if (polygon.At<byte>(row, col) != 0)
                    contact[label]++;
                if (core.At<byte>(row, col) != 0)
                    interior[label]++;
            }
        int area = Cv2.CountNonZero(polygon);
        int coreArea = Cv2.CountNonZero(core);
        bool[] accepted = new bool[count];
        int acceptedContact = 0;
        int acceptedCore = 0;
        int ignored = 0;
        for (int label = 1; label < count; label++)
        {
            int width = stats.At<int>(label, (int)ConnectedComponentsTypes.Width);
            int height = stats.At<int>(label, (int)ConnectedComponentsTypes.Height);
            int componentArea = stats.At<int>(label, (int)ConnectedComponentsTypes.Area);
            double extent = (double)componentArea / (width * height);
            accepted[label] = width >= 3 && height >= 3 && extent >= .2
                && componentArea >= Math.Max(6, area * .015)
                && contact[label] >= Math.Max(4, area * .03)
                && interior[label] >= Math.Max(4, coreArea * .015);
            if (accepted[label])
            {
                acceptedContact += contact[label];
                acceptedCore += interior[label];
            }
            if (contact[label] == 0)
                ignored += componentArea;
        }
        Mat gated = Mat.Zeros(binary.Size(), MatType.CV_8UC1);
        Mat outside = Mat.Zeros(binary.Size(), MatType.CV_8UC1);
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int label = labels.At<int>(row, col);
                if (label > 0 && polygon.At<byte>(row, col) == 0)
                    outside.Set(row, col, (byte)255);
                if (label > 0 && accepted[label] && polygon.At<byte>(row, col) != 0)
                    gated.Set(row, col, (byte)255);
            }
        return new(.5 * acceptedContact / area + .5 * acceptedCore / coreArea,
            ignored, accepted.Count(value => value), gated, outside);
    }

    public FieldChange Score(FeaturePatch feature, int index, string method, string view, Limits limits,
        string? exclude = null, string? diagnostic = null, Mat? warped = null)
    {
        ScoreRegion region = Regions[index];
        using ChangeMaps maps = Compare(feature, index, method, view, exclude);
        using Mat binary = Mat.Zeros(region.Mask.Size(), MatType.CV_8UC1);
        int rows = binary.Rows;
        int columns = binary.Cols;
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                float residual = maps.Residual.At<float>(row, col);
                float gradient = maps.Gradient.At<float>(row, col);
                if (residual > limits.Residual && (gradient > limits.Gradient || residual > 2 * limits.Residual))
                    binary.Set(row, col, (byte)255);
            }
        using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        Cv2.MorphologyEx(binary, binary, MorphTypes.Open, kernel);
        Cv2.MorphologyEx(binary, binary, MorphTypes.Close, kernel);
        using ContactResult contact = Measure(binary, region.Mask, region.Core);
        if (diagnostic is not null && warped is not null)
            Export(diagnostic, warped, index, maps, binary, contact);
        bool candidate = SupportsCandidate(region.Value, feature.Visibility, contact.Score, limits.Occupancy);
        return new(region.Track, region.Value, contact.Score, candidate, feature.Visibility, contact.IgnoredOutsidePixels, contact.Components);
    }

    internal static bool SupportsCandidate(int value, double visibility, double contactScore, double threshold)
        => value != 0 && visibility >= .97 && contactScore > threshold;

    private void Export(string path, Mat warped, int index, ChangeMaps maps, Mat binary, ContactResult contact)
    {
        ScoreRegion region = Regions[index];
        using Mat current = new(warped, region.Patch);
        using Mat referenceImage = Cv2.ImRead(Normals[0].CachePath);
        using Mat reference = new(referenceImage, region.Patch);
        using Mat residual = Heat(maps.Residual, 1.5);
        using Mat gradient = Heat(maps.Gradient, .6);
        using Mat polygon = region.Mask.Clone();
        using Mat core = region.Core.Clone();
        using Mat overlay = current.Clone();
        Cv2.FindContours(polygon, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Cv2.DrawContours(overlay, contours, -1, Scalar.Yellow, 1);
        Cv2.FindContours(core, out Point[][] cores, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Cv2.DrawContours(overlay, cores, -1, Scalar.Green, 1);
        Mat[] panels = [reference, current, overlay, residual, gradient, binary, contact.Gated, contact.Outside];
        string[] labels = ["ref (pool #1)", "photo/context", "polygon/core", "residual", "edge change", "components", "contact only", "outside ignored"];
        int width = Math.Max(130, region.Patch.Width);
        int height = region.Patch.Height + 22;
        using Mat canvas = new(height * 2, width * 4, MatType.CV_8UC3, Scalar.Black);
        for (int panel = 0; panel < panels.Length; panel++)
        {
            using Mat color = new();
            if (panels[panel].Channels() == 1)
                Cv2.CvtColor(panels[panel], color, ColorConversionCodes.GRAY2BGR);
            else
                panels[panel].CopyTo(color);
            int x = panel % 4 * width;
            int y = panel / 4 * height;
            using Mat destination = new(canvas, new Rect(x, y + 22, color.Width, color.Height));
            color.CopyTo(destination);
            Cv2.PutText(canvas, labels[panel], new Point(x + 2, y + 14), HersheyFonts.HersheySimplex, .32, Scalar.White);
        }
        using Mat large = new();
        Cv2.Resize(canvas, large, new Size(), 2, 2, InterpolationFlags.Nearest);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, large.ImEncode(".png"));
        Rect contextBounds = new(Math.Max(0, region.Patch.X - 60), Math.Max(0, region.Patch.Y - 60), 0, 0);
        contextBounds.Width = Math.Min(warped.Width, region.Patch.Right + 60) - contextBounds.X;
        contextBounds.Height = Math.Min(warped.Height, region.Patch.Bottom + 60) - contextBounds.Y;
        using Mat contextView = new(warped, contextBounds);
        using Mat context = contextView.Clone();
        Point shift = new(region.Patch.X - contextBounds.X, region.Patch.Y - contextBounds.Y);
        Cv2.DrawContours(context, contours.Select(points => points.Select(point => point + shift).ToArray()), -1, Scalar.Yellow, 1);
        Cv2.DrawContours(context, cores.Select(points => points.Select(point => point + shift).ToArray()), -1, Scalar.Green, 1);
        File.WriteAllBytes(Path.ChangeExtension(path, ".context.png"), context.ImEncode(".png"));
    }

    private static Mat Heat(Mat input, double maximum)
    {
        using Mat bytes = new();
        input.ConvertTo(bytes, MatType.CV_8UC1, 255 / maximum);
        Mat heat = new();
        Cv2.ApplyColorMap(bytes, heat, ColormapTypes.Turbo);
        return heat;
    }

    public void Dispose()
    {
        foreach (ScoreRegion region in Regions)
        {
            region.Mask.Dispose();
            region.Core.Dispose();
        }
    }

    internal sealed record ScoreRegion(string Track, int Value, Rect Patch, Mat Mask, Mat Core);
    internal sealed record FeaturePatch(float[] Light, float[] Dx, float[] Dy, double Visibility)
    {
        public float[] Channel(int channel) => channel == 0 ? Light : channel == 1 ? Dx : Dy;
    }
    internal sealed record NormalFrame(string Name, string View, string CachePath, FeaturePatch[] Features);
    internal sealed record Limits(double Residual, double Gradient, double Occupancy);
    internal sealed record FieldChange(string Track, int Value, double Score, bool Candidate, double Visibility, int IgnoredOutsidePixels, int Components);
    internal sealed record ChangeMaps(Mat Residual, Mat Gradient) : IDisposable
    {
        public void Dispose() { Residual.Dispose(); Gradient.Dispose(); }
    }
    internal sealed record ContactResult(double Score, int IgnoredOutsidePixels, int Components, Mat Gated, Mat Outside) : IDisposable
    {
        public void Dispose() { Gated.Dispose(); Outside.Dispose(); }
    }
}

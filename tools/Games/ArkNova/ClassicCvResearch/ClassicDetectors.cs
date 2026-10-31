using System.Numerics;
using System.Text.Json;
using OpenCvSharp;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>Classical geometry, local order statistics and linear algebra only.</summary>
internal sealed class ClassicDetectors : IDisposable
{
    public List<Region> Regions { get; } = [];
    public List<Normal> Normals { get; } = [];
    private readonly Mat _reference;
    public static readonly string[] Methods = ["C1", "C2a", "C2b", "C3"];

    public ClassicDetectors(string assets)
    {
        using Mat original = Cv2.ImRead(Path.Combine(assets, "board_reference_master_clean.png"));
        using Mat resized = new();
        Cv2.Resize(original, resized, new Size(2000, 1400));
        _reference = Gray(resized);
        using JsonDocument geometry = JsonDocument.Parse(File.ReadAllText(Path.Combine(assets, "scoring_regions_v2.json")));
        foreach (JsonProperty track in geometry.RootElement.GetProperty("tracks").EnumerateObject())
            foreach (JsonElement field in track.Value.GetProperty("fields").EnumerateArray())
            {
                Point[] polygon = field.GetProperty("polygon").EnumerateArray().Select(p =>
                    new Point((int)Math.Round(p[0].GetDouble() * .5), (int)Math.Round(p[1].GetDouble() * .5))).ToArray();
                Rect bounds = Cv2.BoundingRect(polygon);
                Rect patch = Expand(bounds, 24, new Size(2000, 1400));
                Mat mask = Mat.Zeros(patch.Size, MatType.CV_8UC1);
                Cv2.FillPoly(mask, [polygon.Select(p => p - patch.Location).ToArray()], Scalar.White);
                Mat core = new();
                int erosion = Math.Max(1, (int)(Math.Min(bounds.Width, bounds.Height) * .12));
                using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(2 * erosion + 1, 2 * erosion + 1));
                Cv2.Erode(mask, core, kernel, borderType: BorderTypes.Constant, borderValue: Scalar.Black);
                Moments moments = Cv2.Moments(mask, true);
                Regions.Add(new(track.Name, field.GetProperty("value").GetInt32(), bounds, patch, mask, core,
                    new Point2d(moments.M10 / moments.M00, moments.M01 / moments.M00)));
            }
    }

    public static Mat Gray(Mat image)
    {
        Mat gray = new();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(gray, gray, new Size(3, 3), 1);
        return gray;
    }

    public AlignedTracks AlignTracks(Mat gray)
    {
        Dictionary<string, Mat> corrected = [];
        List<EccInfo> diagnostics = [];
        foreach (IGrouping<string, Region> group in Regions.GroupBy(region => region.Track))
        {
            Mat trackFrame = gray.Clone();
            corrected[group.Key] = trackFrame;
            Rect bounds = Union(group.Select(region => region.Bounds));
            Rect roi = Expand(bounds, 12, gray.Size());
            using Mat target = new(_reference, roi);
            using Mat source = new(gray, roi);
            using Mat mask = new(roi.Size, MatType.CV_8UC1, Scalar.White);
            foreach (Region region in group)
            {
                using Mat sourceCore = new(region.Core, new Rect(0, 0, region.Core.Cols, region.Core.Rows));
                Rect overlap = region.Patch & roi;
                using Mat core = new(sourceCore, new Rect(overlap.X - region.Patch.X, overlap.Y - region.Patch.Y, overlap.Width, overlap.Height));
                using Mat maskRoi = new(mask, new Rect(overlap.X - roi.X, overlap.Y - roi.Y, overlap.Width, overlap.Height));
                maskRoi.SetTo(Scalar.Black, core);
            }
            double before = Correlation(target, source, mask);
            using Mat warp = Mat.Eye(2, 3, MatType.CV_32FC1);
            double dx = 0;
            double dy = 0;
            bool applied = false;
            string? failure = null;
            double after = before;
            try
            {
                Cv2.FindTransformECC(target, source, warp, MotionTypes.Translation,
                    new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, 30, 1e-4), mask, 5);
                dx = warp.At<float>(0, 2);
                dy = warp.At<float>(1, 2);
                if (!double.IsFinite(dx) || !double.IsFinite(dy) || Math.Abs(dx) > 4 || Math.Abs(dy) > 4)
                    failure = "translation_out_of_bounds";
                else
                {
                    using Mat aligned = new();
                    Cv2.WarpAffine(source, aligned, warp, roi.Size, InterpolationFlags.Linear | InterpolationFlags.WarpInverseMap, BorderTypes.Reflect101);
                    after = Correlation(target, aligned, mask);
                    // Do not apply a numerically degraded correction.
                    if (after + 1e-6 >= before)
                    {
                        using Mat destination = new(trackFrame, roi);
                        aligned.CopyTo(destination);
                        applied = true;
                    }
                    else
                        failure = "correlation_degraded";
                }
            }
            catch (OpenCVException error)
            {
                failure = "ecc_failed: " + error.Message;
            }
            catch (EntryPointNotFoundException error)
            {
                failure = "ecc_runtime_unavailable: " + error.Message;
            }
            diagnostics.Add(new(group.Key, dx, dy, before, applied ? after : before, applied, failure));
        }
        return new(corrected, diagnostics.ToArray());
    }

    public Features Extract(Mat gray, Mat validity)
    {
        uint[] census = Census(gray);
        return new(Regions.Select(region =>
        {
            uint[] codes = new uint[region.Patch.Width * region.Patch.Height];
            for (int y = 0; y < region.Patch.Height; y++)
                Array.Copy(census, (region.Patch.Y + y) * gray.Width + region.Patch.X, codes, y * region.Patch.Width, region.Patch.Width);
            using Mat patch = new(gray, region.Patch);
            using Mat small = new();
            Cv2.Resize(patch, small, new Size(16, 16), 0, 0, InterpolationFlags.Area);
            double[] intensity = new double[256];
            for (int pixel = 0; pixel < 256; pixel++)
                intensity[pixel] = small.At<byte>(pixel / 16, pixel % 16) / 255.0;
            using Mat visible = new(validity, region.Patch);
            return new PatchFeatures(codes, intensity, Math.Clamp(Cv2.Mean(visible, region.Mask).Val0 / 255, 0, 1));
        }).ToArray());
    }

    internal static uint[] Census(Mat gray)
    {
        int width = gray.Width;
        int height = gray.Height;
        byte[] pixels = new byte[width * height];
        gray.GetArray(out pixels);
        uint[] codes = new uint[pixels.Length];
        for (int y = 2; y < height - 2; y++)
            for (int x = 2; x < width - 2; x++)
            {
                uint code = 0;
                byte center = pixels[y * width + x];
                for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;
                        code = (code << 1) | (pixels[(y + dy) * width + x + dx] < center ? 1U : 0U);
                    }
                codes[y * width + x] = code;
            }
        return codes;
    }

    public float[] CensusMap(PatchFeatures current, int index, bool ecc, string? exclude)
    {
        uint[][] references = Normals.Where(normal => normal.Name != exclude && normal.Raw.Patches[index].Visibility >= .97)
            .Select(normal => (ecc ? normal.Ecc : normal.Raw).Patches[index].Codes).ToArray();
        float[] change = new float[current.Codes.Length];
        for (int pixel = 0; pixel < change.Length; pixel++)
        {
            int distance = 24;
            foreach (uint[] reference in references)
                distance = Math.Min(distance, BitOperations.PopCount(current.Codes[pixel] ^ reference[pixel]));
            change[pixel] = distance / 24f;
        }
        return change;
    }

    public LowRankModel FitSubspace(int index, string? exclude = null)
    {
        double[][] samples = Normals.Where(normal => normal.Name != exclude && normal.Raw.Patches[index].Visibility >= .97)
            .Select(normal => normal.Raw.Patches[index].Intensity).ToArray();
        double[] mean = new double[256];
        foreach (double[] sample in samples)
            for (int pixel = 0; pixel < 256; pixel++)
                mean[pixel] += sample[pixel] / samples.Length;
        using Mat centered = new(samples.Length, 256, MatType.CV_64FC1);
        for (int row = 0; row < samples.Length; row++)
            for (int pixel = 0; pixel < 256; pixel++)
                centered.Set(row, pixel, samples[row][pixel] - mean[pixel]);
        using Mat w = new();
        using Mat u = new();
        using Mat vt = new();
        Cv2.SVDecomp(centered, w, u, vt);
        double[][] basis = Enumerable.Range(0, Math.Min(3, samples.Length - 1)).Select(row =>
            Enumerable.Range(0, 256).Select(pixel => vt.At<double>(row, pixel)).ToArray()).ToArray();
        return new(mean, basis);
    }

    internal static float[] Sparse(double[] query, LowRankModel normal)
    {
        double[] sparse = new double[256];
        double[] low = new double[256];
        for (int iteration = 0; iteration < 8; iteration++)
        {
            Array.Copy(normal.Mean, low, 256);
            foreach (double[] basis in normal.Basis)
            {
                double coefficient = 0;
                for (int pixel = 0; pixel < 256; pixel++)
                    coefficient += basis[pixel] * (query[pixel] - normal.Mean[pixel] - sparse[pixel]);
                for (int pixel = 0; pixel < 256; pixel++)
                    low[pixel] += coefficient * basis[pixel];
            }
            for (int pixel = 0; pixel < 256; pixel++)
            {
                double residual = query[pixel] - low[pixel];
                sparse[pixel] = Math.Sign(residual) * Math.Max(0, Math.Abs(residual) - .04);
            }
        }
        return sparse.Select(value => (float)Math.Abs(value)).ToArray();
    }

    public Detection ChangeScore(string method, int index, float[] map, double visibility, Limits limits, string? diagnostic = null, Mat? gray = null)
    {
        Region region = Regions[index];
        using Mat change = new(method == "C3" ? 16 : region.Patch.Height, method == "C3" ? 16 : region.Patch.Width, MatType.CV_32FC1);
        change.SetArray(map);
        using Mat full = new();
        Cv2.Resize(change, full, region.Patch.Size, 0, 0, InterpolationFlags.Linear);
        using Mat binary = new();
        Cv2.Threshold(full, binary, limits.Pixel, 255, ThresholdTypes.Binary);
        binary.ConvertTo(binary, MatType.CV_8UC1);
        using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        Cv2.MorphologyEx(binary, binary, MorphTypes.Open, kernel);
        using Mat labels = new();
        using Mat stats = new();
        using Mat centers = new();
        int components = Cv2.ConnectedComponentsWithStats(binary, labels, stats, centers);
        int[] contacts = new int[components];
        int[] cores = new int[components];
        int rows = binary.Rows;
        int columns = binary.Cols;
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int label = labels.At<int>(y, x);
                if (label == 0)
                    continue;
                contacts[label] += region.Mask.At<byte>(y, x) != 0 ? 1 : 0;
                cores[label] += region.Core.At<byte>(y, x) != 0 ? 1 : 0;
            }
        int area = Cv2.CountNonZero(region.Mask);
        int coreArea = Cv2.CountNonZero(region.Core);
        bool[] accepted = Enumerable.Range(0, components).Select(label => label > 0 && stats.At<int>(label, (int)ConnectedComponentsTypes.Area) >= 8
            && contacts[label] >= .03 * area && cores[label] >= .01 * coreArea).ToArray();
        int contact = Enumerable.Range(0, components).Where(label => accepted[label]).Sum(label => contacts[label]);
        int interior = Enumerable.Range(0, components).Where(label => accepted[label]).Sum(label => cores[label]);
        double score = .5 * contact / area + .5 * interior / coreArea;
        Detection result = new(method, region.Track, region.Value, score, Supported(region.Value, visibility, score, limits.Occupancy),
            score > 2 * limits.Occupancy, null, null, [], visibility, contacts.Skip(1).Count(value => value == 0));
        if (diagnostic is not null && gray is not null)
            Export(diagnostic, gray, region, full, binary, result);
        return result;
    }

    public Detection Shape(Mat gray, int index, double visibility, Limits limits, string? diagnostic = null)
    {
        Region region = Regions[index];
        using Mat patch = new(gray, region.Patch);
        using Mat edge = new();
        Cv2.Canny(patch, edge, 60, 150);
        using Mat dx = new();
        using Mat dy = new();
        Cv2.Sobel(patch, dx, MatType.CV_32F, 1, 0);
        Cv2.Sobel(patch, dy, MatType.CV_32F, 0, 1);
        int width = patch.Width;
        int height = patch.Height;
        float[][] distances = new float[8][];
        for (int bin = 0; bin < 8; bin++)
        {
            using Mat seeds = new(patch.Size(), MatType.CV_8UC1, Scalar.White);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (edge.At<byte>(y, x) != 0)
                    {
                        double angle = Math.Atan2(dy.At<float>(y, x), dx.At<float>(y, x));
                        int orientation = ((int)Math.Round(angle / Math.PI * 8) % 8 + 8) % 8;
                        int delta = Math.Abs(orientation - bin);
                        if (Math.Min(delta, 8 - delta) <= 1)
                            seeds.Set(y, x, (byte)0);
                    }
            using Mat distance = new();
            Cv2.DistanceTransform(seeds, distance, DistanceTypes.L2, DistanceTransformMasks.Mask3);
            distance.GetArray(out float[] values);
            distances[bin] = values;
        }
        double markerWidth = region.Track == "attractiveness" ? region.Bounds.Width * .8
            : region.Track == "conservation" ? region.Bounds.Width * .42 : region.Bounds.Height * .72;
        double markerHeight = region.Track == "attractiveness" ? region.Bounds.Height * .75
            : region.Track == "conservation" ? region.Bounds.Height * .55 : markerWidth;
        double best = double.PositiveInfinity;
        double centerX = 0;
        double centerY = 0;
        Point[] contour = [];
        foreach (double scale in new[] { .85, 1, 1.15 })
            foreach (double degrees in new[] { -12.0, 0, 12.0 })
            {
                (TemplatePoint[] template, Point[] outline) = Template(region.Track == "attractiveness", markerWidth * scale, markerHeight * scale, degrees);
                int rangeX = (int)(region.Bounds.Width * .25);
                int rangeY = (int)(region.Bounds.Height * .25);
                // Anchor the grid at zero so an exactly aligned shape is always
                // a hypothesis, independent of the polygon's integer dimensions.
                for (int oy = -(rangeY / 4) * 4; oy <= rangeY; oy += 4)
                    for (int ox = -(rangeX / 4) * 4; ox <= rangeX; ox += 4)
                    {
                        int cx = (int)Math.Round(region.Center.X) + ox;
                        int cy = (int)Math.Round(region.Center.Y) + oy;
                        double sum = 0;
                        foreach (TemplatePoint point in template)
                        {
                            int x = cx + point.X;
                            int y = cy + point.Y;
                            sum += x < 0 || y < 0 || x >= width || y >= height ? 8 : Math.Min(8, distances[point.Bin][y * width + x]);
                        }
                        double cost = sum / template.Length;
                        if (cost < best)
                        {
                            best = cost;
                            centerX = cx;
                            centerY = cy;
                            contour = outline.Select(point => point + new Point(cx, cy)).ToArray();
                        }
                    }
            }
        using Mat silhouette = Mat.Zeros(region.Patch.Size, MatType.CV_8UC1);
        Cv2.FillPoly(silhouette, [contour], Scalar.White);
        using Mat contact = new();
        using Mat core = new();
        Cv2.BitwiseAnd(silhouette, region.Mask, contact);
        Cv2.BitwiseAnd(silhouette, region.Core, core);
        bool intersects = Cv2.CountNonZero(contact) >= Math.Max(8, Cv2.CountNonZero(region.Mask) * .03)
            && Cv2.CountNonZero(core) >= Math.Max(4, Cv2.CountNonZero(region.Core) * .01);
        double score = Math.Exp(-best / 2.5);
        Detection result = new("C1", region.Track, region.Value, score, intersects && Supported(region.Value, visibility, score, limits.Occupancy),
            score >= limits.Occupancy + .08, centerX + region.Patch.X, centerY + region.Patch.Y,
            contour.Select(point => new PixelPoint(point.X + region.Patch.X, point.Y + region.Patch.Y)).ToArray(), visibility, 0);
        if (diagnostic is not null)
        {
            using Mat map = new();
            edge.ConvertTo(map, MatType.CV_32FC1, 1.0 / 255);
            Export(diagnostic, gray, region, map, silhouette, result);
        }
        return result;
    }

    private static (TemplatePoint[], Point[]) Template(bool notched, double width, double height, double degrees)
    {
        Point2d[] shape = notched
            ? [new(-.5, -.5), new(.5, -.5), new(.5, -.32), new(.35, -.22), new(.35, .22), new(.5, .32), new(.5, .5),
                new(-.5, .5), new(-.5, .32), new(-.35, .22), new(-.35, -.22), new(-.5, -.32)]
            : [new(-.5, -.5), new(.5, -.5), new(.5, .5), new(-.5, .5)];
        double theta = degrees * Math.PI / 180;
        Point2d Rotate(Point2d p) => new(p.X * width * Math.Cos(theta) - p.Y * height * Math.Sin(theta),
            p.X * width * Math.Sin(theta) + p.Y * height * Math.Cos(theta));
        Point2d[] points = shape.Select(Rotate).ToArray();
        List<TemplatePoint> template = [];
        for (int segment = 0; segment < points.Length; segment++)
        {
            Point2d a = points[segment];
            Point2d b = points[(segment + 1) % points.Length];
            int samples = Math.Max(2, (int)Math.Ceiling(a.DistanceTo(b) / 3));
            double angle = Math.Atan2(b.Y - a.Y, b.X - a.X) + Math.PI / 2;
            int bin = ((int)Math.Round(angle / Math.PI * 8) % 8 + 8) % 8;
            for (int step = 0; step < samples; step++)
                template.Add(new((int)Math.Round(a.X + (b.X - a.X) * step / samples),
                    (int)Math.Round(a.Y + (b.Y - a.Y) * step / samples), bin));
        }
        return (template.ToArray(), points.Select(point => new Point((int)Math.Round(point.X), (int)Math.Round(point.Y))).ToArray());
    }

    public static List<Detection> SuppressDuplicateShapes(List<Detection> detections)
    {
        List<Detection> kept = [];
        foreach (Detection candidate in detections.OrderByDescending(value => value.Score))
        {
            bool duplicate = candidate.Candidate && kept.Any(other => other.Candidate && other.Track == candidate.Track
                && Math.Sqrt(Math.Pow(other.CenterX!.Value - candidate.CenterX!.Value, 2) + Math.Pow(other.CenterY!.Value - candidate.CenterY!.Value, 2)) < 18);
            kept.Add(duplicate ? candidate with { Candidate = false } : candidate);
        }
        return kept;
    }

    private static void Export(string path, Mat gray, Region region, Mat map, Mat binary, Detection detection)
    {
        using Mat patch = new(gray, region.Patch);
        using Mat overlay = new();
        Cv2.CvtColor(patch, overlay, ColorConversionCodes.GRAY2BGR);
        using Mat mask = region.Mask.Clone();
        Cv2.FindContours(mask, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Cv2.DrawContours(overlay, contours, -1, Scalar.Yellow, 1);
        if (detection.Contour.Length > 0)
        {
            Cv2.Polylines(overlay, [detection.Contour.Select(point => new Point(point.X - region.Patch.X, point.Y - region.Patch.Y)).ToArray()], true, Scalar.LimeGreen, 1);
            Cv2.Circle(overlay, new Point(detection.CenterX!.Value - region.Patch.X, detection.CenterY!.Value - region.Patch.Y), 2, Scalar.Red, -1);
        }
        using Mat heat8 = new();
        using Mat heat = new();
        map.ConvertTo(heat8, MatType.CV_8UC1, 255);
        Cv2.ApplyColorMap(heat8, heat, ColormapTypes.Turbo);
        using Mat components = new();
        Cv2.CvtColor(binary, components, ColorConversionCodes.GRAY2BGR);
        using Mat canvas = new();
        Cv2.HConcat([overlay, heat, components], canvas);
        using Mat large = new();
        Cv2.Resize(canvas, large, new Size(), 3, 3, InterpolationFlags.Nearest);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, large.ImEncode(".png"));
    }

    internal static bool Supported(int value, double visibility, double score, double threshold)
        => value != 0 && visibility >= .97 && score > threshold;

    private static double Correlation(Mat target, Mat current, Mat mask)
    {
        double sumX = 0, sumY = 0, squareX = 0, squareY = 0, product = 0;
        int count = 0;
        int width = target.Width;
        int height = target.Height;
        for (int y = 0; y < height; y += 3)
            for (int x = 0; x < width; x += 3)
                if (mask.At<byte>(y, x) != 0)
                {
                    double a = target.At<byte>(y, x);
                    double b = current.At<byte>(y, x);
                    sumX += a;
                    sumY += b;
                    squareX += a * a;
                    squareY += b * b;
                    product += a * b;
                    count++;
                }
        return count > 0 ? (product - sumX * sumY / count) / Math.Sqrt(Math.Max(1e-9, (squareX - sumX * sumX / count) * (squareY - sumY * sumY / count))) : 0;
    }

    private static Rect Union(IEnumerable<Rect> source)
    {
        Rect[] boxes = source.ToArray();
        int left = boxes.Min(box => box.Left);
        int top = boxes.Min(box => box.Top);
        return new(left, top, boxes.Max(box => box.Right) - left, boxes.Max(box => box.Bottom) - top);
    }
    private static Rect Expand(Rect box, int margin, Size size)
    {
        int left = Math.Max(0, box.X - margin);
        int top = Math.Max(0, box.Y - margin);
        return new(left, top, Math.Min(size.Width, box.Right + margin) - left, Math.Min(size.Height, box.Bottom + margin) - top);
    }
    public void Dispose()
    {
        _reference.Dispose();
        foreach (Region region in Regions)
        {
            region.Mask.Dispose();
            region.Core.Dispose();
        }
    }

    internal sealed record Region(string Track, int Value, Rect Bounds, Rect Patch, Mat Mask, Mat Core, Point2d Center);
    internal sealed record TemplatePoint(int X, int Y, int Bin);
    internal sealed record PixelPoint(int X, int Y);
    internal sealed record PatchFeatures(uint[] Codes, double[] Intensity, double Visibility);
    internal sealed record Features(PatchFeatures[] Patches);
    internal sealed record Normal(string Name, Features Raw, Features Ecc);
    internal sealed record Limits(double Pixel, double Occupancy);
    internal sealed record LowRankModel(double[] Mean, double[][] Basis);
    internal sealed record Detection(string Method, string Track, int Value, double Score, bool Candidate, bool Strong,
        double? CenterX, double? CenterY, PixelPoint[] Contour, double Visibility, int IgnoredComponents);
    internal sealed record EccInfo(string Track, double Dx, double Dy, double Before, double After, bool Applied, string? Failure);
    internal sealed record AlignedTracks(Dictionary<string, Mat> Frames, EccInfo[] Diagnostics) : IDisposable
    {
        public void Dispose()
        {
            foreach (Mat frame in Frames.Values)
                frame.Dispose();
        }
    }
}

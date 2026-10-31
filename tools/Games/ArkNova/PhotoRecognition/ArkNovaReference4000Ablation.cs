using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using static BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.ArkNovaReferenceValidation;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>One reference-to-reference homography, frozen before either score ablation.</summary>
internal static class ArkNovaReference4000Ablation
{
    private const string ImageName = "board_reference_single_shot_4000x2800.png";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Prepare(string root, string handoff, string output)
    {
        if (Directory.Exists(output))
            throw new IOException("Preparation output already exists; never refit this ablation.");
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        string oldPath = Path.Combine(assets, "board_reference_master_clean.png");
        string newPath = Path.Combine(handoff, "board_reference_master_single_shot.png");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(handoff, "MANIFEST.json")));
        foreach (JsonElement file in manifest.RootElement.GetProperty("files").EnumerateArray())
            if (Hash(Path.Combine(handoff, file.GetProperty("file").GetString()!)) != file.GetProperty("sha256").GetString())
                throw new InvalidDataException("Handoff hash mismatch.");
        using Mat original = Cv2.ImDecode(File.ReadAllBytes(newPath), ImreadModes.Color);
        using Mat target = Cv2.ImDecode(File.ReadAllBytes(oldPath), ImreadModes.Color);
        if (target.Width != 4000 || target.Height != 2800)
            throw new InvalidDataException("Expected the historical 4000x2800 coordinate system.");
        // Exactly the existing registration's SIFT, scale, ratio, and RANSAC settings.
        // Only the two empty references participate; no evaluation photo or GT is read here.
        double sourceScale = 1600.0 / Math.Max(original.Width, original.Height);
        double targetScale = 1600.0 / Math.Max(target.Width, target.Height);
        using Mat sourceSmall = new();
        using Mat targetSmall = new();
        Cv2.Resize(original, sourceSmall, new Size(), sourceScale, sourceScale, InterpolationFlags.Area);
        Cv2.Resize(target, targetSmall, new Size(), targetScale, targetScale, InterpolationFlags.Area);
        using Mat sourceGray = new();
        using Mat targetGray = new();
        Cv2.CvtColor(sourceSmall, sourceGray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(targetSmall, targetGray, ColorConversionCodes.BGR2GRAY);
        using SIFT sift = SIFT.Create(6000);
        using Mat sourceDescriptors = new();
        using Mat targetDescriptors = new();
        sift.DetectAndCompute(sourceGray, null, out KeyPoint[] sourcePoints, sourceDescriptors);
        sift.DetectAndCompute(targetGray, null, out KeyPoint[] targetPoints, targetDescriptors);
        using BFMatcher matcher = new(NormTypes.L2);
        DMatch[] matches = matcher.KnnMatch(sourceDescriptors, targetDescriptors, 2)
            .Where(pair => pair.Length == 2 && pair[0].Distance < .72 * pair[1].Distance).Select(pair => pair[0]).ToArray();
        if (matches.Length < 24)
            throw new InvalidDataException("Too few reference matches.");
        Point2d[] source = matches.Select(match => new Point2d(sourcePoints[match.QueryIdx].Pt.X, sourcePoints[match.QueryIdx].Pt.Y)).ToArray();
        Point2d[] destination = matches.Select(match => new Point2d(targetPoints[match.TrainIdx].Pt.X, targetPoints[match.TrainIdx].Pt.Y)).ToArray();
        using Mat inlierMask = new();
        using Mat registration = Cv2.FindHomography(InputArray.Create(source), InputArray.Create(destination), HomographyMethods.Ransac, 3, inlierMask);
        if (registration.Empty() || !Cv2.CheckRange(registration))
            throw new InvalidDataException("No finite reference homography.");
        Point2d[] projected = Cv2.PerspectiveTransform(source, registration);
        double[] errors = Enumerable.Range(0, matches.Length).Where(index => inlierMask.At<byte>(index) != 0)
            .Select(index => projected[index].DistanceTo(destination[index])).Order().ToArray();
        int inliers = errors.Length;
        double p95 = errors.Length > 0 ? errors[(int)((errors.Length - 1) * .95)] : double.MaxValue;
        if (inliers < 20 || (double)inliers / matches.Length < .35 || p95 > 3)
            throw new InvalidDataException("Existing registration quality requirements are not met.");
        // H_full = inverse(S_target) * H_registration * S_source. No free resize.
        using Mat transform = registration.Clone();
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                transform.Set(row, column, registration.At<double>(row, column)
                    * (column < 2 ? sourceScale : 1) / (row < 2 ? targetScale : 1));
        using Mat warped = new();
        Cv2.WarpPerspective(original, warped, transform, target.Size());
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, ImageName), warped.ImEncode(".png"));
        JsonObject oldGeometry = JsonNode.Parse(File.ReadAllText(Path.Combine(assets, "scoring_regions_v2.json")))!.AsObject();
        oldGeometry["reference"]!["image"] = ImageName;
        Write(Path.Combine(output, "test-a.json"), oldGeometry);
        JsonObject newGeometry = JsonNode.Parse(File.ReadAllText(Path.Combine(handoff, "scoring_regions_v3_candidate5.json")))!.AsObject();
        newGeometry["reference"]!["image"] = ImageName;
        newGeometry["reference"]!["width_px"] = 4000;
        newGeometry["reference"]!["height_px"] = 2800;
        foreach ((string _, JsonNode? track) in newGeometry["tracks"]!.AsObject())
            foreach (JsonNode? field in track!["fields"]!.AsArray())
            {
                Point2d[] polygon = field!["polygon"]!.AsArray().Select(point => new Point2d(point![0]!.GetValue<double>(), point[1]!.GetValue<double>())).ToArray();
                Point2d[] mapped = Cv2.PerspectiveTransform(polygon, transform);
                field["polygon"] = JsonSerializer.SerializeToNode(mapped.Select(point => new[] { point.X, point.Y }));
                JsonNode centroid = field["centroid"]!;
                Point2d center = Cv2.PerspectiveTransform(new[] { new Point2d(centroid[0]!.GetValue<double>(), centroid[1]!.GetValue<double>()) }, transform)[0];
                field["centroid"] = JsonSerializer.SerializeToNode(new[] { center.X, center.Y });
                // Derived metadata only; the analyzer reads the transformed vertices.
                double left = mapped.Min(point => point.X);
                double top = mapped.Min(point => point.Y);
                field["bbox"] = JsonSerializer.SerializeToNode(new[] { left, top, mapped.Max(point => point.X) - left, mapped.Max(point => point.Y) - top });
            }
        Write(Path.Combine(output, "test-b.json"), newGeometry);
        double[][] matrix = Enumerable.Range(0, 3).Select(row => Enumerable.Range(0, 3).Select(column => transform.At<double>(row, column)).ToArray()).ToArray();
        Write(Path.Combine(output, "preparation.json"), new
        {
            protocol = "One empty-reference SIFT/RANSAC homography before scores; no tuning. Same H for image and every Candidate5 vertex; no rounding or clipping.",
            matrix, matches = matches.Length, inliers, reprojectionP95RegistrationPixels = p95,
            sourceScale, targetScale, width = 4000, height = 2800,
            inputHashes = new Dictionary<string, string>
            {
                ["oldReference"] = Hash(oldPath), ["singleShot"] = Hash(newPath),
                ["oldGeometry"] = Hash(Path.Combine(assets, "scoring_regions_v2.json")),
                ["candidate5"] = Hash(Path.Combine(handoff, "scoring_regions_v3_candidate5.json"))
            },
            frozenOutputs = new[] { ImageName, "test-a.json", "test-b.json" }.ToDictionary(name => name, name => Hash(Path.Combine(output, name)))
        });
        Console.WriteLine($"Prepared once: {matches.Length} matches, {inliers} inliers, p95={p95:F4} registration px. No scores read.");
        return 0;
    }

    public static int Run(string root, string prepared, string output)
    {
        using FileStream report = new(output, FileMode.CreateNew);
        using JsonDocument preparation = JsonDocument.Parse(File.ReadAllText(Path.Combine(prepared, "preparation.json")));
        foreach (JsonProperty file in preparation.RootElement.GetProperty("frozenOutputs").EnumerateObject())
            if (Hash(Path.Combine(prepared, file.Name)) != file.Value.GetString())
                throw new InvalidDataException("Prepared ablation assets changed.");
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        List<Input> inputs = LoadInputs(root, assets);
        using ArkNovaPhotoAnalyzer baseline = new(assets);
        using ArkNovaPhotoAnalyzer testA = new(prepared, "test-a.json");
        using ArkNovaPhotoAnalyzer testB = new(prepared, "test-b.json");
        List<AblationPhoto> photos = [];
        foreach (Input input in inputs)
        {
            Measurement old = Measure(baseline, input);
            Measurement a = Measure(testA, input);
            Measurement b = Measure(testB, input);
            photos.Add(new(input.Batch, input.Name, Hash(input.Path), old, a, b));
            Console.WriteLine($"{input.Batch}/{input.Name}: registered {old.Registration.Success}/{a.Registration.Success}/{b.Registration.Success}; raw {Correct(old)}/{Correct(a)}/{Correct(b)}");
        }
        WriteReport();
        return 0;

        void WriteReport() => JsonSerializer.Serialize(report, new
        {
            protocol = "Baseline / Test A / Test B, unchanged hardened A_NoZero and Confidence E. Same historical denominators including null. No fitting or tuning.",
            preparationSha256 = Hash(Path.Combine(prepared, "preparation.json")),
            sourceHashes = Directory.GetFiles(Path.Combine(assets, ".."), "*.cs").ToDictionary(path => Path.GetFileName(path)!, Hash),
            negatives = new
            {
                baseline = ArkNovaRegistrationChecks.Run(baseline, assets, prepared, ArkNovaConfidencePolicy.Selected),
                testA = ArkNovaRegistrationChecks.Run(testA, prepared, prepared, ArkNovaConfidencePolicy.Selected, ImageName),
                testB = ArkNovaRegistrationChecks.Run(testB, prepared, prepared, ArkNovaConfidencePolicy.Selected, ImageName)
            },
            summaries = photos.GroupBy(photo => photo.Batch).Select(batch => new
            {
                batch = batch.Key, baseline = Summarize(batch.Select(photo => photo.Baseline)),
                testA = Summarize(batch.Select(photo => photo.TestA)), testB = Summarize(batch.Select(photo => photo.TestB))
            }), photos
        }, Json);
    }

    private static int Correct(Measurement measurement) => measurement.Rows.Count(row => row.Expected.HasValue && row.Raw?.Value == row.Expected);
    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json));
    private sealed record AblationPhoto(string Batch, string Name, string Sha256, Measurement Baseline, Measurement TestA, Measurement TestB);
}

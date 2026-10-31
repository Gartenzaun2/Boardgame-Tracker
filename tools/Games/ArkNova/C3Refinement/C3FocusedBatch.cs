using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using OpenCvSharp;
using static BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.ClassicDetectors;
using static BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.C3LocalReadout;
using Field = BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.C3LocalReadout.Field;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal static class C3FocusedBatch
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] Tracks = ["attractiveness", "conservation", "reputation"];
    private static readonly string[] Variants = ["A_legacy", "A_hardened", "A_accepted", "P0", "P1", "P2", "F1", "F2"];
    private const string Report = "tools/Games/ArkNova/PhotoRecognition/Reports/C3RefinementV5";
    private const string Cache = "artifacts/C3V5";
    private const string OldCache = "artifacts/ClassicCvV4";
    private const string V4Report = "tools/Games/ArkNova/PhotoRecognition/Reports/ClassicCvResearchV4";

    public static int Run(string command, string root)
    {
        string output = Path.Combine(root, Report);
        string cache = Path.Combine(root, Cache);
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(cache);
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        if (command == "self-test")
            return Checks(assets);
        List<Input> inputs = Read<List<Input>>(Path.Combine(root, OldCache, "images.json"));
        if (command == "prepare")
            return Prepare(root, assets, inputs);
        if (command == "compare")
        {
            Verify(root, Read<Dictionary<string, string>>(Path.Combine(output, "freeze.json")));
            using FileStream latch = new(Path.Combine(output, "comparison-started.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(latch, new { startedUtc = DateTimeOffset.UtcNow, reusedComparisonsOnly = true }, Json);
        }
        else if (File.Exists(Path.Combine(output, "freeze.json")))
            throw new InvalidOperationException("Focused experiment is frozen; no refitting.");
        Dictionary<string, Prepared> prepared = Read<Prepared[]>(Path.Combine(cache, "prepared.json")).ToDictionary(input => input.Id);
        V4Model old = Read<V4Model>(Path.Combine(root, V4Report, "model.json"));
        using ClassicDetectors geometry = new(assets);
        if (command == "calibrate")
        {
            Limits[] floor = FitFields(root, inputs, geometry, old);
            List<ImageResult> calibration = [];
            // First occupancy ablation only. No V3/V2 metric is consulted here.
            foreach (Input input in inputs.Where(input => input.Partition == "calibration"))
                calibration.Add(Evaluate(root, input, prepared[input.Id], geometry, old, floor, useFieldFloors: true));
            Metric before = Metrics(calibration, "P0", track: "attractiveness");
            Metric after = Metrics(calibration, "P1", track: "attractiveness");
            bool useField = after.FP <= .75 * before.FP && after.Recall >= before.Recall - .05;
            // Local readout uses the occupancy chosen by that gate, not a grid.
            if (!useField)
                foreach (ImageResult image in calibration)
                    SetLocal(image, image.Fields["P0"]);
            Colored legacy = ColoredMetrics(calibration.SelectMany(image => image.Rows), useField ? "P1" : "P0");
            Colored local = ColoredMetrics(calibration.SelectMany(image => image.Rows), "P2");
            int blackGain = ColoredMetrics(calibration.SelectMany(image => image.Rows).Where(row => row.Color == "black"), "P2").Top1
                - ColoredMetrics(calibration.SelectMany(image => image.Rows).Where(row => row.Color == "black"), useField ? "P1" : "P0").Top1;
            int sharedGain = ColoredMetrics(calibration.SelectMany(image => image.Rows).Where(row => row.Shared), "P2").Top1
                - ColoredMetrics(calibration.SelectMany(image => image.Rows).Where(row => row.Shared), useField ? "P1" : "P0").Top1;
            bool useLocal = (blackGain >= 2 || sharedGain >= 2) && local.Top1 >= legacy.Top1 - 2 && local.Phantoms <= legacy.Phantoms + 2;
            string selectedC3 = useLocal ? "P2" : useField ? "P1" : "P0";
            foreach (ImageResult image in calibration)
                FuseRows(image, selectedC3);
            string[] safe = new[] { "F1", "F2" }.Where(method =>
                ColoredMetrics(calibration.SelectMany(image => image.Rows), method).OutputAccuracy >= .98
                && ColoredMetrics(calibration.SelectMany(image => image.Rows), method).Phantoms == 0).ToArray();
            string selectedFusion = safe.Length > 0
                ? safe.OrderByDescending(method => ColoredMetrics(calibration.SelectMany(image => image.Rows), method).Top1)
                    .ThenByDescending(method => ColoredMetrics(calibration.SelectMany(image => image.Rows), method).OutputAccuracy).First()
                : new[] { "F1", "F2" }.OrderByDescending(method => ColoredMetrics(calibration.SelectMany(image => image.Rows), method).OutputAccuracy)
                    .ThenByDescending(method => ColoredMetrics(calibration.SelectMany(image => image.Rows), method).Top1).First();
            Selection selection = new(useField, useLocal, selectedC3, selectedFusion, blackGain, sharedGain,
                "Old calibration only; P0/P1 occupancy gate, then one localized-color ablation, then two fixed fusion rules. No V3 tuning.");
            Write(Path.Combine(output, "field-floors.json"), floor);
            Write(Path.Combine(output, "selection.json"), selection);
            Write(Path.Combine(output, "calibration.json"), ReportMetrics(calibration, selection));
            Write(Path.Combine(output, "freeze.json"), HashInputs(root, inputs));
            Console.WriteLine(JsonSerializer.Serialize(selection, Json));
            return 0;
        }
        Selection chosen = Read<Selection>(Path.Combine(output, "selection.json"));
        Limits[] floors = Read<Limits[]>(Path.Combine(output, "field-floors.json"));
        foreach (string partition in new[] { "old_holdout_reused", "v3_reused" })
        {
            List<ImageResult> results = [];
            foreach (Input input in inputs.Where(input => input.Partition == partition))
            {
                ImageResult image = Evaluate(root, input, prepared[input.Id], geometry, old, floors, chosen.UseFieldFloors);
                FuseRows(image, chosen.SelectedC3);
                results.Add(image);
                Write(Path.Combine(output, partition + "-progress.json"), results);
            }
            Write(Path.Combine(output, partition + ".json"), ReportMetrics(results, chosen));
        }
        Verify(root, Read<Dictionary<string, string>>(Path.Combine(output, "freeze.json")));
        return 0;
    }

    private static int Prepare(string root, string assets, List<Input> inputs)
    {
        string cache = Path.Combine(root, Cache);
        if (File.Exists(Path.Combine(cache, "prepared.json")))
            throw new InvalidOperationException("Prepared masks already exist; do not overwrite.");
        // Historical files are read, never rewritten. Only the explicitly hardened
        // two A source files are allowed to differ from V4's source freeze.
        Dictionary<string, string> oldFreeze = Read<Dictionary<string, string>>(Path.Combine(root, V4Report, "freeze.json"));
        string[] changed = ["tools/Games/ArkNova/PhotoRecognition/ArkNovaPhotoAnalyzer.cs", "tools/Games/ArkNova/PhotoRecognition/ArkNovaRankingCapture.cs"];
        Verify(root, oldFreeze.Where(pair => !changed.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value));
        List<Prepared> prepared = [];
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        int unchanged = 0;
        foreach (Input input in inputs)
        {
            string folder = Path.Combine(cache, input.Id);
            Directory.CreateDirectory(folder);
            bool sameFrame = false;
            ArkNovaRankingCapture capture = new() { FrameObserver = frame =>
            {
                sameFrame = HashBytes(frame.Warped.ImEncode(".png")) == Hash(Path.Combine(root, OldCache, input.Id + ".png"));
                if (!sameFrame)
                    throw new InvalidDataException("Registration replay changed: " + input.Name);
                SaveColorMasks(folder, frame);
            } };
            RecognitionResult result = analyzer.Analyze(Path.Combine(root, input.Source), rankingCapture: capture);
            Dictionary<string, ColorScores> raw = ArkNovaRankingRules.Predict(capture.Observations, ArkNovaRankingRules.Selected);
            Dictionary<string, ColorScores> confidence = raw.ToDictionary(pair => pair.Key,
                pair => new ColorScores(ArkNovaConfidencePolicy.Evaluate(pair.Value.Tracks, ArkNovaConfidencePolicy.Selected)));
            if (!result.Registration.Success || !sameFrame)
                throw new InvalidDataException("Expected valid identical registration.");
            if (input.AError is null)
            {
                foreach (string color in Colors)
                    foreach (string track in Tracks)
                    {
                        FieldPrediction before = input.A[color].Tracks[track];
                        FieldPrediction after = raw[color].Tracks[track];
                        if (before.Value != after.Value || Math.Abs(before.Score - after.Score) > 1e-12
                            || input.Confidence[color].Tracks[track].Value != confidence[color].Tracks[track].Value)
                            throw new InvalidDataException("Technical hardening changed an otherwise successful A decision.");
                        unchanged++;
                    }
            }
            prepared.Add(new(input.Id, raw, confidence, sameFrame, result.Registration.Success));
            Console.WriteLine($"prepare {input.Name}; identical warp; A success");
        }
        Write(Path.Combine(cache, "prepared.json"), prepared);
        Write(Path.Combine(root, Report, "a-hardening.json"), new
        {
            tolerance = 1e-12, unchangedSuccessfulDecisions = unchanged,
            formerlyFailedImages = inputs.Where(input => input.AError is not null).Select(input => input.Name).ToArray(),
            nowAllImagesRegisterAndAnalyze = true, sameFrameCount = prepared.Count, gtMetricsNotUsed = true,
            description = "Only near-bound floating-point fractions clamped. All shared warps identical; old successful selected values/scores and confidence values preserved. No scoring/threshold change."
        });
        return 0;
    }

    private static void SaveColorMasks(string folder, ArkNovaBlackDiagnostics.Frame frame)
    {
        using Mat red = Mat.Zeros(frame.Hsv.Size(), MatType.CV_8UC1);
        using Mat blue = Mat.Zeros(frame.Hsv.Size(), MatType.CV_8UC1);
        using Mat yellow = Mat.Zeros(frame.Hsv.Size(), MatType.CV_8UC1);
        int height = frame.Hsv.Rows;
        int width = frame.Hsv.Cols;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (frame.Difference.At<byte>(y, x) == 0)
                    continue;
                Vec3b hsv = frame.Hsv.At<Vec3b>(y, x);
                // These are exactly A's existing strict masks, not a new color
                // search. Black strict/relaxed Mats are borrowed from A itself.
                if ((hsv.Item0 < 12 || hsv.Item0 > 170) && hsv.Item1 > 95 && hsv.Item2 > 55)
                    red.Set(y, x, (byte)255);
                if (hsv.Item0 >= 80 && hsv.Item0 <= 135 && hsv.Item1 > 70 && hsv.Item2 > 45)
                    blue.Set(y, x, (byte)255);
                if (hsv.Item0 >= 18 && hsv.Item0 <= 40 && hsv.Item1 > 95 && hsv.Item2 > 90)
                    yellow.Set(y, x, (byte)255);
            }
        using Mat blackGrown = ArkNovaPhotoAnalyzer.GrowMask(frame.Strict, frame.Relaxed);
        Mat[] masks = [red, blue, yellow, frame.Strict, blackGrown];
        for (int index = 0; index < masks.Length; index++)
        {
            using Mat full = Mat.Zeros(frame.Warped.Size(), MatType.CV_8UC1);
            using Mat destination = new(full, new Rect(frame.Origin, masks[index].Size()));
            masks[index].CopyTo(destination);
            File.WriteAllBytes(Path.Combine(folder, index + ".png"), full.ImEncode(".png"));
        }
    }

    private static Limits[] FitFields(string root, List<Input> inputs, ClassicDetectors geometry, V4Model old)
    {
        foreach (Input input in inputs.Where(input => input.Partition == "empty"))
        {
            using Mat image = Cv2.ImRead(Path.Combine(root, OldCache, input.Id + ".png"));
            double[][] vectors = Intensities(image, geometry);
            Features features = new(geometry.Regions.Select((region, index) =>
                new PatchFeatures([], vectors[index], Visibility(input, region))).ToArray());
            geometry.Normals.Add(new(input.Name, features, features));
        }
        Limits[] result = geometry.Regions.Select(region => old.Limits["C3"][region.Track]).ToArray();
        for (int index = 0; index < geometry.Regions.Count; index++)
        {
            Region region = geometry.Regions[index];
            if (region.Track != "attractiveness")
                continue;
            List<float[]> samples = [];
            List<double> normalPixels = [];
            foreach (Normal normal in geometry.Normals)
            {
                float[] map = Sparse(normal.Raw.Patches[index].Intensity, geometry.FitSubspace(index, normal.Name));
                samples.Add(map);
                using Mat small = new(16, 16, MatType.CV_32FC1);
                small.SetArray(map);
                using Mat full = new();
                Cv2.Resize(small, full, region.Patch.Size, 0, 0, InterpolationFlags.Linear);
                for (int y = 0; y < region.Patch.Height; y++)
                    for (int x = 0; x < region.Patch.Width; x++)
                        if (region.Mask.At<byte>(y, x) != 0)
                            normalPixels.Add(full.At<float>(y, x));
            }
            normalPixels.Sort();
            Limits start = result[index];
            double pixel = Math.Max(start.Pixel, normalPixels[(int)((normalPixels.Count - 1) * .99)]);
            double occupancy = start.Occupancy;
            foreach (float[] sample in samples)
            {
                using Measurement normal = Measure(sample, region, new(pixel, start.Occupancy), 1);
                occupancy = Math.Max(occupancy, normal.Field.Score);
            }
            result[index] = new(pixel, occupancy);
        }
        return result;
    }

    private static ImageResult Evaluate(string root, Input input, Prepared current, ClassicDetectors geometry, V4Model old,
        Limits[] floors, bool useFieldFloors)
    {
        Stopwatch timer = Stopwatch.StartNew();
        using Mat color = Cv2.ImRead(Path.Combine(root, OldCache, input.Id + ".png"));
        double[][] intensity = Intensities(color, geometry);
        byte[][] masks = Enumerable.Range(0, 5).Select(index =>
        {
            using Mat mask = Cv2.ImRead(Path.Combine(root, Cache, input.Id, index + ".png"), ImreadModes.Grayscale);
            mask.GetArray(out byte[] pixels);
            return pixels;
        }).ToArray();
        List<Field> baseline = [];
        List<Field> refined = [];
        for (int index = 0; index < geometry.Regions.Count; index++)
        {
            Region region = geometry.Regions[index];
            float[] sparse = Sparse(intensity[index], old.Subspaces[index]);
            using Measurement before = Measure(sparse, region, old.Limits["C3"][region.Track], Visibility(input, region), masks);
            using Measurement after = Measure(sparse, region, floors[index], Visibility(input, region), masks);
            baseline.Add(before.Field);
            refined.Add(after.Field);
        }
        Field[] baseFields = baseline.ToArray();
        Field[] fieldFields = refined.ToArray();
        Dictionary<string, Prediction> p0 = Predict(baseFields, input.Observations, local: false);
        Dictionary<string, Prediction> p1 = Predict(fieldFields, input.Observations, local: false);
        Field[] chosen = useFieldFloors ? fieldFields : baseFields;
        Dictionary<string, Prediction> p2 = Predict(chosen, input.Observations, local: true);
        Dictionary<string, Dictionary<string, int>?> truth = Truth(root, input);
        List<Row> rows = [];
        foreach (string player in Colors)
            foreach (string track in Tracks)
            {
                int? expected = truth.GetValueOrDefault(player)?.GetValueOrDefault(track);
                FieldPrediction? legacy = input.A.GetValueOrDefault(player)?.Tracks.GetValueOrDefault(track);
                FieldPrediction? hardened = current.Raw.GetValueOrDefault(player)?.Tracks.GetValueOrDefault(track);
                FieldPrediction? confidence = current.Confidence.GetValueOrDefault(player)?.Tracks.GetValueOrDefault(track);
                int[] aTop3 = ArkNovaRankingRules.Rank(input.Observations.Where(observation => observation.Color == player && observation.Track == track)
                    .Select(Normalize), ArkNovaRankingRules.Selected).Fields.Take(3).Select(field => field.Observation.Value).ToArray();
                Dictionary<string, Prediction> predictions = new()
                {
                    ["A_legacy"] = APrediction(legacy, input.AError is null ? aTop3 : []), ["A_hardened"] = APrediction(hardened, aTop3),
                    ["A_accepted"] = APrediction(confidence?.Confidence is "High" or "Medium" ? confidence : null),
                    ["P0"] = p0[player + "/" + track], ["P1"] = p1[player + "/" + track], ["P2"] = p2[player + "/" + track]
                };
                bool shared = expected.HasValue && truth.Values.Count(value => value is not null && value[track] == expected) > 1;
                bool dense = expected.HasValue && truth.Any(pair => pair.Key != player && pair.Value is not null && Math.Abs(pair.Value[track] - expected.Value) == 1);
                rows.Add(new(player, track, expected, legacy?.Value, hardened, confidence, predictions, shared, dense));
            }
        ImageResult result = new(input.Name, input.Partition, input.View, timer.Elapsed.TotalMilliseconds,
            new() { ["P0"] = baseFields, ["P1"] = fieldFields, ["P2"] = chosen }, rows);
        if (input.Partition != "calibration" && input.Name is "IMG_4268.jpg" or "Fall3Senkrecht.jpg" or "Fall5Senkrecht.jpg")
        {
            string track = input.Name == "IMG_4268.jpg" ? "conservation" : "reputation";
            int? value = rows.Single(row => row.Color == "black" && row.Track == track).Expected;
            if (value.HasValue)
            {
                int index = geometry.Regions.FindIndex(region => region.Track == track && region.Value == value);
                Region region = geometry.Regions[index];
                using Measurement patch = Measure(Sparse(intensity[index], old.Subspaces[index]), region,
                    useFieldFloors ? floors[index] : old.Limits["C3"][track], Visibility(input, region), masks);
                string label = $"GT black={value}; local={p2["black/" + track].Value?.ToString() ?? "null"}; A={current.Raw["black"].Tracks[track].Value?.ToString() ?? "null"}";
                Export(Path.Combine(root, Report, "Diagnostics", input.Id + ".png"), color, region, patch, label);
                Write(Path.Combine(root, Report, "Diagnostics", input.Id + ".json"), new { patch.Field, rows });
            }
        }
        Console.WriteLine($"{input.Partition}/{input.Name}: P0={rows.Count(row => row.Expected.HasValue && row.Predictions["P0"].Value == row.Expected)}, P2={rows.Count(row => row.Expected.HasValue && row.Predictions["P2"].Value == row.Expected)}");
        return result;
    }

    private static void SetLocal(ImageResult image, Field[] fields)
    {
        image.Fields["P2"] = fields;
        Dictionary<string, Prediction> local = Predict(fields, [], local: true);
        foreach (Row row in image.Rows)
            row.Predictions["P2"] = local[row.Color + "/" + row.Track];
    }

    private static void FuseRows(ImageResult image, string method)
    {
        foreach (Row row in image.Rows)
        {
            Prediction selected = row.Predictions[method];
            bool elsewhere = image.Rows.Where(other => other.Color == row.Color && other.Track != row.Track).Any(other =>
                (other.A?.CoreColor ?? 0) >= .15 || other.Predictions[method].Strong);
            foreach (string fusion in new[] { "F1", "F2" })
            {
                int? value = Fuse(fusion, row.Track, row.A, row.Confidence, selected, elsewhere);
                row.Predictions[fusion] = new(value, value.HasValue ? [value.Value] : [], false, false, 0, []);
            }
        }
    }

    private static Prediction APrediction(FieldPrediction? a, int[]? top3 = null) => new(a?.Value,
        top3 ?? (a?.Value.HasValue == true ? [a.Value.Value] : []), false, false, 0, []);

    private static RankingObservation Normalize(RankingObservation value) => value with
    {
        FullColor = ArkNovaMaskFraction.ClampRoundoff(value.FullColor), CoreColor = ArkNovaMaskFraction.ClampRoundoff(value.CoreColor),
        FullDifference = ArkNovaMaskFraction.ClampRoundoff(value.FullDifference), CoreDifference = ArkNovaMaskFraction.ClampRoundoff(value.CoreDifference),
        Grown = ArkNovaMaskFraction.ClampRoundoff(value.Grown), Visibility = ArkNovaMaskFraction.ClampRoundoff(value.Visibility),
        TrackVisibility = ArkNovaMaskFraction.ClampRoundoff(value.TrackVisibility)
    };

    private static double Visibility(Input input, Region region) => input.Observations.First(observation => observation.Track == region.Track && observation.Value == region.Value).Visibility;

    private static Dictionary<string, Dictionary<string, int>?> Truth(string root, Input input)
    {
        string path = input.Partition == "v3_reused" ? "artifacts/ResearchV3/dataset_v3/ground_truth_v3.json"
            : "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/scoring_ground_truth_v2.json";
        using JsonDocument truth = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, path)));
        JsonElement expected = input.Partition == "v3_reused" ? truth.RootElement.GetProperty("cases").GetProperty(input.Name[..5])
            : truth.RootElement.GetProperty("photos").GetProperty(input.Name).GetProperty("expected");
        return Colors.ToDictionary(color => color, color => expected.TryGetProperty(color, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? Tracks.ToDictionary(track => track, track => value.GetProperty(track).GetInt32()) : null);
    }

    private static object ReportMetrics(List<ImageResult> images, Selection selection) => new
    {
        validation = "Reused comparisons only; all selection on old calibration/Empty.", selection,
        methods = Variants.ToDictionary(method => method, method => new
        {
            summary = Metrics(images, method),
            byTrack = Tracks.ToDictionary(track => track, track => Metrics(images, method, track)),
            byColor = Colors.ToDictionary(color => color, color => ColoredMetrics(images.SelectMany(image => image.Rows).Where(row => row.Color == color), method)),
            byView = images.GroupBy(image => image.View).ToDictionary(group => group.Key, group => Metrics(group.ToList(), method)),
            shared = ColoredMetrics(images.SelectMany(image => image.Rows).Where(row => row.Shared), method),
            dense = ColoredMetrics(images.SelectMany(image => image.Rows).Where(row => row.Dense), method),
            aUncertainOrNull = ColoredMetrics(images.SelectMany(image => image.Rows).Where(row => row.Confidence?.Value is null || row.Confidence.Confidence == "Uncertain"), method)
        }),
        meanImageMilliseconds = images.Average(image => image.Milliseconds),
        peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64, images
    };

    private static Metric Metrics(List<ImageResult> images, string method, string? track = null)
    {
        int tp = 0, fp = 0, fn = 0;
        foreach (ImageResult image in images)
            foreach (string selectedTrack in Tracks.Where(value => track is null || value == track))
            {
                HashSet<int> expected = image.Rows.Where(row => row.Track == selectedTrack && row.Expected.HasValue && row.Expected != 0).Select(row => row.Expected!.Value).ToHashSet();
                HashSet<int> predicted = method.StartsWith("P", StringComparison.Ordinal)
                    ? image.Fields[method].Where(field => field.Track == selectedTrack && field.Candidate).Select(field => field.Value).ToHashSet()
                    : image.Rows.Where(row => row.Track == selectedTrack && row.Predictions[method].Value.HasValue)
                        .Select(row => row.Predictions[method].Value!.Value).ToHashSet();
                tp += expected.Intersect(predicted).Count();
                fp += predicted.Except(expected).Count();
                fn += expected.Except(predicted).Count();
            }
        double precision = tp + fp > 0 ? (double)tp / (tp + fp) : 0;
        double recall = tp + fn > 0 ? (double)tp / (tp + fn) : 0;
        return new(tp, fp, fn, precision, recall, precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0,
            ColoredMetrics(images.SelectMany(image => image.Rows).Where(row => track is null || row.Track == track), method));
    }

    private static Colored ColoredMetrics(IEnumerable<Row> source, string method)
    {
        Row[] all = source.ToArray();
        Row[] rows = all.Where(row => row.Expected.HasValue).ToArray();
        int top1 = rows.Count(row => row.Predictions[method].Value == row.Expected);
        int outputs = all.Count(row => row.Predictions[method].Value.HasValue);
        return new(rows.Length, top1, rows.Count(row => row.Predictions[method].Top3.Take(2).Contains(row.Expected!.Value)),
            rows.Count(row => row.Predictions[method].Top3.Contains(row.Expected!.Value)), rows.Count(row => row.Predictions[method].Value is null),
            all.Count(row => !row.Expected.HasValue && row.Predictions[method].Value.HasValue), outputs, outputs > 0 ? (double)top1 / outputs : 0,
            rows.Length > 0 ? (double)rows.Count(row => row.Predictions[method].Value.HasValue) / rows.Length : 0,
            rows.Count(row => row.A?.Value == row.Expected && row.Predictions[method].Value == row.Expected),
            rows.Count(row => row.A?.Value == row.Expected && row.Predictions[method].Value != row.Expected),
            rows.Count(row => row.A?.Value != row.Expected && row.Predictions[method].Value == row.Expected),
            rows.Count(row => row.A?.Value != row.Expected && row.Predictions[method].Value != row.Expected),
            rows.Count(row => row.A?.Value == row.Expected || row.Predictions[method].Value == row.Expected));
    }

    private static Dictionary<string, string> HashInputs(string root, List<Input> inputs)
    {
        IEnumerable<string> files = Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/C3Refinement"), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/C3Refinement"), "*.csproj"))
            .Concat(Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition"), "*.cs"))
            .Concat(Directory.GetFiles(Path.Combine(root, Cache), "*", SearchOption.AllDirectories))
            .Concat(inputs.SelectMany(input => new[] { Path.Combine(root, input.Source), Path.Combine(root, OldCache, input.Id + ".png") }))
            .Concat(new[] { Path.Combine(root, V4Report, "model.json"), Path.Combine(root, OldCache, "images.json"),
                Path.Combine(root, "tools/Games/ArkNova/ClassicCvResearch/ClassicDetectors.cs"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/scoring_regions_v2.json"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/board_reference_master_clean.png"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/scoring_ground_truth_v2.json"),
                Path.Combine(root, "artifacts/ResearchV3/dataset_v3/ground_truth_v3.json"),
                Path.Combine(root, "Directory.Packages.props"), Path.Combine(root, "Directory.Build.props"), Path.Combine(root, "global.json") })
            .Concat(new[] { "PLAN.md", "selection.json", "field-floors.json", "calibration.json", "a-hardening.json" }.Select(name => Path.Combine(root, Report, name)));
        return files.Distinct().Order().ToDictionary(path => Path.GetRelativePath(root, path).Replace('\\', '/'), Hash);
    }

    private static int Checks(string assets)
    {
        if (ArkNovaMaskFraction.ClampRoundoff(1.0000000000000002) != 1 || ArkNovaMaskFraction.ClampRoundoff(.15) != .15)
            throw new InvalidDataException("Numerical hardening must only affect boundary roundoff.");
        Prediction strong = new(20, [20], true, true, .1, []);
        if (Fuse("F1", "conservation", null, null, strong, true) is not null)
            throw new InvalidDataException("Agreement rule must not manufacture agreement.");
        if (Fuse("F2", "attractiveness", null, null, strong, true) is not null)
            throw new InvalidDataException("Attractiveness rescue requires agreement.");
        if (Fuse("F2", "conservation", null, null, strong, true) != 20)
            throw new InvalidDataException("Conservation may use strong isolated C3 without A.");
        using ClassicDetectors geometry = new(assets);
        Region region = geometry.Regions.First(region => region.Track == "reputation" && region.Value == 8);
        using Measurement noChange = Measure(new float[256], region, new(.04, .06), 1);
        if (noChange.Field.Candidate || noChange.Field.Score != 0 || noChange.Field.Colors.Any(evidence => evidence.Supported))
            throw new InvalidDataException("No sparse change cannot produce any color or candidate.");
        byte[][] colors = Enumerable.Range(0, 5).Select(_ => new byte[2000 * 1400]).ToArray();
        for (int y = 0; y < region.Patch.Height; y++)
            for (int x = 0; x < region.Patch.Width; x++)
            {
                int pixel = (region.Patch.Y + y) * 2000 + region.Patch.X + x;
                colors[x < region.Center.X ? 0 : 1][pixel] = 255;
                colors[4][pixel] = 255; // Relaxed dark pixels without any strict seed.
            }
        using Measurement shared = Measure(Enumerable.Repeat(1f, 256).ToArray(), region, new(.04, .06), 1, colors);
        Dictionary<string, Prediction> sharedReadout = Predict([shared.Field], [], local: true);
        if (sharedReadout["red/reputation"].Value != 8 || sharedReadout["blue/reputation"].Value != 8)
            throw new InvalidDataException("Two independently supported colors must be allowed on one field.");
        if (sharedReadout["black/reputation"].Value.HasValue)
            throw new InvalidDataException("Relaxed dark evidence without local strict seeds must not create black.");
        float[] outside = new float[256];
        for (int y = 0; y < 16; y++)
            outside[y * 16] = 1;
        using Measurement near = Measure(outside, region, new(.04, .06), 1, colors);
        Region zero = geometry.Regions.First(value => value.Value == 0);
        using Measurement unsupported = Measure(Enumerable.Repeat(1f, 256).ToArray(), zero, new(.04, .06), 1);
        if (near.Field.Score != 0 || near.Field.Candidate || unsupported.Field.Candidate)
            throw new InvalidDataException("Outside evidence and unsupported zero must not become score candidates.");
        Console.WriteLine("8 focused research checks passed, including shared colors, black seed gate and outside/zero exclusion.");
        return 0;
    }

    private static string Hash(string path) => HashBytes(File.ReadAllBytes(path));
    private static string HashBytes(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static void Verify(string root, Dictionary<string, string> hashes)
    {
        if (hashes.Any(pair => Hash(Path.Combine(root, pair.Key)) != pair.Value))
            throw new InvalidDataException("Frozen source/input mismatch.");
    }
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)!;
    private static void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json));

    internal sealed record Input(string Name, string Id, string Partition, string View, string Source, string Sha256,
        RegistrationDiagnostics Registration, Dictionary<string, ColorScores> A, Dictionary<string, ColorScores> Confidence,
        RankingObservation[] Observations, string? AError);
    internal sealed record Prepared(string Id, Dictionary<string, ColorScores> Raw, Dictionary<string, ColorScores> Confidence, bool SameFrame, bool RegistrationSuccess);
    internal sealed record V4Model(Dictionary<string, Dictionary<string, Limits>> Limits, LowRankModel[] Subspaces, double FitMilliseconds);
    internal sealed record Selection(bool UseFieldFloors, bool UseLocal, string SelectedC3, string SelectedFusion, int BlackGain, int SharedGain, string Discipline);
    internal sealed record Row(string Color, string Track, int? Expected, int? LegacyA, FieldPrediction? A, FieldPrediction? Confidence,
        Dictionary<string, Prediction> Predictions, bool Shared, bool Dense);
    internal sealed record ImageResult(string Name, string Partition, string View, double Milliseconds, Dictionary<string, Field[]> Fields, List<Row> Rows);
    internal sealed record Metric(int TP, int FP, int FN, double Precision, double Recall, double F1, Colored Colored);
    internal sealed record Colored(int Total, int Top1, int Top2, int Top3, int Null, int Phantoms, int OutputCount, double OutputAccuracy, double Coverage,
        int ARightRight, int ARightWrong, int AWrongRight, int AWrongWrong, int Oracle);
}

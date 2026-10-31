using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using OpenCvSharp;
using static BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.ClassicDetectors;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal static class ClassicBatch
{
    internal static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] Tracks = ["attractiveness", "conservation", "reputation"];
    private static readonly string[] Colors = ["red", "blue", "yellow", "black"];
    private const string ToolPath = "tools/Games/ArkNova/ClassicCvResearch";
    private const string ReportPath = "tools/Games/ArkNova/PhotoRecognition/Reports/ClassicCvResearchV4";
    private const string CachePath = "artifacts/ClassicCvV4";

    public static int Run(string command, string root)
    {
        string output = Path.Combine(root, ReportPath);
        string cache = Path.Combine(root, CachePath);
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(cache);
        if (command == "self-test")
            return SelfTest(assets);
        if (command == "prepare")
            return Prepare(root, assets, cache);
        if (command != "evaluate" && File.Exists(Path.Combine(output, "comparison-started.json")))
            throw new InvalidOperationException("Comparison already started. No fitting or selection after reused comparison metrics.");
        if (command == "evaluate")
        {
            Dictionary<string, string> hashes = Read<Dictionary<string, string>>(Path.Combine(output, "freeze.json"));
            if (hashes.Any(pair => Hash(Path.Combine(root, pair.Key)) != pair.Value))
                throw new InvalidDataException("Frozen inputs/sources changed.");
            using FileStream latch = new(Path.Combine(output, "comparison-started.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(latch, new { startedUtc = DateTimeOffset.UtcNow, reuseOnly = true }, Json);
        }
        List<CachedImage> inputs = Read<List<CachedImage>>(Path.Combine(cache, "images.json"));
        Stopwatch init = Stopwatch.StartNew();
        using ClassicDetectors detector = new(assets);
        List<EccInfo> normalEcc = [];
        foreach (CachedImage input in inputs.Where(image => image.Partition == "empty"))
        {
            using Frame frame = LoadFrame(input, cache, detector);
            detector.Normals.Add(new(input.Name, frame.Raw, frame.Ecc));
            normalEcc.AddRange(frame.Alignment.Diagnostics);
        }
        double initializationMs = init.Elapsed.TotalMilliseconds;
        if (command == "fit")
        {
            Model model = Fit(detector, inputs.Where(image => image.Partition == "empty").ToArray(), cache, output);
            Write(Path.Combine(output, "model.json"), model);
            Write(Path.Combine(output, "normal-ecc.json"), normalEcc);
            return 0;
        }
        Model config = Read<Model>(Path.Combine(output, "model.json"));
        if (command == "calibrate")
        {
            List<ImageEvaluation> calibration = [];
            foreach (CachedImage input in inputs.Where(image => image.Partition == "calibration"))
                calibration.Add(Evaluate(root, cache, detector, config, input, Methods, null));
            Write(Path.Combine(output, "calibration-independent.json"), Report(calibration, Methods, initializationMs));
            Summary c1 = Metrics(calibration, "C1");
            Summary c2a = Metrics(calibration, "C2a");
            Summary c2b = Metrics(calibration, "C2b");
            Summary c3 = Metrics(calibration, "C3");
            double eccGain = calibration.SelectMany(image => image.Ecc).Average(info => info.After - info.Before);
            bool k1 = c1.Recall >= .25 && c1.Precision >= .25;
            bool k2 = c2b.F1 > c2a.F1 && eccGain >= .01;
            bool k3 = c3.Recall >= .25 && c1.Precision >= .30;
            List<string> comparison = [.. Methods];
            if (k2)
            {
                List<double> normalScores = [];
                List<Detection> normalShapes = [];
                foreach (CachedImage input in inputs.Where(image => image.Partition == "empty"))
                {
                    using Frame frame = LoadFrame(input, cache, detector);
                    for (int index = 0; index < detector.Regions.Count; index++)
                        normalShapes.Add(detector.Shape(frame.Alignment.Frames[detector.Regions[index].Track], index,
                            frame.Ecc.Patches[index].Visibility, new(0, .65)) with { Method = "K2" });
                }
                config.Limits["K2"] = Tracks.ToDictionary(track => track,
                    track => new Limits(0, Math.Max(.65, Quantile(normalShapes.Where(field => field.Track == track).Select(field => field.Score), .995))));
                foreach (ImageEvaluation image in calibration)
                {
                    CachedImage input = inputs.Single(input => input.Name == image.Name && input.Partition == "calibration");
                    using Frame frame = LoadFrame(input, cache, detector);
                    Stopwatch timer = Stopwatch.StartNew();
                    image.Fields["K2"] = SuppressDuplicateShapes(detector.Regions.Select((region, index) => detector.Shape(frame.Alignment.Frames[region.Track], index,
                        frame.Ecc.Patches[index].Visibility, config.Limits["K2"][region.Track]) with { Method = "K2" }).ToList());
                    image.Milliseconds["K2"] = timer.Elapsed.TotalMilliseconds;
                    AddReadout(image, input, "K2");
                }
                comparison.Add("K2");
            }
            if (k3)
            {
                foreach (ImageEvaluation image in calibration)
                    AddIntersection(image, inputs.Single(input => input.Name == image.Name && input.Partition == "calibration"));
                comparison.Add("K3");
            }
            string? ensemble = Methods.Where(method => Metrics(calibration, method).AWrongReadoutRight >= 2 && Metrics(calibration, method).Recall >= .15)
                .OrderByDescending(method => Metrics(calibration, method).AWrongReadoutRight).ThenByDescending(method => Metrics(calibration, method).F1).FirstOrDefault();
            Selection selection = new(comparison.ToArray(), k1, k2, k3, ensemble, eccGain,
                "All gates use old calibration only. V2 comparison and V3 are reused research comparisons, not fresh holdouts.");
            Write(Path.Combine(output, "selection.json"), selection);
            Write(Path.Combine(output, "model.json"), config);
            Write(Path.Combine(output, "calibration.json"), Report(calibration, comparison.ToArray(), initializationMs, ensemble));
            Write(Path.Combine(output, "freeze.json"), InputHashes(root, output, cache));
            Console.WriteLine(JsonSerializer.Serialize(selection, Json));
            return 0;
        }
        Selection chosen = Read<Selection>(Path.Combine(output, "selection.json"));
        foreach (string partition in new[] { "old_holdout_reused", "v3_reused" })
        {
            List<ImageEvaluation> evaluations = [];
            int diagnosticBudget = 0;
            foreach (CachedImage input in inputs.Where(input => input.Partition == partition))
            {
                bool export = partition == "old_holdout_reused" ? diagnosticBudget++ < 3
                    : input.Name is "Fall1Vorne.jpg" or "Fall3Senkrecht.jpg" or "Fall5Senkrecht.jpg";
                string? diagnostics = export ? Path.Combine(output, "Diagnostics", partition, Path.GetFileNameWithoutExtension(input.Name)) : null;
                evaluations.Add(Evaluate(root, cache, detector, config, input, chosen.Methods, diagnostics));
                // Durable per-image output prevents losing a long batch to later I/O failures.
                Write(Path.Combine(output, partition + "-progress.json"), evaluations);
            }
            Write(Path.Combine(output, partition + ".json"), Report(evaluations, chosen.Methods, initializationMs, chosen.EnsembleMethod));
        }
        Dictionary<string, string> finalHashes = Read<Dictionary<string, string>>(Path.Combine(output, "freeze.json"));
        if (finalHashes.Any(pair => Hash(Path.Combine(root, pair.Key)) != pair.Value))
            throw new InvalidDataException("Frozen source/input changed during comparison.");
        Console.WriteLine("Fixed V4 comparison completed. All datasets are reused, not independent holdouts.");
        return 0;
    }

    private static int Prepare(string root, string assets, string cache)
    {
        if (File.Exists(Path.Combine(cache, "images.json")))
            throw new InvalidOperationException("Prepared cache already exists; do not overwrite provenance.");
        List<ImageInput> inputs = [];
        string v2 = Path.Combine(assets, "ConfidenceV2");
        using JsonDocument split = JsonDocument.Parse(File.ReadAllText(Path.Combine(v2, "split.json")));
        foreach (string partition in new[] { "calibration", "holdout" })
            foreach (JsonElement name in split.RootElement.GetProperty(partition).EnumerateArray())
                inputs.Add(new(name.GetString()!, Path.Combine(v2, "images", name.GetString()!), partition == "calibration" ? partition : "old_holdout_reused", "unknown_v2"));
        string v3 = Path.Combine(root, "artifacts/ResearchV3/dataset_v3");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(v3, "manifest_v3.json")));
        foreach (JsonElement image in manifest.RootElement.GetProperty("images").EnumerateArray())
        {
            string name = image.GetProperty("file").GetString()!;
            string path = Path.Combine(v3, "images", name);
            if (Hash(path) != image.GetProperty("scaled_sha256").GetString())
                throw new InvalidDataException("V3 input hash mismatch.");
            inputs.Add(new(name, path, name.StartsWith("Empty", StringComparison.Ordinal) ? "empty" : "v3_reused", image.GetProperty("view").GetString()!));
        }
        using JsonDocument v2manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(v2, "manifest.json")));
        foreach (JsonElement file in v2manifest.RootElement.GetProperty("files").EnumerateArray())
            if (Hash(Path.Combine(v2, "images", file.GetProperty("name").GetString()!)) != file.GetProperty("sha256").GetString())
                throw new InvalidDataException("V2 input hash mismatch.");
        List<CachedImage> images = [];
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        foreach (ImageInput input in inputs)
        {
            string id = input.Partition + "_" + Path.GetFileNameWithoutExtension(input.Name);
            ArkNovaBlackDiagnostics observer = new("", _ => { });
            ArkNovaRankingCapture capture = new() { FrameObserver = frame =>
            {
                File.WriteAllBytes(Path.Combine(cache, id + ".png"), frame.Warped.ImEncode(".png"));
                File.WriteAllBytes(Path.Combine(cache, id + ".valid.png"), frame.Validity.ImEncode(".png"));
            } };
            string? error = null;
            RecognitionResult result;
            try
            {
                result = analyzer.Analyze(input.Path, observer, capture);
            }
            catch (ArgumentOutOfRangeException failure) when (failure.ParamName == "value" && failure.Message.StartsWith("Evidence fractions must be finite", StringComparison.Ordinal))
            {
                error = failure.Message;
                result = new(observer.Registration!, [], new(0, 0, 0, 0));
            }
            Dictionary<string, ColorScores> raw = error is null ? ArkNovaRankingRules.Predict(capture.Observations, ArkNovaRankingRules.Selected) : [];
            Dictionary<string, ColorScores> confidence = raw.ToDictionary(pair => pair.Key,
                pair => new ColorScores(ArkNovaConfidencePolicy.Evaluate(pair.Value.Tracks, ArkNovaConfidencePolicy.Selected)));
            images.Add(new(input.Name, id, input.Partition, input.View, Path.GetRelativePath(root, input.Path).Replace('\\', '/'),
                Hash(input.Path), result.Registration, raw, confidence, capture.Observations.ToArray(), error));
            Console.WriteLine($"prepare {input.Partition}/{input.Name}; registration={result.Registration.Success}; Aerror={error is not null}");
        }
        Write(Path.Combine(cache, "images.json"), images);
        return 0;
    }

    private static Frame LoadFrame(CachedImage image, string cache, ClassicDetectors detector)
    {
        using Mat color = Cv2.ImRead(Path.Combine(cache, image.Id + ".png"));
        Mat gray = ClassicDetectors.Gray(color);
        Mat validity = Cv2.ImRead(Path.Combine(cache, image.Id + ".valid.png"), ImreadModes.Grayscale);
        Stopwatch timer = Stopwatch.StartNew();
        AlignedTracks alignment = detector.AlignTracks(gray);
        double eccMs = timer.Elapsed.TotalMilliseconds;
        timer.Restart();
        Features raw = detector.Extract(gray, validity);
        double rawMs = timer.Elapsed.TotalMilliseconds;
        using Mat conservativeValidity = new();
        using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(9, 9));
        Cv2.Erode(validity, conservativeValidity, kernel, borderType: BorderTypes.Constant, borderValue: Scalar.Black);
        timer.Restart();
        Dictionary<string, Features> aligned = alignment.Frames.ToDictionary(pair => pair.Key, pair => detector.Extract(pair.Value, conservativeValidity));
        Features ecc = new(detector.Regions.Select((region, index) => aligned[region.Track].Patches[index]).ToArray());
        return new(gray, validity, alignment, raw, ecc, eccMs, rawMs, timer.Elapsed.TotalMilliseconds);
    }

    private static Model Fit(ClassicDetectors detector, CachedImage[] empty, string cache, string output)
    {
        Stopwatch timer = Stopwatch.StartNew();
        Dictionary<string, Dictionary<string, Limits>> limits = [];
        List<Detection> shapes = [];
        foreach (CachedImage image in empty)
        {
            using Frame frame = LoadFrame(image, cache, detector);
            for (int index = 0; index < detector.Regions.Count; index++)
                shapes.Add(detector.Shape(frame.Gray, index, frame.Raw.Patches[index].Visibility, new(0, .65)));
            Console.WriteLine($"fit shape normal {image.Name}");
        }
        limits["C1"] = Tracks.ToDictionary(track => track, track => new Limits(0,
            Math.Max(.65, Quantile(shapes.Where(field => field.Track == track && field.Value != 0).Select(field => field.Score), .995))));
        foreach (string method in new[] { "C2a", "C2b", "C3" })
        {
            Dictionary<string, List<double>> pixels = Tracks.ToDictionary(track => track, _ => new List<double>());
            List<(string Name, int Index, float[] Map, double Visible)> maps = [];
            foreach (Normal normal in detector.Normals)
                for (int index = 0; index < detector.Regions.Count; index++)
                {
                    Region region = detector.Regions[index];
                    PatchFeatures patch = (method == "C2b" ? normal.Ecc : normal.Raw).Patches[index];
                    float[] map = method == "C3" ? Sparse(patch.Intensity, detector.FitSubspace(index, normal.Name))
                        : detector.CensusMap(patch, index, method == "C2b", normal.Name);
                    if (patch.Visibility >= .97)
                        for (int pixel = 0; pixel < map.Length; pixel += 7)
                            pixels[region.Track].Add(map[pixel]);
                    maps.Add((normal.Name, index, map, patch.Visibility));
                }
            Dictionary<string, Limits> thresholds = Tracks.ToDictionary(track => track, track => new Limits(
                Math.Max(method == "C3" ? .04 : .25, Quantile(pixels[track], .99)), .06));
            List<Detection> normalDecisions = maps.Select(entry => detector.ChangeScore(method, entry.Index, entry.Map, entry.Visible,
                thresholds[detector.Regions[entry.Index].Track])).ToList();
            foreach (string track in Tracks)
                thresholds[track] = thresholds[track] with { Occupancy = Math.Max(.06,
                    Quantile(normalDecisions.Where(field => field.Track == track && field.Value != 0 && field.Visibility >= .97).Select(field => field.Score), .995)) };
            limits[method] = thresholds;
            Write(Path.Combine(output, method + "-empty-fit.json"), new { thresholds,
                finalCandidates = normalDecisions.Count(field => field.Value != 0 && field.Visibility >= .97 && field.Score > thresholds[field.Track].Occupancy),
                fieldObservations = normalDecisions.Count, description = "Normal leave-one-image-out fit, not independent specificity validation." });
            Console.WriteLine($"fit {method}: {JsonSerializer.Serialize(thresholds)}");
        }
        Write(Path.Combine(output, "C1-empty-fit.json"), new { thresholds = limits["C1"], shapes, description = "Geometric templates, Empty-only background quantiles; no occupied template extraction." });
        LowRankModel[] models = detector.Regions.Select((_, index) => detector.FitSubspace(index)).ToArray();
        Console.WriteLine($"fit complete: {timer.Elapsed.TotalSeconds:F1} seconds");
        return new(limits, models, timer.Elapsed.TotalMilliseconds);
    }

    private static ImageEvaluation Evaluate(string root, string cache, ClassicDetectors detector, Model config, CachedImage input, string[] methods, string? diagnostics)
    {
        Dictionary<string, Dictionary<string, int>?> truth = Truth(root, input);
        using Frame frame = LoadFrame(input, cache, detector);
        Dictionary<string, List<Detection>> fields = [];
        Dictionary<string, double> timings = [];
        foreach (string method in methods.Where(method => method != "K3"))
        {
            Stopwatch timer = Stopwatch.StartNew();
            List<Detection> detections = [];
            for (int index = 0; index < detector.Regions.Count; index++)
            {
                Region region = detector.Regions[index];
                bool ecc = method is "C2b" or "K2";
                PatchFeatures patch = (ecc ? frame.Ecc : frame.Raw).Patches[index];
                string diagnosticColor = region.Track == "conservation" ? "blue" : "black";
                string? export = diagnostics is not null && truth.GetValueOrDefault(diagnosticColor)?.GetValueOrDefault(region.Track) == region.Value
                    ? Path.Combine(diagnostics, $"{method}-{region.Track}-{region.Value}.png") : null;
                if (method is "C1" or "K2")
                    detections.Add(detector.Shape(ecc ? frame.Alignment.Frames[region.Track] : frame.Gray, index, patch.Visibility,
                        config.Limits[method][region.Track], export) with { Method = method });
                else
                {
                    float[] map = method == "C3" ? Sparse(patch.Intensity, config.Subspaces[index]) : detector.CensusMap(patch, index, ecc, null);
                    detections.Add(detector.ChangeScore(method, index, map, patch.Visibility, config.Limits[method][region.Track], export, frame.Gray));
                }
            }
            fields[method] = method is "C1" or "K2" ? SuppressDuplicateShapes(detections) : detections;
            timings[method] = timer.Elapsed.TotalMilliseconds
                + (method == "C2b" ? frame.EccMilliseconds + frame.EccFeatureMilliseconds : method == "K2" ? frame.EccMilliseconds
                    : method is "C2a" or "C3" ? frame.RawFeatureMilliseconds : 0);
        }
        List<Decision> decisions = [];
        foreach (string color in Colors)
            foreach (string track in Tracks)
            {
                int? expected = truth.GetValueOrDefault(color)?.GetValueOrDefault(track);
                FieldPrediction? raw = input.A.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                FieldPrediction? accepted = input.Confidence.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                decisions.Add(new(color, track, expected, raw?.Value, accepted, []));
            }
        ImageEvaluation evaluation = new(input.Name, input.Partition, input.View, input.Registration.Success, input.AError,
            frame.Alignment.Diagnostics, frame.EccMilliseconds, frame.RawFeatureMilliseconds, frame.EccFeatureMilliseconds, timings, decisions, fields);
        foreach (string method in methods.Where(method => method != "K3"))
            AddReadout(evaluation, input, method);
        if (methods.Contains("K3"))
            AddIntersection(evaluation, input);
        if (diagnostics is not null)
        {
            Directory.CreateDirectory(diagnostics);
            Write(Path.Combine(diagnostics, "decisions.json"), decisions);
            foreach (string path in Directory.GetFiles(diagnostics, "*.png"))
            {
                string[] parts = Path.GetFileNameWithoutExtension(path).Split('-');
                string method = parts[0];
                string track = parts[1];
                using Mat pixels = Cv2.ImRead(path);
                using Mat annotated = new(pixels.Height + 44, pixels.Width, MatType.CV_8UC3, Scalar.Black);
                using Mat destination = new(annotated, new Rect(0, 44, pixels.Width, pixels.Height));
                pixels.CopyTo(destination);
                string gt = "GT " + string.Join(" / ", decisions.Where(row => row.Track == track && row.Color is "black" or "blue")
                    .Select(row => $"{row.Color}:{row.Expected?.ToString() ?? "absent"}"));
                string emitted = method + " output " + string.Join(" / ", decisions.Where(row => row.Track == track && row.Color is "black" or "blue")
                    .Select(row => $"{row.Color}:{row.New[method].Value?.ToString() ?? "null"}"));
                Cv2.PutText(annotated, gt, new Point(3, 16), HersheyFonts.HersheySimplex, .38, Scalar.White);
                Cv2.PutText(annotated, emitted, new Point(3, 35), HersheyFonts.HersheySimplex, .38, Scalar.White);
                File.WriteAllBytes(path, annotated.ImEncode(".png"));
            }
        }
        Console.WriteLine($"{input.Partition}: {input.Name}; " + string.Join("; ", methods.Select(method =>
            $"{method}={evaluation.Decisions.Count(row => row.Expected.HasValue && row.New[method].Value == row.Expected)}/{evaluation.Decisions.Count(row => row.Expected.HasValue)}")));
        return evaluation;
    }

    private static void AddReadout(ImageEvaluation image, CachedImage input, string method)
    {
        List<(Detection Field, string Color, bool Strong)> colored = [];
        foreach (Detection field in image.Fields[method].Where(field => field.Candidate))
        {
            RankingObservation[] colors = input.Observations.Where(observation => observation.Track == field.Track && observation.Value == field.Value)
                .OrderByDescending(observation => observation.CoreColor).ToArray();
            if (colors.Length < 2 || colors[0].CoreColor < .06 || colors[0].CoreColor < 1.5 * Math.Max(.001, colors[1].CoreColor))
                continue;
            bool strong = field.Strong && colors[0].CoreColor >= .15 && colors[0].CoreColor >= 2 * Math.Max(.001, colors[1].CoreColor);
            colored.Add((field, colors[0].Color, strong));
        }
        foreach (Decision decision in image.Decisions)
        {
            Detection[] ranked = image.Fields[method].Where(field => field.Track == decision.Track && field.Candidate)
                .OrderByDescending(field => field.Score).ToArray();
            (Detection Field, string Color, bool Strong)[] matching = colored.Where(pair => pair.Color == decision.Color && pair.Field.Track == decision.Track)
                .OrderByDescending(pair => pair.Field.Score).ToArray();
            decision.New[method] = new(matching.FirstOrDefault().Field?.Value, matching.Length > 0 && matching[0].Strong,
                matching.Take(3).Select(pair => pair.Field.Value).ToArray(), ranked.Take(3).Select(field => field.Value).ToArray(),
                ranked.Select(field => field.Value).ToArray());
        }
    }

    private static void AddIntersection(ImageEvaluation image, CachedImage input)
    {
        HashSet<(string Track, int Value)> sparse = image.Fields["C3"].Where(field => field.Candidate).Select(field => (field.Track, field.Value)).ToHashSet();
        image.Fields["K3"] = image.Fields["C1"].Select(field => field with { Method = "K3", Candidate = field.Candidate && sparse.Contains((field.Track, field.Value)) }).ToList();
        image.Milliseconds["K3"] = image.Milliseconds["C1"] + image.Milliseconds["C3"];
        AddReadout(image, input, "K3");
    }

    private static Dictionary<string, Dictionary<string, int>?> Truth(string root, CachedImage input)
    {
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        using JsonDocument truth = JsonDocument.Parse(File.ReadAllText(input.Partition == "v3_reused"
            ? Path.Combine(root, "artifacts/ResearchV3/dataset_v3/ground_truth_v3.json")
            : Path.Combine(assets, "ConfidenceV2/scoring_ground_truth_v2.json")));
        JsonElement expected = input.Partition == "v3_reused"
            ? truth.RootElement.GetProperty("cases").GetProperty(input.Name.Split(new[] { "Gegenueber", "Links", "Rechts", "Senkrecht", "Vorne", "Unten" }, StringSplitOptions.None)[0])
            : truth.RootElement.GetProperty("photos").GetProperty(input.Name).GetProperty("expected");
        return Colors.ToDictionary(color => color, color => expected.TryGetProperty(color, out JsonElement player) && player.ValueKind != JsonValueKind.Null
            ? Tracks.ToDictionary(track => track, track => player.GetProperty(track).GetInt32()) : null);
    }

    private static object Report(List<ImageEvaluation> images, string[] methods, double initializationMs, string? ensemble = null) => new
    {
        validation = "All occupied datasets reused; old calibration selects combinations. No independent holdout claim.", initializationMs,
        peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64,
        baseline = new { total = images.SelectMany(image => image.Decisions).Count(row => row.Expected.HasValue),
            correct = images.SelectMany(image => image.Decisions).Count(row => row.Expected.HasValue && row.A == row.Expected),
            nullPresent = images.SelectMany(image => image.Decisions).Count(row => row.Expected.HasValue && row.A is null),
            errorImages = images.Count(image => image.AError is not null) },
        methods = methods.ToDictionary(method => method, method => new
        {
            summary = Metrics(images, method),
            byTrack = Tracks.ToDictionary(track => track, track => Metrics(images, method, track: track)),
            byColor = Colors.ToDictionary(color => color, color => Metrics(images, method, color: color)),
            byView = images.Select(image => image.View).Distinct().ToDictionary(view => view, view => Metrics(images.Where(image => image.View == view).ToList(), method)),
            averageMilliseconds = images.Average(image => image.Milliseconds[method])
        }),
        ensemble = ensemble is null ? null : EnsembleMetrics(images, ensemble), images
    };

    private static Summary Metrics(List<ImageEvaluation> images, string method, string? track = null, string? color = null)
    {
        int tp = 0, fp = 0, fn = 0;
        foreach (ImageEvaluation image in images)
            foreach (string selectedTrack in Tracks.Where(selectedTrack => track is null || track == selectedTrack))
            {
                HashSet<int> expected = image.Decisions.Where(row => row.Track == selectedTrack && row.Expected.HasValue)
                    .Select(row => row.Expected!.Value).Where(value => value != 0).ToHashSet();
                HashSet<int> predicted = image.Fields[method].Where(field => field.Track == selectedTrack && field.Candidate).Select(field => field.Value).ToHashSet();
                tp += expected.Intersect(predicted).Count();
                fp += predicted.Except(expected).Count();
                fn += expected.Except(predicted).Count();
            }
        Decision[] all = images.SelectMany(image => image.Decisions).Where(row => (track is null || row.Track == track) && (color is null || row.Color == color)).ToArray();
        Decision[] rows = all.Where(row => row.Expected.HasValue).ToArray();
        double precision = tp + fp > 0 ? (double)tp / (tp + fp) : 0;
        double recall = tp + fn > 0 ? (double)tp / (tp + fn) : 0;
        return new(tp, fp, fn, precision, recall, precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0,
            rows.Length, rows.Count(row => row.New[method].Value == row.Expected),
            rows.Count(row => row.New[method].Top3.Take(2).Contains(row.Expected!.Value)),
            rows.Count(row => row.New[method].Top3.Contains(row.Expected!.Value)), rows.Count(row => row.New[method].Value is null),
            rows.Count(row => row.A == row.Expected && row.New[method].Value == row.Expected),
            rows.Count(row => row.A == row.Expected && row.New[method].Value != row.Expected),
            rows.Count(row => row.A != row.Expected && row.New[method].Value == row.Expected),
            rows.Count(row => row.A != row.Expected && row.New[method].Value != row.Expected),
            rows.Count(row => row.A == row.Expected || row.New[method].Value == row.Expected),
            rows.Count(row => row.A != row.Expected && row.New[method].Occupied.Contains(row.Expected!.Value)),
            rows.Count(row => row.A == row.Expected || row.New[method].Occupied.Contains(row.Expected!.Value)),
            rows.Count(row => row.New[method].PhysicalTop3.Take(1).Contains(row.Expected!.Value)),
            rows.Count(row => row.New[method].PhysicalTop3.Take(2).Contains(row.Expected!.Value)),
            rows.Count(row => row.New[method].PhysicalTop3.Contains(row.Expected!.Value)),
            all.Count(row => !row.Expected.HasValue && row.New[method].Value.HasValue));
    }

    private static object EnsembleMetrics(List<ImageEvaluation> images, string method)
    {
        List<object> decisions = [];
        int correct = 0, total = 0, absent = 0, phantom = 0, missing = 0;
        foreach (ImageEvaluation image in images)
            foreach (Decision row in image.Decisions)
            {
                bool aSafe = row.Confidence?.Value.HasValue == true && row.Confidence.Confidence is "High" or "Medium";
                Readout x = row.New[method];
                int? value = aSafe && x.Strong && row.Confidence!.Value != x.Value ? null : aSafe ? row.Confidence!.Value : x.Strong ? x.Value : null;
                if (row.Expected.HasValue)
                {
                    total++;
                    correct += value == row.Expected ? 1 : 0;
                    missing += value is null ? 1 : 0;
                }
                else
                {
                    absent++;
                    phantom += value.HasValue ? 1 : 0;
                }
                decisions.Add(new { image.Name, row.Color, row.Track, row.Expected, value, aSafe, x.Strong });
            }
        return new { method = "K4_A+" + method, correct, total, missing, absent, phantom, decisions };
    }

    private static Dictionary<string, string> InputHashes(string root, string output, string cache)
    {
        IEnumerable<string> files = Directory.GetFiles(Path.Combine(root, ToolPath), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(root, ToolPath), "*.csproj"))
            .Concat(Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition"), "*.cs"))
            .Concat(Directory.GetFiles(cache))
            .Concat(Read<List<CachedImage>>(Path.Combine(cache, "images.json")).Select(image => Path.Combine(root, image.Source)))
            .Concat(new[] { "EXPERIMENT_PLAN.md", "model.json", "selection.json", "calibration-independent.json", "calibration.json" }.Select(name => Path.Combine(output, name)))
            .Concat(new[] { "Directory.Packages.props", "Directory.Build.props", "global.json", "tools/Games/ArkNova/PhotoRecognition/Assets/scoring_regions_v2.json",
                "tools/Games/ArkNova/PhotoRecognition/Assets/board_reference_master_clean.png",
                "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/scoring_ground_truth_v2.json", "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/split.json",
                "artifacts/ResearchV3/dataset_v3/ground_truth_v3.json", "artifacts/ResearchV3/dataset_v3/manifest_v3.json",
                "src/BoardgameTracker.Application/Games/ArkNova/GameSessions/Scoring/ArkNovaFieldOccupancy.cs" }.Select(path => Path.Combine(root, path)));
        return files.Distinct().Order().ToDictionary(path => Path.GetRelativePath(root, path).Replace('\\', '/'), Hash);
    }

    private static int SelfTest(string assets)
    {
        using Mat gray = new(40, 40, MatType.CV_8UC1);
        using Mat shifted = new(40, 40, MatType.CV_8UC1);
        for (int y = 0; y < 40; y++)
            for (int x = 0; x < 40; x++)
            {
                byte value = (byte)(20 + (x * 3 + y * 5) % 140);
                gray.Set(y, x, value);
                shifted.Set(y, x, (byte)(value + 30));
            }
        if (!Census(gray).SequenceEqual(Census(shifted)))
            throw new InvalidDataException("Census must preserve monotone brightness shifts.");
        LowRankModel constant = new(Enumerable.Repeat(.5, 256).ToArray(), []);
        double[] query = Enumerable.Repeat(.5, 256).ToArray();
        query[42] = .9;
        float[] sparse = Sparse(query, constant);
        if (Math.Abs(sparse[42] - .36) > 1e-6 || sparse.Count(value => value > 0) != 1)
            throw new InvalidDataException("Sparse projection must localize a new occlusion.");
        if (Supported(0, 1, 1, .1) || Supported(1, .96, 1, .1) || Supported(1, 1, 0, .1))
            throw new InvalidDataException("Unsupported zero/invisible/empty fields cannot snap.");
        using ClassicDetectors detector = new(assets);
        int index = detector.Regions.FindIndex(region => region.Track == "reputation" && region.Value == 8);
        Region region = detector.Regions[index];
        float[] outside = new float[region.Patch.Width * region.Patch.Height];
        for (int y = 0; y < region.Patch.Height; y++)
            for (int x = 0; x < region.Patch.Width; x++)
                if (region.Mask.At<byte>(y, x) == 0)
                    outside[y * region.Patch.Width + x] = 1;
        Detection none = detector.ChangeScore("C2a", index, outside, 1, new(.2, .06));
        if (none.Candidate || none.Score != 0)
            throw new InvalidDataException("Even a large outside anomaly cannot contribute occupancy.");
        using Mat synthetic = new(1400, 2000, MatType.CV_8UC1, new Scalar(110));
        int markerSize = (int)Math.Round(region.Bounds.Height * .72);
        Point center = new(region.Center.X + region.Patch.X, region.Center.Y + region.Patch.Y);
        Cv2.Rectangle(synthetic, new Rect(center.X - markerSize / 2, center.Y - markerSize / 2, markerSize, markerSize), new Scalar(20), -1);
        Detection shape = detector.Shape(synthetic, index, 1, new(0, .65));
        using Mat blankShape = new(1400, 2000, MatType.CV_8UC1, new Scalar(110));
        Detection blank = detector.Shape(blankShape, index, 1, new(0, .65));
        // Chamfer scores are raster distances, not probabilities. Test localization
        // and separation from an edge-free negative instead of a probability cutoff.
        if (!shape.Candidate || blank.Candidate || shape.Score < 10 * blank.Score || Math.Abs(shape.CenterX!.Value - center.X) > 6 || Math.Abs(shape.CenterY!.Value - center.Y) > 6)
            throw new InvalidDataException("Oriented Chamfer must localize a known physical rectangular marker: " + JsonSerializer.Serialize(new { shape, center, markerSize }, Json));
        using Mat master = Cv2.ImRead(Path.Combine(assets, "board_reference_master_clean.png"));
        using Mat resized = new();
        Cv2.Resize(master, resized, new Size(2000, 1400));
        using Mat masterGray = ClassicDetectors.Gray(resized);
        using Mat translation = Mat.Eye(2, 3, MatType.CV_32FC1);
        translation.Set(0, 2, 2f);
        translation.Set(1, 2, -1f);
        using Mat translated = new();
        Cv2.WarpAffine(masterGray, translated, translation, masterGray.Size(), borderMode: BorderTypes.Reflect101);
        using AlignedTracks aligned = detector.AlignTracks(translated);
        if (aligned.Diagnostics.Count(info => info.Applied && Math.Abs(info.Dx - 2) < .5 && Math.Abs(info.Dy + 1) < .5) < 2)
            throw new InvalidDataException("Track ECC must recover a known bounded translation.");
        Console.WriteLine("6 research checks passed: Census order, sparse localization, unsupported candidates, outside contact exclusion, shape localization, known ECC translation.");
        return 0;
    }

    private static double Quantile(IEnumerable<double> source, double probability)
    {
        double[] values = source.Order().ToArray();
        return values.Length > 0 ? values[(int)((values.Length - 1) * probability)] : throw new InvalidDataException("No normal fit samples.");
    }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)!;
    private static void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json));

    internal sealed record ImageInput(string Name, string Path, string Partition, string View);
    internal sealed record CachedImage(string Name, string Id, string Partition, string View, string Source, string Sha256,
        RegistrationDiagnostics Registration, Dictionary<string, ColorScores> A, Dictionary<string, ColorScores> Confidence,
        RankingObservation[] Observations, string? AError);
    internal sealed record Frame(Mat Gray, Mat Validity, AlignedTracks Alignment, Features Raw, Features Ecc, double EccMilliseconds,
        double RawFeatureMilliseconds, double EccFeatureMilliseconds) : IDisposable
    {
        public void Dispose() { Gray.Dispose(); Validity.Dispose(); Alignment.Dispose(); }
    }
    internal sealed record Model(Dictionary<string, Dictionary<string, Limits>> Limits, LowRankModel[] Subspaces, double FitMilliseconds);
    internal sealed record Selection(string[] Methods, bool K1, bool K2, bool K3, string? EnsembleMethod, double CalibrationEccGain, string Discipline);
    internal sealed record Readout(int? Value, bool Strong, int[] Top3, int[] PhysicalTop3, int[] Occupied);
    internal sealed record Decision(string Color, string Track, int? Expected, int? A, FieldPrediction? Confidence, Dictionary<string, Readout> New);
    internal sealed record ImageEvaluation(string Name, string Partition, string View, bool Registered, string? AError, EccInfo[] Ecc,
        double EccMilliseconds, double RawFeatureMilliseconds, double EccFeatureMilliseconds,
        Dictionary<string, double> Milliseconds, List<Decision> Decisions, Dictionary<string, List<Detection>> Fields);
    internal sealed record Summary(int TP, int FP, int FN, double Precision, double Recall, double F1, int Total, int Top1, int Top2, int Top3, int Abstention,
        int ARightReadoutRight, int ARightReadoutWrong, int AWrongReadoutRight, int AWrongReadoutWrong, int ReadoutOracle,
        int AWrongContactRight, int ContactOracle, int PhysicalTop1, int PhysicalTop2, int PhysicalTop3, int AbsentReadoutPhantoms);
}

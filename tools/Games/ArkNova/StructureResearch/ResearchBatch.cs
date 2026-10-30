using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using OpenCvSharp;
using static BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.StructureDetector;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal static class ResearchBatch
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] Tracks = ["attractiveness", "conservation", "reputation"];
    private static readonly string[] Colors = ["red", "blue", "yellow", "black"];

    public static int Run(string command, string root, string dataset, string output)
    {
        Directory.CreateDirectory(output);
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        if (command == "self-test")
            return SelfTest();
        if (command == "recovery-freeze")
        {
            Freeze original = Read<Freeze>(Path.Combine(output, "freeze.json"));
            string[] plumbing = ["tools/Games/ArkNova/PhotoRecognition/ArkNovaRankingCapture.cs",
                "tools/Games/ArkNova/PhotoRecognition/ArkNovaBlackDiagnostics.cs",
                "tools/Games/ArkNova/StructureResearch/ResearchBatch.cs", "tools/Games/ArkNova/StructureResearch/Program.cs"];
            foreach ((string path, string expectedHash) in original.Hashes)
                if (!plumbing.Contains(path) && Hash(Path.Combine(root, path)) != expectedHash)
                    throw new InvalidDataException($"Recovery may not change frozen algorithms, config or data: {path}");
            if (!File.Exists(Path.Combine(output, "holdout-started.json")) || File.Exists(Path.Combine(output, "holdout.json"))
                || File.Exists(Path.Combine(output, "recovery-freeze.json")))
                throw new InvalidOperationException("Recovery requires exactly one interrupted run, without a completed report or existing recovery.");
            Write(Path.Combine(output, "recovery-freeze.json"), original with
            {
                FrozenUtc = DateTimeOffset.UtcNow, Hashes = Hashes(root, dataset, output),
                ParentFreezeHash = Hash(Path.Combine(output, "freeze.json")),
                Discipline = original.Discipline + " One failed attempt stopped after 10 images at Fall3Senkrecht (A fraction validation). Recovery changes observer plumbing/error accounting only; A failures count as misses. Parameters and detector source remain identical. Repeated images reproduce the same fixed experiment, not a second validation."
            });
            Console.WriteLine("Recovery freeze: every original non-plumbing hash verified unchanged.");
            return 0;
        }
        if (command == "freeze")
        {
            if (File.Exists(Path.Combine(output, "freeze.json")))
                throw new InvalidOperationException("Freeze already exists; no overwrite allowed.");
            Model model = Read<Model>(Path.Combine(output, "model.json"));
            Write(Path.Combine(output, "freeze.json"), new Freeze(DateTimeOffset.UtcNow, model.Methods, Hashes(root, dataset, output),
                "B2 unavailable: no DINOv3 checkpoint/runtime supplied in repository or authorized archive. No external model access authorized.",
                "Empty-only thresholds; old V2 calibration only for method comparison. No V3 occupied metrics or predictions before freeze. GT schema/integrity prefix inspected, not used for selection."));
            Console.WriteLine("Frozen sources, config, normal caches, all dataset bytes and calibration reports.");
            return 0;
        }
        Freeze? freeze = null;
        bool isHoldout = command is "holdout" or "holdout-recovery";
        string freezePath = Path.Combine(output, command == "holdout-recovery" ? "recovery-freeze.json" : "freeze.json");
        if (isHoldout)
        {
            freeze = Read<Freeze>(freezePath);
            VerifyFreeze(freeze, root, dataset, output);
            // CreateNew is a durable one-run latch, before loading occupied GT.
            using FileStream latch = new(Path.Combine(output, command == "holdout-recovery" ? "holdout-recovery-started.json" : "holdout-started.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(latch, new { startedUtc = DateTimeOffset.UtcNow, freezeSha256 = Hash(freezePath) }, Json);
        }
        else if (File.Exists(Path.Combine(output, "freeze.json")))
            throw new InvalidOperationException("Cannot fit after freeze.");

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataset, "manifest_v3.json")));
        JsonElement[] images = manifest.RootElement.GetProperty("images").EnumerateArray().ToArray();
        foreach (JsonElement image in images)
        {
            string name = image.GetProperty("file").GetString()!;
            if (Hash(Path.Combine(dataset, "images", name)) != image.GetProperty("scaled_sha256").GetString())
                throw new InvalidDataException($"Image hash mismatch: {name}");
        }
        Stopwatch initialization = Stopwatch.StartNew();
        using StructureDetector detector = new(assets);
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        if (command == "fit-b1")
        {
            string cache = Path.Combine(output, "Normals");
            Directory.CreateDirectory(cache);
            foreach (JsonElement image in images.Where(image => image.GetProperty("kind").GetString() == "empty"))
            {
                string name = image.GetProperty("file").GetString()!;
                string view = image.GetProperty("view").GetString()!;
                string path = Path.Combine(cache, name + ".png");
                FeaturePatch[]? features = null;
                ArkNovaBlackDiagnostics observer = new("", frame =>
                {
                    features = detector.Extract(frame.Warped, frame.Validity);
                    File.WriteAllBytes(path, frame.Warped.ImEncode(".png"));
                    File.WriteAllBytes(path + ".valid.png", frame.Validity.ImEncode(".png"));
                });
                RecognitionResult result = analyzer.Analyze(Path.Combine(dataset, "images", name), observer);
                if (!result.Registration.Success || features is null)
                    throw new InvalidDataException($"Normal registration failed: {name}");
                detector.Normals.Add(new(name, view, path, features));
                Console.WriteLine($"normal: {name}; {result.Timings.TotalMilliseconds:F0} ms");
            }
            Write(Path.Combine(output, "normals.json"), detector.Normals.Select(normal => new NormalCache(normal.Name, normal.View, Path.GetRelativePath(output, normal.CachePath))).ToArray());
        }
        else
            LoadNormals(detector, output);
        double initializationMs = initialization.Elapsed.TotalMilliseconds;
        Model config = command == "fit-b1" ? new([Pooled, ViewMatched], [], null) : Read<Model>(Path.Combine(output, "model.json"));
        if (command is "fit-b1" or "fit-b3")
        {
            string[] methods = command == "fit-b1" ? [Pooled, ViewMatched] : [Padim];
            if (command == "fit-b3")
            {
                using JsonDocument prior = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "b1-calibration.json")));
                if (prior.RootElement.GetProperty("b3JustifiedByCalibration").GetBoolean() != true)
                    throw new InvalidOperationException("B3 requires complementary B1 occupancy evidence on old calibration.");
            }
            foreach (string method in methods)
                config.Limits[method] = FitLimits(detector, method, output);
            if (command == "fit-b3")
                config = config with { Methods = config.Methods.Concat([Padim]).ToArray() };
            Write(Path.Combine(output, "model.json"), config);
            string v2 = Path.Combine(assets, "ConfidenceV2");
            using JsonDocument split = JsonDocument.Parse(File.ReadAllText(Path.Combine(v2, "split.json")));
            using JsonDocument truth = JsonDocument.Parse(File.ReadAllText(Path.Combine(v2, "scoring_ground_truth_v2.json")));
            List<EvaluatedImage> calibration = [];
            foreach (JsonElement nameElement in split.RootElement.GetProperty("calibration").EnumerateArray())
            {
                string name = nameElement.GetString()!;
                calibration.Add(Evaluate(analyzer, detector, Path.Combine(v2, "images", name), name, "unknown_v2", "old_calibration",
                    truth.RootElement.GetProperty("photos").GetProperty(name).GetProperty("expected"), config, methods, null));
            }
            bool justified = methods.Any(method => Complementarity(calibration, method).AWrongBContactRight > 0
                && Occupancy(calibration, method).Recall > .25);
            Write(Path.Combine(output, command == "fit-b1" ? "b1-calibration.json" : "b3-calibration.json"), new
            {
                split = "old V2 calibration only, 17 photos; no old holdout for method selection", initializationMs,
                b3JustifiedByCalibration = justified, methods = Summaries(calibration, methods), images = calibration
            });
            if (command == "fit-b1")
            {
                string selected = methods.OrderByDescending(method => Occupancy(calibration, method).F1).First();
                config = config with { SelectedB1 = selected };
                Write(Path.Combine(output, "model.json"), config);
                Console.WriteLine($"Selected B1 from old calibration occupancy F1: {selected}; B3 justified={justified}");
            }
            return 0;
        }

        // This is the first occupied-GT metric access for V3, after the latch/freeze.
        using JsonDocument v3truth = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataset, "ground_truth_v3.json")));
        List<EvaluatedImage> holdout = [];
        foreach (JsonElement image in images.Where(image => image.GetProperty("kind").GetString() == "occupied"))
        {
            string name = image.GetProperty("file").GetString()!;
            string view = image.GetProperty("view").GetString()!;
            string caseName = "Fall" + image.GetProperty("case").GetInt32();
            string? diagnostics = name is "Fall1Senkrecht.jpg" or "Fall1Vorne.jpg" or "Fall2Senkrecht.jpg"
                or "Fall3Senkrecht.jpg" or "Fall4Senkrecht.jpg" or "Fall5Senkrecht.jpg"
                ? Path.Combine(output, "Diagnostics", Path.GetFileNameWithoutExtension(name)) : null;
            holdout.Add(Evaluate(analyzer, detector, Path.Combine(dataset, "images", name), name, view, caseName,
                v3truth.RootElement.GetProperty("cases").GetProperty(caseName), config, config.Methods, diagnostics));
        }
        VerifyFreeze(freeze!, root, dataset, output);
        Write(Path.Combine(output, "holdout.json"), new
        {
            freezeSha256 = Hash(freezePath), initializationMs,
            peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64,
            semantics = "A raw selected ranking (post no-zero abstention); A confidence separately. B is a color-free track ranking, repeated unchanged for each present GT color. Contact oracle is an occupancy-set bound, not a color prediction or implementable ensemble.",
            methods = Summaries(holdout, config.Methods), images = holdout
        });
        Console.WriteLine("Exactly one V3 holdout completed; no refitting permitted.");
        return 0;
    }

    private static Dictionary<string, Limits> FitLimits(StructureDetector detector, string method, string output)
    {
        Stopwatch timer = Stopwatch.StartNew();
        Dictionary<string, List<double>> residuals = Tracks.ToDictionary(track => track, _ => new List<double>());
        Dictionary<string, List<double>> gradients = Tracks.ToDictionary(track => track, _ => new List<double>());
        foreach (NormalFrame normal in detector.Normals)
            for (int index = 0; index < detector.Regions.Count; index++)
            {
                ScoreRegion region = detector.Regions[index];
                if (normal.Features[index].Visibility < .97)
                    continue;
                using ChangeMaps maps = detector.Compare(normal.Features[index], index, method, normal.View, normal.Name);
                // Fixed spatial subsample; no occupied input in quantile estimation.
                for (int pixel = 0; pixel < normal.Features[index].Light.Length; pixel += 7)
                {
                    int row = pixel / region.Patch.Width;
                    int col = pixel % region.Patch.Width;
                    if (region.Mask.At<byte>(row, col) == 0)
                        continue;
                    residuals[region.Track].Add(maps.Residual.At<float>(row, col));
                    gradients[region.Track].Add(maps.Gradient.At<float>(row, col));
                }
            }
        Dictionary<string, Limits> thresholds = Tracks.ToDictionary(track => track, track => new Limits(
            Math.Max(method == Padim ? 2.5 : .45, Quantile(residuals[track], .99)),
            method == Padim ? 0 : Math.Max(.25, Quantile(gradients[track], .99)), .06));
        List<object> loo = [];
        Dictionary<string, List<double>> scores = Tracks.ToDictionary(track => track, _ => new List<double>());
        foreach (NormalFrame normal in detector.Normals)
        {
            List<FieldChange> changes = [];
            for (int index = 0; index < detector.Regions.Count; index++)
            {
                FieldChange change = detector.Score(normal.Features[index], index, method, normal.View,
                    thresholds[detector.Regions[index].Track], normal.Name);
                changes.Add(change);
                if (change.Visibility >= .97 && change.Value != 0)
                    scores[change.Track].Add(change.Score);
            }
            loo.Add(new { normal.Name, normal.View, changes });
        }
        foreach (string track in Tracks)
            thresholds[track] = thresholds[track] with { Occupancy = Math.Max(.06, Quantile(scores[track], .995)) };
        Write(Path.Combine(output, method + "-empty-loo.json"), new
        {
            description = "Leave-one-image-out residuals, pooled 99th percentile pixel limits and 99.5th percentile contact score per track; floors fixed before holdout. Same samples estimate quantiles and residual counts: normal-fit diagnostic, not independent specificity validation.",
            thresholds, fitMilliseconds = timer.Elapsed.TotalMilliseconds, images = loo
        });
        Console.WriteLine($"fit {method}: {timer.Elapsed.TotalSeconds:F1}s; {JsonSerializer.Serialize(thresholds)}");
        return thresholds;
    }

    private static EvaluatedImage Evaluate(ArkNovaPhotoAnalyzer analyzer, StructureDetector detector, string path, string name,
        string view, string caseName, JsonElement expected, Model config, string[] methods, string? diagnostics)
    {
        FeaturePatch[]? features = null;
        using Mat warped = new();
        double extractionMs = 0;
        Action<ArkNovaBlackDiagnostics.Frame> extract = frame =>
        {
            Stopwatch extraction = Stopwatch.StartNew();
            features = detector.Extract(frame.Warped, frame.Validity);
            extractionMs = extraction.Elapsed.TotalMilliseconds;
            if (diagnostics is not null)
                frame.Warped.CopyTo(warped);
        };
        ArkNovaBlackDiagnostics observer = new("", _ => { });
        ArkNovaRankingCapture capture = new() { FrameObserver = extract };
        Stopwatch aTimer = Stopwatch.StartNew();
        RecognitionResult result;
        string? aError = null;
        try
        {
            result = analyzer.Analyze(path, observer, capture);
        }
        catch (ArgumentOutOfRangeException error) when (error.ParamName == "value" && error.Message.StartsWith("Evidence fractions must be finite", StringComparison.Ordinal))
        {
            // Preserve A's failure, do not clamp or salvage its scores. The B
            // observer already copied the same registered frame before scoring.
            aError = error.Message;
            result = new(observer.Registration ?? throw new InvalidDataException("A failed before registration was observed."), [],
                new(0, 0, 0, aTimer.Elapsed.TotalMilliseconds));
        }
        Dictionary<string, ColorScores> a = aError is null
            ? ArkNovaRankingRules.Predict(capture.Observations, ArkNovaRankingRules.Selected) : [];
        Dictionary<string, ColorScores> confidence = a.ToDictionary(pair => pair.Key,
            pair => new ColorScores(ArkNovaConfidencePolicy.Evaluate(pair.Value.Tracks, ArkNovaConfidencePolicy.Selected)));
        Dictionary<string, List<FieldChange>> fields = [];
        Dictionary<string, double> milliseconds = [];
        foreach (string method in methods)
        {
            Stopwatch timer = Stopwatch.StartNew();
            List<FieldChange> changes = [];
            if (features is not null)
                for (int index = 0; index < detector.Regions.Count; index++)
                    changes.Add(detector.Score(features[index], index, method, view, config.Limits[method][detector.Regions[index].Track]));
            fields[method] = changes;
            milliseconds[method] = timer.Elapsed.TotalMilliseconds;
        }
        List<Decision> decisions = [];
        foreach (string color in Colors)
            foreach (string track in Tracks)
            {
                int? value = expected.TryGetProperty(color, out JsonElement player) && player.ValueKind != JsonValueKind.Null
                    ? player.GetProperty(track).GetInt32() : null;
                RankingResult rank = aError is null
                    ? ArkNovaRankingRules.Rank(capture.Observations.Where(field => field.Color == color && field.Track == track), ArkNovaRankingRules.Selected)
                    : new(null, "a_pipeline_exception", [], 0);
                Dictionary<string, BDecision> b = methods.ToDictionary(method => method, method =>
                {
                    FieldChange[] candidates = fields[method].Where(field => field.Track == track && field.Candidate)
                        .OrderByDescending(field => field.Score).ThenBy(field => field.Value).ToArray();
                    return new BDecision(candidates.FirstOrDefault()?.Value, candidates.Take(3).Select(field => new Candidate(field.Value, field.Score)).ToArray(),
                        candidates.Select(field => field.Value).ToArray());
                });
                decisions.Add(new(color, track, value, rank.Value, rank.Abstention,
                    rank.Fields.Take(3).Select(field => new Candidate(field.Observation.Value, field.Score)).ToArray(),
                    confidence.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track), b));
            }
        if (diagnostics is not null && features is not null)
        {
            Directory.CreateDirectory(diagnostics);
            File.WriteAllBytes(Path.Combine(diagnostics, "registered.png"), warped.ImEncode(".png"));
            foreach (string track in Tracks)
            {
                // Fixed diagnostic budget: black and blue GT fields, plus strongest
                // context-only anomaly in reputation. GT only chooses exports.
                IEnumerable<int> targets = decisions.Where(row => row.Track == track && row.Color is "black" or "blue" && row.Expected.HasValue)
                    .Select(row => row.Expected!.Value);
                if (track == "reputation")
                    targets = targets.Concat(fields[config.SelectedB1!].Where(field => field.Track == track)
                        .OrderByDescending(field => field.IgnoredOutsidePixels).Take(1).Select(field => field.Value));
                foreach (int target in targets.Distinct())
                {
                    int index = detector.Regions.FindIndex(region => region.Track == track && region.Value == target);
                    foreach (string method in methods.Where(method => method == config.SelectedB1 || method == Padim))
                        detector.Score(features[index], index, method, view, config.Limits[method][track], diagnostic:
                            Path.Combine(diagnostics, $"{track}-{target}-{method}.png"), warped: warped);
                }
            }
        }
        object[] invalidFractions = capture.Observations.Where(field => field.FullColor > 1 || field.CoreColor > 1 || field.FullDifference > 1 || field.CoreDifference > 1)
            .Select(field => (object)new { field.Color, field.Track, field.Value, field.FullColor, field.CoreColor, field.FullDifference, field.CoreDifference }).ToArray();
        EvaluatedImage evaluated = new(name, view, caseName, result.Registration, result.Timings, extractionMs, milliseconds, decisions, fields, aError, invalidFractions);
        Console.WriteLine($"{caseName}: {name}; reg={result.Registration.Success}; A={decisions.Count(row => row.Expected.HasValue && row.A == row.Expected)}/{decisions.Count(row => row.Expected.HasValue)}");
        return evaluated;
    }

    private static object Summaries(List<EvaluatedImage> images, string[] methods) => new
    {
        a = new
        {
            summary = AMetrics(images.SelectMany(image => image.Decisions)),
            byTrack = Tracks.ToDictionary(track => track, track => AMetrics(images.SelectMany(image => image.Decisions).Where(row => row.Track == track))),
            byColor = Colors.ToDictionary(color => color, color => AMetrics(images.SelectMany(image => image.Decisions).Where(row => row.Color == color))),
            byView = images.GroupBy(image => image.View).ToDictionary(group => group.Key, group => AMetrics(group.SelectMany(image => image.Decisions))),
            byCaseView = images.ToDictionary(image => image.Name, image => AMetrics(image.Decisions))
        },
        b = methods.ToDictionary(method => method, method => new
        {
            summary = BMetrics(images.SelectMany(image => image.Decisions), method), occupancy = Occupancy(images, method),
            complementarity = Complementarity(images, method),
            byTrack = Tracks.ToDictionary(track => track, track => BMetrics(images.SelectMany(image => image.Decisions).Where(row => row.Track == track), method)),
            byColor = Colors.ToDictionary(color => color, color => BMetrics(images.SelectMany(image => image.Decisions).Where(row => row.Color == color), method)),
            byView = images.GroupBy(image => image.View).ToDictionary(group => group.Key, group => new
            {
                metrics = BMetrics(group.SelectMany(image => image.Decisions), method), occupancy = Occupancy(group.ToList(), method),
                complementarity = Complementarity(group.ToList(), method)
            }),
            byCaseView = images.ToDictionary(image => image.Name, image => new { metrics = BMetrics(image.Decisions, method), occupancy = Occupancy([image], method) }),
            averageImageMilliseconds = images.Average(image => image.MethodMilliseconds[method]),
            averageFieldMilliseconds = images.Average(image => image.MethodMilliseconds[method]) / 171
        })
    };

    private static object AMetrics(IEnumerable<Decision> source)
    {
        Decision[] all = source.ToArray();
        Decision[] present = all.Where(row => row.Expected.HasValue).ToArray();
        Decision[] accepted = all.Where(row => row.Confidence?.Value.HasValue == true && row.Confidence.Confidence is "High" or "Medium").ToArray();
        return new
        {
            total = present.Length, top1 = present.Count(row => row.A == row.Expected),
            top2 = present.Count(row => row.ATop3.Take(2).Any(field => field.Value == row.Expected)),
            top3 = present.Count(row => row.ATop3.Any(field => field.Value == row.Expected)),
            abstention = present.Count(row => row.A is null),
            confidenceNullPresent = present.Count(row => row.Confidence?.Value is null),
            accepted = accepted.Length, acceptedCorrect = accepted.Count(row => row.Confidence!.Value == row.Expected),
            absent = all.Count(row => !row.Expected.HasValue), phantomRaw = all.Count(row => !row.Expected.HasValue && row.A.HasValue),
            phantomAfterConfidence = all.Count(row => !row.Expected.HasValue && row.Confidence?.Value.HasValue == true)
        };
    }

    private static object BMetrics(IEnumerable<Decision> source, string method)
    {
        Decision[] rows = source.Where(row => row.Expected.HasValue).ToArray();
        return new
        {
            total = rows.Length, top1 = rows.Count(row => row.B[method].Value == row.Expected),
            top2 = rows.Count(row => row.B[method].Top3.Take(2).Any(field => field.Value == row.Expected)),
            top3 = rows.Count(row => row.B[method].Top3.Any(field => field.Value == row.Expected)),
            abstention = rows.Count(row => row.B[method].Value is null),
            contactSetContainsTruth = rows.Count(row => row.B[method].Occupied.Contains(row.Expected!.Value))
        };
    }

    private static Complement Complementarity(List<EvaluatedImage> images, string method)
    {
        Decision[] rows = images.SelectMany(image => image.Decisions).Where(row => row.Expected.HasValue).ToArray();
        bool BCorrect(Decision row) => row.B[method].Value == row.Expected;
        return new(rows.Count(row => row.A == row.Expected && BCorrect(row)), rows.Count(row => row.A == row.Expected && !BCorrect(row)),
            rows.Count(row => row.A != row.Expected && BCorrect(row)), rows.Count(row => row.A != row.Expected && !BCorrect(row)),
            rows.Count(row => row.A == row.Expected || BCorrect(row)),
            rows.Count(row => row.A != row.Expected && row.B[method].Occupied.Contains(row.Expected!.Value)),
            rows.Count(row => row.A == row.Expected || row.B[method].Occupied.Contains(row.Expected!.Value)));
    }

    private static OccupancyMetrics Occupancy(List<EvaluatedImage> images, string method)
    {
        int tp = 0;
        int fp = 0;
        int fn = 0;
        int zero = 0;
        foreach (EvaluatedImage image in images)
            foreach (string track in Tracks)
            {
                HashSet<int> expected = image.Decisions.Where(row => row.Track == track && row.Expected.HasValue).Select(row => row.Expected!.Value).ToHashSet();
                zero += expected.Remove(0) ? 1 : 0;
                HashSet<int> predicted = image.Fields[method].Where(field => field.Track == track && field.Candidate).Select(field => field.Value).ToHashSet();
                tp += predicted.Intersect(expected).Count();
                fp += predicted.Except(expected).Count();
                fn += expected.Except(predicted).Count();
            }
        double precision = tp + fp > 0 ? (double)tp / (tp + fp) : 0;
        double recall = tp + fn > 0 ? (double)tp / (tp + fn) : 0;
        return new(tp, fp, fn, zero, precision, recall, precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0);
    }

    private static void LoadNormals(StructureDetector detector, string output)
    {
        foreach (NormalCache normal in Read<NormalCache[]>(Path.Combine(output, "normals.json")))
        {
            string path = Path.Combine(output, normal.Path);
            using Mat warped = Cv2.ImRead(path);
            using Mat validity = Cv2.ImRead(path + ".valid.png", ImreadModes.Grayscale);
            detector.Normals.Add(new(normal.Name, normal.View, path, detector.Extract(warped, validity)));
        }
    }

    private static Dictionary<string, string> Hashes(string root, string dataset, string output)
    {
        IEnumerable<string> files = Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition"), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/StructureResearch"), "*.cs"))
            .Concat(Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/StructureResearch"), "*.csproj"))
            .Concat(Directory.GetFiles(dataset, "*", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Path.Combine(output, "Normals")))
            .Concat(Directory.GetFiles(output, "*-calibration.json"))
            .Concat(Directory.GetFiles(output, "*-empty-loo.json"))
            .Concat(Directory.GetFiles(Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/images")))
            .Concat(new[] { Path.Combine(output, "model.json"), Path.Combine(output, "normals.json"),
                Path.Combine(output, "input-provenance.json"),
                Path.Combine(root, "Directory.Packages.props"), Path.Combine(root, "Directory.Build.props"), Path.Combine(root, "global.json"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/scoring_regions_v2.json"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/board_reference_master_clean.png"),
                Path.Combine(root, "src/BoardgameTracker.Application/Games/ArkNova/GameSessions/Scoring/ArkNovaFieldOccupancy.cs"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/split.json"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/scoring_ground_truth_v2.json"),
                Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets/ConfidenceV2/manifest.json") });
        return files.Distinct().Order().ToDictionary(path => Path.GetRelativePath(root, path).Replace('\\', '/'), Hash);
    }

    private static void VerifyFreeze(Freeze freeze, string root, string dataset, string output)
    {
        Dictionary<string, string> current = Hashes(root, dataset, output);
        if (current.Count != freeze.Hashes.Count || freeze.Hashes.Any(pair => current.GetValueOrDefault(pair.Key) != pair.Value))
            throw new InvalidDataException("Frozen source/config/input changed. Evaluation refused.");
    }

    private static int SelfTest()
    {
        using Mat polygon = Mat.Zeros(60, 60, MatType.CV_8UC1);
        using Mat core = Mat.Zeros(60, 60, MatType.CV_8UC1);
        Cv2.Rectangle(polygon, new Rect(20, 20, 20, 20), Scalar.White, -1);
        Cv2.Rectangle(core, new Rect(23, 23, 14, 14), Scalar.White, -1);
        using Mat outside = Mat.Zeros(60, 60, MatType.CV_8UC1);
        Cv2.Rectangle(outside, new Rect(0, 0, 60, 18), Scalar.White, -1);
        using ContactResult ignored = StructureDetector.Measure(outside, polygon, core);
        if (ignored.Score != 0 || ignored.Components != 0 || ignored.IgnoredOutsidePixels != 1080)
            throw new InvalidDataException("Large adjacent object must not produce occupancy.");
        Cv2.Rectangle(outside, new Rect(24, 24, 10, 10), Scalar.White, -1);
        using ContactResult touched = StructureDetector.Measure(outside, polygon, core);
        if (touched.Score <= .1 || touched.Components != 1)
            throw new InvalidDataException("Compact contacting component must contribute.");
        using Mat line = Mat.Zeros(60, 60, MatType.CV_8UC1);
        Cv2.Line(line, new Point(24, 24), new Point(34, 24), Scalar.White);
        using ContactResult thin = StructureDetector.Measure(line, polygon, core);
        if (thin.Score != 0)
            throw new InvalidDataException("Thin print-like edge must not produce occupancy.");
        if (StructureDetector.SupportsCandidate(0, 1, 1, .06))
            throw new InvalidDataException("Score zero must remain diagnostic only.");
        if (StructureDetector.SupportsCandidate(1, 1, 0, .06) || StructureDetector.SupportsCandidate(1, .96, 1, .06))
            throw new InvalidDataException("Excluding zero cannot support an empty or invisible replacement field.");
        Console.WriteLine("5 research checks passed: outside/contact/thin line/no CV zero/no unsupported replacement.");
        return 0;
    }

    private static double Quantile(List<double> values, double fraction)
    {
        values.Sort();
        return values.Count > 0 ? values[(int)((values.Count - 1) * fraction)] : throw new InvalidDataException("No normal measurements.");
    }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)!;
    private static void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json));

    internal sealed record Model(string[] Methods, Dictionary<string, Dictionary<string, Limits>> Limits, string? SelectedB1);
    internal sealed record NormalCache(string Name, string View, string Path);
    internal sealed record Freeze(DateTimeOffset FrozenUtc, string[] Methods, Dictionary<string, string> Hashes, string B2Status, string Discipline, string? ParentFreezeHash = null);
    internal sealed record Candidate(int Value, double Score);
    internal sealed record BDecision(int? Value, Candidate[] Top3, int[] Occupied);
    internal sealed record Decision(string Color, string Track, int? Expected, int? A, string? AAbstention, Candidate[] ATop3,
        FieldPrediction? Confidence, Dictionary<string, BDecision> B);
    internal sealed record EvaluatedImage(string Name, string View, string Case, RegistrationDiagnostics Registration,
        StageTimings ATimings, double ExtractionMilliseconds, Dictionary<string, double> MethodMilliseconds,
        List<Decision> Decisions, Dictionary<string, List<FieldChange>> Fields, string? AError, object[] InvalidFractions);
    internal sealed record Complement(int ARightBRight, int ARightBWrong, int AWrongBRight, int AWrongBWrong, int OracleTop1,
        int AWrongBContactRight, int ContactSetOracle);
    internal sealed record OccupancyMetrics(int TruePositive, int FalsePositive, int FalseNegative, int UnsupportedZeroFields,
        double Precision, double Recall, double F1);
}

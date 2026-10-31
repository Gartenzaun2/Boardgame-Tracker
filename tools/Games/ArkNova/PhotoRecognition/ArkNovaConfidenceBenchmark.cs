using System.Security.Cryptography;
using System.Text.Json;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal static class ArkNovaConfidenceBenchmark
{
    internal static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Run(string referenceDirectory, string datasetDirectory, string partition, string reportPath)
    {
        if (partition is not ("calibration" or "holdout"))
            throw new ArgumentException("Partition must be calibration or holdout.");
        // Check the freeze before touching holdout images, not after analysis.
        string policySourceHash = Hash(Path.Combine(referenceDirectory, "..", ArkNovaRankingExperiment.SharedRecognitionSources, "ArkNovaConfidencePolicy.cs"));
        if (partition == "holdout")
        {
            using JsonDocument frozen = JsonDocument.Parse(File.ReadAllText(Path.Combine(referenceDirectory, "..", "confidence-policy-selection.json")));
            if (frozen.RootElement.GetProperty("splitSha256").GetString() != Hash(Path.Combine(datasetDirectory, "split.json"))
                || frozen.RootElement.GetProperty("policySourceSha256").GetString() != policySourceHash
                || frozen.RootElement.GetProperty("chosenVariant").GetString() != ArkNovaConfidencePolicy.Selected.ToString())
                throw new InvalidDataException("Holdout requires the frozen split and policy source hashes.");
        }
        using JsonDocument split = JsonDocument.Parse(File.ReadAllText(Path.Combine(datasetDirectory, "split.json")));
        using JsonDocument truth = JsonDocument.Parse(File.ReadAllText(Path.Combine(datasetDirectory, "scoring_ground_truth_v2.json")));
        string[] calibration = ReadNames(split.RootElement.GetProperty("calibration"));
        string[] holdout = ReadNames(split.RootElement.GetProperty("holdout"));
        string[] all = calibration.Concat(holdout).ToArray();
        JsonElement photos = truth.RootElement.GetProperty("photos");
        if (calibration.Length != 17 || holdout.Length != 8 || all.Distinct().Count() != 25
            || !all.Order().SequenceEqual(photos.EnumerateObject().Select(photo => photo.Name).Order()))
            throw new InvalidDataException("Frozen split must cover all 25 photos exactly once.");
        VerifyManifest(datasetDirectory);
        List<Decision> decisions = [];
        List<PhotoDiagnostics> images = [];
        using ArkNovaPhotoAnalyzer analyzer = new(referenceDirectory);
        foreach (string name in partition == "calibration" ? calibration : holdout)
        {
            JsonElement photo = photos.GetProperty(name);
            RecognitionResult result = analyzer.Analyze(Path.Combine(datasetDirectory, "images", name));
            int view = photo.GetProperty("viewIndex").GetInt32();
            string scenario = photo.GetProperty("scenario").GetString()!;
            images.Add(new(name, scenario, view, result.Registration, result.Timings));
            foreach (string color in new[] { "red", "blue", "yellow", "black" })
            {
                JsonElement expectedColor = photo.GetProperty("expected").GetProperty(color);
                foreach (string track in new[] { "attractiveness", "conservation", "reputation" })
                {
                    int? expected = expectedColor.ValueKind == JsonValueKind.Null ? null : expectedColor.GetProperty(track).GetInt32();
                    FieldPrediction? prediction = result.Colors.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                    decisions.Add(new(name, scenario, view, color, track, expected, prediction, prediction));
                }
            }
            Console.WriteLine($"{partition}: {name}; registration={result.Registration.Success}");
        }
        int expectedDecisions = (partition == "calibration" ? 17 : 8) * 12;
        if (decisions.Count != expectedDecisions)
            throw new InvalidDataException("Unexpected number of ground-truth decisions.");
        ConfidenceVariant[] variants = Enum.GetValues<ConfidenceVariant>();
        if (partition == "holdout")
        {
            variants = [ConfidenceVariant.A_Baseline, ArkNovaConfidencePolicy.Selected];
        }
        Dictionary<string, object> variantReports = [];
        bool safetyPassed = true;
        foreach (ConfidenceVariant variant in variants.Distinct())
        {
            List<Decision> evaluated = [];
            foreach (IGrouping<string, Decision> group in decisions.GroupBy(item => item.Image + "/" + item.Color))
            {
                Dictionary<string, FieldPrediction> raw = group.Where(item => item.RawPrediction is not null)
                    .ToDictionary(item => item.Track, item => item.RawPrediction!);
                Dictionary<string, FieldPrediction> predictions = ArkNovaConfidencePolicy.Evaluate(raw, variant);
                evaluated.AddRange(group.Select(item => item with { Prediction = predictions.GetValueOrDefault(item.Track) }));
            }
            RegistrationChecks negatives = ArkNovaRegistrationChecks.Run(analyzer, referenceDirectory,
                Path.GetDirectoryName(Path.GetFullPath(reportPath))!, variant);
            bool noWrongHigh = !evaluated.Any(item => !item.Correct && item.Prediction?.Confidence == "High");
            bool noPhantomHigh = !evaluated.Any(item => !item.Expected.HasValue && item.Prediction?.Value is not null && item.Prediction.Confidence == "High");
            bool safetyGate = negatives.Passed && noWrongHigh && noPhantomHigh && evaluated.Count == expectedDecisions;
            if (variant == ArkNovaConfidencePolicy.Selected)
                safetyPassed = safetyGate;
            variantReports[variant.ToString()] = new
            {
                safetyGate, noWrongHigh, noPhantomHigh, negatives,
                coverageTargetMet = evaluated.Count(item => item.Expected.HasValue && item.Accepted) >= 0.75 * evaluated.Count(item => item.Expected.HasValue),
                accuracyTargetMet = evaluated.Any(item => item.Accepted)
                    && evaluated.Count(item => item.Accepted && item.Correct) >= 0.98 * evaluated.Count(item => item.Accepted),
                evaluation = BuildPartitionReport(evaluated, images)
            };
        }
        object report = new
        {
            schemaVersion = 2, partition,
            metricDefinitions = new
            {
                rawAccuracy = "Correct raw candidates / present ground-truth values; absence is reported separately.",
                coverage = "Accepted present values / all present values, including registration failures.",
                accuracy = "Correct accepted values / ALL accepted values; accepted phantom colors count as errors.",
                buckets = "High/Medium/Uncertain refer to present values; confidenceAllDecisions also includes absent colors.",
                missing = "Raw candidate and final abstention are retained separately; null is never score zero."
            },
            splitSha256 = Hash(Path.Combine(datasetDirectory, "split.json")),
            truthSha256 = Hash(Path.Combine(datasetDirectory, "scoring_ground_truth_v2.json")),
            manifestSha256 = Hash(Path.Combine(datasetDirectory, "manifest.json")),
            expectedDecisions, policySourceHash, variants = variantReports
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine($"{partition}: {decisions.Count} decisions; selected safety gate={safetyPassed}; report={reportPath}");
        return safetyPassed ? 0 : 1;
    }

    private static object BuildPartitionReport(List<Decision> decisions, List<PhotoDiagnostics> images) => new
    {
            summary = Summarize(decisions),
            byTrack = decisions.GroupBy(item => item.Track).ToDictionary(group => group.Key, group => Summarize(group)),
            byColor = decisions.GroupBy(item => item.Color).ToDictionary(group => group.Key, group => Summarize(group)),
            byViewIndex = decisions.GroupBy(item => item.ViewIndex).ToDictionary(group => group.Key.ToString(), group => new
            {
                metrics = Summarize(group),
                registrationSuccess = images.Count(image => image.ViewIndex == group.Key && image.Registration.Success),
                imageCount = images.Count(image => image.ViewIndex == group.Key),
                reprojectionP95 = images.Where(image => image.ViewIndex == group.Key).Select(image => image.Registration.ReprojectionP95).ToArray(),
                meanReprojectionP95 = images.Where(image => image.ViewIndex == group.Key).Average(image => image.Registration.ReprojectionP95),
                byTrack = group.GroupBy(item => item.Track).ToDictionary(track => track.Key, track => Summarize(track))
            }),
            correctUncertainCauses = decisions.Where(item => item.Expected.HasValue && item.RawCorrect && item.Prediction?.Confidence == "Uncertain")
                .GroupBy(item => item.Prediction!.Warning ?? "low_score_or_margin").ToDictionary(group => group.Key, group => new
                {
                    count = group.Count(), byTrack = group.GroupBy(item => item.Track).ToDictionary(track => track.Key, track => track.Count()),
                    byColor = group.GroupBy(item => item.Color).ToDictionary(color => color.Key, color => color.Count())
                }),
            images, decisions
    };

    private static object Summarize(IEnumerable<Decision> source)
    {
        Decision[] all = source.ToArray();
        Decision[] present = all.Where(item => item.Expected.HasValue).ToArray();
        Decision[] absent = all.Where(item => !item.Expected.HasValue).ToArray();
        Decision[] accepted = present.Where(item => item.Accepted).ToArray();
        Decision[] allAccepted = all.Where(item => item.Accepted).ToArray();
        int highCount = present.Count(item => item.Prediction?.Confidence == "High" && item.Prediction.Value.HasValue);
        return new
        {
            correct = present.Count(item => item.RawPrediction?.Value == item.Expected), total = present.Length,
            rawAccuracy = Ratio(present.Count(item => item.RawPrediction?.Value == item.Expected), present.Length),
            finalCorrect = present.Count(item => item.Correct),
            high = Bucket(present, "High"), medium = Bucket(present, "Medium"), uncertain = Bucket(present, "Uncertain"),
            confidenceAllDecisions = new { high = Bucket(all, "High"), medium = Bucket(all, "Medium"), uncertain = Bucket(all, "Uncertain") },
            highCoverage = Ratio(highCount, present.Length), acceptedCoverage = Ratio(accepted.Length, present.Length),
            highAccuracy = Ratio(present.Count(item => item.Correct && item.Prediction?.Confidence == "High"), all.Count(item => item.Prediction?.Value is not null && item.Prediction.Confidence == "High")),
            acceptedAccuracy = Ratio(allAccepted.Count(item => item.Correct), allAccepted.Length),
            presentHighAccuracy = Ratio(present.Count(item => item.Correct && item.Prediction?.Confidence == "High"), highCount),
            presentAcceptedAccuracy = Ratio(accepted.Count(item => item.Correct), accepted.Length),
            expectedDecisions = all.Length, registrationMissing = all.Count(item => item.Prediction is null),
            absentDecisions = absent.Length, correctNull = absent.Count(item => item.Prediction?.Value is null),
            explicitCorrectNull = absent.Count(item => item.Prediction is not null && item.Prediction.Value is null),
            phantomValues = absent.Count(item => item.Prediction?.Value is not null),
            phantomHigh = absent.Count(item => item.Prediction?.Value is not null && item.Prediction.Confidence == "High"),
            phantomAccepted = absent.Count(item => item.Accepted),
            missingPresent = present.Count(item => item.Prediction?.Value is null),
            rawMissingPresent = present.Count(item => item.RawPrediction?.Value is null),
            neighborWarnings = all.Count(item => item.Prediction?.Warning?.Contains("neighbor") == true),
            incompleteTrackWarnings = all.Count(item => item.Prediction?.Warning == "incomplete_track"),
            rawNeighborWarnings = all.Count(item => item.RawPrediction?.Warning?.Contains("neighbor") == true),
            incompleteTracks = all.Count(item => item.RawPrediction?.TrackVisibility < 0.99),
            warnings = all.Where(item => item.Prediction?.Warning is not null).GroupBy(item => item.Prediction!.Warning!)
                .ToDictionary(group => group.Key, group => group.Count()),
            manualChecksPerTwelvePresent = 12 * (1 - (Ratio(accepted.Length, present.Length) ?? 0))
        };
    }

    private static object Bucket(Decision[] decisions, string confidence)
    {
        Decision[] bucket = decisions.Where(item => (item.Prediction?.Confidence ?? "Uncertain") == confidence).ToArray();
        return new { count = bucket.Length, correct = bucket.Count(item => item.Correct), wrong = bucket.Count(item => !item.Correct), rawCorrect = bucket.Count(item => item.RawCorrect) };
    }

    private static double? Ratio(int numerator, int denominator) => denominator > 0 ? (double)numerator / denominator : null;
    private static string[] ReadNames(JsonElement element) => element.EnumerateArray().Select(item => item.GetString()!).ToArray();
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static void VerifyManifest(string directory)
    {
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        foreach (JsonElement file in manifest.RootElement.GetProperty("files").EnumerateArray())
        {
            string name = file.GetProperty("name").GetString()!;
            if (Path.GetFileName(name) != name || Hash(Path.Combine(directory, "images", name)) != file.GetProperty("sha256").GetString())
                throw new InvalidDataException($"Dataset checksum mismatch: {name}");
        }
    }

    private sealed record PhotoDiagnostics(string Image, string Scenario, int ViewIndex, RegistrationDiagnostics Registration, StageTimings Timings);
    private sealed record Decision(string Image, string Scenario, int ViewIndex, string Color, string Track, int? Expected,
        FieldPrediction? RawPrediction, FieldPrediction? Prediction)
    {
        public bool Correct => Prediction?.Value == Expected;
        public bool RawCorrect => RawPrediction?.Value == Expected;
        public bool Accepted => Prediction?.Value is not null && Prediction.Confidence is "High" or "Medium";
    }
}

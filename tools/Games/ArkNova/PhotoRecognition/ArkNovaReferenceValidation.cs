using System.Security.Cryptography;
using System.Text.Json;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>Paired replay only: no fitting, selection, or historical freeze/report writes.</summary>
internal static class ArkNovaReferenceValidation
{
    private static readonly string[] Colors = ["red", "blue", "yellow", "black"];
    private static readonly string[] Tracks = ["attractiveness", "conservation", "reputation"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Run(string root, string candidateDirectory, string output, bool negativesOnly = false)
    {
        root = Path.GetFullPath(root);
        candidateDirectory = Path.GetFullPath(candidateDirectory);
        // Refuse overwriting even this experiment's previous results.
        using FileStream report = new(output, FileMode.CreateNew);
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        List<Input> inputs = LoadInputs(root, assets);
        using JsonDocument manifest = Read(Path.Combine(candidateDirectory, "MANIFEST.json"));
        foreach (JsonElement file in manifest.RootElement.GetProperty("files").EnumerateArray())
            Verify(Path.Combine(candidateDirectory, file.GetProperty("file").GetString()!), file.GetProperty("sha256").GetString()!);
        using ArkNovaPhotoAnalyzer baseline = new(assets);
        using ArkNovaPhotoAnalyzer candidate = new(candidateDirectory, "scoring_regions_v3_candidate5.json");
        List<Photo> photos = [];
        foreach (Input input in inputs.Where(_ => !negativesOnly))
        {
            Measurement old = Measure(baseline, input);
            Measurement current = Measure(candidate, input);
            photos.Add(new(input.Batch, input.Name, Hash(input.Path), old, current));
            Console.WriteLine($"{input.Batch}/{input.Name}: registration {old.Registration.Success} -> {current.Registration.Success}; raw {old.Rows.Count(row => row.Expected.HasValue && row.Raw?.Value == row.Expected)} -> {current.Rows.Count(row => row.Expected.HasValue && row.Raw?.Value == row.Expected)}");
        }
        object[] summaries = photos.GroupBy(photo => photo.Batch).Select(batch => (object)new
        {
            batch = batch.Key,
            baseline = Summarize(batch.Select(photo => photo.Baseline)),
            candidate = Summarize(batch.Select(photo => photo.Candidate))
        }).ToArray();
        JsonSerializer.Serialize(report, new
        {
            protocol = "Paired hardened A_NoZero / unchanged Confidence E replay. Reused data, no independent holdout, no tuning. Present-value denominators include null and failed registrations. Absent-color decisions are separate.",
            baselineReferenceSha256 = Hash(Path.Combine(assets, "board_reference_master_clean.png")),
            baselineGeometrySha256 = Hash(Path.Combine(assets, "scoring_regions_v2.json")),
            candidateReferenceSha256 = Hash(Path.Combine(candidateDirectory, "board_reference_master_single_shot.png")),
            candidateGeometrySha256 = Hash(Path.Combine(candidateDirectory, "scoring_regions_v3_candidate5.json")),
            sourceHashes = Directory.GetFiles(Path.Combine(assets, ".."), "*.cs").ToDictionary(path => Path.GetFileName(path)!, Hash),
            missingStress = "IMG_4263.jpeg is absent from repository; historical 5/6 is not a new-reference measurement.",
            negatives = new
            {
                baseline = ArkNovaRegistrationChecks.Run(baseline, assets, Path.GetDirectoryName(Path.GetFullPath(output))!, ArkNovaConfidencePolicy.Selected),
                candidate = ArkNovaRegistrationChecks.Run(candidate, candidateDirectory, Path.GetDirectoryName(Path.GetFullPath(output))!,
                    ArkNovaConfidencePolicy.Selected, "board_reference_master_single_shot.png")
            },
            summaries, photos
        }, Json);
        return 0;
    }

    internal static Measurement Measure(ArkNovaPhotoAnalyzer analyzer, Input input)
    {
        ArkNovaRankingCapture capture = new();
        RecognitionResult result = analyzer.Analyze(input.Path, rankingCapture: capture);
        Dictionary<string, ColorScores> raw = ArkNovaRankingRules.Predict(capture.Observations, ArkNovaRankingRules.Selected);
        List<Row> rows = [];
        foreach (string color in Colors)
        {
            Dictionary<string, FieldPrediction> tracks = raw.GetValueOrDefault(color)?.Tracks ?? [];
            Dictionary<string, FieldPrediction> evaluated = ArkNovaConfidencePolicy.Evaluate(tracks, ArkNovaConfidencePolicy.Selected);
            foreach (string track in Tracks)
            {
                int? expected = input.Truth.TryGetProperty(color, out JsonElement values) && values.ValueKind != JsonValueKind.Null
                    ? values.GetProperty(track).GetInt32() : null;
                rows.Add(new(color, track, expected, tracks.GetValueOrDefault(track), evaluated.GetValueOrDefault(track)));
            }
        }
        return new(result.Registration, result.Timings, rows);
    }

    internal static object Summarize(IEnumerable<Measurement> source)
    {
        Measurement[] images = source.ToArray();
        Row[] rows = images.SelectMany(image => image.Rows).ToArray();
        return new
        {
            images = images.Length, registered = images.Count(image => image.Registration.Success),
            failed = images.Count(image => !image.Registration.Success),
            overall = Counts(rows),
            tracks = rows.GroupBy(row => row.Track).ToDictionary(group => group.Key, group => Counts(group)),
            colors = rows.GroupBy(row => row.Color).ToDictionary(group => group.Key, group => Counts(group))
        };
    }

    private static object Counts(IEnumerable<Row> source)
    {
        Row[] all = source.ToArray();
        Row[] present = all.Where(row => row.Expected.HasValue).ToArray();
        object Tally(Func<Row, FieldPrediction?> prediction) => new
        {
            correct = present.Count(row => prediction(row)?.Value == row.Expected),
            wrong = present.Count(row => prediction(row)?.Value is int value && value != row.Expected),
            nulls = present.Count(row => prediction(row)?.Value is null),
            top1 = present.Length == 0 ? (double?)null : (double)present.Count(row => prediction(row)?.Value == row.Expected) / present.Length,
            phantoms = all.Count(row => !row.Expected.HasValue && prediction(row)?.Value is not null)
        };
        return new
        {
            total = present.Length, absent = all.Length - present.Length,
            raw = Tally(row => row.Raw), evaluated = Tally(row => row.Evaluated),
            confidence = present.GroupBy(row => row.Evaluated?.Confidence ?? "Uncertain").ToDictionary(group => group.Key,
                group => new { count = group.Count(), correct = group.Count(row => row.Evaluated?.Value == row.Expected),
                    wrong = group.Count(row => row.Evaluated?.Value is int value && value != row.Expected), nulls = group.Count(row => row.Evaluated?.Value is null) })
        };
    }

    internal static List<Input> LoadInputs(string root, string assets)
    {
        List<Input> inputs = [];
        string v2 = Path.Combine(assets, "ConfidenceV2");
        using JsonDocument split = Read(Path.Combine(v2, "split.json"));
        using JsonDocument truth = Read(Path.Combine(v2, "scoring_ground_truth_v2.json"));
        using JsonDocument manifest = Read(Path.Combine(v2, "manifest.json"));
        Dictionary<string, string> hashes = manifest.RootElement.GetProperty("files").EnumerateArray()
            .ToDictionary(file => file.GetProperty("name").GetString()!, file => file.GetProperty("sha256").GetString()!);
        foreach (string partition in new[] { "calibration", "holdout" })
            foreach (JsonElement name in split.RootElement.GetProperty(partition).EnumerateArray())
            {
                string file = name.GetString()!;
                string path = Path.Combine(v2, "images", file);
                Verify(path, hashes[file]);
                inputs.Add(new(partition, file, path, truth.RootElement.GetProperty("photos").GetProperty(file).GetProperty("expected").Clone()));
            }
        string v3 = Path.Combine(root, "artifacts/ResearchV3/dataset_v3");
        using JsonDocument v3Manifest = Read(Path.Combine(v3, "manifest_v3.json"));
        using JsonDocument v3Truth = Read(Path.Combine(v3, "ground_truth_v3.json"));
        foreach (JsonElement image in v3Manifest.RootElement.GetProperty("images").EnumerateArray())
        {
            string name = image.GetProperty("file").GetString()!;
            string path = Path.Combine(v3, "images", name);
            Verify(path, image.GetProperty("scaled_sha256").GetString()!);
            bool empty = image.GetProperty("kind").GetString() == "empty";
            JsonElement expected = empty ? JsonSerializer.SerializeToElement(new { })
                : v3Truth.RootElement.GetProperty("cases").GetProperty("Fall" + image.GetProperty("case").GetInt32()).Clone();
            inputs.Add(new(empty ? "v3_empty" : "v3", name, path, expected));
        }
        using JsonDocument legacy = Read(Path.Combine(assets, "scoring_ground_truth.json"));
        foreach (JsonProperty image in legacy.RootElement.GetProperty("images").EnumerateObject())
            inputs.Add(new("legacy6", image.Name, Path.Combine(assets, image.Name), image.Value.Clone()));
        if (inputs.Count(input => input.Batch == "calibration") != 17 || inputs.Count(input => input.Batch == "holdout") != 8
            || inputs.Count(input => input.Batch == "v3") != 13 || inputs.Count(input => input.Batch == "v3_empty") != 10)
            throw new InvalidDataException("Historical image denominators changed.");
        return inputs;
    }

    private static JsonDocument Read(string path) => JsonDocument.Parse(File.ReadAllText(path));
    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Verify(string path, string expected)
    {
        if (Hash(path) != expected)
            throw new InvalidDataException("Input hash mismatch: " + path);
    }

    internal sealed record Input(string Batch, string Name, string Path, JsonElement Truth);
    internal sealed record Row(string Color, string Track, int? Expected, FieldPrediction? Raw, FieldPrediction? Evaluated);
    internal sealed record Measurement(RegistrationDiagnostics Registration, StageTimings Timings, List<Row> Rows);
    private sealed record Photo(string Batch, string Name, string Sha256, Measurement Baseline, Measurement Candidate);
}

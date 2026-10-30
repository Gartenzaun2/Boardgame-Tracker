using System.Security.Cryptography;
using System.Text.Json;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal static class ArkNovaRankingExperiment
{
    internal static readonly string[] FrozenSources = ["ArkNovaRankingRules.cs", "ArkNovaRankingCapture.cs", "ArkNovaPhotoAnalyzer.cs",
        "ArkNovaConfidencePolicy.cs", "ArkNovaRankingExperiment.cs", "Program.cs", "Assets/scoring_regions_v2.json",
        "Assets/ConfidenceV2/scoring_ground_truth_v2.json", "Assets/ConfidenceV2/manifest.json",
        "../../../../src/BoardgameTracker.Application/Games/ArkNova/GameSessions/Scoring/ArkNovaFieldOccupancy.cs"];

    public static int WriteCalibrationDiagnostics(string assets, string dataset, string output)
    {
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        // IMG_4268 is exported only inside the single frozen holdout evaluation.
        foreach (string name in new[] { "IMG_4265", "IMG_4266", "IMG_4267" })
        {
            ArkNovaRankingCapture capture = new(Path.Combine(output, name), ArkNovaRankingRules.Selected);
            analyzer.Analyze(Path.Combine(dataset, "images", name + ".jpg"), rankingCapture: capture);
        }
        return 0;
    }

    public static int Run(string assets, string dataset, string partition, string reportPath)
    {
        if (partition is not ("calibration" or "holdout"))
            throw new ArgumentException("Expected calibration or holdout.");
        string tool = Path.GetFullPath(Path.Combine(assets, ".."));
        using JsonDocument split = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataset, "split.json")));
        using JsonDocument truth = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataset, "scoring_ground_truth_v2.json")));
        RankingVariant selected = RankingVariant.Baseline;
        if (partition == "holdout")
        {
            using JsonDocument freeze = JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, "ranking-selection.json")));
            foreach (string source in FrozenSources)
                if (freeze.RootElement.GetProperty("sourceHashes").GetProperty(source).GetString() != Hash(Path.Combine(tool, source)))
                    throw new InvalidDataException($"Frozen source changed: {source}");
            if (freeze.RootElement.GetProperty("splitSha256").GetString() != Hash(Path.Combine(dataset, "split.json")))
                throw new InvalidDataException("Frozen split changed.");
            selected = (RankingVariant)freeze.RootElement.GetProperty("selectedVariant").GetInt32();
            if (selected != ArkNovaRankingRules.Selected)
                throw new InvalidDataException("Frozen variant differs from analyze default.");
        }
        string[] names = split.RootElement.GetProperty(partition).EnumerateArray().Select(item => item.GetString()!).ToArray();
        if (names.Length != (partition == "calibration" ? 17 : 8))
            throw new InvalidDataException("Unexpected image count.");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataset, "manifest.json")));
        foreach (JsonElement file in manifest.RootElement.GetProperty("files").EnumerateArray())
            if (Hash(Path.Combine(dataset, "images", file.GetProperty("name").GetString()!)) != file.GetProperty("sha256").GetString())
                throw new InvalidDataException("Dataset image changed.");
        RankingVariant[] variants = partition == "calibration" ? Enumerable.Range(0, 8).Select(number => (RankingVariant)number).ToArray()
            : new[] { RankingVariant.Baseline, selected }.Distinct().ToArray();
        Dictionary<RankingVariant, List<Decision>> decisions = variants.ToDictionary(variant => variant, _ => new List<Decision>());
        List<object> registration = [];
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        foreach (string name in names)
        {
            string? diagnosticDirectory = partition == "holdout" && name == "IMG_4268.jpg"
                ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath))!, "RankingDiagnostics", Path.GetFileNameWithoutExtension(name)) : null;
            ArkNovaRankingCapture capture = new(diagnosticDirectory, selected);
            RecognitionResult baseline = analyzer.Analyze(Path.Combine(dataset, "images", name), rankingCapture: capture);
            registration.Add(new { image = name, baseline.Registration });
            JsonElement expected = truth.RootElement.GetProperty("photos").GetProperty(name).GetProperty("expected");
            foreach (RankingVariant variant in variants)
            {
                Dictionary<string, ColorScores> raw = ArkNovaRankingRules.Predict(capture.Observations, variant);
                Dictionary<string, ColorScores> confidence = raw.ToDictionary(color => color.Key,
                    color => new ColorScores(ArkNovaConfidencePolicy.Evaluate(color.Value.Tracks, ArkNovaConfidencePolicy.Selected)));
                foreach (string color in new[] { "red", "blue", "yellow", "black" })
                    foreach (string track in new[] { "attractiveness", "conservation", "reputation" })
                    {
                        JsonElement expectedColor = expected.GetProperty(color);
                        int? value = expectedColor.ValueKind == JsonValueKind.Null ? null : expectedColor.GetProperty(track).GetInt32();
                        RankingResult ranking = ArkNovaRankingRules.Rank(capture.Observations.Where(field => field.Color == color && field.Track == track), variant);
                        FieldPrediction? predicted = raw.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                        if (variant == RankingVariant.Baseline && baseline.Registration.Success)
                        {
                            FieldPrediction original = baseline.Colors[color].Tracks[track];
                            if (predicted is null || original.Value != predicted.Value || Math.Abs(original.Score - predicted.Score) > 1e-9)
                                throw new InvalidDataException("Reconstructed baseline differs from analyzer.");
                        }
                        int rank = value.HasValue ? Array.FindIndex(ranking.Fields, field => field.Observation.Value == value) + 1 : 0;
                        RankedField? twenty = ranking.Fields.FirstOrDefault(field => field.Observation.Value == 20);
                        RankedField? bestWrong = value.HasValue ? ranking.Fields.FirstOrDefault(field => field.Observation.Value != value) : null;
                        decisions[variant].Add(new(name, color, track, value, ranking.Value, ranking.Fields.FirstOrDefault()?.Observation.Value,
                            rank > 0 ? rank : null, ranking.Abstention, predicted?.Margin ?? 0, ranking.RankingGap,
                            confidence.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track),
                            ranking.Fields.Take(3).Select(field => new TopField(field.Observation.Value, field.Score, field.FullColor, field.CoreColor)).ToArray(),
                            twenty is null ? null : new TopField(20, twenty.Score, twenty.FullColor, twenty.CoreColor),
                            bestWrong is null ? null : new TopField(bestWrong.Observation.Value, bestWrong.Score, bestWrong.FullColor, bestWrong.CoreColor)));
                    }
            }
            Console.WriteLine($"{partition}: {name}; registration={baseline.Registration.Success}");
        }
        RegistrationChecks negatives = ArkNovaRegistrationChecks.Run(analyzer, assets, Path.GetDirectoryName(Path.GetFullPath(reportPath))!, ArkNovaConfidencePolicy.Selected);
        ArkNovaRankingCapture emptyCapture = new();
        analyzer.Analyze(Path.Combine(assets, "board_reference_master_clean.png"), rankingCapture: emptyCapture);
        Dictionary<string, int> emptyStrong = variants.ToDictionary(variant => ((int)variant).ToString(), variant =>
            ArkNovaRankingRules.Predict(emptyCapture.Observations, variant).Values
                .SelectMany(color => ArkNovaConfidencePolicy.Evaluate(color.Tracks, ArkNovaConfidencePolicy.Selected).Values)
                .Count(prediction => prediction.Value.HasValue && prediction.Confidence is "High" or "Medium"));
        object report = new
        {
            schemaVersion = 1, partition, splitSha256 = Hash(Path.Combine(dataset, "split.json")),
            sourceHashes = FrozenSources.ToDictionary(source => source, source => Hash(Path.Combine(tool, source))),
            registration, negatives, emptyReferenceStrongValues = emptyStrong,
            variants = decisions.ToDictionary(pair => ((int)pair.Key).ToString(), pair => new
            {
                name = pair.Key.ToString(), summary = Metrics(pair.Value),
                byTrack = pair.Value.GroupBy(row => row.Track).ToDictionary(group => group.Key, group => Metrics(group)),
                byColor = pair.Value.GroupBy(row => row.Color).ToDictionary(group => group.Key, group => Metrics(group)),
                blackConservation = Metrics(pair.Value.Where(row => row.Color == "black" && row.Track == "conservation")),
                zeroReplacement = CompareZero(decisions[RankingVariant.Baseline], pair.Value), decisions = pair.Value
            })
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, ArkNovaConfidenceBenchmark.JsonOptions));
        foreach ((RankingVariant variant, List<Decision> rows) in decisions)
            Console.WriteLine($"{variant}: {JsonSerializer.Serialize(Metrics(rows))}");
        return negatives.Passed && emptyStrong[((int)ArkNovaRankingRules.Selected).ToString()] == 0 ? 0 : 1;
    }

    private static object Metrics(IEnumerable<Decision> source)
    {
        Decision[] all = source.ToArray();
        Decision[] present = all.Where(row => row.Expected.HasValue).ToArray();
        Decision[] absent = all.Where(row => !row.Expected.HasValue).ToArray();
        Decision[] accepted = all.Where(row => row.Accepted).ToArray();
        double[] gaps = present.Select(row => row.RankingGap).Order().ToArray();
        double[] signedScoreGaps = present.Where(row => row.Top3.Length >= 2).Select(row => row.Top3[0].Score - row.Top3[1].Score).Order().ToArray();
        return new
        {
            correct = present.Count(row => row.Correct), total = present.Length,
            rawAccuracy = Ratio(present.Count(row => row.Correct), present.Length),
            top1Correct = present.Count(row => row.Correct), topRankCorrectBeforeAbstention = present.Count(row => row.TopRanked == row.Expected),
            correctAtRank2 = present.Count(row => row.ExpectedRank == 2), nullCount = all.Count(row => row.Value is null),
            nullPresent = present.Count(row => row.Value is null), phantomValues = absent.Count(row => row.Value is not null),
            phantomAfterConfidence = absent.Count(row => row.Confidence?.Value is not null),
            phantomAccepted = absent.Count(row => row.Accepted),
            meanRankingGap = gaps.Length > 0 ? gaps.Average() : (double?)null,
            medianRankingGap = gaps.Length > 0 ? (gaps[(gaps.Length - 1) / 2] + gaps[gaps.Length / 2]) / 2 : (double?)null,
            meanSignedScoreGap = signedScoreGaps.Length > 0 ? signedScoreGaps.Average() : (double?)null,
            medianSignedScoreGap = signedScoreGaps.Length > 0 ? (signedScoreGaps[(signedScoreGaps.Length - 1) / 2] + signedScoreGaps[signedScoreGaps.Length / 2]) / 2 : (double?)null,
            acceptedCoverage = Ratio(present.Count(row => row.Accepted), present.Length),
            acceptedAccuracy = Ratio(accepted.Count(row => row.Correct), accepted.Length),
            wrongHigh = all.Count(row => row.Confidence?.Value is not null && row.Confidence.Confidence == "High" && !row.Correct)
        };
    }

    private static object CompareZero(List<Decision> baseline, List<Decision> changed)
    {
        Decision[] zero = changed.Where((row, index) => baseline[index].Value == 0 && baseline[index].Expected.HasValue).ToArray();
        return new
        {
            formerZeroErrors = zero.Length, corrected = zero.Count(row => row.Correct),
            substituteErrors = zero.Count(row => !row.Correct && row.Value.HasValue && row.Value != 0), abstained = zero.Count(row => row.Value is null),
            naiveNonZeroCorrect = zero.Count(row => row.TopRanked == row.Expected),
            naiveNonZeroWrong = zero.Count(row => row.TopRanked.HasValue && row.TopRanked != 0 && row.TopRanked != row.Expected),
            replacements = zero.Select(row => new { row.Image, row.Color, row.Track, row.Expected, row.Value, row.Abstention }),
            newlyWrong = changed.Where((row, index) => baseline[index].Expected.HasValue && baseline[index].Correct && !row.Correct)
                .Select(row => new { row.Image, row.Color, row.Track, row.Expected, row.Value, row.Abstention }).ToArray()
        };
    }

    private static double? Ratio(int numerator, int denominator) => denominator > 0 ? (double)numerator / denominator : null;
    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private sealed record TopField(int Value, double Score, double FullColor, double CoreColor);
    private sealed record Decision(string Image, string Color, string Track, int? Expected, int? Value, int? TopRanked,
        int? ExpectedRank, string? Abstention, double ScoreGap, double RankingGap, FieldPrediction? Confidence, TopField[] Top3, TopField? Field20, TopField? BestWrong)
    {
        public bool Correct => Value == Expected;
        public bool Accepted => Confidence?.Value is not null && Confidence.Confidence is "High" or "Medium";
    }
}

using System.Security.Cryptography;
using System.Text.Json;
using OpenCvSharp;
using static BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition.ArkNovaReferenceValidation;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>Replay the three supplied pairs verbatim; no asset generation or parameter selection.</summary>
internal static class ArkNovaReference3WayComparison
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Run(string root, string handoff, string output, string? existingReport = null)
    {
        using FileStream report = new(output, FileMode.CreateNew);
        using JsonDocument manifest = Read(Path.Combine(handoff, "MANIFEST.json"));
        foreach (JsonElement file in manifest.RootElement.GetProperty("files").EnumerateArray())
            if (Hash(Path.Combine(handoff, file.GetProperty(existingReport is null ? "path" : "file").GetString()!)) != file.GetProperty("sha256").GetString())
                throw new InvalidDataException("Handoff hash mismatch.");
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        if (existingReport is null)
        {
            VerifyBaseline("board_reference_master_clean.png");
            VerifyBaseline("scoring_regions_v2.json");
        }
        Variant[] variants = existingReport is not null
            ? [new("C_rectified_scoring_4000_v2_boundsfix", "scoring_regions_v3_rectified_candidate5_scoringonly_v2.json")]
            :
        [
            new("A_current", "scoring_regions_v2.json"),
            new("B_new_scaled_4000x2800", "scoring_regions_v3_candidate5_scaled_4000x2800.json"),
            new("C_rectified_scoring_4000", "scoring_regions_v3_rectified_candidate5_scoringonly.json")
        ];
        List<Input> inputs = LoadInputs(root, assets);
        List<object> results = [];
        if (existingReport is not null)
        {
            using JsonDocument saved = Read(existingReport);
            foreach (string name in new[] { "A_current", "B_new_scaled_4000x2800" })
            {
                JsonElement variant = saved.RootElement.GetProperty("results").EnumerateArray()
                    .Single(item => item.GetProperty("variant").GetString() == name);
                JsonElement[] photos = variant.GetProperty("photos").EnumerateArray().ToArray();
                if (photos.Length != inputs.Count)
                    throw new InvalidDataException("Cached input count differs.");
                foreach (Input input in inputs)
                    if (photos.Single(photo => photo.GetProperty("batch").GetString() == input.Batch
                        && photo.GetProperty("name").GetString() == input.Name).GetProperty("sha256").GetString() != Hash(input.Path))
                        throw new InvalidDataException("Cached input hash differs: " + input.Name);
                results.Add(variant.Clone());
            }
        }
        Dictionary<string, string> hashes = Directory.GetFiles(Path.Combine(assets, ".."), "*.cs").ToDictionary(path => Path.GetFileName(path)!, Hash);
        foreach (Variant variant in variants)
        {
            string directory = existingReport is null ? Path.Combine(handoff, variant.Name) : handoff;
            using JsonDocument geometry = Read(Path.Combine(directory, variant.Geometry));
            JsonElement reference = geometry.RootElement.GetProperty("reference");
            string image = reference.GetProperty("image").GetString()!;
            if (existingReport is not null)
            {
                using Mat referenceImage = Cv2.ImDecode(File.ReadAllBytes(Path.Combine(directory, image)), ImreadModes.Color);
                if (referenceImage.Height != 1544 || referenceImage.Width != 4000
                    || reference.GetProperty("height_px").GetInt32() != 1544)
                    throw new InvalidDataException("STOP: reference must be 4000x1544. Check asset paths.");
                Console.WriteLine($"PRECHECK reference: {referenceImage.Width}x{referenceImage.Height}; path={Path.GetFullPath(Path.Combine(directory, image))}");
            }
            using ArkNovaPhotoAnalyzer analyzer = new(directory, variant.Geometry);
            if (existingReport is not null)
            {
                if (analyzer.AnalysisSize.Height != 772 || analyzer.AnalysisSize.Width != 2000)
                    throw new InvalidDataException("STOP: actual analyzer size must be 2000x772. Check asset paths.");
                Console.WriteLine($"PRECHECK actual analyzer: {analyzer.AnalysisSize.Width}x{analyzer.AnalysisSize.Height}; only C will run; A/B loaded from report.");
            }
            List<Photo> photos = [];
            foreach (Input input in inputs)
            {
                using Mat decoded = Cv2.ImDecode(File.ReadAllBytes(input.Path), ImreadModes.Color);
                string? error = null;
                Measurement measurement;
                try
                {
                    measurement = Measure(analyzer, input);
                }
                catch (OpenCVException exception)
                {
                    // Measurement plumbing only: preserve every GT denominator on a technical failure.
                    error = exception.Message;
                    List<Row> rows = [];
                    foreach (string color in new[] { "red", "blue", "yellow", "black" })
                        foreach (string track in new[] { "attractiveness", "conservation", "reputation" })
                        {
                            int? expected = input.Truth.TryGetProperty(color, out JsonElement values) && values.ValueKind != JsonValueKind.Null
                                ? values.GetProperty(track).GetInt32() : null;
                            rows.Add(new(color, track, expected, null, null));
                        }
                    measurement = new(new(false, 0, 0, null, 0, 0, "Technical failure; registration diagnostics unavailable: " + error),
                        new(0, 0, 0, 0), rows);
                }
                photos.Add(new(input.Batch, input.Name, Hash(input.Path), decoded.Width, decoded.Height,
                    Math.Min(1, 1600.0 / Math.Max(decoded.Width, decoded.Height)), measurement, error));
                Console.WriteLine($"{variant.Name}/{input.Batch}/{input.Name}: registration={measurement.Registration.Success}; raw={measurement.Rows.Count(row => row.Expected.HasValue && row.Raw?.Value == row.Expected)}");
            }
            RegistrationChecks? negatives = null;
            string? negativeError = null;
            try
            {
                negatives = ArkNovaRegistrationChecks.Run(analyzer, directory,
                    Path.GetDirectoryName(Path.GetFullPath(output))!, ArkNovaConfidencePolicy.Selected, image);
            }
            catch (OpenCVException exception)
            {
                negativeError = exception.Message;
            }
            object variantResult = new
            {
                variant = variant.Name, referenceImage = image, geometryFile = variant.Geometry,
                referenceWidth = reference.GetProperty("width_px").GetInt32(), referenceHeight = reference.GetProperty("height_px").GetInt32(),
                analysisScale = .5, registrationEdge = 1600,
                actualAnalysisWidth = analyzer.AnalysisSize.Width, actualAnalysisHeight = analyzer.AnalysisSize.Height,
                summaries = photos.GroupBy(photo => photo.Batch).Select(batch => new { batch = batch.Key, metrics = Summarize(batch.Select(photo => photo.Measurement)) }),
                negatives, negativeError, technicalFailures = photos.Count(photo => photo.TechnicalFailure is not null), photos
            };
            // Preserve completed variants if a later variant encounters a different technical error.
            using (FileStream checkpoint = new(output + "." + variant.Name + ".json", FileMode.CreateNew))
                JsonSerializer.Serialize(checkpoint, variantResult, Json);
            results.Add(variantResult);
        }
        JsonSerializer.Serialize(report, new
        {
            protocol = "Only three supplied reference/geometry pairs. Unchanged hardened A_NoZero / Confidence E / RegistrationEdge 1600 / AnalysisScale 0.5. Same historical datasets and denominators including null. No tuning, production switch or new reference transform.",
            manifestSha256 = Hash(Path.Combine(handoff, "MANIFEST.json")), sourceHashes = hashes,
            reusedABReport = existingReport, reusedABReportSha256 = existingReport is null ? null : Hash(existingReport),
            newlyEvaluatedVariants = variants.Select(variant => variant.Name), results
        }, Json);
        return 0;

        void VerifyBaseline(string file)
        {
            if (Hash(Path.Combine(handoff, "A_current", file)) != Hash(Path.Combine(assets, file)))
                throw new InvalidDataException("Supplied A is not the current baseline: " + file);
        }
    }

    private static JsonDocument Read(string path) => JsonDocument.Parse(File.ReadAllText(path));
    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private sealed record Variant(string Name, string Geometry);
    private sealed record Photo(string Batch, string Name, string Sha256, int InputWidth, int InputHeight, double InputRegistrationScale,
        Measurement Measurement, string? TechnicalFailure);
}

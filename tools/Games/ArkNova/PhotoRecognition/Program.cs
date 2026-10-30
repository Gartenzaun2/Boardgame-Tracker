using System.Diagnostics;
using System.Text.Json;
using OpenCvSharp;
using BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

if (args.Length == 4 && args[0] == "diagnose-black")
    return ArkNovaBlackDiagnostics.Run(args[1], args[2], args[3]);

if (args.Length == 5 && args[0] == "confidence")
    return ArkNovaConfidenceBenchmark.Run(args[1], args[2], args[3], args[4]);

// Explicit paths keep research input separate from the production app.
if (args.Length < 3 || args[0] is not ("analyze" or "benchmark"))
{
    Console.Error.WriteLine("Usage: analyze <asset-directory> <photo> [report.json] | benchmark <asset-directory> <report.json> | confidence <asset-directory> <dataset-directory> <calibration|holdout> <report.json> | diagnose-black <asset-directory> <dataset-directory> <output-directory>");
    return 2;
}
JsonSerializerOptions jsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
using ArkNovaPhotoAnalyzer analyzer = new(Path.GetFullPath(args[1]));
if (args[0] == "analyze")
{
    RecognitionResult raw = analyzer.Analyze(Path.GetFullPath(args[2]));
    RecognitionResult result = raw with { Colors = raw.Colors.ToDictionary(pair => pair.Key,
        pair => new ColorScores(ArkNovaConfidencePolicy.Evaluate(pair.Value.Tracks, ArkNovaConfidencePolicy.Selected))) };
    string json = JsonSerializer.Serialize(result, jsonOptions);
    if (args.Length > 3)
        File.WriteAllText(args[3], json);
    else
        Console.WriteLine(json);
    return result.Registration.Success ? 0 : 1;
}

using JsonDocument truth = JsonDocument.Parse(File.ReadAllText(Path.Combine(args[1], "scoring_ground_truth.json")));
List<object> predictions = [];
Dictionary<string, RecognitionResult> results = [];
int correct = 0;
int total = 0;
int wrongHigh = 0;
int criticalHigh = 0;
foreach (JsonProperty image in truth.RootElement.GetProperty("images").EnumerateObject())
{
    RecognitionResult result = analyzer.Analyze(Path.Combine(args[1], image.Name));
    results[image.Name] = result;
    int imageCorrect = 0;
    foreach (JsonProperty color in image.Value.EnumerateObject())
    {
        foreach (JsonProperty track in color.Value.EnumerateObject())
        {
            FieldPrediction? prediction = result.Colors.GetValueOrDefault(color.Name)?.Tracks.GetValueOrDefault(track.Name);
            bool match = prediction?.Value == track.Value.GetInt32();
            if (match)
            {
                correct++;
                imageCorrect++;
            }
            if (!match && prediction?.Confidence == "High")
                wrongHigh++;
            if (image.Name is "IMG_4259.JPG" or "IMG_4260.JPG" or "IMG_4261.JPG"
                && color.Name == "red" && track.Name == "conservation" && !match && prediction?.Confidence == "High")
                criticalHigh++;
            total++;
            predictions.Add(new { image = image.Name, color = color.Name, track = track.Name,
                expected = track.Value.GetInt32(), correct = match, prediction });
        }
    }
    Console.WriteLine($"{image.Name}: {imageCorrect}/12; registration={result.Registration.Success}; {result.Timings.TotalMilliseconds:F0} ms");
}
// Automated negative registration checks never inspect UI or generate screenshots.
string negativePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[2]))!, $"ark-nova-negative-{Guid.NewGuid():N}.tmp.png");
Directory.CreateDirectory(Path.GetDirectoryName(negativePath)!);
bool blankRejected;
bool partialRejected;
bool emptyBoardHasNoHighScores;
try
{
    using Mat blank = new(600, 900, MatType.CV_8UC3, Scalar.White);
    File.WriteAllBytes(negativePath, blank.ImEncode(".png"));
    RecognitionResult blankResult = analyzer.Analyze(negativePath);
    blankRejected = !blankResult.Registration.Success && blankResult.Colors.Count == 0;
    using Mat reference = Cv2.ImDecode(File.ReadAllBytes(Path.Combine(args[1], "board_reference_master_clean.png")), ImreadModes.Color);
    RecognitionResult emptyBoard = analyzer.Analyze(Path.Combine(args[1], "board_reference_master_clean.png"));
    emptyBoardHasNoHighScores = emptyBoard.Registration.Success
        && emptyBoard.Colors.Values.SelectMany(color => color.Tracks.Values).All(prediction => prediction.Confidence != "High");
    using Mat partial = new(reference, new Rect(0, 0, reference.Width / 2, reference.Height));
    File.WriteAllBytes(negativePath, partial.ImEncode(".png"));
    RecognitionResult partialResult = analyzer.Analyze(negativePath);
    partialRejected = !partialResult.Registration.Success && partialResult.Colors.Count == 0;
}
finally
{
    File.Delete(negativePath);
}
bool accepted = total == 72 && correct >= 60 && criticalHigh == 0 && blankRejected && partialRejected && emptyBoardHasNoHighScores;
Process process = Process.GetCurrentProcess();
object report = new
{
    schemaVersion = 1, implementation = "Reconstructed .NET development spike; not the original Python pipeline",
    environment = new { os = Environment.OSVersion.ToString(), runtime = Environment.Version.ToString(),
        openCv = Cv2.GetVersionString(), processors = Environment.ProcessorCount, peakWorkingSetBytes = process.PeakWorkingSet64 },
    preparationMilliseconds = analyzer.PreparationMilliseconds,
    summary = new { correct, total, wrongHigh, criticalHigh, blankRejected, partialRejected, emptyBoardHasNoHighScores, accepted },
    results, predictions
};
File.WriteAllText(args[2], JsonSerializer.Serialize(report, jsonOptions));
Console.WriteLine($"Total: {correct}/{total}; wrong High={wrongHigh}; critical High={criticalHigh}; blank rejected={blankRejected}; partial rejected={partialRejected}");
return accepted ? 0 : 1;

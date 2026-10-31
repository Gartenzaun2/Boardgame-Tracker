using System.Runtime.InteropServices;
using System.Text.Json;
using OpenCvSharp;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal static class ArkNovaNativeParity
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    private static readonly string[] Tracks = ["attractiveness", "conservation", "reputation"];
    private static readonly string[] Colors = ["red", "blue", "yellow", "black"];

    public static int Export(string root, string output)
    {
        Directory.CreateDirectory(output);
        string assets = Path.Combine(root, "tools/Games/ArkNova/PhotoRecognition/Assets");
        File.Copy(Path.Combine(assets, "board_reference_master_clean.png"), Path.Combine(output, "reference.png"), true);
        File.Copy(Path.Combine(assets, "scoring_regions_v2.json"), Path.Combine(output, "scoring_regions_v2.json"), true);
        using JsonDocument geometry = Read(Path.Combine(assets, "scoring_regions_v2.json"));
        List<int[]> fields = [];
        List<int[]> points = [];
        for (int track = 0; track < 3; track++)
            foreach (JsonElement field in geometry.RootElement.GetProperty("tracks").GetProperty(Tracks[track]).GetProperty("fields").EnumerateArray())
            {
                JsonElement polygon = field.GetProperty("polygon");
                fields.Add([track, field.GetProperty("value").GetInt32(), points.Count, polygon.GetArrayLength()]);
                foreach (JsonElement point in polygon.EnumerateArray())
                    points.Add([(int)Math.Round(point[0].GetDouble() * .5), (int)Math.Round(point[1].GetDouble() * .5)]);
            }
        using (BinaryWriter writer = new(File.Create(Path.Combine(output, "fields.bin"))))
        {
            writer.Write(fields.Count); writer.Write(points.Count);
            foreach (int[] field in fields) foreach (int number in field) writer.Write(number);
            foreach (int[] point in points) foreach (int number in point) writer.Write(number);
        }
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        List<object> goldens = [];
        List<string> names = [];
        int index = 0;
        foreach (ArkNovaReferenceValidation.Input input in ArkNovaReferenceValidation.LoadInputs(root, assets))
        {
            string id = (index++).ToString("D3");
            using Mat image = Cv2.ImDecode(File.ReadAllBytes(input.Path), ImreadModes.Color);
            // Normalize once with the Windows decode contract. Native PNG decode must preserve these BGR bytes.
            File.WriteAllBytes(Path.Combine(output, id + ".png"), image.ImEncode(".png"));
            double[] homography = [];
            ArkNovaRankingCapture capture = new() { RegistrationObserver = transform =>
            {
                homography = Enumerable.Range(0, 9).Select(i => transform.At<double>(i / 3, i % 3)).ToArray();
            }, FrameObserver = frame =>
            {
                if (index <= 6 || input.Batch == "legacy6")
                {
                    byte[] bytes = new byte[checked(frame.Warped.Rows * frame.Warped.Cols * 3)];
                    Marshal.Copy(frame.Warped.Data, bytes, 0, bytes.Length);
                    File.WriteAllBytes(Path.Combine(output, id + ".warp.bgr"), bytes);
                }
            } };
            RecognitionResult result = analyzer.Analyze(input.Path, rankingCapture: capture);
            Dictionary<string, ColorScores> raw = ArkNovaRankingRules.Predict(capture.Observations, ArkNovaRankingRules.Selected);
            Dictionary<string, ColorScores> evaluated = raw.ToDictionary(pair => pair.Key,
                pair => new ColorScores(ArkNovaConfidencePolicy.Evaluate(pair.Value.Tracks, ArkNovaConfidencePolicy.Selected)));
            goldens.Add(new { id, input.Name, input.Batch, registration = result.Registration, homography, raw, evaluated, fields = capture.Observations });
            names.Add(id);
            Console.WriteLine("Golden " + id + " " + input.Name);
        }
        using (Mat blank = new(600, 900, MatType.CV_8UC3, Scalar.White))
            File.WriteAllBytes(Path.Combine(output, "blank.png"), blank.ImEncode(".png"));
        File.WriteAllLines(Path.Combine(output, "inputs.txt"), names);
        File.WriteAllText(Path.Combine(output, "golden.json"), JsonSerializer.Serialize(goldens, Json));
        return 0;
    }

    public static int Compare(string directory, string nativePath, string reportPath)
    {
        using JsonDocument golden = Read(Path.Combine(directory, "golden.json"));
        Dictionary<string, JsonElement> expected = golden.RootElement.EnumerateArray().ToDictionary(photo => photo.GetProperty("id").GetString()!, photo => photo);
        List<object> photos = [];
        HashSet<string> seen = [];
        int decisions = 0, differentRaw = 0, differentE = 0, differentRegistration = 0;
        foreach (string line in File.ReadLines(nativePath).Where(line => line.StartsWith('{')))
        {
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement result = document.RootElement;
            string id = result.GetProperty("id").GetString()!;
            if (!seen.Add(id))
                throw new InvalidDataException("Duplicate native parity input: " + id);
            JsonElement old = expected[id];
            RankingObservation[] fields = result.GetProperty("fields").EnumerateArray().Select(field => new RankingObservation(
                Colors[field[0].GetInt32()], Tracks[field[1].GetInt32()], field[2].GetInt32(),
                field[3].GetDouble(), field[4].GetDouble(), field[5].GetDouble(), field[6].GetDouble(), field[7].GetDouble(),
                field[8].GetDouble(), field[9].GetDouble(), field[3].GetDouble(), field[4].GetDouble(), field[7].GetDouble())).ToArray();
            Dictionary<string, ColorScores> raw = ArkNovaRankingRules.Predict(fields, ArkNovaRankingRules.Selected);
            Dictionary<string, ColorScores> evaluated = raw.ToDictionary(pair => pair.Key,
                pair => new ColorScores(ArkNovaConfidencePolicy.Evaluate(pair.Value.Tracks, ArkNovaConfidencePolicy.Selected)));
            Dictionary<string, ColorScores> oldRaw = old.GetProperty("raw").Deserialize<Dictionary<string, ColorScores>>(Json)!;
            Dictionary<string, ColorScores> oldE = old.GetProperty("evaluated").Deserialize<Dictionary<string, ColorScores>>(Json)!;
            List<object> differences = [];
            foreach (string color in Colors) foreach (string track in Tracks)
            {
                decisions++;
                FieldPrediction? a = oldRaw.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                FieldPrediction? b = raw.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                FieldPrediction? ae = oldE.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                FieldPrediction? be = evaluated.GetValueOrDefault(color)?.Tracks.GetValueOrDefault(track);
                bool rawDiffers = a?.Value != b?.Value;
                bool eDiffers = ae?.Value != be?.Value || ae?.Confidence != be?.Confidence;
                if (rawDiffers) differentRaw++;
                if (eDiffers) differentE++;
                if (rawDiffers || eDiffers) differences.Add(new { color, track, rawBefore = a?.Value, rawAfter = b?.Value, eBefore = ae, eAfter = be });
            }
            bool registered = result.GetProperty("status").GetInt32() == 0;
            if (registered != old.GetProperty("registration").GetProperty("success").GetBoolean()) differentRegistration++;
            double maxFractionDelta = 0;
            RankingObservation[] before = old.GetProperty("fields").Deserialize<RankingObservation[]>(Json)!;
            foreach (RankingObservation field in fields)
            {
                RankingObservation? original = before.FirstOrDefault(item => item.Color == field.Color && item.Track == field.Track && item.Value == field.Value);
                if (original is not null) maxFractionDelta = Math.Max(maxFractionDelta, new[]
                { Math.Abs(field.FullColor-original.FullColor), Math.Abs(field.CoreColor-original.CoreColor), Math.Abs(field.Grown-original.Grown), Math.Abs(field.FullDifference-original.FullDifference) }.Max());
            }
            double[] windowsHomography = old.GetProperty("homography").Deserialize<double[]>(Json)!;
            photos.Add(new { id, name = old.GetProperty("name").GetString(), windowsHomography, native = result.Clone(), maxFractionDelta, differences });
        }
        bool passed = photos.Count == expected.Count && differentRaw == 0 && differentE == 0 && differentRegistration == 0;
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new { passed, images = photos.Count, expectedImages = expected.Count,
            decisions, differentRaw, differentE, differentRegistration, photos }, Json));
        Console.WriteLine($"Functional parity={passed}, photos={photos.Count}/{expected.Count}, decisions={decisions}, raw differences={differentRaw}, E differences={differentE}, registration differences={differentRegistration}");
        return passed ? 0 : 1;
    }
    private static JsonDocument Read(string path) => JsonDocument.Parse(File.ReadAllText(path));
}

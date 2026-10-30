using OpenCvSharp;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

internal static class ArkNovaRegistrationChecks
{
    public static RegistrationChecks Run(ArkNovaPhotoAnalyzer analyzer, string assets, string scratchDirectory, ConfidenceVariant policy)
    {
        Directory.CreateDirectory(scratchDirectory);
        string path = Path.Combine(scratchDirectory, $"negative-{Guid.NewGuid():N}.png");
        try
        {
            using Mat blank = new(600, 900, MatType.CV_8UC3, Scalar.White);
            File.WriteAllBytes(path, blank.ImEncode(".png"));
            RecognitionResult empty = analyzer.Analyze(path);
            bool blankRejected = !empty.Registration.Success && empty.Colors.Count == 0;
            string referencePath = Path.Combine(assets, "board_reference_master_clean.png");
            RecognitionResult reference = analyzer.Analyze(referencePath);
            bool referenceNoHigh = reference.Registration.Success && reference.Colors.Values
                .SelectMany(color => ArkNovaConfidencePolicy.Evaluate(color.Tracks, policy).Values)
                .All(prediction => prediction.Confidence != "High");
            using Mat image = Cv2.ImDecode(File.ReadAllBytes(referencePath), ImreadModes.Color);
            using Mat partial = new(image, new Rect(0, 0, image.Width / 2, image.Height));
            File.WriteAllBytes(path, partial.ImEncode(".png"));
            RecognitionResult partialResult = analyzer.Analyze(path);
            return new(blankRejected, !partialResult.Registration.Success && partialResult.Colors.Count == 0, referenceNoHigh);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

internal sealed record RegistrationChecks(bool BlankRejected, bool PartialRejected, bool EmptyReferenceNoHigh)
{
    public bool Passed => BlankRejected && PartialRejected && EmptyReferenceNoHigh;
}

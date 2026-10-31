using System.Text.Json;
using BoardgameTracker.Tools.Games.ArkNova.NativeVisionSpike;

internal static class ProbeCheck
{
    // Numerical parity limits, fixed before the first native run. Not CV thresholds.
    public static async Task<string> RunAsync(Func<string, Stream> open)
    {
        using Stream referenceStream = open("reference.bgr");
        using Stream photoStream = open("photo.bgr");
        ArkNovaVisionImage reference = ReadImage(referenceStream);
        ArkNovaVisionImage photo = ReadImage(photoStream);
        IArkNovaVisionEngine engine = new NativeArkNovaVisionEngine();
        ArkNovaRegistrationProbe result = engine.ProbeRegistration(reference, photo);
        using Stream goldenStream = open("reference-warp.bgr");
        using MemoryStream goldenBuffer = new();
        await goldenStream.CopyToAsync(goldenBuffer);
        byte[] golden = goldenBuffer.ToArray();
        if (golden.Length != result.WarpedBgr.Length)
            throw new InvalidOperationException("Warp length differs from reference.");
        long sum = 0;
        int maximum = 0;
        for (int index = 0; index < golden.Length; index++)
        {
            int difference = Math.Abs(golden[index] - result.WarpedBgr[index]);
            sum += difference;
            maximum = Math.Max(maximum, difference);
        }
        double mean = sum / (double)golden.Length;
        using Stream expectedStream = open("reference.json");
        using JsonDocument expected = await JsonDocument.ParseAsync(expectedStream);
        JsonElement registration = expected.RootElement.GetProperty("registration");
        bool passed = result.Matches == registration.GetProperty("matches").GetInt32()
            && result.Inliers == registration.GetProperty("inliers").GetInt32()
            && Math.Abs(result.ReprojectionP95 - registration.GetProperty("reprojectionP95").GetDouble()) <= 1e-6
            && mean <= 0.1 && maximum <= 8;
        return FormattableString.Invariant($"{{\"passed\":{passed.ToString().ToLowerInvariant()},\"matches\":{result.Matches},\"inliers\":{result.Inliers},\"p95\":{result.ReprojectionP95:R},\"warpMeanAbsoluteError\":{mean:R},\"warpMaxAbsoluteError\":{maximum}}}");
    }

    private static ArkNovaVisionImage ReadImage(Stream stream)
    {
        using BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (!reader.ReadBytes(4).AsSpan().SequenceEqual("ANB1"u8))
            throw new InvalidDataException("Invalid parity fixture header.");
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        int stride = reader.ReadInt32();
        int length = checked(stride * height);
        byte[] bytes = reader.ReadBytes(length);
        ArkNovaVisionImage image = new(bytes, width, height, stride);
        image.Validate();
        return image;
    }
}

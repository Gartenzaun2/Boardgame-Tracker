using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using OpenCvSharp;

namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>Exports one existing development fixture for native registration parity, never the final stress image.</summary>
internal static class ArkNovaNativeReference
{
    public static int Export(string assets, string output)
    {
        Directory.CreateDirectory(output);
        string referencePath = Path.Combine(assets, "board_reference_master_clean.png");
        string photoPath = Path.Combine(assets, "IMG_4257.JPG");
        using Mat reference = Cv2.ImDecode(File.ReadAllBytes(referencePath), ImreadModes.Color);
        using Mat photo = Cv2.ImDecode(File.ReadAllBytes(photoPath), ImreadModes.Color);
        WriteImage(reference, Path.Combine(output, "reference.bgr"));
        WriteImage(photo, Path.Combine(output, "photo.bgr"));
        byte[]? warpBytes = null;
        ArkNovaRankingCapture capture = new()
        {
            FrameObserver = frame => warpBytes = ReadBytes(frame.Warped)
        };
        using ArkNovaPhotoAnalyzer analyzer = new(assets);
        RecognitionResult result = analyzer.Analyze(photoPath, rankingCapture: capture);
        if (!result.Registration.Success || warpBytes is null)
            throw new InvalidOperationException("Known fixture failed registration.");
        File.WriteAllBytes(Path.Combine(output, "reference-warp.bgr"), warpBytes);
        File.WriteAllText(Path.Combine(output, "reference.json"), JsonSerializer.Serialize(new
        {
            fixture = "IMG_4257.JPG", openCv = Cv2.GetVersionString(),
            referenceSha256 = Hash(File.ReadAllBytes(referencePath)), photoSha256 = Hash(File.ReadAllBytes(photoPath)),
            registration = result.Registration, warpSha256 = Hash(warpBytes), warpBytes = warpBytes.Length,
            scope = "Normalized BGR input; existing A registration and warp only. No stress re-evaluation or threshold changes."
        }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return 0;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static byte[] ReadBytes(Mat image)
    {
        using Mat contiguous = image.Clone();
        byte[] bytes = new byte[checked(contiguous.Rows * contiguous.Cols * 3)];
        Marshal.Copy(contiguous.Data, bytes, 0, bytes.Length);
        return bytes;
    }

    private static void WriteImage(Mat image, string path)
    {
        using BinaryWriter writer = new(File.Create(path));
        writer.Write("ANB1"u8);
        writer.Write(image.Width);
        writer.Write(image.Height);
        writer.Write(checked(image.Width * 3));
        writer.Write(ReadBytes(image));
    }
}

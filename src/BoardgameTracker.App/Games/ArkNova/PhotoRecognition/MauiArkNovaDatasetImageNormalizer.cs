using System.Security.Cryptography;
using BoardgameTracker.Application.Games.ArkNova.PhotoDataset;

namespace BoardgameTracker.App.Games.ArkNova.PhotoRecognition;

public sealed class MauiArkNovaDatasetImageNormalizer : IArkNovaDatasetImageNormalizer
{
    public Task<ArkNovaDatasetImage> NormalizeAsync(byte[] image, CancellationToken cancellationToken = default) =>
        Task.Run(() => Normalize(image), cancellationToken);

    private static ArkNovaDatasetImage Normalize(byte[] image)
    {
#if ANDROID
        using Java.Nio.ByteBuffer data = Java.Nio.ByteBuffer.Wrap(image)!;
        using Android.Graphics.ImageDecoder.Source source = Android.Graphics.ImageDecoder.CreateSource(data);
        // ImageDecoder applies EXIF orientation, including mirrored orientations.
        using DatasetHeaderListener listener = new();
        using Android.Graphics.Bitmap bitmap = Android.Graphics.ImageDecoder.DecodeBitmap(source, listener);
        int[] pixels = new int[bitmap.Width * bitmap.Height];
        bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);
        byte[] content = new byte[pixels.Length * sizeof(int)];
        Buffer.BlockCopy(pixels, 0, content, 0, content.Length);
        using MemoryStream jpeg = new();
        if (!bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!, 90, jpeg))
            throw new InvalidDataException("Cannot encode dataset image.");
        return new(jpeg.ToArray(), Hash(bitmap.Width, bitmap.Height, content));
#elif IOS
        using Foundation.NSData data = Foundation.NSData.FromArray(image);
        using UIKit.UIImage original = UIKit.UIImage.LoadFromData(data) ?? throw new InvalidDataException("Unsupported dataset photo.");
        double scale = Math.Min(1, 1600d / Math.Max(original.Size.Width, original.Size.Height));
        CoreGraphics.CGSize size = new(Math.Max(1, Math.Round(original.Size.Width * scale)), Math.Max(1, Math.Round(original.Size.Height * scale)));
        using UIKit.UIGraphicsImageRendererFormat format = new() { Scale = 1, Opaque = true };
        using UIKit.UIGraphicsImageRenderer renderer = new(size, format);
        using UIKit.UIImage normalized = renderer.CreateImage(_ => original.Draw(new CoreGraphics.CGRect(CoreGraphics.CGPoint.Empty, size)));
        using CoreGraphics.CGImage cgImage = normalized.CGImage ?? throw new InvalidDataException("Cannot read dataset pixels.");
        using Foundation.NSData pixels = cgImage.DataProvider.CopyData() ?? throw new InvalidDataException("Cannot read dataset pixels.");
        // Renderer rows may have unused alignment bytes. Only actual pixels define image identity.
        byte[] dataBytes = pixels.ToArray();
        int rowLength = ((int)cgImage.Width * (int)cgImage.BitsPerPixel + 7) / 8;
        byte[] content = new byte[rowLength * (int)cgImage.Height];
        for (int row = 0; row < (int)cgImage.Height; row++)
            Buffer.BlockCopy(dataBytes, row * (int)cgImage.BytesPerRow, content, row * rowLength, rowLength);
        using Foundation.NSData jpeg = normalized.AsJPEG(0.9f) ?? throw new InvalidDataException("Cannot encode dataset image.");
        return new(jpeg.ToArray(), Hash((int)cgImage.Width, (int)cgImage.Height, content));
#else
        throw new PlatformNotSupportedException();
#endif
    }

#if ANDROID
    private sealed class DatasetHeaderListener : Java.Lang.Object, Android.Graphics.ImageDecoder.IOnHeaderDecodedListener
    {
        public void OnHeaderDecoded(Android.Graphics.ImageDecoder decoder, Android.Graphics.ImageDecoder.ImageInfo info,
            Android.Graphics.ImageDecoder.Source source)
        {
            double scale = Math.Min(1, 1600d / Math.Max(info.Size.Width, info.Size.Height));
            decoder.SetTargetSize(Math.Max(1, (int)Math.Round(info.Size.Width * scale)), Math.Max(1, (int)Math.Round(info.Size.Height * scale)));
            decoder.Allocator = Android.Graphics.ImageDecoderAllocator.Software;
        }
    }
#endif

    private static string Hash(int width, int height, byte[] pixels)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(BitConverter.GetBytes(width));
        hash.AppendData(BitConverter.GetBytes(height));
        hash.AppendData(pixels);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    public static string GetLocalDirectory()
    {
#if ANDROID
        return Path.Combine(Android.App.Application.Context.NoBackupFilesDir!.AbsolutePath, "ArkNovaPhotoDataset");
#elif IOS
        return Path.Combine(FileSystem.AppDataDirectory, "ArkNovaPhotoDataset");
#else
        throw new PlatformNotSupportedException();
#endif
    }

    public static void ExcludeFromBackup()
    {
#if IOS
        using Foundation.NSUrl url = Foundation.NSUrl.FromFilename(GetLocalDirectory());
        if (!url.SetResource(Foundation.NSUrl.IsExcludedFromBackupKey, Foundation.NSNumber.FromBoolean(true), out Foundation.NSError error))
            throw new IOException("Cannot exclude dataset from backup.", new Foundation.NSErrorException(error));
#endif
    }
}

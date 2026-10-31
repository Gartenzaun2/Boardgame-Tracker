using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Games.ArkNova.PhotoRecognition;

/// <summary>The system camera and system picker both produce the same orientation-aware analyzer input.</summary>
public sealed class MauiArkNovaPhotoInput(IMediaPicker picker) : IArkNovaPhotoInput
{
    public async Task<byte[]?> ReadAsync(ArkNovaPhotoSource source, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        if (source == ArkNovaPhotoSource.Camera)
            return await MainThread.InvokeOnMainThreadAsync(() =>
                (Platform.CurrentActivity as MainActivity ?? throw new InvalidOperationException("Camera activity unavailable."))
                    .CaptureArkNovaPhotoAsync(cancellationToken));
#endif
        FileResult? selected = await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (source == ArkNovaPhotoSource.Camera)
            {
                if (!picker.IsCaptureSupported)
                    throw new FeatureNotSupportedException("Camera unavailable.");
                return await picker.CapturePhotoAsync(new MediaPickerOptions { Title = AppResources.PhotoRecognition });
            }
            List<FileResult> photos = await picker.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1, Title = AppResources.PhotoRecognition });
            return photos.FirstOrDefault();
        });
        if (selected is null)
            return null;
        cancellationToken.ThrowIfCancellationRequested();
        using Stream stream = await selected.OpenReadAsync();
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer, cancellationToken);
        byte[] bytes = buffer.ToArray();
        // JPEG/PNG use OpenCV's shared EXIF/BGR decoder on both platforms. HEIF
        // and other platform-supported formats are normalized without resizing.
        bool jpeg = bytes.Length > 2 && bytes[0] == 0xff && bytes[1] == 0xd8;
        bool png = bytes.Length > 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        if (jpeg || png)
            return bytes;
#if IOS
        return await MainThread.InvokeOnMainThreadAsync(() =>
        {
            using Foundation.NSData data = Foundation.NSData.FromArray(bytes);
            using UIKit.UIImage image = UIKit.UIImage.LoadFromData(data) ?? throw new InvalidDataException("Unsupported photo.");
            using UIKit.UIGraphicsImageRendererFormat format = new() { Scale = 1 };
            using UIKit.UIGraphicsImageRenderer renderer = new(image.Size, format);
            using UIKit.UIImage normalized = renderer.CreateImage(_ => image.Draw(new CoreGraphics.CGRect(CoreGraphics.CGPoint.Empty, image.Size)));
            using Foundation.NSData encoded = normalized.AsPNG() ?? throw new InvalidDataException("Cannot normalize photo.");
            return encoded.ToArray();
        });
#elif ANDROID
        return await Task.Run(() =>
        {
            using Java.Nio.ByteBuffer data = Java.Nio.ByteBuffer.Wrap(bytes)!;
            using Android.Graphics.ImageDecoder.Source imageSource = Android.Graphics.ImageDecoder.CreateSource(data);
            using Android.Graphics.Bitmap bitmap = Android.Graphics.ImageDecoder.DecodeBitmap(imageSource);
            using MemoryStream encoded = new();
            if (!bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, encoded))
                throw new InvalidDataException("Cannot normalize photo.");
            return encoded.ToArray();
        }, cancellationToken);
#else
        throw new InvalidDataException("Unsupported photo encoding.");
#endif
    }
}

global using Microsoft.Maui.ApplicationModel;
global using Microsoft.Maui.Media;
global using Microsoft.Maui.Storage;

// These doubles exercise the linked adapter's resource scopes, not native UI or codecs.
namespace Microsoft.Maui.ApplicationModel
{
    public static class MainThread
    {
        public static Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> action) => action();
    }

    public sealed class FeatureNotSupportedException(string message) : NotSupportedException(message);
}

namespace Microsoft.Maui.Media
{
    public interface IMediaPicker
    {
        bool IsCaptureSupported { get; }
        Task<FileResult?> CapturePhotoAsync(MediaPickerOptions options);
        Task<List<FileResult>> PickPhotosAsync(MediaPickerOptions options);
    }

    public sealed class MediaPickerOptions
    {
        public string? Title { get; set; }
        public int SelectionLimit { get; set; }
    }
}

namespace Microsoft.Maui.Storage
{
    public class FileResult(Func<Task<Stream>> openRead)
    {
        public Task<Stream> OpenReadAsync() => openRead();
    }
}

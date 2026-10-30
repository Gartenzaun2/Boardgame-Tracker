using Foundation;
using UIKit;
using BoardgameTracker.Application.History.Transfer;

namespace BoardgameTracker.App.Features.History.Export;

public sealed partial class HistoryExportFileSaver
{
    public async partial Task SaveAsync(BgtExportContent content)
    {
        string directory = Path.Combine(FileSystem.CacheDirectory, Guid.CreateVersion7().ToString());
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, content.FileName);
        try
        {
            await File.WriteAllBytesAsync(path, content.Bytes.ToArray());
            UIViewController owner = Platform.GetCurrentUIViewController()
                ?? throw new InvalidOperationException("No current iOS view controller.");
            using NSUrl source = NSUrl.FromFilename(path);
            using UIDocumentPickerViewController picker = new([source], asCopy: true);
            TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using ExportDocumentDelegate pickerDelegate = new(completion);
            picker.Delegate = pickerDelegate;
            picker.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
            picker.ShouldShowFileExtensions = true;
            await owner.PresentViewControllerAsync(picker, true);
            await completion.Task;
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }

    private sealed class ExportDocumentDelegate(TaskCompletionSource<bool> completion) : UIDocumentPickerDelegate
    {
        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls) => completion.TrySetResult(true);
        public override void WasCancelled(UIDocumentPickerViewController controller) => completion.TrySetResult(false);
    }
}

using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.History.Export;

public sealed class SaveHistoryExportTarget(HistoryExportFileSaver saver) : IHistoryExportTarget
{
    public string DisplayName => AppResources.Save;
    public Task WriteAsync(BgtExportContent content) => saver.SaveAsync(content);
}

public sealed partial class HistoryExportFileSaver
{
    public partial Task SaveAsync(BgtExportContent content);
}

public sealed class ShareHistoryExportTarget : IHistoryExportTarget
{
    public string DisplayName => AppResources.TransferShare;
    public async Task WriteAsync(BgtExportContent content)
    {
        string path = Path.Combine(FileSystem.CacheDirectory, content.FileName);
        await File.WriteAllBytesAsync(path, content.Bytes.ToArray());
        // Receivers can read after the sheet closes; retain the platform-managed cache file.
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = AppResources.TransferExportTitle,
            File = new ShareFile(path, "application/json")
        });
    }
}

using BoardgameTracker.Application.History.Transfer;

namespace BoardgameTracker.App.Features.History.Export;

public sealed partial class HistoryExportFileSaver
{
    public async partial Task SaveAsync(BgtExportContent content)
    {
        MainActivity activity = Platform.CurrentActivity as MainActivity
            ?? throw new InvalidOperationException("No current Android activity.");
        Android.Net.Uri? destination = await activity.CreateExportDocumentAsync(content.FileName);
        if (destination is null)
            return;
        await using Stream output = activity.ContentResolver!.OpenOutputStream(destination, "wt")
            ?? throw new IOException("The selected document could not be opened.");
        await output.WriteAsync(content.Bytes);
        await output.FlushAsync();
    }
}

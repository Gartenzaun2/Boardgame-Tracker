using BoardgameTracker.Application.History.Transfer;

namespace BoardgameTracker.App.Features.History.Export;

/// <summary>Separates native save/share destinations from snapshot creation so the common flow can reuse one snapshot and tests can replace platform transport.</summary>
public interface IHistoryExportTarget {
    string DisplayName { get; }
    Task WriteAsync(BgtExportContent content);
}

/// <summary>Creates one export snapshot and lets the selected native target handle writing; destination cancellation is an expected end of the flow.</summary>
public sealed class HistoryExportFlow(HistoryTransferUseCase transfer, IEnumerable<IHistoryExportTarget> targets) {
    public static bool CanExport(IReadOnlyCollection<Guid> gameSessionIds)
        => gameSessionIds.Count != 0;

    public async Task RunAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, Func<IReadOnlyList<IHistoryExportTarget>, Task<int>> chooseTarget) {
        Guid[] selectedIds = gameSessionIds.ToArray();
        if (!CanExport(selectedIds))
            return;

        BgtExportContent content = await transfer.CreateExportContentAsync(selectedIds, appVersion);
        IHistoryExportTarget[] choices = targets.ToArray();
        int selected = await chooseTarget(choices);
        if (selected < 0)
            return;

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(selected, choices.Length);

        try {
            await choices[selected].WriteAsync(content);
        } catch (OperationCanceledException) {
            // Native destination cancellation is a normal end of this export.
        }
    }
}

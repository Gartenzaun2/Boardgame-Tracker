namespace BoardgameTracker.Application.History.Transfer;

public sealed record TransferPreview(int GamesExisting, int ReferencesExisting, int ReferencesNew);

public interface IHistoryTransferStore
{
    Task<BgtFile> ExportAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default);
    Task<TransferSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default);
    Task<TransferPreview> PreviewAsync(ImportPlan plan, CancellationToken cancellationToken = default);
    Task CommitAsync(ImportPlan plan, CancellationToken cancellationToken = default);
}

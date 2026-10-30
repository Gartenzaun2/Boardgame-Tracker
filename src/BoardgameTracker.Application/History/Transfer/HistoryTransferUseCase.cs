using System.Text.Json;

namespace BoardgameTracker.Application.History.Transfer;

public sealed class HistoryTransferUseCase(HistoryTransferPersistence persistence, BgtFormat format)
{
    public async Task<BgtFile> ExportAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default)
    {
        BgtFile file = await persistence.ExportAsync(gameSessionIds, appVersion, cancellationToken);
        format.Validate(file);
        return file;
    }
    public async Task<BgtExportContent> CreateExportContentAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default)
    {
        BgtFile file = await ExportAsync(gameSessionIds, appVersion, cancellationToken);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(file, BgtFormat.JsonOptions);
        if (bytes.Length > BgtFormat.MaximumBytes)
            throw new InvalidDataException("SizeLimit");
        return new($"boardgame-tracker-{Guid.CreateVersion7()}.bgt", bytes);
    }
    public async Task<ImportPlan> AnalyzeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        BgtFile file = await format.ReadAsync(stream, cancellationToken);
        return ImportPlanner.Analyze(file, await persistence.ReadSnapshotAsync(cancellationToken));
    }
    public Task<TransferPreview> PreviewAsync(ImportPlan plan, CancellationToken cancellationToken = default) => persistence.PreviewAsync(plan, cancellationToken);
    public Task CommitAsync(ImportPlan plan, CancellationToken cancellationToken = default)
    {
        format.Validate(plan.Source);
        if (!plan.CanCommit)
            throw new InvalidOperationException("UnresolvedImport");
        return persistence.CommitAsync(plan, cancellationToken);
    }
}

public sealed record TransferPreview(int GamesExisting, int ReferencesExisting, int ReferencesNew);

using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoardgameTracker.Application.Tests.Persistence;

/// <summary>Controls a real EF save to exercise UI serialization and failure recovery.</summary>
internal sealed class SaveProbe : SaveChangesInterceptor
{
    public int SaveCount { get; private set; }
    public bool BlockFirstSave { get; set; }
    public bool FailNextSave { get; set; }
    public TaskCompletionSource FirstSaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseFirstSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _activeSaves;
    public int MaxConcurrentSaves { get; private set; }
    public bool ActiveSaveWasCancelled { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        _activeSaves++;
        MaxConcurrentSaves = Math.Max(MaxConcurrentSaves, _activeSaves);
        if (BlockFirstSave && SaveCount == 1)
        {
            FirstSaveStarted.TrySetResult();
            await ReleaseFirstSave.Task.WaitAsync(TestContext.Current.CancellationToken);
            ActiveSaveWasCancelled = cancellationToken.IsCancellationRequested;
        }
        if (FailNextSave)
        {
            FailNextSave = false;
            _activeSaves--;
            throw new InvalidOperationException("Simulated write failure.");
        }
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        _activeSaves--;
        return ValueTask.FromResult(result);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _activeSaves--;
        return Task.CompletedTask;
    }
}

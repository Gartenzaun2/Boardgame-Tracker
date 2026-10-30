using System.Text.Json;
using BoardgameTracker.App.Features.History.Export;
using BoardgameTracker.Application.History.Transfer;

namespace BoardgameTracker.Application.Tests.History.Transfer;

public sealed class ExportDestinationTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public async Task SaveOrShareReceivesTheSinglePreparedContent_WithoutInvokingOtherDestinations(int choice, bool cancel)
    {
        Store store = new();
        Target save = new("Save", cancel);
        Target share = new("Share", cancel);
        HistoryExportFlow flow = new(new(store, new BgtFormat([])), [save, share]);
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid()];
        int dialogCalls = 0;
        await flow.RunAsync(ids, "test", options =>
        {
            dialogCalls++;
            Assert.Equal(1, store.Calls);
            Assert.Equal(2, options.Count);
            Assert.Empty(save.Received);
            Assert.Empty(share.Received);
            return Task.FromResult(choice);
        });
        BgtExportContent content = Assert.Single(choice == 0 ? save.Received : share.Received);
        Assert.Empty(choice == 0 ? share.Received : save.Received);
        Assert.Equal(1, dialogCalls);
        Assert.Equal(1, store.Calls);
        Assert.Equal(ids, Assert.Single(store.RequestedIds));
        Assert.EndsWith(".bgt", content.FileName);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(store.File, BgtFormat.JsonOptions), content.Bytes.ToArray());
    }

    [Fact]
    public async Task CancelingDestinationDialogDoesNotInvokeTransport()
    {
        Store store = new();
        Target save = new("Save");
        HistoryExportFlow flow = new(new(store, new BgtFormat([])), [save]);
        await flow.RunAsync([Guid.NewGuid()], "test", _ => Task.FromResult(-1));
        Assert.Equal(1, store.Calls);
        Assert.Empty(save.Received);
    }

    [Fact]
    public async Task EmptyHistoryDisablesExportAndDoesNotGenerateOrChoose()
    {
        Store store = new();
        HistoryExportFlow flow = new(new(store, new BgtFormat([])), []);
        Assert.False(HistoryExportFlow.CanExport([]));
        Assert.True(HistoryExportFlow.CanExport([Guid.NewGuid()]));
        await flow.RunAsync([], "test", _ => throw new InvalidOperationException("Dialog must not open."));
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task CurrentFilterSelectionIsCapturedBeforeAwait_AndNextExportUsesNewFilters()
    {
        Store store = new();
        Target target = new("Save");
        HistoryExportFlow flow = new(new(store, new BgtFormat([])), [target]);
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        List<Guid> visible = [first];
        await flow.RunAsync(visible, "test", _ =>
        {
            visible.Clear();
            visible.Add(second);
            return Task.FromResult(0);
        });
        await flow.RunAsync(visible, "test", _ => Task.FromResult(0));
        Assert.Equal(new[] { first }, store.RequestedIds[0]);
        Assert.Equal(new[] { second }, store.RequestedIds[1]);
        Assert.Equal(2, store.Calls);
    }

    [Fact]
    public async Task ThirdDestinationUsesSameContentContractWithoutExporterChanges()
    {
        Store store = new();
        Target first = new("Save");
        Target second = new("Share");
        Target third = new("Future transport");
        HistoryExportFlow flow = new(new(store, new BgtFormat([])), [first, second, third]);
        await flow.RunAsync([Guid.NewGuid()], "test", choices =>
        {
            Assert.Equal("Future transport", choices[2].DisplayName);
            return Task.FromResult(2);
        });
        Assert.Single(third.Received);
        Assert.Empty(first.Received);
        Assert.Empty(second.Received);
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public void TransportsHaveNoSerializationOrMatchingRules()
    {
        System.Reflection.Assembly assembly = typeof(ExportDestinationTests).Assembly;
        string[] names = assembly.GetManifestResourceNames().Where(name => name.StartsWith("ExportTransport.", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, names.Length);
        foreach (string name in names)
        {
            using Stream stream = assembly.GetManifestResourceStream(name)!;
            using StreamReader reader = new(stream);
            string source = reader.ReadToEnd();
            Assert.DoesNotContain("JsonSerializer", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ImportPlanner", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BgtFormat", source, StringComparison.Ordinal);
        }
    }

    private sealed class Target(string name, bool cancel = false) : IHistoryExportTarget
    {
        public string DisplayName => name;
        public List<BgtExportContent> Received { get; } = [];
        public Task WriteAsync(BgtExportContent content)
        {
            Received.Add(content);
            return cancel ? Task.FromCanceled(new CancellationToken(true)) : Task.CompletedTask;
        }
    }
    private sealed class Store : IHistoryTransferStore
    {
        public int Calls { get; private set; }
        public List<Guid[]> RequestedIds { get; } = [];
        public BgtFile File { get; } = new(1, "test", DateTimeOffset.UtcNow, [], [], [], []);
        public Task<BgtFile> ExportAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default)
        {
            Calls++;
            RequestedIds.Add(gameSessionIds.ToArray());
            return Task.FromResult(File);
        }
        public Task<TransferSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TransferPreview> PreviewAsync(ImportPlan plan, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CommitAsync(ImportPlan plan, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

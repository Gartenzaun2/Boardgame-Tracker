using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Games.ArkNova.History.Transfer;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
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
        await using ExportDatabase store = await ExportDatabase.CreateAsync();
        Target save = new("Save", cancel);
        Target share = new("Share", cancel);
        HistoryExportFlow flow = new(new(new HistoryTransferPersistence(store, [new ArkNovaTransferPersistence()]), new BgtFormat([new ArkNovaTransferSchema()])), [save, share]);
        Guid[] ids = store.Ids;
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
        Assert.Equal(ids.Order(), Read(content).GameSessions.Select(session => session.Id).Order());
        Assert.EndsWith(".bgt", content.FileName);
        Assert.Equal("test", Read(content).AppVersion);
    }

    [Fact]
    public async Task CancelingDestinationDialogDoesNotInvokeTransport()
    {
        await using ExportDatabase store = await ExportDatabase.CreateAsync();
        Target save = new("Save");
        HistoryExportFlow flow = new(new(new HistoryTransferPersistence(store, [new ArkNovaTransferPersistence()]), new BgtFormat([new ArkNovaTransferSchema()])), [save]);
        await flow.RunAsync([store.Ids[0]], "test", _ => Task.FromResult(-1));
        Assert.Equal(1, store.Calls);
        Assert.Empty(save.Received);
    }

    [Fact]
    public async Task EmptyHistoryDisablesExportAndDoesNotGenerateOrChoose()
    {
        await using ExportDatabase store = await ExportDatabase.CreateAsync();
        HistoryExportFlow flow = new(new(new HistoryTransferPersistence(store, [new ArkNovaTransferPersistence()]), new BgtFormat([new ArkNovaTransferSchema()])), []);
        Assert.False(HistoryExportFlow.CanExport([]));
        Assert.True(HistoryExportFlow.CanExport([Guid.NewGuid()]));
        await flow.RunAsync([], "test", _ => throw new InvalidOperationException("Dialog must not open."));
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task CurrentFilterSelectionIsCapturedBeforeAwait_AndNextExportUsesNewFilters()
    {
        await using ExportDatabase store = await ExportDatabase.CreateAsync();
        Target target = new("Save");
        HistoryExportFlow flow = new(new(new HistoryTransferPersistence(store, [new ArkNovaTransferPersistence()]), new BgtFormat([new ArkNovaTransferSchema()])), [target]);
        Guid first = store.Ids[0];
        Guid second = store.Ids[1];
        List<Guid> visible = [first];
        await flow.RunAsync(visible, "test", _ =>
        {
            visible.Clear();
            visible.Add(second);
            return Task.FromResult(0);
        });
        await flow.RunAsync(visible, "test", _ => Task.FromResult(0));
        Assert.Equal(new[] { first }, Read(target.Received[0]).GameSessions.Select(session => session.Id));
        Assert.Equal(new[] { second }, Read(target.Received[1]).GameSessions.Select(session => session.Id));
        Assert.Equal(2, store.Calls);
    }

    [Fact]
    public async Task ThirdDestinationUsesSameContentContractWithoutExporterChanges()
    {
        await using ExportDatabase store = await ExportDatabase.CreateAsync();
        Target first = new("Save");
        Target second = new("Share");
        Target third = new("Future transport");
        HistoryExportFlow flow = new(new(new HistoryTransferPersistence(store, [new ArkNovaTransferPersistence()]), new BgtFormat([new ArkNovaTransferSchema()])), [first, second, third]);
        await flow.RunAsync([store.Ids[0]], "test", choices =>
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
    private static BgtFile Read(BgtExportContent content) => JsonSerializer.Deserialize<BgtFile>(content.Bytes.Span, BgtFormat.JsonOptions)!;

    private sealed class ExportDatabase(SliceDatabase database) : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        public int Calls { get; private set; }
        public Guid[] Ids { get; private set; } = [];
        public BoardgameTrackerDbContext CreateDbContext()
        {
            Calls++;
            return database.CreateDbContext();
        }
        public static async Task<ExportDatabase> CreateAsync()
        {
            SliceDatabase database = await SliceDatabase.CreateAsync();
            List<Guid> ids = [];
            for (int index = 0; index < 2; index++)
            {
                GameSession session = await database.AddSessionAsync(scoring: true);
                await using BoardgameTrackerDbContext db = database.CreateDbContext();
                session = await db.GameSessions.Include(s => s.Players).SingleAsync(s => s.Id == session.Id, TestContext.Current.CancellationToken);
                foreach (GameSessionPlayer player in session.Players)
                {
                    player.SetPlacement(1);
                    int plan = player.PlayerId == database.First.Id ? 1 : 3;
                    ArkNovaGameSessionPlayer values = new(session.Id, player.PlayerId,
                        plan == 1 ? PlayerColor.Blue : PlayerColor.Red, plan, plan + 1, plan);
                    values.SetScoreValues(70, 20, null);
                    db.ArkNovaGameSessionPlayers.Add(values);
                }
                session.Complete(DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                ids.Add(session.Id);
            }
            return new(database) { Ids = ids.ToArray() };
        }
        public ValueTask DisposeAsync() => database.DisposeAsync();
    }
}

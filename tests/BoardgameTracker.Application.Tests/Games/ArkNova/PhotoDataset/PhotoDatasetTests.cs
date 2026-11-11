using System.IO.Compression;
using System.Text.Json;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ArkNova.PhotoRecognition;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;
using BoardgameTracker.Application.Games.ArkNova.PhotoDataset;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.PhotoDataset;

public sealed class PhotoDatasetTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ark-nova-dataset-test-" + Guid.NewGuid());
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private ArkNovaPhotoDataset Create(IArkNovaDatasetImageNormalizer? normalizer = null) =>
        new(_directory, normalizer ?? new Normalizer(), "test-version", NullLogger<ArkNovaPhotoDataset>.Instance);

    [Fact]
    public async Task DefaultEnabledAndPreferenceSurvivesDeletionAndRestart()
    {
        ArkNovaPhotoDataset dataset = Create();
        Assert.True((await dataset.GetSummaryAsync()).Enabled);
        await dataset.SetEnabledAsync(false);
        Assert.Null(await dataset.CaptureAsync(Guid.NewGuid(), "camera", [1], await PredictionAsync(), Token));
        await dataset.DeleteSamplesAsync();
        Assert.False((await Create().GetSummaryAsync()).Enabled);
    }

    [Fact]
    public async Task DeduplicationUsesNormalizedContentWithinTheSameScoringAndPredictionIsASnapshot()
    {
        ArkNovaPhotoDataset dataset = Create();
        Guid session = Guid.NewGuid();
        ArkNovaPhotoRecognitionResult prediction = await PredictionAsync();
        Guid? first = await dataset.CaptureAsync(session, "camera", [1, 11], prediction, Token);
        // The fake normalizer ignores the second byte, like stripped EXIF metadata.
        Assert.Equal(first, await dataset.CaptureAsync(session, "library", [1, 99], prediction, Token));
        Assert.Equal(first, await Create().CaptureAsync(session, "library", [1, 88], prediction, Token));
        Assert.NotEqual(first, await dataset.CaptureAsync(session, "camera", [2], prediction, Token));
        Assert.NotEqual(first, await dataset.CaptureAsync(Guid.NewGuid(), "camera", [1], prediction, Token));
        prediction.Colors["red"].Tracks.Clear();
        Assert.Equal(3, (await dataset.GetSummaryAsync()).Count);
        ArkNovaDatasetManifest exported = await ManifestAsync(dataset);
        Assert.All(exported.Samples, sample => Assert.Equal(63,
            sample.Prediction.GetProperty("colors").GetProperty("red").GetProperty("tracks").GetProperty("attractiveness").GetProperty("value").GetInt32()));
        Assert.True((await Create().GetSummaryAsync()).Bytes > 0);
    }

    [Fact]
    public async Task FlowRetainsCancelledRecognizedAttemptsButRejectsEmptyOrFailedPredictions()
    {
        ArkNovaPhotoDataset dataset = Create();
        Guid session = Guid.NewGuid();
        ArkNovaPhotoReviewFlow flow = new(new Input(), new(new Engine()), dataset);
        Assert.NotNull(await flow.AnalyzeAsync(ArkNovaPhotoSource.Camera, Token, session));
        await flow.SetReviewStatusAsync("cancelled");
        await flow.SetReviewStatusAsync("confirmed"); // Closing a later source menu cannot relabel the old review.
        Assert.Equal("cancelled", Assert.Single((await ManifestAsync(dataset)).Samples).ReviewStatus);
        Assert.Null(await dataset.CaptureAsync(session, "camera", [2],
            new(new(false, 0, 0, null, 0, 0, []), new Dictionary<string, ColorScores>()), Token));
        Assert.Null(await dataset.CaptureAsync(session, "camera", [3],
            new(new(true, 50, 40, 1, 1, 1, []), new Dictionary<string, ColorScores>()), Token));
        ArkNovaPhotoReviewFlow cancelledPicker = new(new Input { Cancel = true }, new(new Engine()), dataset);
        Assert.Null(await cancelledPicker.AnalyzeAsync(ArkNovaPhotoSource.Library, Token, session));
        Assert.Equal(1, (await dataset.GetSummaryAsync()).Count);
    }

    [Fact]
    public async Task OnlySuccessfulCompletionLabelsAllRetakesAndHistoryCorrectionsCannotChangeThem()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        var session = await db.AddSessionAsync(scoring: true);
        await using (var context = db.CreateDbContext())
        {
            context.AddRange(new ArkNovaGameSessionPlayer(session.Id, db.First.Id, 1), new BoardgameTracker.Core.GameSessions.GameSessionPlayerColor(session.Id, db.First.Id, PlayerColor.Red),
                new ArkNovaGameSessionPlayer(session.Id, db.Second.Id, 2), new BoardgameTracker.Core.GameSessions.GameSessionPlayerColor(session.Id, db.Second.Id, PlayerColor.Blue));
            await context.SaveChangesAsync(Token);
        }
        ArkNovaPhotoDataset dataset = Create();
        ArkNovaPhotoRecognitionResult prediction = await PredictionAsync();
        await dataset.CaptureAsync(session.Id, "camera", [1], prediction, Token);
        await dataset.CaptureAsync(session.Id, "camera", [2], prediction, Token);
        Guid otherSession = Guid.NewGuid();
        await dataset.CaptureAsync(otherSession, "library", [1], prediction, Token);
        ArkNovaScoringUseCase scoring = new(db, new(db, TestGameContent.Create()), dataset);
        await scoring.SaveConfirmedPhotoScoresAsync(session.Id,
            [new(db.First.Id, 70, 20, 8), new(db.Second.Id, 60, 19, 7)], Token);
        await dataset.SetReviewStatusAsync((await ManifestAsync(dataset)).Samples[0].SampleId, "confirmed");
        Assert.All((await ManifestAsync(dataset)).Samples, sample => Assert.Null(sample.GroundTruth));
        await Assert.ThrowsAsync<ArgumentException>(() => scoring.CompleteAsync(session.Id,
            new Dictionary<Guid, int>(), DateTimeOffset.UtcNow, Token));
        Assert.All((await ManifestAsync(dataset)).Samples, sample => Assert.Null(sample.GroundTruth));
        var plan = await scoring.PrepareCompletionAsync(session.Id, Token);
        await dataset.SetEnabledAsync(false); // Retained samples must still receive labels when collection is off.
        await scoring.CompleteAsync(session.Id, plan.DefaultPlacements, DateTimeOffset.UtcNow, Token);
        ArkNovaDatasetManifest completed = await ManifestAsync(dataset);
        Assert.Equal(2, completed.Samples.Count(sample => sample.GroundTruth is not null));
        Assert.All(completed.Samples.Where(sample => sample.GroundTruth is not null), sample =>
            Assert.Equal(70, sample.GroundTruth!.Single(score => score.Color == "red").Attractiveness));
        ArkNovaHistoryCorrectionUseCase correction = new(db, new(db, TestGameContent.Create()));
        var draft = await correction.LoadCorrectionDraftAsync(session.Id, Token);
        draft.SetScore(db.First.Id, 90, 21, 9);
        await correction.SaveCorrectionAsync(draft, correction.CreateCorrectionCompletionPlan(draft).DefaultPlacements, Token);
        await dataset.FreezeGroundTruthAsync(session.Id, [new("red", 99, 30, 10)]);
        Assert.Equal(JsonSerializer.Serialize(completed), JsonSerializer.Serialize(await ManifestAsync(dataset)));
    }

    [Fact]
    public async Task ExportIsCopyOnlyWithPublicIdentifiersDiagnosticsAndRelativeImages()
    {
        ArkNovaPhotoDataset dataset = Create();
        Guid privateSessionId = Guid.NewGuid();
        await dataset.CaptureAsync(privateSessionId, "camera", [1], await PredictionAsync(), Token);
        using MemoryStream bytes = new();
        await dataset.ExportAsync(bytes);
        bytes.Position = 0;
        using ZipArchive zip = new(bytes, ZipArchiveMode.Read);
        using StreamReader reader = new(zip.GetEntry("dataset.json")!.Open());
        string json = await reader.ReadToEndAsync(Token);
        Assert.DoesNotContain(privateSessionId.ToString(), json);
        Assert.DoesNotContain("gameSession", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("playerId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("group", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("name", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contentHash", json);
        ArkNovaDatasetManifest manifest = JsonSerializer.Deserialize<ArkNovaDatasetManifest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        ArkNovaDatasetSample sample = Assert.Single(manifest.Samples);
        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Null(sample.GroundTruth);
        Assert.NotEmpty(sample.Prediction.GetProperty("evidence").EnumerateArray());
        Assert.Equal(80, sample.Prediction.GetProperty("registration").GetProperty("inliers").GetInt32());
        Assert.Equal($"images/{sample.SampleId}.jpg", sample.ImagePath);
        Assert.NotNull(zip.GetEntry(sample.ImagePath));
        Assert.Equal(2, zip.Entries.Count);
        Assert.Equal(1, (await dataset.GetSummaryAsync()).Count);
        await dataset.DeleteSamplesAsync();
        Assert.Equal(0, (await dataset.GetSummaryAsync()).Count);
    }

    [Fact]
    public async Task DatasetFailureCannotFailRecognitionOrGameCompletion()
    {
        ArkNovaPhotoDataset dataset = Create(new BrokenNormalizer());
        ArkNovaPhotoReviewFlow flow = new(new Input(), new(new Engine()), dataset);
        Assert.NotNull(await flow.AnalyzeAsync(ArkNovaPhotoSource.Camera, Token, Guid.NewGuid()));
        Assert.Equal(0, (await dataset.GetSummaryAsync()).Count);
        // An inaccessible collection must also contain its write failures.
        Directory.CreateDirectory(_directory);
        string blocked = Path.Combine(_directory, "not-a-directory");
        await File.WriteAllTextAsync(blocked, "blocked", Token);
        ArkNovaPhotoDataset broken = new(blocked, new Normalizer(), "test", NullLogger<ArkNovaPhotoDataset>.Instance);
        Assert.Null(await broken.CaptureAsync(Guid.NewGuid(), "camera", [1], await PredictionAsync(), Token));

        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        var session = await db.AddSessionAsync(scoring: true);
        await using (var context = db.CreateDbContext())
        {
            ArkNovaGameSessionPlayer first = new(session.Id, db.First.Id, 1);
            context.GameSessionPlayerColors.Add(new(session.Id, db.First.Id, PlayerColor.Red));
            ArkNovaGameSessionPlayer second = new(session.Id, db.Second.Id, 2);
            context.GameSessionPlayerColors.Add(new(session.Id, db.Second.Id, PlayerColor.Blue));
            first.SetScoreValues(70, 20, null);
            second.SetScoreValues(60, 19, null);
            context.AddRange(first, second);
            await context.SaveChangesAsync(Token);
        }
        // Malformed diagnostic metadata makes label association fail after the DB commit.
        await File.WriteAllTextAsync(Path.Combine(_directory, $"{Guid.NewGuid()}.json"), "invalid", Token);
        ArkNovaGameSessionResultQuery query = new(db, TestGameContent.Create());
        ArkNovaScoringUseCase scoring = new(db, query, dataset);
        var plan = await scoring.PrepareCompletionAsync(session.Id, Token);
        await scoring.CompleteAsync(session.Id, plan.DefaultPlacements, DateTimeOffset.UtcNow, Token);
        Assert.Equal(BoardgameTracker.Core.GameSessions.GameSessionStatus.Completed,
            (await query.LoadGameSessionResultAsync(session.Id, Token))!.Status);
    }

    private static async Task<ArkNovaPhotoRecognitionResult> PredictionAsync() =>
        await new ArkNovaPhotoRecognition(new Engine()).AnalyzeAsync(new byte[] { 1 }, Token);

    private static async Task<ArkNovaDatasetManifest> ManifestAsync(ArkNovaPhotoDataset dataset)
    {
        using MemoryStream bytes = new();
        await dataset.ExportAsync(bytes);
        bytes.Position = 0;
        using ZipArchive zip = new(bytes, ZipArchiveMode.Read);
        await using Stream stream = zip.GetEntry("dataset.json")!.Open();
        return (await JsonSerializer.DeserializeAsync<ArkNovaDatasetManifest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), Token))!;
    }

    private sealed class Normalizer : IArkNovaDatasetImageNormalizer
    {
        public Task<ArkNovaDatasetImage> NormalizeAsync(byte[] image, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ArkNovaDatasetImage([0xff, 0xd8, image[0], 0xff, 0xd9], image[0].ToString()));
    }

    private sealed class BrokenNormalizer : IArkNovaDatasetImageNormalizer
    {
        public Task<ArkNovaDatasetImage> NormalizeAsync(byte[] image, CancellationToken cancellationToken = default) =>
            throw new IOException("Test normalization failure.");
    }

    private sealed class Input : IArkNovaPhotoInput
    {
        public bool Cancel { get; init; }
        public Task<byte[]?> ReadAsync(ArkNovaPhotoSource source, CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]?>(Cancel ? null : [1]);
    }

    private sealed class Engine : IArkNovaVisionEngine
    {
        public Task<ArkNovaVisionEvidence> AnalyzeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
        {
            List<RankingObservation> fields = [];
            foreach ((string track, int value) in new[] { ("attractiveness", 63), ("conservation", 21), ("reputation", 11) })
            {
                fields.Add(new("red", track, value, .4, .5, .5, .5, .5, 1, 1, .4, .5, .5));
                fields.Add(new("red", track, value + 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0));
            }
            return Task.FromResult(new ArkNovaVisionEvidence(new(true, 100, 80, 1, 1, 1, []), fields));
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}

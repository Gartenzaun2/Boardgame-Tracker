using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.Application.Games.ArkNova.PhotoDataset;

/// <summary>Local Ark Nova diagnostics. Collection failures never escape into scoring or recognition.</summary>
public sealed class ArkNovaPhotoDataset(string directory, IArkNovaDatasetImageNormalizer normalizer,
    string appVersion, ILogger<ArkNovaPhotoDataset> logger, Action? prepareDirectory = null)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public async Task<ArkNovaDatasetSummary> GetSummaryAsync()
    {
        await _gate.WaitAsync();
        try
        {
            List<LocalSample> samples = await ReadSamplesAsync();
            long bytes = Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory).Sum(path => new FileInfo(path).Length) : 0;
            return new(await IsEnabledAsync(), samples.Count, bytes);
        }
        finally { _gate.Release(); }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        await _gate.WaitAsync();
        try { await WriteJsonAsync(Path.Combine(directory, "settings.json"), enabled); }
        finally { _gate.Release(); }
    }

    // The explicit pre-1.0 default is deliberately independent of the app version.
    private async Task<bool> IsEnabledAsync() => !File.Exists(Path.Combine(directory, "settings.json"))
        || JsonSerializer.Deserialize<bool>(await File.ReadAllTextAsync(Path.Combine(directory, "settings.json")), Json);

    public async Task<Guid?> CaptureAsync(Guid gameSessionId, string source, byte[] image,
        ArkNovaPhotoRecognitionResult prediction, CancellationToken cancellationToken = default)
    {
        try
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (!await IsEnabledAsync() || !prediction.Registration.Success
                    || !prediction.Colors.Values.Any(color => color.Tracks.Values.Any(field => field.Value.HasValue)))
                    return null;
                ArkNovaDatasetImage normalized = await normalizer.NormalizeAsync(image, cancellationToken);
                List<LocalSample> samples = await ReadSamplesAsync();
                LocalSample? duplicate = samples.FirstOrDefault(sample => sample.GameSessionId == gameSessionId
                    && sample.ContentHash == normalized.ContentHash);
                if (duplicate is not null)
                    return duplicate.Sample.SampleId;
                Guid id = Guid.NewGuid();
                // Snapshot mutable CV dictionaries before returning control to the editable review.
                ArkNovaDatasetSample sample = new(id, "ark-nova", source, appVersion,
                    "native-mobile/managed-ranking-1", $"V5-hardened-A/reference-master-clean/regions-v2/{ArkNovaRankingRules.Selected}/{ArkNovaConfidencePolicy.Selected}",
                    JsonSerializer.SerializeToElement(prediction, Json), null, "pending", $"images/{id}.jpg");
                LocalSample local = new(gameSessionId, normalized.ContentHash, sample);
                PrepareDirectory();
                await File.WriteAllBytesAsync(Path.Combine(directory, $"{id}.jpg"), normalized.Jpeg, cancellationToken);
                await WriteJsonAsync(Path.Combine(directory, $"{id}.json"), local);
                return id;
            }
            finally { _gate.Release(); }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Ark Nova dataset capture failed; recognition remains available.");
            return null;
        }
    }

    public async Task SetReviewStatusAsync(Guid? sampleId, string status)
    {
        if (sampleId is null)
            return;
        await TryUpdateAsync(async samples =>
        {
            LocalSample? local = samples.FirstOrDefault(sample => sample.Sample.SampleId == sampleId);
            if (local is not null)
                await WriteJsonAsync(Path.Combine(directory, $"{sampleId}.json"),
                    local with { Sample = local.Sample with { ReviewStatus = status } });
        });
    }

    public Task FreezeGroundTruthAsync(Guid gameSessionId, IReadOnlyList<ArkNovaDatasetScore> scores) =>
        TryUpdateAsync(async samples =>
        {
            foreach (LocalSample local in samples.Where(sample => sample.GameSessionId == gameSessionId && sample.Sample.GroundTruth is null))
                await WriteJsonAsync(Path.Combine(directory, $"{local.Sample.SampleId}.json"),
                    local with { Sample = local.Sample with { GroundTruth = scores.ToArray() } });
        });

    private async Task TryUpdateAsync(Func<List<LocalSample>, Task> update)
    {
        try
        {
            await _gate.WaitAsync();
            try { await update(await ReadSamplesAsync()); }
            finally { _gate.Release(); }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Ark Nova dataset metadata update failed; scoring remains available.");
        }
    }

    public async Task ExportAsync(Stream output)
    {
        await _gate.WaitAsync();
        try
        {
            List<LocalSample> samples = await ReadSamplesAsync();
            using ZipArchive zip = new(output, ZipArchiveMode.Create, leaveOpen: true);
            // This public projection deliberately excludes the internal GameSession association and hash.
            await using (Stream manifest = zip.CreateEntry("dataset.json").Open())
                await JsonSerializer.SerializeAsync(manifest, new ArkNovaDatasetManifest(1, 1, samples.Select(local => local.Sample).ToArray()), Json);
            foreach (LocalSample local in samples)
            {
                await using Stream image = File.OpenRead(Path.Combine(directory, $"{local.Sample.SampleId}.jpg"));
                await using Stream target = zip.CreateEntry(local.Sample.ImagePath).Open();
                await image.CopyToAsync(target);
            }
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteSamplesAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!Directory.Exists(directory))
                return;
            // Keep the user's preference when deleting samples.
            foreach (string path in Directory.EnumerateFiles(directory))
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(path), out _) || path.EndsWith(".tmp", StringComparison.Ordinal))
                    File.Delete(path);
        }
        finally { _gate.Release(); }
    }

    private async Task<List<LocalSample>> ReadSamplesAsync()
    {
        List<LocalSample> samples = [];
        if (!Directory.Exists(directory))
            return samples;
        foreach (string path in Directory.EnumerateFiles(directory, "*.json"))
        {
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(path), out _))
                continue;
            LocalSample sample = JsonSerializer.Deserialize<LocalSample>(await File.ReadAllTextAsync(path), Json)
                ?? throw new InvalidDataException("Invalid dataset metadata.");
            samples.Add(sample);
        }
        return samples;
    }

    private async Task WriteJsonAsync<T>(string path, T value)
    {
        PrepareDirectory();
        string temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(value, Json));
        File.Move(temporary, path, overwrite: true);
    }

    private void PrepareDirectory()
    {
        Directory.CreateDirectory(directory);
        prepareDirectory?.Invoke();
    }

    private sealed record LocalSample(Guid GameSessionId, string ContentHash, ArkNovaDatasetSample Sample);
}

public sealed record ArkNovaDatasetSummary(bool Enabled, int Count, long Bytes);
public sealed record ArkNovaDatasetScore(string Color, int? Attractiveness, int? Conservation, int? Reputation);
public sealed record ArkNovaDatasetSample(Guid SampleId, string Game, string PhotoSource, string AppVersion,
    string RecognitionVersion, string BaselineVersion, JsonElement Prediction,
    IReadOnlyList<ArkNovaDatasetScore>? GroundTruth, string ReviewStatus, string ImagePath);
public sealed record ArkNovaDatasetManifest(int DatasetVersion, int SchemaVersion, IReadOnlyList<ArkNovaDatasetSample> Samples);

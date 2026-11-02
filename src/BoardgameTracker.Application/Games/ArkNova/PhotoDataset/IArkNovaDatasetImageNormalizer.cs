namespace BoardgameTracker.Application.Games.ArkNova.PhotoDataset;

/// <summary>Creates an independent, oriented, metadata-free JPEG and hashes its normalized pixels.</summary>
public interface IArkNovaDatasetImageNormalizer
{
    Task<ArkNovaDatasetImage> NormalizeAsync(byte[] image, CancellationToken cancellationToken = default);
}

public sealed record ArkNovaDatasetImage(byte[] Jpeg, string ContentHash);

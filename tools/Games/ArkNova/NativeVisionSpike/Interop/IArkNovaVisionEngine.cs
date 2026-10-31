namespace BoardgameTracker.Tools.Games.ArkNova.NativeVisionSpike;

/// <summary>Capability spike boundary; not yet a score-producing engine.</summary>
public interface IArkNovaVisionEngine
{
    ArkNovaRegistrationProbe ProbeRegistration(ArkNovaVisionImage reference, ArkNovaVisionImage photo);
}

/// <summary>EXIF-normalized, top-left BGR8. The caller retains ownership.</summary>
public sealed record ArkNovaVisionImage(ReadOnlyMemory<byte> Pixels, int Width, int Height, int Stride)
{
    public void Validate()
    {
        if (Width is <= 0 or > 16384 || Height is <= 0 or > 16384
            || Stride < (long)Width * 3 || Pixels.Length < (long)Stride * Height)
            throw new ArgumentException("Invalid normalized BGR image buffer.");
    }
}

public sealed record ArkNovaRegistrationProbe(int Matches, int Inliers, double ReprojectionP95,
    int WarpWidth, int WarpHeight, byte[] WarpedBgr);

public enum ArkNovaVisionStatus
{
    Success = 0, InvalidInput = 1, BufferTooSmall = 2, RegistrationFailed = 3, InternalError = 4
}

public sealed class ArkNovaVisionException(ArkNovaVisionStatus status)
    : Exception($"Native vision probe returned {status}.")
{
    public ArkNovaVisionStatus Status { get; } = status;
}

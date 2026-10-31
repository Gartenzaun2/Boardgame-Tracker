namespace BoardgameTracker.App.Games.ArkNova.PhotoRecognition;

public enum ArkNovaPhotoSource { Camera, Library }

/// <summary>User-controlled acquisition boundary. Null is a normal picker/camera cancellation.</summary>
public interface IArkNovaPhotoInput
{
    Task<byte[]?> ReadAsync(ArkNovaPhotoSource source, CancellationToken cancellationToken = default);
}

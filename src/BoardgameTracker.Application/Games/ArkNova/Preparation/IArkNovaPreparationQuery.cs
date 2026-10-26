using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.Preparation;

public interface IArkNovaPreparationQuery
{
    Task<ArkNovaPreparationData?> ExecuteAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}

public sealed record ArkNovaPreparationData(
    Guid GroupId,
    string GroupName,
    bool IsArchived,
    ArkNovaLastSession? LastSession,
    IReadOnlyList<ArkNovaPreparationPlayer> Players);

public sealed record ArkNovaLastSession(
    DateTimeOffset StartedAt,
    Guid? LastPlacePersonId,
    string? WinnerName);

public sealed record ArkNovaPreparationPlayer(
    Guid PersonId,
    string Name,
    int DefaultOrder,
    ArkNovaPlayerColor? DefaultColor);

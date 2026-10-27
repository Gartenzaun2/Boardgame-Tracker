using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.Preparation;

/// <summary>
/// Read boundary for loading exactly the persisted information required to prepare
/// a new Ark Nova session for a group.
/// </summary>
public interface IArkNovaPreparationQuery
{
    Task<ArkNovaPreparationData?> ExecuteAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Application read model for the preparation screen. It combines group defaults
/// and the previous completed session without exposing persistence entities.
/// </summary>
public sealed record ArkNovaPreparationData(
    Guid GroupId,
    string GroupName,
    bool IsArchived,
    ArkNovaLastSession? LastSession,
    IReadOnlyList<ArkNovaPreparationPlayer> Players);

/// <summary>
/// Snapshot of the last completed Ark Nova session needed by preparation.
/// </summary>
public sealed record ArkNovaLastSession(
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaLastSessionPlacement> Placements);

/// <summary>
/// One displayed placement from the previous completed session.
/// </summary>
public sealed record ArkNovaLastSessionPlacement(
    Guid PersonId,
    string Name,
    int Placement);

/// <summary>
/// Group member plus Ark Nova defaults required to initialize one preparation tile.
/// </summary>
public sealed record ArkNovaPreparationPlayer(
    Guid PersonId,
    string Name,
    int DefaultOrder,
    ArkNovaPlayerColor? DefaultColor);

using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;

/// <summary>
/// Read boundary for loading exactly the persisted information required to prepare
/// a new Ark Nova GameSession for a group.
/// </summary>
public interface IArkNovaPreparationQuery
{
    Task<ArkNovaPreparationData?> ExecuteAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Application read model for the preparation screen. It combines group defaults
/// and the previous completed gameSession without exposing persistence entities.
/// </summary>
public sealed record ArkNovaPreparationData(
    Guid GroupId,
    string GroupName,
    bool IsArchived,
    ArkNovaLastGameSession? LastGameSession,
    IReadOnlyList<ArkNovaPreparationPlayer> Players);

/// <summary>
/// Snapshot of the last completed Ark Nova GameSession needed by preparation.
/// </summary>
public sealed record ArkNovaLastGameSession(
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaPreviousPlacement> Placements);

/// <summary>
/// Group player plus Ark Nova defaults required to initialize one preparation tile.
/// </summary>
public sealed record ArkNovaPreparationPlayer(
    Guid PlayerId,
    string Name,
    int DefaultOrder,
    PlayerColor? DefaultColor);

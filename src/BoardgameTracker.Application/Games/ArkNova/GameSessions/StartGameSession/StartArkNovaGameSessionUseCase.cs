using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;

/// <summary>
/// Validates a prepared Ark Nova setup against persisted group/reference data,
/// creates the domain gameSession snapshot and persists it atomically.
/// </summary>
public sealed class StartArkNovaGameSessionUseCase(
    IStartArkNovaGameSessionDataAccess dataAccess)
{
    public async Task<Guid> ExecuteAsync(
        StartArkNovaGameSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(command.Players);

        StartArkNovaGameSessionContext? context = await dataAccess.LoadContextAsync(
            command.GroupId,
            cancellationToken);

        if (context is null)
        {
            throw new InvalidOperationException("The group does not exist.");
        }

        if (context.IsArchived)
        {
            throw new InvalidOperationException(
                "Archived groups cannot start a new gameSession.");
        }

        if (context.HasOpenGameSession)
        {
            throw new InvalidOperationException(
                "An unfinished game gameSession already exists.");
        }

        ArkNovaGameSessionSetup.Validate(command.Players);

        HashSet<Guid> groupPlayers = [.. context.GroupPlayerIds];
        HashSet<Guid> gameSessionPlayers = [.. command.Players.Select(player => player.PlayerId)];

        if (!groupPlayers.SetEquals(gameSessionPlayers))
        {
            throw new InvalidOperationException(
                "The gameSession players must match the fixed group membership.");
        }

        IReadOnlyList<int> requestedZooPlanIds =
        [
            .. command.Players
                .SelectMany(player => new[]
                {
                    player.ZooPlanOption1Number,
                    player.ZooPlanOption2Number
                })
                .Distinct()
        ];

        if (requestedZooPlanIds.Any(id => !context.AvailableZooPlanIds.Contains(id)))
        {
            throw new InvalidOperationException(
                "The gameSession contains an unknown Ark Nova zoo plan.");
        }

        IReadOnlyList<Guid> playerIds = [.. command.Players.Select(player => player.PlayerId)];
        GameSession gameSession = new(
            context.GameId,
            command.GroupId,
            command.StartedAt,
            playerIds);

        IReadOnlyCollection<ArkNovaGameSessionPlayer> arkNovaPlayers =
        [
            .. command.Players.Select(player => new ArkNovaGameSessionPlayer(
                gameSession.Id,
                player.PlayerId,
                player.Color,
                player.ZooPlanOption1Number,
                player.ZooPlanOption2Number,
                player.SelectedZooPlanNumber.GetValueOrDefault()))
        ];

        await dataAccess.SaveAsync(gameSession, arkNovaPlayers, cancellationToken);

        return gameSession.Id;
    }
}

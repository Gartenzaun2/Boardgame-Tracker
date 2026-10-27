using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Setup;

namespace BoardgameTracker.Application.Games.ArkNova.StartSession;

/// <summary>
/// Validates a prepared Ark Nova setup against persisted group/reference data,
/// creates the domain session snapshot and persists it atomically.
/// </summary>
public sealed class StartArkNovaSessionUseCase(
    IStartArkNovaSessionDataAccess dataAccess)
{
    public async Task<Guid> ExecuteAsync(
        StartArkNovaSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(command.Players);

        StartArkNovaSessionContext? context = await dataAccess.LoadContextAsync(
            command.GroupId,
            cancellationToken);

        if (context is null)
        {
            throw new InvalidOperationException("The group does not exist.");
        }

        if (context.IsArchived)
        {
            throw new InvalidOperationException(
                "Archived groups cannot start a new session.");
        }

        if (context.HasOpenSession)
        {
            throw new InvalidOperationException(
                "An unfinished game session already exists.");
        }

        ArkNovaSessionSetup.Validate(command.Players);

        HashSet<Guid> groupMembers = [.. context.GroupMemberIds];
        HashSet<Guid> sessionPlayers = [.. command.Players.Select(player => player.PersonId)];

        if (!groupMembers.SetEquals(sessionPlayers))
        {
            throw new InvalidOperationException(
                "The session players must match the fixed group membership.");
        }

        IReadOnlyList<int> requestedZooPlanIds =
        [
            .. command.Players
                .SelectMany(player => new[]
                {
                    player.ZooPlanOption1Id,
                    player.ZooPlanOption2Id
                })
                .Distinct()
        ];

        if (requestedZooPlanIds.Any(id => !context.AvailableZooPlanIds.Contains(id)))
        {
            throw new InvalidOperationException(
                "The session contains an unknown Ark Nova zoo plan.");
        }

        IReadOnlyList<Guid> playerIds = [.. command.Players.Select(player => player.PersonId)];
        GameSession session = new(
            context.GameId,
            command.GroupId,
            command.StartedAt,
            playerIds);

        IReadOnlyCollection<ArkNovaSessionPlayer> arkNovaPlayers =
        [
            .. command.Players.Select(player => new ArkNovaSessionPlayer(
                session.Id,
                player.PersonId,
                player.Color,
                player.ZooPlanOption1Id,
                player.ZooPlanOption2Id,
                player.SelectedZooPlanId.GetValueOrDefault()))
        ];

        await dataAccess.SaveAsync(session, arkNovaPlayers, cancellationToken);

        return session.Id;
    }
}

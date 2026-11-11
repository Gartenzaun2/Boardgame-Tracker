using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;

/// <summary>
/// Contains the pure Ark Nova rules that must be true when preparation is turned
/// into a running gameSession. It has no knowledge of MAUI, Application use cases or
/// persistence.
/// </summary>
public static class ArkNovaGameSessionSetup
{
    public const int MinimumPlayerCount = ArkNovaGame.MinPlayers;

    public static void Validate(IReadOnlyCollection<ArkNovaGameSessionSetupPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (players.Count < MinimumPlayerCount || players.Count > ArkNovaGame.MaxPlayers)
        {
            throw new ArgumentException(
                "Ark Nova gameSessions must contain between one and four players.",
                nameof(players));
        }

        if (players.Any(player => player.PlayerId == Guid.Empty)
            || players.Select(player => player.PlayerId).Distinct().Count() != players.Count)
        {
            throw new ArgumentException(
                "Every player must occur exactly once.",
                nameof(players));
        }

        if (players.Any(player => !ArkNovaPlayerColors.Rules.Allows(player.Color)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(players),
                "Every Ark Nova player must use a defined player color.");
        }

        if (players.Select(player => player.Color).Distinct().Count() != players.Count)
        {
            throw new InvalidOperationException(
                "Every Ark Nova player must have a unique color when the GameSession starts.");
        }

        foreach (ArkNovaGameSessionSetupPlayer player in players)
        {
            if (player.SelectedZooPlanNumber is null)
                throw new InvalidOperationException(
                    "Every Ark Nova player must select a zoo plan before the GameSession starts.");

            ArkNovaZooPlan.ValidateNumber(player.SelectedZooPlanNumber.Value);
        }
    }
}

/// <summary>
/// Represents one player in the not-yet-persisted Ark Nova setup at the moment a
/// GameSession is about to start. This is intentionally different from
/// <see cref="ArkNovaGameSessionPlayer"/>, which represents the persisted snapshot of
/// a player after the GameSession has been created.
/// </summary>
public sealed record ArkNovaGameSessionSetupPlayer(
    Guid PlayerId,
    PlayerColor Color,
    int? SelectedZooPlanNumber);

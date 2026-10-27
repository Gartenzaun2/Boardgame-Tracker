using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.Players;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;

/// <summary>
/// Contains the pure Ark Nova rules that must be true when preparation is turned
/// into a running gameSession. It has no knowledge of MAUI, Application use cases or
/// persistence.
/// </summary>
public static class ArkNovaGameSessionSetup
{
    public const int MinimumPlayerCount = 1;
    public const int MaximumPlayerCount = 4;

    public static void Validate(IReadOnlyCollection<ArkNovaGameSessionSetupPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (players.Count is < MinimumPlayerCount or > MaximumPlayerCount)
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

        if (players.Any(player => !Enum.IsDefined(player.Color)))
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
            if (player.ZooPlanOption1Number <= 0 || player.ZooPlanOption2Number <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(players),
                    "Zoo plan identifiers must be positive.");
            }

            if (player.ZooPlanOption1Number == player.ZooPlanOption2Number)
            {
                throw new InvalidOperationException(
                    "The two offered zoo plans for a player must be different.");
            }

            if (player.SelectedZooPlanNumber is null)
            {
                throw new InvalidOperationException(
                    "Every Ark Nova player must select one of the offered zoo plans before the GameSession starts.");
            }

            if (player.SelectedZooPlanNumber != player.ZooPlanOption1Number
                && player.SelectedZooPlanNumber != player.ZooPlanOption2Number)
            {
                throw new InvalidOperationException(
                    "The selected zoo plan must be one of the two offered plans.");
            }
        }

        IReadOnlyList<int> offeredZooPlans =
        [
            .. players.SelectMany(player => new[]
            {
                player.ZooPlanOption1Number,
                player.ZooPlanOption2Number
            })
        ];

        if (offeredZooPlans.Distinct().Count() != offeredZooPlans.Count)
        {
            throw new InvalidOperationException(
                "A zoo plan may only be offered once within an Ark Nova gameSession.");
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
    ArkNovaPlayerColor Color,
    int ZooPlanOption1Number,
    int ZooPlanOption2Number,
    int? SelectedZooPlanNumber);

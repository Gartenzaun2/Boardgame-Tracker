using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Core.Games.ArkNova.Setup;

/// <summary>
/// Contains the pure Ark Nova rules that must be true when preparation is turned
/// into a running session. It has no knowledge of MAUI, Application use cases or
/// persistence.
/// </summary>
public static class ArkNovaSessionSetup
{
    public const int MinimumPlayerCount = 1;
    public const int MaximumPlayerCount = 4;

    public static void Validate(IReadOnlyCollection<ArkNovaSessionSetupPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (players.Count is < MinimumPlayerCount or > MaximumPlayerCount)
        {
            throw new ArgumentException(
                "Ark Nova sessions must contain between one and four players.",
                nameof(players));
        }

        if (players.Any(player => player.PersonId == Guid.Empty)
            || players.Select(player => player.PersonId).Distinct().Count() != players.Count)
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
                "Every Ark Nova player must have a unique color when the session starts.");
        }

        foreach (ArkNovaSessionSetupPlayer player in players)
        {
            if (player.ZooPlanOption1Id <= 0 || player.ZooPlanOption2Id <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(players),
                    "Zoo plan identifiers must be positive.");
            }

            if (player.ZooPlanOption1Id == player.ZooPlanOption2Id)
            {
                throw new InvalidOperationException(
                    "The two offered zoo plans for a player must be different.");
            }

            if (player.SelectedZooPlanId is null)
            {
                throw new InvalidOperationException(
                    "Every Ark Nova player must select one of the offered zoo plans before the session starts.");
            }

            if (player.SelectedZooPlanId != player.ZooPlanOption1Id
                && player.SelectedZooPlanId != player.ZooPlanOption2Id)
            {
                throw new InvalidOperationException(
                    "The selected zoo plan must be one of the two offered plans.");
            }
        }

        IReadOnlyList<int> offeredZooPlans =
        [
            .. players.SelectMany(player => new[]
            {
                player.ZooPlanOption1Id,
                player.ZooPlanOption2Id
            })
        ];

        if (offeredZooPlans.Distinct().Count() != offeredZooPlans.Count)
        {
            throw new InvalidOperationException(
                "A zoo plan may only be offered once within an Ark Nova session.");
        }
    }
}

/// <summary>
/// Represents one player in the not-yet-persisted Ark Nova setup at the moment a
/// session is about to start. This is intentionally different from
/// <see cref="ArkNovaSessionPlayer"/>, which represents the persisted snapshot of
/// a player after the session has been created.
/// </summary>
public sealed record ArkNovaSessionSetupPlayer(
    Guid PersonId,
    ArkNovaPlayerColor Color,
    int ZooPlanOption1Id,
    int ZooPlanOption2Id,
    int? SelectedZooPlanId);

using BoardgameTracker.Core.Games.ArkNova.Setup;

namespace BoardgameTracker.Application.Games.ArkNova.Preparation;

/// <summary>
/// Creates a random, session-wide unique assignment of two Ark Nova zoo plans per player for a non-persisted preparation.
/// </summary>
public static class ArkNovaPreparationDraw
{
    public static IReadOnlyList<ArkNovaZooPlanDraw> Draw(
        IReadOnlyList<Guid> playerIds,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(playerIds);
        ArgumentNullException.ThrowIfNull(random);

        if (playerIds.Count is < ArkNovaSessionSetup.MinimumPlayerCount or > ArkNovaSessionSetup.MaximumPlayerCount)
        {
            throw new ArgumentException(
                "Ark Nova preparation requires between one and four players.",
                nameof(playerIds));
        }

        if (playerIds.Any(id => id == Guid.Empty)
            || playerIds.Distinct().Count() != playerIds.Count)
        {
            throw new ArgumentException(
                "Every player must occur exactly once.",
                nameof(playerIds));
        }

        List<ArkNovaZooPlanOption> available = [.. ArkNovaZooPlanCatalog.All];

        for (int index = available.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (available[index], available[swapIndex]) = (available[swapIndex], available[index]);
        }

        var result = new List<ArkNovaZooPlanDraw>(playerIds.Count);

        for (int index = 0; index < playerIds.Count; index++)
        {
            int offset = index * 2;
            result.Add(new ArkNovaZooPlanDraw(
                playerIds[index],
                available[offset],
                available[offset + 1]));
        }

        return result;
    }
}

/// <summary>
/// Represents the two zoo-plan options drawn for one player during preparation.
/// </summary>
public sealed record ArkNovaZooPlanDraw(
    Guid PersonId,
    ArkNovaZooPlanOption Option1,
    ArkNovaZooPlanOption Option2);

namespace BoardgameTracker.Application.Games.ArkNova.Preparation;

public static class ArkNovaPreparationDraw
{
    public static IReadOnlyList<ArkNovaZooPlanDraw> Draw(
        IReadOnlyList<Guid> playerIds,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(playerIds);
        ArgumentNullException.ThrowIfNull(random);

        if (playerIds.Count is < 1 or > 4)
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

public sealed record ArkNovaZooPlanDraw(
    Guid PersonId,
    ArkNovaZooPlanOption Option1,
    ArkNovaZooPlanOption Option2);

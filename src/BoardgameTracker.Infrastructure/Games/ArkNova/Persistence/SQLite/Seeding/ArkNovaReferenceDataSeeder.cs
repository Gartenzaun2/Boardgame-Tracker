using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Infrastructure.Persistence.SQLite;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Seeding;

/// <summary>
/// Ensures the stable Ark Nova game and zoo-plan reference rows required by
/// persisted gameSessions exist in the local development database.
/// </summary>
internal static class ArkNovaReferenceDataSeeder
{
    public static async Task SeedAsync(
        BoardgameTrackerDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        bool hasArkNova = await dbContext.Games
            .AnyAsync(game => game.Key == ArkNovaGame.Key, cancellationToken);

        if (!hasArkNova)
        {
            dbContext.Games.Add(new Game(ArkNovaGame.Key));
        }

        int[] existingZooPlanNumbers = await dbContext.ArkNovaZooPlans
            .AsNoTracking()
            .Select(plan => plan.Number)
            .ToArrayAsync(cancellationToken);

        HashSet<int> existingNumbers = [.. existingZooPlanNumbers];

        foreach (ArkNovaZooPlanOption option in ArkNovaZooPlanCatalog.All)
        {
            if (existingNumbers.Contains(option.Number))
            {
                continue;
            }

            dbContext.ArkNovaZooPlans.Add(new ArkNovaZooPlan(option.Number));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

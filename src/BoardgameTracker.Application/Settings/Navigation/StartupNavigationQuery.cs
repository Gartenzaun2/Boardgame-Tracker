using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Settings.Navigation;

public sealed class StartupNavigationQuery(IDbContextFactory<BoardgameTrackerDbContext> factory, OpenGameSessionQuery sessions)
{
    public async Task<StartupNavigation> ResolveAsync(AppNavigationSettings settings, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        if (!await db.Players.AnyAsync(cancellationToken))
            return new(AppMainSection.Participants, null);
        OpenGameSession? resume = settings.LastGameSessionId is Guid id ? await sessions.GetAsync(id, cancellationToken) : null;
        return new(AppMainSections.FromStoredValue((int?)settings.LastMainSection) ?? AppMainSection.Play, resume);
    }
}
public sealed record StartupNavigation(AppMainSection Section, OpenGameSession? Resume);

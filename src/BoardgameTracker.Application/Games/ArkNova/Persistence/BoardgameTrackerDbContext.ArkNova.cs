using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Persistence;

public sealed partial class BoardgameTrackerDbContext
{
    public DbSet<ArkNovaUserContent> ArkNovaUserContent => Set<ArkNovaUserContent>();
    public DbSet<ArkNovaGameSessionPlayer> ArkNovaGameSessionPlayers => Set<ArkNovaGameSessionPlayer>();
    public DbSet<ArkNovaSpecialCard> ArkNovaSpecialCards => Set<ArkNovaSpecialCard>();
    public DbSet<ArkNovaZooPlan> ArkNovaZooPlans => Set<ArkNovaZooPlan>();
    public DbSet<ArkNovaGameSessionSpecialCardAssignment> ArkNovaGameSessionSpecialCardAssignments => Set<ArkNovaGameSessionSpecialCardAssignment>();
}

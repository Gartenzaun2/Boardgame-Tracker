using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

public sealed partial class BoardgameTrackerDbContext
{
    public DbSet<ArkNovaGroupPlayer> ArkNovaGroupPlayers => Set<ArkNovaGroupPlayer>();
    public DbSet<ArkNovaGameSessionPlayer> ArkNovaGameSessionPlayers => Set<ArkNovaGameSessionPlayer>();
    public DbSet<ArkNovaSpecialCard> ArkNovaSpecialCards => Set<ArkNovaSpecialCard>();
    public DbSet<ArkNovaZooPlan> ArkNovaZooPlans => Set<ArkNovaZooPlan>();
    public DbSet<ArkNovaGameSessionSpecialCardAssignment> ArkNovaGameSessionSpecialCardAssignments => Set<ArkNovaGameSessionSpecialCardAssignment>();
}

using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.Groups;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

/// <summary>
/// EF Core unit-of-work and mapping root for the local Boardgame Tracker SQLite database.
/// </summary>
public sealed class BoardgameTrackerDbContext(DbContextOptions<BoardgameTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupPlayer> GroupPlayers => Set<GroupPlayer>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameSessionPlayer> GameSessionPlayers => Set<GameSessionPlayer>();
    public DbSet<ArkNovaGroupPlayer> ArkNovaGroupPlayers => Set<ArkNovaGroupPlayer>();
    public DbSet<ArkNovaGameSessionPlayer> ArkNovaGameSessionPlayers => Set<ArkNovaGameSessionPlayer>();
    public DbSet<ArkNovaSpecialCard> ArkNovaSpecialCards => Set<ArkNovaSpecialCard>();
    public DbSet<ArkNovaZooPlan> ArkNovaZooPlans => Set<ArkNovaZooPlan>();
    public DbSet<ArkNovaGameSessionSpecialCardAssignment> ArkNovaGameSessionSpecialCardAssignments => Set<ArkNovaGameSessionSpecialCardAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoardgameTrackerDbContext).Assembly);
    }
}

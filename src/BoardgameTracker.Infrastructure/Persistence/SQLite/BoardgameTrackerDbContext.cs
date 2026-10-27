using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.People;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

/// <summary>
/// EF Core unit-of-work and mapping root for the local Boardgame Tracker SQLite database.
/// </summary>
public sealed class BoardgameTrackerDbContext(DbContextOptions<BoardgameTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupPerson> GroupPersons => Set<GroupPerson>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameSessionPlayer> GameSessionPlayers => Set<GameSessionPlayer>();
    public DbSet<ArkNovaGroupPlayer> ArkNovaGroupPlayers => Set<ArkNovaGroupPlayer>();
    public DbSet<ArkNovaSessionPlayer> ArkNovaSessionPlayers => Set<ArkNovaSessionPlayer>();
    public DbSet<ArkNovaZooPlan> ArkNovaZooPlans => Set<ArkNovaZooPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoardgameTrackerDbContext).Assembly);
    }
}

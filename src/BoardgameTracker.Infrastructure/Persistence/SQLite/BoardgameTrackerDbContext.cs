using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

/// <summary>
/// EF Core unit-of-work and mapping root for the local Boardgame Tracker SQLite database.
/// </summary>
public sealed partial class BoardgameTrackerDbContext : DbContext
{
    public BoardgameTrackerDbContext(DbContextOptions<BoardgameTrackerDbContext> options) : base(options)
    {
        if (Database.GetDbConnection() is Microsoft.Data.Sqlite.SqliteConnection connection)
            connection.CreateCollation("BGT_NAME", (left, right) => BoardgameTracker.Core.Names.EntityName.Comparer.Compare(left?.Trim(), right?.Trim()));
    }
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupPlayer> GroupPlayers => Set<GroupPlayer>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameSessionPlayer> GameSessionPlayers => Set<GameSessionPlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoardgameTrackerDbContext).Assembly);
    }
}

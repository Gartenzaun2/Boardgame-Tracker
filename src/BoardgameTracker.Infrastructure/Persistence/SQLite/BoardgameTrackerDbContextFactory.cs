using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

/// <summary>
/// Design-time DbContext factory used by EF Core tooling to create migrations outside the MAUI runtime.
/// </summary>
public sealed class BoardgameTrackerDbContextFactory
    : IDesignTimeDbContextFactory<BoardgameTrackerDbContext>
{
    public BoardgameTrackerDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<BoardgameTrackerDbContext> optionsBuilder = new();

        BoardgameTrackerDbContextOptions.Configure(
            optionsBuilder,
            "Data Source=boardgame-tracker.design.db");

        return new BoardgameTrackerDbContext(optionsBuilder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

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

using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Persistence;

public sealed partial class BoardgameTrackerDbContext
{
    public DbSet<ManualGame> ManualGames => Set<ManualGame>();
}

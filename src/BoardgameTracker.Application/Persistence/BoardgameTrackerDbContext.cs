using BoardgameTracker.Application.Groups;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Persistence;

/// <summary>
/// EF Core unit-of-work and mapping root for the Boardgame Tracker relational model.
/// </summary>
public sealed partial class BoardgameTrackerDbContext : DbContext
{
    private readonly GamePlayerLimitCatalog _playerLimits;

    public BoardgameTrackerDbContext(DbContextOptions<BoardgameTrackerDbContext> options, GamePlayerLimitCatalog playerLimits) : base(options)
    {
        _playerLimits = playerLimits;
    }

    public int GetMaximumPlayers(string gameKey) => _playerLimits.GetMaximumPlayers(gameKey);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // The synchronous path uses only synchronous database queries.
        ValidateGroupAssignmentsAsync(useAsync: false, CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await ValidateGroupAssignmentsAsync(useAsync: true, cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task ValidateGroupAssignmentsAsync(bool useAsync, CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        GroupGame[] assignments = ChangeTracker.Entries<GroupGame>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity).ToArray();
        foreach (GroupGame assignment in assignments)
        {
            string? gameKey = ChangeTracker.Entries<Game>()
                .FirstOrDefault(entry => entry.Entity.Id == assignment.GameId)?.Entity.Key;
            if (gameKey is null)
            {
                IQueryable<string> keys = Games.Where(game => game.Id == assignment.GameId).Select(game => game.Key);
                gameKey = useAsync ? await keys.SingleOrDefaultAsync(cancellationToken) : keys.SingleOrDefault();
            }
            // Missing references remain subject to the relational foreign-key constraints.
            if (gameKey is null)
                continue;
            IQueryable<Guid> memberIds = GroupPlayers.AsNoTracking()
                .Where(player => player.GroupId == assignment.GroupId).Select(player => player.PlayerId);
            HashSet<Guid> members = (useAsync
                ? await memberIds.ToArrayAsync(cancellationToken)
                : memberIds.ToArray()).ToHashSet();
            foreach (var entry in ChangeTracker.Entries<GroupPlayer>().Where(entry => entry.Entity.GroupId == assignment.GroupId))
            {
                if (entry.State == EntityState.Deleted)
                    members.Remove(entry.Entity.PlayerId);
                else
                    members.Add(entry.Entity.PlayerId);
            }
            int maximumPlayers = GetMaximumPlayers(gameKey);
            if (members.Count > maximumPlayers)
                throw new GroupPlayerLimitExceededException(gameKey, maximumPlayers);
        }
    }
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupPlayer> GroupPlayers => Set<GroupPlayer>();
    public DbSet<GroupGame> GroupGames => Set<GroupGame>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameSessionPlayer> GameSessionPlayers => Set<GameSessionPlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BoardgameTrackerDbContext).Assembly);
    }
}

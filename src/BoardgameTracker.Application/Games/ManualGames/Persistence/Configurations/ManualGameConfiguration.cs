using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Games.ManualGames.Persistence.Configurations;

internal sealed class ManualGameConfiguration : IEntityTypeConfiguration<ManualGame>
{
    public void Configure(EntityTypeBuilder<ManualGame> builder)
    {
        builder.ToTable("ManualGames", table =>
        {
            table.HasCheckConstraint("CK_ManualGames_NegativePoints", "NOT AllowNegativePoints OR CapturePoints");
            table.HasCheckConstraint("CK_ManualGames_Placements", "NOT CapturePlacements OR CaptureWinner");
            table.HasCheckConstraint("CK_ManualGames_Ties", "NOT AllowTies OR CaptureWinner");
            table.HasCheckConstraint("CK_ManualGames_Automatic", "NOT ResultFromPoints OR (CapturePoints AND CaptureWinner)");
            table.HasCheckConstraint("CK_ManualGames_Direction", "(ResultFromPoints = 0 AND PointsDirection IS NULL) OR (ResultFromPoints = 1 AND PointsDirection IS NOT NULL AND PointsDirection IN (1, 2))");
        });
        builder.HasKey(game => game.GameId);
        builder.HasOne<Game>().WithOne().HasForeignKey<ManualGame>(game => game.GameId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(game => game.PointsDirection).HasConversion<int?>();
    }
}

// The manual module owns its name uniqueness rule; integrated names may coincide.
internal sealed class ManualGameNameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(game => game.Name).UseCollation("BGT_NAME");
        builder.HasIndex(game => game.Name).HasDatabaseName("IX_Games_ManualName").IsUnique()
            .HasFilter("\"Key\" LIKE 'manual:%'");
    }
}

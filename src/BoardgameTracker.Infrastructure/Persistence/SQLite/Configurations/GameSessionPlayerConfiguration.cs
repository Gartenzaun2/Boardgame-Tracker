using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations;

internal sealed class GameSessionPlayerConfiguration : IEntityTypeConfiguration<GameSessionPlayer>
{
    public void Configure(EntityTypeBuilder<GameSessionPlayer> builder)
    {
        builder.ToTable(
            "GameSessionPlayers",
            table =>
            {
                table.HasCheckConstraint("CK_GameSessionPlayers_Order_Positive", "\"Order\" > 0");
                table.HasCheckConstraint("CK_GameSessionPlayers_Placement_Positive", "Placement IS NULL OR Placement > 0");
            });

        builder.HasKey(x => new { x.GameSessionId, x.PersonId });
        builder.Property(x => x.Order).IsRequired();

        builder.HasOne<GameSession>()
            .WithMany(x => x.Players)
            .HasForeignKey(x => x.GameSessionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GameSessionId, x.Order }).IsUnique();
    }
}

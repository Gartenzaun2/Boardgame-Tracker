using BoardgameTracker.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations;

internal sealed class GameSessionPlayerConfiguration : IEntityTypeConfiguration<GameSessionPlayer>
{
    public void Configure(EntityTypeBuilder<GameSessionPlayer> builder)
    {
        builder.ToTable("GameSessionPlayers");

        builder.HasKey(x => new { x.GameSessionId, x.PersonId });

        builder.Property(x => x.Order)
            .IsRequired();

        builder.HasOne(x => x.GameSession)
            .WithMany(x => x.Players)
            .HasForeignKey(x => x.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Person)
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GameSessionId, x.Order })
            .IsUnique();
    }
}

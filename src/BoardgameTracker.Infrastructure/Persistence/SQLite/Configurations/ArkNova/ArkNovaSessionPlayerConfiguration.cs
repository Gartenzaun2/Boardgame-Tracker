using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations.ArkNova;

internal sealed class ArkNovaSessionPlayerConfiguration
    : IEntityTypeConfiguration<ArkNovaSessionPlayer>
{
    public void Configure(EntityTypeBuilder<ArkNovaSessionPlayer> builder)
    {
        builder.ToTable("ArkNovaSessionPlayers");

        builder.HasKey(x => new { x.GameSessionId, x.PersonId });

        builder.Property(x => x.Color)
            .HasConversion<int>()
            .IsRequired();
    }
}

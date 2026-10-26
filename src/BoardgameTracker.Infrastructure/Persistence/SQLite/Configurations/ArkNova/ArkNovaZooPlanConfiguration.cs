using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations.ArkNova;

internal sealed class ArkNovaZooPlanConfiguration
    : IEntityTypeConfiguration<ArkNovaZooPlan>
{
    public void Configure(EntityTypeBuilder<ArkNovaZooPlan> builder)
    {
        builder.ToTable("ArkNovaZooPlans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .IsRequired();

        builder.HasIndex(x => x.Number)
            .IsUnique();

        builder.Property(x => x.NameResourceKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IconKey)
            .HasMaxLength(100)
            .IsRequired();
    }
}

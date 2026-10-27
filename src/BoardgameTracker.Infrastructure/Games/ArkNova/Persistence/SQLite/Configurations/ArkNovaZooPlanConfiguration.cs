using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Configurations;

/// <summary>
/// Maps the fixed Ark Nova zoo-plan numbers used for referential integrity.
/// </summary>
internal sealed class ArkNovaZooPlanConfiguration
    : IEntityTypeConfiguration<ArkNovaZooPlan>
{
    public void Configure(EntityTypeBuilder<ArkNovaZooPlan> builder)
    {
        builder.ToTable("ArkNovaZooPlans");
        builder.HasKey(x => x.Number);
        builder.Property(x => x.Number).ValueGeneratedNever();
    }
}

using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Games.ArkNova.Persistence.Configurations;

internal sealed class ArkNovaSpecialCardConfiguration : IEntityTypeConfiguration<ArkNovaSpecialCard>
{
    public void Configure(EntityTypeBuilder<ArkNovaSpecialCard> builder)
    {
        builder.ToTable("ArkNovaSpecialCards", table =>
        {
            table.HasCheckConstraint("CK_ArkNovaSpecialCards_Symbols", "(Symbol1 IS NULL OR Symbol1 BETWEEN 1 AND 14) AND (Symbol2 IS NULL OR Symbol2 BETWEEN 1 AND 14) AND (Symbol3 IS NULL OR Symbol3 BETWEEN 1 AND 14) AND (Symbol2 IS NULL OR Symbol1 IS NOT NULL) AND (Symbol3 IS NULL OR Symbol2 IS NOT NULL)");
        });
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Symbol1).HasConversion<int>();
        builder.Property(c => c.Symbol2).HasConversion<int>();
        builder.Property(c => c.Symbol3).HasConversion<int>();
    }
}

using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Configurations;

internal sealed class ArkNovaSpecialCardConfiguration : IEntityTypeConfiguration<ArkNovaSpecialCard>
{
    public void Configure(EntityTypeBuilder<ArkNovaSpecialCard> builder)
    {
        builder.ToTable("ArkNovaSpecialCards", table =>
        {
            table.HasCheckConstraint("CK_ArkNovaSpecialCards_Name", "length(trim(Name)) BETWEEN 1 AND 120");
            table.HasCheckConstraint("CK_ArkNovaSpecialCards_Symbols", "(Symbol1 IS NULL OR Symbol1 BETWEEN 1 AND 14) AND (Symbol2 IS NULL OR Symbol2 BETWEEN 1 AND 14) AND (Symbol3 IS NULL OR Symbol3 BETWEEN 1 AND 14) AND (Symbol2 IS NULL OR Symbol1 IS NOT NULL) AND (Symbol3 IS NULL OR Symbol2 IS NOT NULL)");
        });
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(ArkNovaSpecialCard.MaximumNameLength).IsRequired();
        builder.Property(c => c.Symbol1).HasConversion<int>();
        builder.Property(c => c.Symbol2).HasConversion<int>();
        builder.Property(c => c.Symbol3).HasConversion<int>();
    }
}

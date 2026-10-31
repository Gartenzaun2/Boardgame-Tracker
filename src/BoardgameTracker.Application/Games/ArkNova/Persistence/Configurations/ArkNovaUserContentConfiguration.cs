using BoardgameTracker.Application.Games.ArkNova.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Games.ArkNova.Persistence.Configurations;

internal sealed class ArkNovaUserContentConfiguration : IEntityTypeConfiguration<ArkNovaUserContent>
{
    public void Configure(EntityTypeBuilder<ArkNovaUserContent> builder)
    {
        builder.ToTable("ArkNovaUserContent", table =>
        {
            table.HasCheckConstraint("CK_ArkNovaUserContent_Type", "ResourceType IN ('special-card', 'zoo-plan')");
            table.HasCheckConstraint("CK_ArkNovaUserContent_Name", "length(trim(Name)) BETWEEN 1 AND 120");
        });
        builder.HasKey(content => new { content.ResourceType, content.ObjectId });
        builder.Property(content => content.ResourceType).HasMaxLength(20);
        builder.Property(content => content.ObjectId).HasMaxLength(36);
        builder.Property(content => content.Name).HasMaxLength(ArkNovaUserContent.MaximumNameLength).IsRequired();
    }
}

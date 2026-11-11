using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Persistence.Configurations;

internal sealed class GameSessionPlayerColorConfiguration : IEntityTypeConfiguration<GameSessionPlayerColor>
{
    public void Configure(EntityTypeBuilder<GameSessionPlayerColor> builder)
    {
        builder.ToTable("GameSessionPlayerColors", table => table.HasCheckConstraint(
            "CK_GameSessionPlayerColors_Color", "Color BETWEEN 1 AND 12"));
        builder.HasKey(color => new { color.GameSessionId, color.PlayerId });
        builder.Property(color => color.Color).HasConversion<int>().IsRequired();
        builder.HasOne<GameSessionPlayer>().WithOne().HasForeignKey<GameSessionPlayerColor>(color => new { color.GameSessionId, color.PlayerId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(color => new { color.GameSessionId, color.Color }).IsUnique();
    }
}

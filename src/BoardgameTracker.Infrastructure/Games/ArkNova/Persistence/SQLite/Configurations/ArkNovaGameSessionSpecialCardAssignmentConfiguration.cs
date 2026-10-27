using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Configurations;

internal sealed class ArkNovaGameSessionSpecialCardAssignmentConfiguration
    : IEntityTypeConfiguration<ArkNovaGameSessionSpecialCardAssignment>
{
    public void Configure(EntityTypeBuilder<ArkNovaGameSessionSpecialCardAssignment> builder)
    {
        builder.ToTable("ArkNovaGameSessionSpecialCardAssignments");
        builder.HasKey(x => new { x.GameSessionId, x.PlayerId, x.SpecialCard });
        builder.Property(x => x.SpecialCard).HasConversion<int>().IsRequired();

        builder.HasOne<GameSessionPlayer>()
            .WithMany()
            .HasForeignKey(x => new { x.GameSessionId, x.PlayerId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

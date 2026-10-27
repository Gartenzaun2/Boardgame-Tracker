using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations.ArkNova;

internal sealed class ArkNovaSessionSpecialCardAssignmentConfiguration
    : IEntityTypeConfiguration<ArkNovaSessionSpecialCardAssignment>
{
    public void Configure(EntityTypeBuilder<ArkNovaSessionSpecialCardAssignment> builder)
    {
        builder.ToTable("ArkNovaSessionSpecialCardAssignments");
        builder.HasKey(x => new { x.GameSessionId, x.PersonId, x.SpecialCard });
        builder.Property(x => x.SpecialCard).HasConversion<int>().IsRequired();

        builder.HasOne<GameSessionPlayer>()
            .WithMany()
            .HasForeignKey(x => new { x.GameSessionId, x.PersonId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

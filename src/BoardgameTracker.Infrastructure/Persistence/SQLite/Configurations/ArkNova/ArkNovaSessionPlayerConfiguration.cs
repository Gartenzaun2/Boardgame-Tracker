using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations.ArkNova;

internal sealed class ArkNovaSessionPlayerConfiguration
    : IEntityTypeConfiguration<ArkNovaSessionPlayer>
{
    public void Configure(EntityTypeBuilder<ArkNovaSessionPlayer> builder)
    {
        builder.ToTable(
            "ArkNovaSessionPlayers",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_ArkNovaSessionPlayers_DifferentZooPlanOptions",
                    "ZooPlanOption1Id <> ZooPlanOption2Id");
                table.HasCheckConstraint(
                    "CK_ArkNovaSessionPlayers_SelectedZooPlanWasOffered",
                    "SelectedZooPlanId IS NULL OR SelectedZooPlanId = ZooPlanOption1Id OR SelectedZooPlanId = ZooPlanOption2Id");
            });

        builder.HasKey(x => new { x.GameSessionId, x.PersonId });

        builder.Property(x => x.Color)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(x => x.GameSessionPlayer)
            .WithOne()
            .HasForeignKey<ArkNovaSessionPlayer>(x => new { x.GameSessionId, x.PersonId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ZooPlanOption1)
            .WithMany()
            .HasForeignKey(x => x.ZooPlanOption1Id)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ZooPlanOption2)
            .WithMany()
            .HasForeignKey(x => x.ZooPlanOption2Id)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SelectedZooPlan)
            .WithMany()
            .HasForeignKey(x => x.SelectedZooPlanId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GameSessionId, x.Color })
            .IsUnique();
    }
}

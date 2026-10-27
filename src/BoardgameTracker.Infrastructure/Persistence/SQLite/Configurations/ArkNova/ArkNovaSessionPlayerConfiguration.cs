using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations.ArkNova;

/// <summary>
/// Maps the Ark Nova per-session player snapshot, zoo-plan relationships and color uniqueness to SQLite.
/// </summary>
internal sealed class ArkNovaSessionPlayerConfiguration : IEntityTypeConfiguration<ArkNovaSessionPlayer>
{
    public void Configure(EntityTypeBuilder<ArkNovaSessionPlayer> builder)
    {
        builder.ToTable(
            "ArkNovaSessionPlayers",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_ArkNovaSessionPlayers_DifferentZooPlanOptions",
                    "ZooPlanOption1Number <> ZooPlanOption2Number");
                table.HasCheckConstraint(
                    "CK_ArkNovaSessionPlayers_SelectedZooPlanWasOffered",
                    "SelectedZooPlanNumber = ZooPlanOption1Number OR SelectedZooPlanNumber = ZooPlanOption2Number");
            });

        builder.HasKey(x => new { x.GameSessionId, x.PersonId });
        builder.Property(x => x.Color).HasConversion<int>().IsRequired();
        builder.Property(x => x.SelectedZooPlanNumber).IsRequired();

        builder.HasOne<GameSessionPlayer>()
            .WithOne()
            .HasForeignKey<ArkNovaSessionPlayer>(x => new { x.GameSessionId, x.PersonId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ArkNovaZooPlan>()
            .WithMany()
            .HasForeignKey(x => x.ZooPlanOption1Number)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ArkNovaZooPlan>()
            .WithMany()
            .HasForeignKey(x => x.ZooPlanOption2Number)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ArkNovaZooPlan>()
            .WithMany()
            .HasForeignKey(x => x.SelectedZooPlanNumber)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GameSessionId, x.Color }).IsUnique();
    }
}

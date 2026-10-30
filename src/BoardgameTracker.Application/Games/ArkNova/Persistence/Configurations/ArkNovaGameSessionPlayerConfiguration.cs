using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.Persistence.Configurations;

/// <summary>
/// Maps the Ark Nova per-GameSession player snapshot, zoo-plan relationships and color uniqueness to the relational model.
/// </summary>
internal sealed class ArkNovaGameSessionPlayerConfiguration : IEntityTypeConfiguration<ArkNovaGameSessionPlayer>
{
    public void Configure(EntityTypeBuilder<ArkNovaGameSessionPlayer> builder)
    {
        builder.ToTable(
            "ArkNovaGameSessionPlayers",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_ArkNovaGameSessionPlayers_DifferentZooPlanOptions",
                    "ZooPlanOption1Number <> ZooPlanOption2Number");
                table.HasCheckConstraint(
                    "CK_ArkNovaGameSessionPlayers_SelectedZooPlanWasOffered",
                    "SelectedZooPlanNumber = ZooPlanOption1Number OR SelectedZooPlanNumber = ZooPlanOption2Number");
                table.HasCheckConstraint(
                    "CK_ArkNovaGameSessionPlayers_Attractiveness_Range",
                    "Attractiveness IS NULL OR (Attractiveness >= 0 AND Attractiveness <= 113)");
                table.HasCheckConstraint(
                    "CK_ArkNovaGameSessionPlayers_Conservation_Range",
                    "Conservation IS NULL OR (Conservation >= 0 AND Conservation <= 41)");
                table.HasCheckConstraint(
                    "CK_ArkNovaGameSessionPlayers_Reputation_Range",
                    "Reputation IS NULL OR (Reputation >= 1 AND Reputation <= 15)");
            });

        builder.HasKey(x => new { x.GameSessionId, x.PlayerId });
        builder.Property(x => x.Color).HasConversion<int>().IsRequired();
        builder.Property(x => x.SelectedZooPlanNumber).IsRequired();

        builder.HasOne<GameSessionPlayer>()
            .WithOne()
            .HasForeignKey<ArkNovaGameSessionPlayer>(x => new { x.GameSessionId, x.PlayerId })
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

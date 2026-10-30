using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.Persistence.Configurations;

/// <summary>
/// Maps Ark Nova group-specific default player colors and their uniqueness constraint to the relational model.
/// </summary>
internal sealed class ArkNovaGroupPlayerConfiguration : IEntityTypeConfiguration<ArkNovaGroupPlayer>
{
    public void Configure(EntityTypeBuilder<ArkNovaGroupPlayer> builder)
    {
        builder.ToTable("ArkNovaGroupPlayers");
        builder.HasKey(x => new { x.GroupId, x.PlayerId });
        builder.Property(x => x.DefaultColor).HasConversion<int>().IsRequired();

        builder.HasOne<GroupPlayer>()
            .WithOne()
            .HasForeignKey<ArkNovaGroupPlayer>(x => new { x.GroupId, x.PlayerId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GroupId, x.DefaultColor }).IsUnique();
    }
}

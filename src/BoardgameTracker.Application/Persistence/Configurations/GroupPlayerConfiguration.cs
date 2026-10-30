using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Persistence.Configurations;

/// <summary>
/// Maps group membership, default ordering and uniqueness constraints to the relational model.
/// </summary>
internal sealed class GroupPlayerConfiguration : IEntityTypeConfiguration<GroupPlayer>
{
    public void Configure(EntityTypeBuilder<GroupPlayer> builder)
    {
        builder.ToTable(
            "GroupPlayers",
            table => table.HasCheckConstraint(
                "CK_GroupPlayers_DefaultOrder_Positive",
                "DefaultOrder > 0"));

        builder.HasKey(x => new { x.GroupId, x.PlayerId });
        builder.Property(x => x.DefaultOrder).IsRequired();

        builder.HasOne<Group>()
            .WithMany(x => x.Players)
            .HasForeignKey(x => x.GroupId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GroupId, x.DefaultOrder }).IsUnique();
    }
}

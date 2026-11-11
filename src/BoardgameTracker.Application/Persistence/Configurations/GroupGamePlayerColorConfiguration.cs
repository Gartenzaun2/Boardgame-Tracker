using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Persistence.Configurations;

internal sealed class GroupGamePlayerColorConfiguration : IEntityTypeConfiguration<GroupGamePlayerColor>
{
    public void Configure(EntityTypeBuilder<GroupGamePlayerColor> builder)
    {
        builder.ToTable("GroupGamePlayerColors", table => table.HasCheckConstraint(
            "CK_GroupGamePlayerColors_Color", "DefaultColor BETWEEN 1 AND 12"));
        builder.HasKey(color => new { color.GroupId, color.GameId, color.PlayerId });
        builder.Property(color => color.DefaultColor).HasConversion<int>().IsRequired();
        builder.HasOne<GroupPlayer>().WithMany().HasForeignKey(color => new { color.GroupId, color.PlayerId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Game>().WithMany().HasForeignKey(color => color.GameId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(color => new { color.GroupId, color.GameId, color.DefaultColor }).IsUnique();
    }
}

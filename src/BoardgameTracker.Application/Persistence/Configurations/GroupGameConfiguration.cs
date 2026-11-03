using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Persistence.Configurations;

internal sealed class GroupGameConfiguration : IEntityTypeConfiguration<GroupGame>
{
    public void Configure(EntityTypeBuilder<GroupGame> builder)
    {
        builder.ToTable("GroupGames");
        builder.HasKey(assignment => new { assignment.GroupId, assignment.GameId });
        builder.HasOne<Group>().WithMany().HasForeignKey(assignment => assignment.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Game>().WithMany().HasForeignKey(assignment => assignment.GameId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

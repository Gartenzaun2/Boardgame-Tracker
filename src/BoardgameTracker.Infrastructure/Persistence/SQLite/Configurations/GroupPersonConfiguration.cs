using BoardgameTracker.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations;

internal sealed class GroupPersonConfiguration : IEntityTypeConfiguration<GroupPerson>
{
    public void Configure(EntityTypeBuilder<GroupPerson> builder)
    {
        builder.ToTable("GroupPersons");

        builder.HasKey(x => new { x.GroupId, x.PersonId });

        builder.Property(x => x.DefaultOrder)
            .IsRequired();

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Person)
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using BoardgameTracker.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations;

internal sealed class GroupPersonConfiguration : IEntityTypeConfiguration<GroupPerson>
{
    public void Configure(EntityTypeBuilder<GroupPerson> builder)
    {
        builder.ToTable(
            "GroupPersons",
            table => table.HasCheckConstraint(
                "CK_GroupPersons_DefaultOrder_Positive",
                "DefaultOrder > 0"));

        builder.HasKey(x => new { x.GroupId, x.PersonId });

        builder.Property(x => x.DefaultOrder)
            .IsRequired();

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.GroupId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Person)
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GroupId, x.DefaultOrder })
            .IsUnique();
    }
}

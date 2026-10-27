using BoardgameTracker.Core.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations;

/// <summary>
/// Maps reusable person/guest records and activity state to SQLite.
/// </summary>
internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IsGuest)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();
    }
}

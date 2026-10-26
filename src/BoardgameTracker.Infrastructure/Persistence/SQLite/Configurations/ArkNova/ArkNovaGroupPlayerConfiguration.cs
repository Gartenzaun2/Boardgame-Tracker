using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Configurations.ArkNova;

internal sealed class ArkNovaGroupPlayerConfiguration : IEntityTypeConfiguration<ArkNovaGroupPlayer>
{
    public void Configure(EntityTypeBuilder<ArkNovaGroupPlayer> builder)
    {
        builder.ToTable("ArkNovaGroupPlayers");
        builder.HasKey(x => new { x.GroupId, x.PersonId });
        builder.Property(x => x.DefaultColor).HasConversion<int>().IsRequired();

        builder.HasOne<GroupPerson>()
            .WithOne()
            .HasForeignKey<ArkNovaGroupPlayer>(x => new { x.GroupId, x.PersonId })
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GroupId, x.DefaultColor }).IsUnique();
    }
}

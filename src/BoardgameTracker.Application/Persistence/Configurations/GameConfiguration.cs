using BoardgameTracker.Core.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Persistence.Configurations;

/// <summary>
/// Maps supported-game reference data and its stable unique key to the relational model.
/// </summary>
internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games", table =>
        {
            table.HasCheckConstraint("CK_Games_Key", "length(trim(\"Key\")) BETWEEN 1 AND 100 AND \"Key\" = trim(\"Key\")");
            table.HasCheckConstraint("CK_Games_Name", "length(trim(\"Name\")) BETWEEN 1 AND 100 AND \"Name\" = trim(\"Name\")");
            table.HasCheckConstraint("CK_Games_BadgeText", "length(trim(\"BadgeText\")) > 0 AND \"BadgeText\" = trim(\"BadgeText\")");
            table.HasCheckConstraint("CK_Games_PlayerLimits", "\"MinPlayers\" >= 1 AND \"MaxPlayers\" >= \"MinPlayers\"");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BadgeText).IsRequired();
        builder.Property(x => x.MinPlayers).IsRequired();
        builder.Property(x => x.MaxPlayers).IsRequired();
        builder.Property(x => x.Key).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Throw);
    }
}

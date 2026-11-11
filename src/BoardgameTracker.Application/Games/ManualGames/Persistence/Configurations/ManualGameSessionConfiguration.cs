using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardgameTracker.Application.Games.ManualGames.Persistence.Configurations;

internal sealed class ManualGameSessionConfiguration : IEntityTypeConfiguration<ManualGameSession>
{
    public void Configure(EntityTypeBuilder<ManualGameSession> builder)
    {
        builder.ToTable("ManualGameSessions");
        builder.HasKey(session => session.GameSessionId);
        builder.HasOne<GameSession>().WithOne().HasForeignKey<ManualGameSession>(session => session.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ComplexProperty(session => session.ScoringRulesSnapshot, snapshot =>
        {
            snapshot.IsRequired();
            snapshot.Property(rules => rules.PointsDirection).HasConversion<int?>();
        });
    }
}

internal sealed class ManualGameSessionPlayerConfiguration : IEntityTypeConfiguration<ManualGameSessionPlayer>
{
    public void Configure(EntityTypeBuilder<ManualGameSessionPlayer> builder)
    {
        builder.ToTable("ManualGameSessionPlayers");
        builder.HasKey(player => new { player.GameSessionId, player.PlayerId });
        builder.HasOne<GameSessionPlayer>().WithOne().HasForeignKey<ManualGameSessionPlayer>(player => new { player.GameSessionId, player.PlayerId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ManualGameSession>().WithMany().HasForeignKey(player => player.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

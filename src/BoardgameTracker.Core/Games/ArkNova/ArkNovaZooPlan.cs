namespace BoardgameTracker.Core.Games.ArkNova;

public sealed class ArkNovaZooPlan
{
    public int Id { get; init; }

    public int Number { get; init; }

    public required string NameResourceKey { get; init; }

    public required string IconKey { get; init; }
}

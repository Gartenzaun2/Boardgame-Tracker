namespace BoardgameTracker.Core.Games.ArkNova;

public sealed class ArkNovaSessionPlayer
{
    public Guid GameSessionId { get; init; }

    public Guid PersonId { get; init; }

    public ArkNovaPlayerColor Color { get; set; }

    public int ZooPlanOption1Id { get; set; }

    public int ZooPlanOption2Id { get; set; }

    public int? SelectedZooPlanId { get; set; }

    public int? Attractiveness { get; set; }

    public int? Conservation { get; set; }

    public int? Reputation { get; set; }
}

using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
namespace BoardgameTracker.Core.Games.ArkNova.GameSessions;

/// <summary>
/// Represents the Ark Nova-specific snapshot for one player in a persisted game gameSession,
/// including the selected zoo plan and score values.
/// </summary>
public sealed class ArkNovaGameSessionPlayer
{
    private ArkNovaGameSessionPlayer()
    {
    }

    public ArkNovaGameSessionPlayer(
        Guid gameSessionId,
        Guid playerId,
        int selectedZooPlanNumber)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        ArkNovaZooPlan.ValidateNumber(selectedZooPlanNumber);

        GameSessionId = gameSessionId;
        PlayerId = playerId;
        SelectedZooPlanNumber = selectedZooPlanNumber;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public int SelectedZooPlanNumber { get; private set; }
    public int? Attractiveness { get; private set; }
    public int? Conservation { get; private set; }
    public int? Reputation { get; private set; }

    public void SetScoreValues(int? attractiveness, int? conservation, int? reputation)
    {
        ArkNovaScoringRules.ValidateScoreValues(attractiveness, conservation, reputation);

        Attractiveness = attractiveness;
        Conservation = conservation;
        Reputation = reputation;
    }

}

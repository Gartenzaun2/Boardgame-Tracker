using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Core.Games.ArkNova.GameSessions;

/// <summary>
/// Represents the Ark Nova-specific snapshot for one player in a persisted game gameSession,
/// including the actual color and the selected zoo plan.
/// </summary>
public sealed class ArkNovaGameSessionPlayer
{
    private ArkNovaGameSessionPlayer()
    {
    }

    public ArkNovaGameSessionPlayer(
        Guid gameSessionId,
        Guid playerId,
        PlayerColor color,
        int selectedZooPlanNumber)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        ArkNovaZooPlan.ValidateNumber(selectedZooPlanNumber);
        ValidateColor(color);

        GameSessionId = gameSessionId;
        PlayerId = playerId;
        Color = color;
        SelectedZooPlanNumber = selectedZooPlanNumber;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public PlayerColor Color { get; private set; }
    public int SelectedZooPlanNumber { get; private set; }
    public int? Attractiveness { get; private set; }
    public int? Conservation { get; private set; }
    public int? Reputation { get; private set; }

    public void SetColor(PlayerColor color)
    {
        ValidateColor(color);
        Color = color;
    }

    public void SetScoreValues(int? attractiveness, int? conservation, int? reputation)
    {
        ArkNovaScoringRules.ValidateScoreValues(attractiveness, conservation, reputation);

        Attractiveness = attractiveness;
        Conservation = conservation;
        Reputation = reputation;
    }

    private static void ValidateColor(PlayerColor color)
    {
        if (!BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColors.Rules.Allows(color))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown Ark Nova player color.");
        }
    }
}

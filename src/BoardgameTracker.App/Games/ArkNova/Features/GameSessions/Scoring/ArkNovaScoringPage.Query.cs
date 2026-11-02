using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaScoringPage
{
    private Guid _gameSessionId;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? value))
        {
            // Shell may reapply a query without the single-use session ID on return.
            if (_gameSessionId != Guid.Empty)
                return;
        }
        else if (value is Guid gameSessionId && gameSessionId != Guid.Empty)
        {
            _gameSessionId = gameSessionId;
            return;
        }

        throw new InvalidOperationException($"Required navigation parameter '{NavigationParameters.GameSessionId}' is missing or invalid.");
    }
}

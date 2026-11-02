using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;

public partial class ArkNovaResultPage
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
            bool historical = query.TryGetValue(NavigationParameters.ResultIsHistorical, out object? context) && context is true;
            BackButton.IsVisible = historical;
            EditButton.IsVisible = historical;
            DoneButton.IsVisible = !historical;
            return;
        }

        throw new InvalidOperationException($"Required navigation parameter '{NavigationParameters.GameSessionId}' is missing or invalid.");
    }
}

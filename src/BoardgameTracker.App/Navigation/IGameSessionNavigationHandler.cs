using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.App.Navigation;

public interface IGameSessionNavigationHandler
{
    bool CanHandle(string gameKey);
    Task OpenGameSessionAsync(Guid gameSessionId);
    Task OpenGroupAsync(string gameKey, Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus);
}

using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.App.Navigation;

public interface IGameSessionNavigationHandler
{
    string GameKey { get; }
    string DisplayName { get; }
    Task OpenGameSessionAsync(Guid gameSessionId);
    Task OpenGroupAsync(Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus);
}

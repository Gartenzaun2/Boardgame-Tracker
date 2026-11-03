using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.App.Navigation;

public interface IGameSessionNavigationHandler
{
    string GameKey { get; }
    string DisplayName { get; }
    string BadgeText { get; }
    Task OpenGameSessionAsync(Guid gameSessionId);
    Task OpenGroupAsync(Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus);
}

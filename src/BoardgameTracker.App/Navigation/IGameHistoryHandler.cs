namespace BoardgameTracker.App.Navigation;

public interface IGameHistoryHandler
{
    string GameKey { get; }
    string DisplayName { get; }
    Task OpenAsync(Guid gameSessionId);
}

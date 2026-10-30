namespace BoardgameTracker.App.Navigation;

public interface IGameHistoryHandler
{
    string GameKey { get; }
    Task OpenAsync(Guid gameSessionId);
}

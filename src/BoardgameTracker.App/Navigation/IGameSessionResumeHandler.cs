using BoardgameTracker.Application.GameSessions;

namespace BoardgameTracker.App.Navigation;

public interface IGameSessionResumeHandler
{
    bool CanHandle(string gameKey);

    Task ResumeAsync(AppShell shell, OpenGameSession gameSession);
}

using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Games.ArkNova.Navigation;

public sealed class ArkNovaGameSessionResumeHandler : IGameSessionResumeHandler
{
    public bool CanHandle(string gameKey) => gameKey == ArkNovaGame.Key;

    public async Task ResumeAsync(AppShell shell, OpenGameSession gameSession)
    {
        await shell.NavigateToGroupsForGameAsync(gameSession.GameKey, remember: false);
        shell.RememberGameSession(gameSession.GameSessionId, gameSession.GameKey);

        string route = gameSession.Status switch
        {
            GameSessionStatus.Running => nameof(ArkNovaRunningPage),
            GameSessionStatus.Scoring => nameof(ArkNovaScoringPage),
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(route))
            return;

        await shell.GoToAsync(route, new ShellNavigationQueryParameters
        {
            [NavigationParameters.GameSessionId] = gameSession.GameSessionId
        });
    }
}

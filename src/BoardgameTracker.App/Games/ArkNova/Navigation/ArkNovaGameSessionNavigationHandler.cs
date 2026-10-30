using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Games.ArkNova.Navigation;

public sealed class ArkNovaGameSessionNavigationHandler(ArkNovaScoringUseCase scoring) : IGameSessionNavigationHandler, IGameSessionResumeHandler
{
    public string GameKey => ArkNovaGame.Key;
    public string DisplayName => AppResources.ArkNova;

    public async Task OpenGameSessionAsync(Guid gameSessionId)
    {
        ArkNovaGameSessionResultData data = await scoring.LoadGameSessionResultAsync(gameSessionId)
            ?? throw new InvalidOperationException("The game session does not exist.");
        await NavigateToGameSessionAsync(Shell.Current, gameSessionId, data.Status);
    }
    public async Task OpenGroupAsync(Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus)
    {
        if (openGameSessionId is Guid gameSessionId)
        {
            if (openGameSessionStatus is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
                throw new InvalidOperationException($"Unsupported open GameSession status '{openGameSessionStatus}'.");
            if (Shell.Current is AppShell shell)
                shell.RememberGameSession(gameSessionId, GameKey);
            await NavigateToGameSessionAsync(Shell.Current, gameSessionId, openGameSessionStatus.Value);
            return;
        }
        await Shell.Current.GoToAsync(nameof(ArkNovaPreparationPage), new ShellNavigationQueryParameters
        {
            [NavigationParameters.GroupId] = groupId
        });
    }
    public bool CanHandle(string gameKey) => gameKey == GameKey;

    public async Task ResumeAsync(AppShell shell, OpenGameSession gameSession)
    {
        await shell.NavigateToGroupsForGameAsync(gameSession.GameKey, remember: false);
        shell.RememberGameSession(gameSession.GameSessionId, gameSession.GameKey);
        // Resume restores context only for open GameSessions; history also opens Completed.
        if (gameSession.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            return;
        await NavigateToGameSessionAsync(shell, gameSession.GameSessionId, gameSession.Status);
    }

    private static Task NavigateToGameSessionAsync(Shell shell, Guid gameSessionId, GameSessionStatus status)
    {
        string route = status == GameSessionStatus.Running ? nameof(ArkNovaRunningPage) : nameof(ArkNovaScoringPage);
        return shell.GoToAsync(route, new ShellNavigationQueryParameters
        {
            [NavigationParameters.GameSessionId] = gameSessionId
        });
    }
}

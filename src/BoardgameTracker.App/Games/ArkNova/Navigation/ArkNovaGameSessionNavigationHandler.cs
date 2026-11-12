using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;

namespace BoardgameTracker.App.Games.ArkNova.Navigation;

public sealed class ArkNovaGameSessionNavigationHandler(ArkNovaScoringUseCase scoring) : IGameSessionNavigationHandler, IGameSessionResumeHandler
{
    public string GameKey => ArkNovaGame.Key;
    public static string GameDisplayName => AppResources.ArkNova;
    public static string GameBadgeText => AppResources.ArkNova;

    public async Task OpenGameSessionAsync(Guid gameSessionId)
    {
        ArkNovaGameSessionResultData data = await scoring.LoadGameSessionResultAsync(gameSessionId)
            ?? throw new InvalidOperationException("The game session does not exist.");
        await NavigateToGameSessionAsync(Shell.Current, gameSessionId, data.Status);
    }
    public async Task OpenGroupAsync(string gameKey, Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus)
    {
        if (openGameSessionId is Guid gameSessionId)
        {
            if (openGameSessionStatus is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
                throw new InvalidOperationException($"Unsupported open GameSession status '{openGameSessionStatus}'.");
            await NavigateToGameSessionAsync(Shell.Current, gameSessionId, openGameSessionStatus.Value);
            return;
        }
        await Shell.Current.GoToAsync(nameof(ArkNovaPreparationPage), false, new ShellNavigationQueryParameters
        {
            [NavigationParameters.GroupId] = groupId
        });
    }
    public bool CanHandle(string gameKey) => gameKey == GameKey;

    public async Task ResumeAsync(AppShell shell, OpenGameSession gameSession)
    {
        if (gameSession.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            return;
        await shell.NavigateToGroupsForGameAsync(gameSession.GameKey, remember: false,
            destinationRoute: GetSessionRoute(gameSession.Status),
            parameters: new() { [NavigationParameters.GameSessionId] = gameSession.GameSessionId },
            resumeGameSessionId: gameSession.GameSessionId);
    }

    private static Task NavigateToGameSessionAsync(Shell shell, Guid gameSessionId, GameSessionStatus status)
    {
        string route = GetSessionRoute(status);
        ShellNavigationQueryParameters parameters = new()
        {
            [NavigationParameters.GameSessionId] = gameSessionId,
            [NavigationParameters.ResultIsHistorical] = status == GameSessionStatus.Completed
        };
        return shell is AppShell appShell
            ? appShell.OpenGameSessionPageAsync(route, parameters, ArkNovaGame.Key,
                status is GameSessionStatus.Running or GameSessionStatus.Scoring ? gameSessionId : null)
            : shell.GoToAsync(route, false, parameters);
    }

    private static string GetSessionRoute(GameSessionStatus status) => status switch
        {
            GameSessionStatus.Running => nameof(ArkNovaRunningPage),
            GameSessionStatus.Scoring => nameof(ArkNovaScoringPage),
            GameSessionStatus.Completed => nameof(ArkNovaResultPage),
            _ => throw new InvalidOperationException($"Unsupported GameSession status '{status}'.")
        };
}

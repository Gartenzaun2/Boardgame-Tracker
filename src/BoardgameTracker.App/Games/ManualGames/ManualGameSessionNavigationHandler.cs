using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Running;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;

namespace BoardgameTracker.App.Games.ManualGames;

public sealed class ManualGameSessionNavigationHandler(ManualRunningGameSessionQuery query)
    : IGameSessionNavigationHandler, IGameSessionResumeHandler
{
    public bool CanHandle(string gameKey) => ManualGame.MatchesKey(gameKey);
    public async Task OpenGameSessionAsync(Guid gameSessionId)
    {
        ManualRunningData data = await query.GetAsync(gameSessionId)
            ?? throw new InvalidOperationException("The manual session does not exist.");
        if (data.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            throw new InvalidOperationException("Manual results are not available yet.");
        if (Shell.Current is AppShell shell)
        {
            shell.CaptureGameSessionCaller();
            shell.RememberGameSession(gameSessionId, data.GameKey);
        }
        await Shell.Current.GoToAsync(nameof(ManualRunningPage), false,
            new ShellNavigationQueryParameters { [NavigationParameters.GameSessionId] = gameSessionId });
    }
    public async Task OpenGroupAsync(string gameKey, Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus)
    {
        if (!CanHandle(gameKey))
            throw new ArgumentException("The key does not belong to ManualGames.", nameof(gameKey));
        Guid? current = await query.FindOpenAsync(gameKey, groupId);
        if (current is Guid id)
        {
            await OpenGameSessionAsync(id);
            return;
        }
        await Shell.Current.GoToAsync(nameof(ManualPreparationPage), false, new ShellNavigationQueryParameters
        {
            [NavigationParameters.GameKey] = gameKey,
            [NavigationParameters.GroupId] = groupId
        });
    }
    public async Task ResumeAsync(AppShell shell, OpenGameSession gameSession)
    {
        if (!CanHandle(gameSession.GameKey) || gameSession.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            return;
        ManualRunningData? data = await query.GetAsync(gameSession.GameSessionId);
        if (data is null || data.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            return;
        await shell.NavigateToGroupsForGameAsync(data.GameKey, remember: false,
            destinationRoute: nameof(ManualRunningPage),
            parameters: new() { [NavigationParameters.GameSessionId] = data.SessionId });
        shell.CaptureGameSessionCaller(useRoot: true);
        shell.RememberGameSession(data.SessionId, data.GameKey);
    }
}

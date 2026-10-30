using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Games.ArkNova.Navigation;

public sealed class ArkNovaGameHistoryHandler(ArkNovaScoringUseCase scoring) : IGameHistoryHandler
{
    public string GameKey => ArkNovaGame.Key;
    public string DisplayName => AppResources.ArkNova;

    public async Task OpenAsync(Guid gameSessionId)
    {
        ArkNovaScoringGameSessionData data = await scoring.LoadAsync(gameSessionId)
            ?? throw new InvalidOperationException("The game session does not exist.");
        string route = data.Status == GameSessionStatus.Running ? nameof(ArkNovaRunningPage) : nameof(ArkNovaScoringPage);
        await Shell.Current.GoToAsync(route, new ShellNavigationQueryParameters { [NavigationParameters.GameSessionId] = gameSessionId });
    }
}

using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Result;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;

namespace BoardgameTracker.App.Games.ManualGames;

public sealed class ManualGameSessionNavigationHandler(ManualRunningGameSessionQuery query, ManualScoringQuery scoring,
    CompleteManualGameSessionUseCase complete) : IGameSessionNavigationHandler, IGameSessionResumeHandler
{
    public const string CorrectionParameter = "ManualCorrection";
    public const string CorrectionRoute = "ManualCorrectionPage";
    public const string PointsRoute = "ManualPointsPage";
    public const string DecisionRoute = "ManualDecisionPage";
    public const string PointsStepParameter = "ManualPointsStep";
    public bool CanHandle(string gameKey) => ManualGame.MatchesKey(gameKey);

    private async Task<string> ResolveRouteAsync(Guid id)
    {
        ManualScoringData data = await scoring.GetAsync(id) ?? throw new InvalidOperationException("The manual session does not exist.");
        if (data.Session.Status == GameSessionStatus.Running)
            return nameof(ManualRunningPage);
        if (data.Session.Status == GameSessionStatus.Completed)
            return nameof(ManualResultPage);
        ScoringRulesSnapshot rules = data.Session.ScoringRules;
        if (!rules.CapturePoints && !rules.CaptureWinner)
        {
            await complete.CompleteAsync(id);
            return nameof(ManualResultPage);
        }
        ManualDecision decision = ManualScoringRules.GetDecision(rules, data.Values);
        return rules.CapturePoints && (data.Values.Any(player => player.Points is null) || decision.Kind == ManualDecisionKind.None)
            ? PointsRoute : DecisionRoute;
    }
    private static ShellNavigationQueryParameters Parameters(Guid id, string route) => new()
    {
        [NavigationParameters.GameSessionId] = id,
        [PointsStepParameter] = route == PointsRoute
    };
    public async Task OpenGameSessionAsync(Guid gameSessionId)
    {
        string route = await ResolveRouteAsync(gameSessionId);
        ManualRunningData data = await query.GetAsync(gameSessionId) ?? throw new InvalidOperationException("Manual session not found.");
        if (Shell.Current is AppShell shell)
        {
            await shell.OpenGameSessionPageAsync(route, Parameters(gameSessionId, route), data.GameKey,
                data.Status != GameSessionStatus.Completed ? gameSessionId : null);
        }
        else
            await Shell.Current.GoToAsync(route, false, Parameters(gameSessionId, route));
    }
    public async Task OpenCorrectionAsync(Guid gameSessionId)
    {
        ManualScoringData data = await scoring.GetAsync(gameSessionId) ?? throw new InvalidOperationException("Manual session not found.");
        if (data.Session.Status != GameSessionStatus.Completed
            || !ManualHistoricalResult.FromValues(data.Values, data.Session.ScoringRules).CanCorrect)
            throw new InvalidOperationException("Only completed sessions with recorded results can be corrected.");
        await Shell.Current.GoToAsync(CorrectionRoute, false, new ShellNavigationQueryParameters
        {
            [NavigationParameters.GameSessionId] = gameSessionId,
            [CorrectionParameter] = true
        });
    }

    // Both cancel and save pop the single correction page; the retained result reloads on appearing.
    public Task ReturnFromCorrectionAsync() => Shell.Current.GoToAsync("..", false);

    // Internal flow transitions retain the caller captured at entry, including a history caller.
    public async Task ContinueAsync(Guid id)
    {
        string route = await ResolveRouteAsync(id);
        await Shell.Current.GoToAsync(route, false, Parameters(id, route));
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
            [NavigationParameters.GameKey] = gameKey, [NavigationParameters.GroupId] = groupId
        });
    }
    public async Task ResumeAsync(AppShell shell, OpenGameSession gameSession)
    {
        if (!CanHandle(gameSession.GameKey) || gameSession.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            return;
        ManualRunningData? data = await query.GetAsync(gameSession.GameSessionId);
        if (data is null)
            return;
        string route = await ResolveRouteAsync(data.SessionId);
        await shell.NavigateToGroupsForGameAsync(data.GameKey, remember: false, destinationRoute: route,
            parameters: Parameters(data.SessionId, route),
            resumeGameSessionId: route != nameof(ManualResultPage) ? data.SessionId : null);
    }
}

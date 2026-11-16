using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.Application.Settings.Navigation;

namespace BoardgameTracker.App.Navigation;

/// <summary>Adapts ViewModel navigation requests to the active MAUI AppShell.</summary>
public sealed class ShellNavigationService : INavigationService {
    private readonly IEnumerable<IGameSessionNavigationHandler> _gameSessionNavigationHandlers;

    public ShellNavigationService(IEnumerable<IGameSessionNavigationHandler> gameSessionNavigationHandlers) {
        _gameSessionNavigationHandlers = gameSessionNavigationHandlers;
    }

    // Resolve the active Shell only when called: constructor injection could create
    // a cycle through AppShell's Pages and their ViewModels.
    public Task GoBackAsync()
        => AppShell.CurrentInstance.GoToAsync("..", false);

    public Task NavigateToMainSectionAsync(AppMainSection section)
        => AppShell.CurrentInstance.NavigateToMainSectionAsync(section);

    public Task OpenGroupCreationAsync(string gameKey)
        => AppShell.CurrentInstance.GoToAsync(nameof(CreateGroupPage), false,
            new ShellNavigationQueryParameters { [NavigationParameters.GameKey] = gameKey });

    public Task ShowGroupDuplicatesAsync(CreateGroupViewModel draft)
        => AppShell.CurrentInstance.GoToAsync(nameof(DuplicateGroupPage), false,
            new ShellNavigationQueryParameters { [NavigationParameters.GroupDraft] = draft });

    public async Task CompleteGroupCreationAsync(CreateGroupViewModel draft, GroupSaveOutcome outcome) {
        if (outcome.GameKey is null) {
            await AppShell.CurrentInstance.GoToAsync(outcome.FromDuplicates ? "../.." : "..", false);
            return;
        }

        IGameSessionNavigationHandler handler = _gameSessionNavigationHandlers.Single(handler => handler.CanHandle(outcome.GameKey));
        await handler.OpenGroupAsync(outcome.GameKey, outcome.GroupId, null, null);
        // Remove only this completed draft, and only after preparation opened successfully.
        var navigation = AppShell.CurrentInstance.CurrentItem.CurrentItem.Navigation;
        foreach (Page page in navigation.NavigationStack.Where(page =>
            page is CreateGroupPage creation && ReferenceEquals(creation.ViewModel, draft)
            || page is DuplicateGroupPage duplicate && ReferenceEquals(duplicate.ViewModel, draft)).ToArray())
            navigation.RemovePage(page);
    }
}

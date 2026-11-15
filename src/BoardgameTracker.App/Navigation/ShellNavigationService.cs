using BoardgameTracker.Application.Settings.Navigation;

namespace BoardgameTracker.App.Navigation;

/// <summary>Adapts ViewModel navigation requests to the active MAUI AppShell.</summary>
public sealed class ShellNavigationService : INavigationService {
    // Resolve the active Shell only when called: constructor injection could create
    // a cycle through AppShell's Pages and their ViewModels.
    public Task GoBackAsync()
        => AppShell.CurrentInstance.GoToAsync("..", false);

    public Task NavigateToMainSectionAsync(AppMainSection section)
        => AppShell.CurrentInstance.NavigateToMainSectionAsync(section);
}

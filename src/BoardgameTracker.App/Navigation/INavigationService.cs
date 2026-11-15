using BoardgameTracker.Application.Settings.Navigation;

namespace BoardgameTracker.App.Navigation;

/// <summary>
/// Lets ViewModels request navigation without depending on MAUI Shell.
/// A Shell adapter performs the navigation, while tests can replace
/// this interface with a fake to verify navigation without a running UI.
/// </summary>
/// <example>
/// After a successful save:
/// <code>await _navigationService.GoBackAsync();</code>
/// </example>
public interface INavigationService {
    /// <summary>Returns to the previous page in the active Shell navigation stack.</summary>
    Task GoBackAsync();

    /// <summary>Selects a main section using the application's existing navigation behavior.</summary>
    Task NavigateToMainSectionAsync(AppMainSection section);
}

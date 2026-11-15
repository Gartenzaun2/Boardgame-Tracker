using BoardgameTracker.Application.Settings.Navigation;

namespace BoardgameTracker.App.Navigation;

/// <summary>
/// Lets ViewModels request navigation by intent without depending on MAUI Shell,
/// concrete Pages or route strings. Direct Shell calls would require a running
/// MAUI UI even when testing only a ViewModel's workflow.
/// </summary>
/// <remarks>
/// <para>
/// A ViewModel coordinates validation, saving and the decision to navigate, then
/// awaits the requested operation. The Shell adapter translates that intent into
/// MAUI navigation and uses AppShell's existing main-section behavior. Pages and
/// their code-behind remain responsible for UI lifecycle and framework events.
/// </para>
/// <para>
/// Tests can inject a fake that records back requests or the requested main
/// section and returns a completed Task. This allows a save workflow to be tested
/// without creating a Page or starting Shell. A fake can also return a failed
/// Task to test navigation errors independently of persistence. The small cost
/// of this separation is one interface, one adapter and one DI registration;
/// it does not make every other ViewModel dependency framework-independent.
/// </para>
/// </remarks>
/// <example>
/// Inject INavigationService into the ViewModel's constructor and store it in
/// _navigationService. After successful validation and saving, request navigation:
/// <code>
/// await _navigationService.GoBackAsync();
/// </code>
/// To select a main section:
/// <code>
/// await _navigationService.NavigateToMainSectionAsync(AppMainSection.History);
/// </code>
/// A test can inject a recording fake and assert that GoBackAsync was called
/// once after saving, or that History was requested, without invoking MAUI.
/// </example>
public interface INavigationService {
    /// <summary>Returns to the previous page in the active Shell navigation stack.</summary>
    Task GoBackAsync();

    /// <summary>Selects a main section using the application's existing navigation behavior.</summary>
    Task NavigateToMainSectionAsync(AppMainSection section);
}

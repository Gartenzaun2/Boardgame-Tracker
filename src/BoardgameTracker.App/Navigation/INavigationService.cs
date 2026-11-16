using BoardgameTracker.App.Features.Groups.CreateGroup;
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

    /// <summary>Opens a new group draft with the selected play context.</summary>
    Task OpenGroupCreationAsync(string gameKey);

    /// <summary>Opens duplicate selection for the checked, unsaved group draft.</summary>
    Task ShowGroupDuplicatesAsync(CreateGroupViewModel draft);

    /// <summary>Leaves completed creation using its persisted identity and original management or play context.</summary>
    Task CompleteGroupCreationAsync(CreateGroupViewModel draft, GroupSaveOutcome outcome);
}

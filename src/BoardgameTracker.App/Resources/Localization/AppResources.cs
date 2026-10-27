using System.Globalization;
using System.Resources;

namespace BoardgameTracker.App.Resources.Localization;

/// <summary>
/// Provides strongly named access to localized UI strings used by the MAUI app.
/// </summary>
public static class AppResources
{
    private static readonly ResourceManager ResourceManager =
        new("BoardgameTracker.App.Resources.Localization.AppResources",
            typeof(AppResources).Assembly);

    public static string Back =>
        GetString(nameof(Back));

    public static string Groups =>
        GetString(nameof(Groups));

    public static string NoGroupsYet =>
        GetString(nameof(NoGroupsYet));

    public static string NewGroup =>
        GetString(nameof(NewGroup));

    public static string CreateFirstGroupHint =>
        GetString(nameof(CreateFirstGroupHint));

    public static string CreateGroup =>
        GetString(nameof(CreateGroup));

    public static string CreateGroupHint =>
        GetString(nameof(CreateGroupHint));

    public static string GroupName =>
        GetString(nameof(GroupName));

    public static string GroupNamePlaceholder =>
        GetString(nameof(GroupNamePlaceholder));

    public static string TemporaryGroup =>
        GetString(nameof(TemporaryGroup));

    public static string NewPerson =>
        GetString(nameof(NewPerson));

    public static string PersonNamePlaceholder =>
        GetString(nameof(PersonNamePlaceholder));

    public static string Guest =>
        GetString(nameof(Guest));

    public static string AddPerson =>
        GetString(nameof(AddPerson));


    public static string NoAvailablePersons =>
        GetString(nameof(NoAvailablePersons));


    public static string GroupMembers =>
        GetString(nameof(GroupMembers));

    public static string DefaultOrderHint =>
        GetString(nameof(DefaultOrderHint));

    public static string NoMembersYet =>
        GetString(nameof(NoMembersYet));


    public static string EnterPersonName =>
        GetString(nameof(EnterPersonName));


    public static string LoadPersonsFailed =>
        GetString(nameof(LoadPersonsFailed));

    public static string SaveGroupFailed =>
        GetString(nameof(SaveGroupFailed));

    public static string LoadGroupsFailed =>
        GetString(nameof(LoadGroupsFailed));

    public static string Save =>
        GetString(nameof(Save));

    public static string Ok =>
        GetString(nameof(Ok));

    public static string AddExistingPerson =>
        GetString(nameof(AddExistingPerson));

    public static string SelectPerson =>
        GetString(nameof(SelectPerson));

    public static string EditGroup =>
        GetString(nameof(EditGroup));

    public static string EditGroupHint =>
        GetString(nameof(EditGroupHint));

    public static string EditGroupMembersHint =>
        GetString(nameof(EditGroupMembersHint));

    public static string GroupNotFound =>
        GetString(nameof(GroupNotFound));

    public static string LoadGroupFailed =>
        GetString(nameof(LoadGroupFailed));

    public static string LastPlayedFormat =>
        GetString(nameof(LastPlayedFormat));

    public static string NeverPlayed =>
        GetString(nameof(NeverPlayed));

    public static string Initializing =>
        GetString(nameof(Initializing));

    public static string StartupErrorTitle =>
        GetString(nameof(StartupErrorTitle));

    public static string StartupErrorMessage =>
        GetString(nameof(StartupErrorMessage));

    public static string Retry =>
        GetString(nameof(Retry));

    public static string TechnicalDetails =>
        GetString(nameof(TechnicalDetails));

    public static string Temporary =>
        GetString(nameof(Temporary));

    public static string Archived =>
        GetString(nameof(Archived));

    public static string MakePermanent =>
        GetString(nameof(MakePermanent));

    public static string ArchiveHint =>
        GetString(nameof(ArchiveHint));

    public static string PrepareGame =>
        GetString(nameof(PrepareGame));

    public static string DetermineStartingPlayer =>
        GetString(nameof(DetermineStartingPlayer));

    public static string DrawZooPlans =>
        GetString(nameof(DrawZooPlans));

    public static string RotateOrder =>
        GetString(nameof(RotateOrder));

    public static string ChooseColor =>
        GetString(nameof(ChooseColor));

    public static string StartGame =>
        GetString(nameof(StartGame));

    public static string RunningGame =>
        GetString(nameof(RunningGame));

    public static string EndGame =>
        GetString(nameof(EndGame));

    public static string Discard =>
        GetString(nameof(Discard));

    public static string Scoring =>
        GetString(nameof(Scoring));

    public static string ScoringPlaceholder =>
        GetString(nameof(ScoringPlaceholder));

    public static string NoPreviousSession =>
        GetString(nameof(NoPreviousSession));

    public static string GroupNotFoundForPreparation =>
        GetString(nameof(GroupNotFoundForPreparation));

    public static string ArchivedGroupCannotStart =>
        GetString(nameof(ArchivedGroupCannotStart));

    public static string ArkNovaPlayerCountError =>
        GetString(nameof(ArkNovaPlayerCountError));

    public static string LoadPreparationFailed =>
        GetString(nameof(LoadPreparationFailed));

    public static string StartGameFailed =>
        GetString(nameof(StartGameFailed));

    public static string PreviousSessionFormat =>
        GetString(nameof(PreviousSessionFormat));

    public static string Blue =>
        GetString(nameof(Blue));

    public static string Yellow =>
        GetString(nameof(Yellow));

    public static string Red =>
        GetString(nameof(Red));

    public static string Black =>
        GetString(nameof(Black));

    public static string StartValidationTitle =>
        GetString(nameof(StartValidationTitle));

    public static string UniqueColorsRequired =>
        GetString(nameof(UniqueColorsRequired));

    public static string ZooPlanSelectionRequired =>
        GetString(nameof(ZooPlanSelectionRequired));

    public static string RunningSessionNotFound =>
        GetString(nameof(RunningSessionNotFound));

    public static string LoadRunningSessionFailed =>
        GetString(nameof(LoadRunningSessionFailed));

    public static string DurationHoursFormat =>
        GetString(nameof(DurationHoursFormat));

    public static string DurationMinutesFormat =>
        GetString(nameof(DurationMinutesFormat));

    public static string EndGameFailed =>
        GetString(nameof(EndGameFailed));

    public static string DiscardGameFailed =>
        GetString(nameof(DiscardGameFailed));

    public static string DiscardGameTitle =>
        GetString(nameof(DiscardGameTitle));

    public static string DiscardGameMessage =>
        GetString(nameof(DiscardGameMessage));

    public static string Cancel =>
        GetString(nameof(Cancel));

    public static string ZooPlanFormat =>
        GetString(nameof(ZooPlanFormat));

    public static string MoveUp =>
        GetString(nameof(MoveUp));

    public static string MoveDown =>
        GetString(nameof(MoveDown));

    public static string Remove =>
        GetString(nameof(Remove));

    /// <summary>
    /// Resolves a persisted localization resource key, for example an Ark Nova
    /// zoo-plan name stored as reference data.
    /// </summary>
    public static string GetByKey(string key) => GetString(key);

    private static string GetString(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new InvalidOperationException(
            $"Localization resource '{key}' was not found.");
}

using System.Globalization;
using System.Resources;

namespace BoardgameTracker.App.Resources.Localization;

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

    public static string ExistingPersons =>
        GetString(nameof(ExistingPersons));

    public static string NoAvailablePersons =>
        GetString(nameof(NoAvailablePersons));

    public static string Add =>
        GetString(nameof(Add));

    public static string GroupMembers =>
        GetString(nameof(GroupMembers));

    public static string DefaultOrderHint =>
        GetString(nameof(DefaultOrderHint));

    public static string NoMembersYet =>
        GetString(nameof(NoMembersYet));

    public static string SaveGroup =>
        GetString(nameof(SaveGroup));

    public static string EnterPersonName =>
        GetString(nameof(EnterPersonName));

    public static string PersonAlreadyInGroup =>
        GetString(nameof(PersonAlreadyInGroup));

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

    private static string GetString(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new InvalidOperationException(
            $"Localization resource '{key}' was not found.");
}

using System.Globalization;
using System.Resources;

namespace BoardgameTracker.App.Resources.Localization;

public static class AppResources
{
    private static readonly ResourceManager ResourceManager =
        new("BoardgameTracker.App.Resources.Localization.AppResources",
            typeof(AppResources).Assembly);

    public static string Groups =>
        GetString(nameof(Groups));

    public static string FirstRunHint =>
        GetString(nameof(FirstRunHint));

    public static string NoGroupsYet =>
        GetString(nameof(NoGroupsYet));


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

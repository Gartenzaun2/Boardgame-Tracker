using AppTheme = BoardgameTracker.Application.Settings.Navigation.AppTheme;
using System.Collections.ObjectModel;
namespace BoardgameTracker.App.Theming;

// Pure palette data; no MAUI types so contrast and completeness can be tested.
public static class ThemePalette
{
    public static IReadOnlyDictionary<string, string> Get(AppTheme theme) => theme switch
    {
        AppTheme.RoyalBlue => RoyalBlue,
        AppTheme.Dark => Dark,
        _ => DeepForest
    };
    private static readonly IReadOnlyDictionary<string, string> DeepForest = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
    {
        ["AppBackground"] = "#D9E6DF",
        ["Surface"] = "#FFFFFF",
        ["SurfaceSecondary"] = "#EEF5F1",
        ["Primary"] = "#466655",
        ["PrimaryPressed"] = "#385346",
        ["PrimaryForeground"] = "#FFFFFF",
        ["SelectedSurface"] = "#C7DCCF",
        ["SelectedBorder"] = "#789A85",
        ["TextPrimary"] = "#263D31",
        ["TextSecondary"] = "#68776F",
        ["Outline"] = "#B8CAC0",
        ["NavigationBackground"] = "#FFFFFF",
        ["NavigationText"] = "#68776F",
        ["NavigationSelectedSurface"] = "#C7DCCF",
        ["NavigationSelectedText"] = "#263D31",
        ["Danger"] = "#B3261E",
        ["DangerSurface"] = "#F9DEDC",
        ["DisabledSurface"] = "#EEF5F1",
        ["DisabledText"] = "#263D31",
        ["BusyOverlay"] = "#99000000",
        ["IconSurface"] = "#EEF5F1",
        ["GameGraphicText"] = "#101713",
        ["BusyIndicator"] = "#FFFFFF",
    });
    private static readonly IReadOnlyDictionary<string, string> RoyalBlue = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
    {
        ["AppBackground"] = "#DCE5F4",
        ["Surface"] = "#FFFFFF",
        ["SurfaceSecondary"] = "#EEF3FB",
        ["Primary"] = "#315B9B",
        ["PrimaryPressed"] = "#274A80",
        ["PrimaryForeground"] = "#FFFFFF",
        ["SelectedSurface"] = "#C8D8F1",
        ["SelectedBorder"] = "#708EBE",
        ["TextPrimary"] = "#18386A",
        ["TextSecondary"] = "#66758E",
        ["Outline"] = "#BCCAE1",
        ["NavigationBackground"] = "#FFFFFF",
        ["NavigationText"] = "#65748D",
        ["NavigationSelectedSurface"] = "#C8D8F1",
        ["NavigationSelectedText"] = "#18386A",
        ["Danger"] = "#B3261E",
        ["DangerSurface"] = "#F9DEDC",
        ["DisabledSurface"] = "#EEF3FB",
        ["DisabledText"] = "#18386A",
        ["BusyOverlay"] = "#99000000",
        ["IconSurface"] = "#EEF5F1",
        ["GameGraphicText"] = "#101713",
        ["BusyIndicator"] = "#FFFFFF",
    });
    private static readonly IReadOnlyDictionary<string, string> Dark = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
    {
        ["AppBackground"] = "#141A17",
        ["Surface"] = "#222A26",
        ["SurfaceSecondary"] = "#29352F",
        ["Primary"] = "#5B8A70",
        ["PrimaryPressed"] = "#6D9B82",
        ["PrimaryForeground"] = "#101713",
        ["SelectedSurface"] = "#355244",
        ["SelectedBorder"] = "#668875",
        ["TextPrimary"] = "#F0F4F1",
        ["TextSecondary"] = "#C6D0CA",
        ["Outline"] = "#3B4942",
        ["NavigationBackground"] = "#222A26",
        ["NavigationText"] = "#AEBBB4",
        ["NavigationSelectedSurface"] = "#355244",
        ["NavigationSelectedText"] = "#FFFFFF",
        ["Danger"] = "#FFB4AB",
        ["DangerSurface"] = "#5F2423",
        ["DisabledSurface"] = "#29352F",
        ["DisabledText"] = "#F0F4F1",
        ["BusyOverlay"] = "#99000000",
        ["IconSurface"] = "#EEF5F1",
        ["GameGraphicText"] = "#101713",
        ["BusyIndicator"] = "#FFFFFF",
    });
}

namespace BoardgameTracker.Application.Settings.Navigation;

public enum AppTheme
{
    DeepForest = 0,
    RoyalBlue = 1,
    Dark = 2
}

public static class AppThemeSelection
{
    public static AppTheme FromStoredValue(int value) => Enum.IsDefined(typeof(AppTheme), value)
        ? (AppTheme)value : AppTheme.DeepForest;
}

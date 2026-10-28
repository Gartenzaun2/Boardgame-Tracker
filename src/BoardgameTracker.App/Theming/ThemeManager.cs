using BoardgameTracker.Application.Settings.Navigation;
using Theme = BoardgameTracker.Application.Settings.Navigation.AppTheme;
namespace BoardgameTracker.App.Theming;

public sealed class ThemeManager(IAppNavigationSettingsStore settings)
{
    public void Apply(Theme theme, bool persist = true)
    {
        theme = AppThemeSelection.FromStoredValue((int)theme);
        var app = Microsoft.Maui.Controls.Application.Current!;
        app.UserAppTheme = theme == Theme.Dark ? Microsoft.Maui.ApplicationModel.AppTheme.Dark : Microsoft.Maui.ApplicationModel.AppTheme.Light;
        foreach (var token in ThemePalette.Get(theme)) app.Resources[token.Key] = Color.FromArgb(token.Value);
        if (persist) settings.Save(settings.Get() with { Theme = theme });
    }
}

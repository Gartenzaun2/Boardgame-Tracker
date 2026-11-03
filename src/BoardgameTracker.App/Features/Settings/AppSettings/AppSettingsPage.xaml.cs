using Microsoft.Maui.ApplicationModel;
using BoardgameTracker.App.Features.Settings.MyGames;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.App.Features.Settings.Licenses;

namespace BoardgameTracker.App.Features.Settings.AppSettings;

public partial class AppSettingsPage : ContentPage
{
    private readonly IAppNavigationSettingsStore _navigationSettingsStore;
    private readonly BoardgameTracker.App.Theming.ThemeManager _themes;
    private bool _choosing;

    public AppSettingsPage(IAppNavigationSettingsStore navigationSettingsStore, BoardgameTracker.App.Theming.ThemeManager themes)
    {
        InitializeComponent();
        AppVersionLabel.Text = string.Format(AppResources.AppVersionFormat, AppInfo.Current.VersionString, AppInfo.Current.BuildString);
        _navigationSettingsStore = navigationSettingsStore;
        _themes = themes;

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        RefreshSelections();
    }

    private string[] ThemeNames => [AppResources.ThemeDeepForest, AppResources.ThemeRoyalBlue, AppResources.ThemeDark];

    private void RefreshSelections()
    {
        AppNavigationSettings settings = _navigationSettingsStore.Get();
        ThemeChoice.Text = ThemeNames[(int)settings.Theme];
    }
    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.AppSettingsTitle, AppResources.GameSettingsHint, AppResources.Ok);

    private async void OnArkNovaSettingsTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage));

    private async void OnMyGamesClicked(object? sender, EventArgs e)
    {
        if (_choosing)
            return;
        _choosing = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(MyGamesPage));
        }
        finally
        {
            _choosing = false;
        }
    }

    private async void OnLicensesClicked(object? sender, EventArgs e)
    {
        if (_choosing)
            return;
        _choosing = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(LicenseListPage));
        }
        finally
        {
            _choosing = false;
        }
    }

    private async void OnThemeClicked(object? sender, EventArgs e)
    {
        if (_choosing)
            return;

        _choosing = true;
        try
        {
            int choice = await AppDialogPage.ChooseAsync(this, AppResources.AppearanceTheme,
                ThemeNames, (int)_navigationSettingsStore.Get().Theme);
            if (choice < 0)
                return;

            _themes.Apply(AppThemeSelection.FromStoredValue(choice));
            RefreshSelections();
        }
        finally
        {
            _choosing = false;
        }
    }

}

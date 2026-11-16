using BoardgameTracker.App.Features.Settings.Licenses;
using BoardgameTracker.App.Features.Settings.MyGames;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Theming;
using BoardgameTracker.Application.Settings.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace BoardgameTracker.App.Features.Settings.AppSettings;

public partial class AppSettingsPage : ContentPage {
    private readonly IAppNavigationSettingsStore _navigationSettingsStore;
    private readonly ThemeManager _themes;
    private readonly ILogger<AppSettingsPage> _logger;

    public AppSettingsPage(IAppNavigationSettingsStore navigationSettingsStore, ThemeManager themes, ILogger<AppSettingsPage> logger) {
        InitializeComponent();
        AppVersionLabel.Text = string.Format(AppResources.AppVersionFormat, AppInfo.Current.VersionString, AppInfo.Current.BuildString);
        _navigationSettingsStore = navigationSettingsStore;
        _themes = themes;
        _logger = logger;
    }

    private bool _isHandlingSettingsAction;

    #region Localized UI Text

    private string[] ThemeNames => [AppResources.ThemeDeepForest, AppResources.ThemeRoyalBlue, AppResources.ThemeDark, AppResources.ThemeDarkTfm];

    #endregion

    /// <summary>Closing an app dialog must not reapply settings while its choice is still being handled.</summary>
    protected override void OnAppearing() {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;

        RefreshSelections();
    }

    private void RefreshSelections() {
        AppNavigationSettings settings = _navigationSettingsStore.Get();
        ThemeChoice.Text = ThemeNames[(int)settings.Theme];
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
        => await AppDialogPage.ShowAsync(this, AppResources.AppSettingsTitle, AppResources.GameSettingsHint, AppResources.Ok);

    private async void OnLanguageClicked(object? sender, EventArgs e) {
        if (_isHandlingSettingsAction)
            return;

        _isHandlingSettingsAction = true;

        try {
            await AppDialogPage.ChooseAsync(this, AppResources.AppLanguage, [AppResources.ContentLanguageGerman], 0);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not complete app settings choice.");
            await ShowNavigationErrorAsync();
        } finally {
            _isHandlingSettingsAction = false;
        }
    }

    private async void OnThemeClicked(object? sender, EventArgs e) {
        if (_isHandlingSettingsAction)
            return;

        _isHandlingSettingsAction = true;

        try {
            int choice = await AppDialogPage.ChooseAsync(this, AppResources.AppearanceTheme,
                ThemeNames, (int)_navigationSettingsStore.Get().Theme);
            if (choice < 0)
                return;

            _themes.Apply(AppThemeSelection.FromStoredValue(choice));
            RefreshSelections();
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not apply app theme selection.");
            await ShowNavigationErrorAsync();
        } finally {
            _isHandlingSettingsAction = false;
        }
    }

    private async void OnMyGamesClicked(object? sender, EventArgs e) {
        if (_isHandlingSettingsAction)
            return;

        _isHandlingSettingsAction = true;

        try {
            await Shell.Current.GoToAsync(nameof(MyGamesPage), false);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not open game management.");
            await ShowNavigationErrorAsync();
        } finally {
            _isHandlingSettingsAction = false;
        }
    }

    private async void OnLicensesClicked(object? sender, EventArgs e) {
        if (_isHandlingSettingsAction)
            return;

        _isHandlingSettingsAction = true;

        try {
            await Shell.Current.GoToAsync(nameof(LicenseListPage), false);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not open third-party licenses.");
            await ShowNavigationErrorAsync();
        } finally {
            _isHandlingSettingsAction = false;
        }
    }

    private async Task ShowNavigationErrorAsync() {
        try {
            await AppDialogPage.ShowAsync(this, AppResources.AppSettingsTitle, AppResources.NavigationFailed, AppResources.Ok);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not display the settings navigation error.");
        }
    }
}

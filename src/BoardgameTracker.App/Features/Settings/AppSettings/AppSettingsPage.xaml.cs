using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Features.Settings.AppSettings;

public partial class AppSettingsPage : ContentPage
{
    private readonly IAppNavigationSettingsStore _navigationSettingsStore;
    private readonly IReadOnlyList<DefaultGameOption> _defaultGameOptions;
    private readonly BoardgameTracker.App.Theming.ThemeManager _themes;
    private bool _isLoading;

    public AppSettingsPage(IAppNavigationSettingsStore navigationSettingsStore, BoardgameTracker.App.Theming.ThemeManager themes)
    {
        InitializeComponent();
        _navigationSettingsStore = navigationSettingsStore;
        _themes = themes;
        ThemePicker.ItemsSource = new[] { AppResources.ThemeDeepForest, AppResources.ThemeRoyalBlue, AppResources.ThemeDark };
        _defaultGameOptions =
        [
            new(null, AppResources.NoDefaultGame),
            new(ArkNovaGame.Key, AppResources.ArkNova)
        ];
        DefaultGamePicker.ItemsSource = _defaultGameOptions.ToList();
        DefaultGamePicker.ItemDisplayBinding = new Binding(nameof(DefaultGameOption.Name));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isLoading = true;
        AppNavigationSettings settings = _navigationSettingsStore.Get();
        ThemePicker.SelectedIndex = (int)settings.Theme;
        DefaultGamePicker.SelectedItem = _defaultGameOptions.FirstOrDefault(option => option.GameKey == settings.DefaultGameKey)
            ?? _defaultGameOptions[0];
        _isLoading = false;
    }

    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync(AppResources.AppSettingsTitle, AppResources.GameSettingsHint, AppResources.Ok);

    private async void OnDefaultGameInfoClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync(AppResources.DefaultGame, AppResources.DefaultGameHint, AppResources.Ok);

    private async void OnArkNovaSettingsTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage));

    private void OnDefaultGameChanged(object? sender, EventArgs e)
    {
        if (_isLoading || DefaultGamePicker.SelectedItem is not DefaultGameOption option)
        {
            return;
        }

        AppNavigationSettings current = _navigationSettingsStore.Get();
        _navigationSettingsStore.Save(current with
        {
            DefaultGameKey = option.GameKey,
            SelectedGameKey = current.SelectedGameKey ?? option.GameKey
        });
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (!_isLoading && ThemePicker.SelectedIndex >= 0)
            _themes.Apply(AppThemeSelection.FromStoredValue(ThemePicker.SelectedIndex));
    }

    private sealed record DefaultGameOption(string? GameKey, string Name);
}

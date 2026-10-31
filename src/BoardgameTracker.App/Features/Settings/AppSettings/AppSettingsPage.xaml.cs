using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.App.Features.Players.ListPlayers;
using BoardgameTracker.App.Features.Settings.Licenses;

namespace BoardgameTracker.App.Features.Settings.AppSettings;

public partial class AppSettingsPage : ContentPage
{
    private readonly IAppNavigationSettingsStore _navigationSettingsStore;
    private readonly IReadOnlyList<DefaultGameOption> _defaultGameOptions;
    private readonly BoardgameTracker.App.Theming.ThemeManager _themes;
    private bool _choosing;

    public AppSettingsPage(IAppNavigationSettingsStore navigationSettingsStore, BoardgameTracker.App.Theming.ThemeManager themes)
    {
        InitializeComponent();
        _navigationSettingsStore = navigationSettingsStore;
        _themes = themes;

        _defaultGameOptions =
        [
            new(null, AppResources.NoDefaultGame),
            new(ArkNovaGame.Key, AppResources.ArkNova)
        ];
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
        DefaultGameChoice.Text = (_defaultGameOptions.FirstOrDefault(option => option.GameKey == settings.DefaultGameKey)
            ?? _defaultGameOptions[0]).Name;
    }
    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.AppSettingsTitle, AppResources.GameSettingsHint, AppResources.Ok);

    private async void OnDefaultGameInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.DefaultGame, AppResources.DefaultGameHint, AppResources.Ok);

    private async void OnArkNovaSettingsTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage));

    private async void OnPlayersTapped(object? sender, TappedEventArgs e)
    {
        if (_choosing)
            return;
        _choosing = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(PlayerListPage));
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

    private async void OnDefaultGameClicked(object? sender, EventArgs e)
    {
        if (_choosing)
            return;

        _choosing = true;
        try
        {
            AppNavigationSettings current = _navigationSettingsStore.Get();
            int selected = Math.Max(0, _defaultGameOptions.ToList().FindIndex(option => option.GameKey == current.DefaultGameKey));
            int choice = await AppDialogPage.ChooseAsync(this, AppResources.DefaultGame,
                _defaultGameOptions.Select(option => option.Name).ToArray(), selected);
            if (choice < 0)
                return;

            DefaultGameOption option = _defaultGameOptions[choice];
            current = _navigationSettingsStore.Get();
            _navigationSettingsStore.Save(current with
            {
                DefaultGameKey = option.GameKey,
                SelectedGameKey = current.SelectedGameKey ?? option.GameKey
            });
            RefreshSelections();
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

    private sealed record DefaultGameOption(string? GameKey, string Name);
}

using BoardgameTracker.App.Features.Games.ArkNova.Settings;

namespace BoardgameTracker.App.Features.Settings.AppSettings;

public partial class AppSettingsPage : ContentPage
{
    public AppSettingsPage() => InitializeComponent();

    private async void OnArkNovaSettingsTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage));
}

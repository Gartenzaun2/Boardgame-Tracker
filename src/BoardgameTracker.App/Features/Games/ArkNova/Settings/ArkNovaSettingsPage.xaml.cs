namespace BoardgameTracker.App.Features.Games.ArkNova.Settings;

public partial class ArkNovaSettingsPage : ContentPage
{
    private readonly ArkNovaSettingsViewModel _viewModel;

    public ArkNovaSettingsPage(ArkNovaSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Load();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Save();
        base.OnDisappearing();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        _viewModel.Save();
        await Shell.Current.GoToAsync("..");
    }
}

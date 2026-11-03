using BoardgameTracker.App.Presentation.Controls;

namespace BoardgameTracker.App.Features.Settings.MyGames;

public partial class MyGamesPage : ContentPage
{
    private readonly MyGamesViewModel _viewModel;
    public MyGamesPage(MyGamesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync();
    }
    private async void OnGameToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is Switch { BindingContext: GameActivationRow row })
            await _viewModel.SetEnabledAsync(row, e.Value);
    }
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.IsBusy)
            await Shell.Current.GoToAsync("..");
    }
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || base.OnBackButtonPressed();
}

using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Presentation.Controls;

namespace BoardgameTracker.App.Features.Settings.MyGames;

public partial class MyGamesPage : ContentPage
{
    private readonly MyGamesViewModel _viewModel;
    private readonly IReadOnlyList<IGameSettingsNavigation> _settings;
    private bool _opening;
    public MyGamesPage(MyGamesViewModel viewModel, IEnumerable<IGameSettingsNavigation> settings)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settings = settings.ToArray();
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync();
    }
    private async void OnGameTapped(object? sender, TappedEventArgs e)
    {
        if (_opening || _viewModel.IsBusy || e.Parameter is not GameActivationRow row)
            return;
        _opening = true;
        try
        {
            IGameSettingsNavigation? entry = _settings.SingleOrDefault(entry => entry.GameKey == row.GameKey);
            if (entry is not null)
                await entry.OpenAsync();
            else
                await AppDialogPage.ShowAsync(this, row.Name, AppResources.GameSettingsUnavailable, AppResources.Ok);
        }
        finally { _opening = false; }
    }
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.IsBusy)
            await Shell.Current.GoToAsync("..", false);
    }
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || base.OnBackButtonPressed();
}

using BoardgameTracker.App.Presentation.Controls;

namespace BoardgameTracker.App.Features.Settings.MyGames;

public partial class MyGamesPage : ContentPage {
    private readonly MyGamesViewModel _viewModel;

    public MyGamesPage(MyGamesViewModel viewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public MyGamesViewModel ViewModel => _viewModel;

    protected override async void OnAppearing() {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync();
    }

    /// <summary>Uses the header command so pending changes and navigation errors share the same guards.</summary>
    protected override bool OnBackButtonPressed() {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}

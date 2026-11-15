using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.Application.Settings.Navigation;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;

public partial class ArkNovaResultPage : ContentPage, IQueryAttributable {
    private readonly ArkNovaResultViewModel _viewModel;

    public ArkNovaResultPage(ArkNovaResultViewModel viewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        await _viewModel.LoadAsync(_gameSessionId);
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
        => await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();

    private async void OnBackClicked(object? sender, EventArgs e)
        => await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();

    private async void OnEditClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ArkNovaScoringPage), false, new ShellNavigationQueryParameters {
            [NavigationParameters.GameSessionId] = _gameSessionId
        });

    /// <summary>Routes system back to the retained caller so the completed scoring flow is left as one unit.</summary>
    protected override bool OnBackButtonPressed() {
        // The synchronous back callback dispatches the asynchronous close/navigation flow.
        Dispatcher.Dispatch(async () => await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync());
        return true;
    }
}

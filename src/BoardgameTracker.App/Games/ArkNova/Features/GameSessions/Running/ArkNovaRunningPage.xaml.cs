using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;
using Microsoft.Maui.Dispatching;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;

/// <summary>
/// MAUI view for an active Ark Nova gameSession. It owns the visual duration timer and
/// delegates lifecycle actions to the running GameSession view model.
/// </summary>
public partial class ArkNovaRunningPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaRunningViewModel _viewModel;
    private readonly IDispatcherTimer _durationTimer;
    private Guid _gameSessionId;

    public ArkNovaRunningPage(ArkNovaRunningViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _durationTimer = Dispatcher.CreateTimer();
        _durationTimer.Interval = TimeSpan.FromSeconds(30);
        _durationTimer.Tick += (_, _) => _viewModel.RefreshDuration(DateTimeOffset.UtcNow);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? value)
            || value is not Guid gameSessionId
            || gameSessionId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Required navigation parameter '{NavigationParameters.GameSessionId}' is missing or invalid.");
        }

        _gameSessionId = gameSessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_gameSessionId != Guid.Empty && _viewModel.GameSessionId != _gameSessionId)
        {
            await _viewModel.LoadAsync(_gameSessionId);
        }

        _viewModel.RefreshDuration(DateTimeOffset.UtcNow);
        _durationTimer.Start();
    }

    protected override void OnDisappearing()
    {
        _durationTimer.Stop();
        base.OnDisappearing();
    }


    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () =>
            await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}"));
        return true;
    }

    private async void OnEndClicked(object? sender, EventArgs e)
    {
        if (!await _viewModel.BeginScoringAsync())
        {
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(ArkNovaScoringPage),
            new ShellNavigationQueryParameters
            {
                [NavigationParameters.GameSessionId] = _viewModel.GameSessionId
            });
    }

    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        bool confirmed = await AppDialogPage.ShowAsync(this,
            AppResources.DiscardGameTitle,
            AppResources.DiscardGameMessage,
            AppResources.Discard,
            AppResources.Cancel, destructive: true);

        if (!confirmed || !await _viewModel.DiscardAsync())
        {
            return;
        }

        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");
    }
}

using BoardgameTracker.App.Features.GameSessions.Scoring;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;
using Microsoft.Maui.Dispatching;

namespace BoardgameTracker.App.Features.GameSessions.Running;

/// <summary>
/// MAUI view for an active Ark Nova session. It owns the visual duration timer and
/// delegates lifecycle actions to the running-session view model.
/// </summary>
public partial class ArkNovaRunningPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaRunningViewModel _viewModel;
    private readonly IDispatcherTimer _durationTimer;
    private Guid _sessionId;

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
        if (!query.TryGetValue(NavigationParameters.SessionId, out object? value)
            || value is not Guid sessionId
            || sessionId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Required navigation parameter '{NavigationParameters.SessionId}' is missing or invalid.");
        }

        _sessionId = sessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_sessionId != Guid.Empty && _viewModel.SessionId != _sessionId)
        {
            await _viewModel.LoadAsync(_sessionId);
        }

        _viewModel.RefreshDuration(DateTimeOffset.UtcNow);
        _durationTimer.Start();
    }

    protected override void OnDisappearing()
    {
        _durationTimer.Stop();
        base.OnDisappearing();
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
                [NavigationParameters.SessionId] = _viewModel.SessionId
            });
    }

    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync(
            AppResources.DiscardGameTitle,
            AppResources.DiscardGameMessage,
            AppResources.Discard,
            AppResources.Cancel);

        if (!confirmed || !await _viewModel.DiscardAsync())
        {
            return;
        }

        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");
    }
}

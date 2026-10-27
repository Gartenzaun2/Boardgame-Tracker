using Microsoft.Maui.Dispatching;
using BoardgameTracker.App.Features.GameSessions.Scoring;
using BoardgameTracker.App.Features.Groups.ListGroups;

namespace BoardgameTracker.App.Features.GameSessions.Running;

[QueryProperty(nameof(SessionId), "SessionId")]
public partial class ArkNovaRunningPage : ContentPage
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

    public Guid SessionId
    {
        get => _sessionId;
        set => _sessionId = value;
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
            new Dictionary<string, object>
            {
                ["SessionId"] = _viewModel.SessionId
            });
    }

    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync(
            "Partie verwerfen?",
            "Die laufende Partie wird dauerhaft entfernt.",
            "Verwerfen",
            "Abbrechen");

        if (!confirmed || !await _viewModel.DiscardAsync())
        {
            return;
        }

        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");
    }
}

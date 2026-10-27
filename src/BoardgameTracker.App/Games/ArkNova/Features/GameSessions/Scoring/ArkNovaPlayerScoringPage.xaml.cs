using BoardgameTracker.App.Navigation;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaPlayerScoringPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaPlayerScoringViewModel _viewModel;
    private Guid _gameSessionId;
    private Guid _playerId;

    public ArkNovaPlayerScoringPage(ArkNovaPlayerScoringViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? gameSessionValue)
            || gameSessionValue is not Guid gameSessionId || gameSessionId == Guid.Empty
            || !query.TryGetValue(NavigationParameters.PlayerId, out object? playerValue)
            || playerValue is not Guid playerId || playerId == Guid.Empty)
        {
            throw new InvalidOperationException("Required scoring navigation parameters are missing or invalid.");
        }
        _gameSessionId = gameSessionId;
        _playerId = playerId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_gameSessionId, _playerId);
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        await _viewModel.FlushAsync();
        await Shell.Current.GoToAsync("..");
    }
}

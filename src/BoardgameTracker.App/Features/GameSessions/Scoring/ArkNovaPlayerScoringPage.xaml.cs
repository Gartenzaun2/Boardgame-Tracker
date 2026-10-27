using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.GameSessions.Scoring;

public partial class ArkNovaPlayerScoringPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaPlayerScoringViewModel _viewModel;
    private Guid _sessionId;
    private Guid _personId;

    public ArkNovaPlayerScoringPage(ArkNovaPlayerScoringViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.SessionId, out object? sessionValue)
            || sessionValue is not Guid sessionId || sessionId == Guid.Empty
            || !query.TryGetValue(NavigationParameters.PersonId, out object? personValue)
            || personValue is not Guid personId || personId == Guid.Empty)
        {
            throw new InvalidOperationException("Required scoring navigation parameters are missing or invalid.");
        }
        _sessionId = sessionId;
        _personId = personId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_sessionId, _personId);
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        await _viewModel.FlushAsync();
        await Shell.Current.GoToAsync("..");
    }
}

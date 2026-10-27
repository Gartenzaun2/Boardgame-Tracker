using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;

public partial class ArkNovaResultPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaResultViewModel _viewModel;
    private Guid _gameSessionId;

    public ArkNovaResultPage(ArkNovaResultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? value)
            || value is not Guid gameSessionId || gameSessionId == Guid.Empty)
        {
            throw new InvalidOperationException("Required gameSession navigation parameter is missing or invalid.");
        }
        _gameSessionId = gameSessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_gameSessionId);
    }

    private async void OnDoneClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");

    protected override bool OnBackButtonPressed() => true;
}

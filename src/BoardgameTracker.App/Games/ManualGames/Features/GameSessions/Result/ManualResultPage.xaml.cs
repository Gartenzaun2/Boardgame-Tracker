using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Result;

public partial class ManualResultPage : ContentPage, IQueryAttributable
{
    private readonly ManualResultViewModel _viewModel;
    private Guid _id;
    private bool _returning;
    public ManualResultPage(ManualResultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParameters.GameSessionId, out object? value) && value is Guid id)
            _id = id;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_id);
    }
    private async Task ReturnAsync()
    {
        if (_returning || _viewModel.IsBusy)
            return;
        _returning = true;
        try
        {
            await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();
        }
        finally
        {
            _returning = false;
        }
    }
    private async void OnDoneClicked(object? sender, EventArgs e) => await ReturnAsync();
    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () => await ReturnAsync());
        return true;
    }
}

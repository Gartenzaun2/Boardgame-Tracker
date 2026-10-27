using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.GameSessions.Result;

public partial class ArkNovaResultPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaResultViewModel _viewModel;
    private Guid _sessionId;

    public ArkNovaResultPage(ArkNovaResultViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.SessionId, out object? value)
            || value is not Guid sessionId || sessionId == Guid.Empty)
        {
            throw new InvalidOperationException("Required session navigation parameter is missing or invalid.");
        }
        _sessionId = sessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_sessionId);
    }

    private async void OnDoneClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");

    protected override bool OnBackButtonPressed() => true;
}

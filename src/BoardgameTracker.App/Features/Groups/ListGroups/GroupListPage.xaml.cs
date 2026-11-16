using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

public partial class GroupListPage : ContentPage, IQueryAttributable {
    private readonly GroupListViewModel _viewModel;
    public GroupListViewModel ViewModel => _viewModel;
    public string GameKey => _viewModel.SelectedGameKey;
    public GroupListPage(GroupListViewModel viewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (!query.TryGetValue(NavigationParameters.GameKey, out object? value) || value is not string key || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("A game context is required.");
        _viewModel.SetGameContext(key);
    }
    protected override async void OnAppearing() {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        _viewModel.EndNavigation();
        await _viewModel.LoadAsync();
    }
    /// <summary>Uses the header flow to close selection first and handle navigation failures consistently.</summary>
    protected override bool OnBackButtonPressed() {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}

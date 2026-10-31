using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Settings.Licenses;

public partial class LicenseDetailPage : ContentPage, IQueryAttributable
{
    private readonly LicenseDetailViewModel _viewModel;
    private string _componentId = string.Empty;
    private bool _navigating;

    public LicenseDetailPage(LicenseDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _componentId = query.TryGetValue(NavigationParameters.LicenseComponentId, out object? value) && value is string id
            ? id : string.Empty;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_componentId);
    }

    private async void OnRetryClicked(object? sender, EventArgs e) => await _viewModel.LoadAsync(_componentId);
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_navigating || !_viewModel.IsIdle)
            return;
        _navigating = true;
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            _navigating = false;
        }
    }
    protected override bool OnBackButtonPressed() => _navigating || _viewModel.IsBusy || base.OnBackButtonPressed();
}

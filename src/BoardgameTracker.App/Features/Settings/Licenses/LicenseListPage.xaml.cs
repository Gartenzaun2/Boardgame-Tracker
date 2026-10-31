using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Presentation.Controls;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.Licenses;

public partial class LicenseListPage : ContentPage
{
    private readonly LicenseListViewModel _viewModel;
    private readonly ILogger<LicenseListPage> _logger;
    private bool _navigating;

    public LicenseListPage(LicenseListViewModel viewModel, ILogger<LicenseListPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnLicenseClicked(object? sender, EventArgs e)
    {
        if (_navigating || !_viewModel.IsIdle || sender is not Button { CommandParameter: string id })
            return;
        _navigating = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(LicenseDetailPage), new ShellNavigationQueryParameters
            {
                [NavigationParameters.LicenseComponentId] = id
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open license detail for {ComponentId}.", id);
            await AppDialogPage.ShowAsync(this, AppResources.OpenSourceLicenses, AppResources.LicensesLoadFailed, AppResources.Ok);
        }
        finally
        {
            _navigating = false;
        }
    }

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

    private async void OnRetryClicked(object? sender, EventArgs e) => await _viewModel.LoadAsync();
    protected override bool OnBackButtonPressed() => _navigating || _viewModel.IsBusy || base.OnBackButtonPressed();
}

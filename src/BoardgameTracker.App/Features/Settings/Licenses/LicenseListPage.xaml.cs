using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.Licenses;

public partial class LicenseListPage : ContentPage {
    private readonly LicenseListViewModel _viewModel;
    private readonly ILogger<LicenseListPage> _logger;

    public LicenseListPage(LicenseListViewModel viewModel, ILogger<LicenseListPage> logger) {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    private bool _navigating;

    protected override async void OnAppearing() {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnLicenseClicked(object? sender, EventArgs e) {
        if (_navigating || _viewModel.IsBusy || sender is not Button { CommandParameter: string id })
            return;

        _navigating = true;

        try {
            await Shell.Current.GoToAsync(nameof(LicenseDetailPage), false, new ShellNavigationQueryParameters {
                [NavigationParameters.LicenseComponentId] = id
            });
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not open license detail for {ComponentId}.", id);
            await AppDialogPage.ShowAsync(this, AppResources.OpenSourceLicenses, AppResources.LicensesLoadFailed, AppResources.Ok);
        } finally {
            _navigating = false;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) {
        if (_navigating || _viewModel.IsBusy)
            return;

        _navigating = true;

        try {
            await Shell.Current.GoToAsync("..", false);
        } finally {
            _navigating = false;
        }
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
        => await _viewModel.LoadAsync();
    /// <summary>Blocks system back during loading or navigation so an unfinished transition is not interrupted.</summary>
    protected override bool OnBackButtonPressed()
        => _navigating || _viewModel.IsBusy || base.OnBackButtonPressed();
}

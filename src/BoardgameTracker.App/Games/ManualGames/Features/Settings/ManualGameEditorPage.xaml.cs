using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Games.ManualGames.Features.Settings;

public partial class ManualGameEditorPage : ContentPage, IQueryAttributable {
    private readonly ManualGameEditorViewModel _viewModel;
    private string? _gameKey;

    public ManualGameEditorPage(ManualGameEditorViewModel viewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (query.TryGetValue(NavigationParameters.GameKey, out object? key))
            _gameKey = key as string;
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync(_gameKey);
    }

    private async void OnEnabledToggled(object? sender, ToggledEventArgs e)
        => await _viewModel.SetEnabledAsync(e.Value);

    private async void OnResultModeClicked(object? sender, EventArgs e) {
        if (!_viewModel.CanEdit)
            return;
        _viewModel.IsChoosing = true;
        try {
            int selected = await AppDialogPage.ChooseAsync(this, AppResources.ResultFromPoints,
                _viewModel.ResultChoices, _viewModel.ResultMode);
            if (selected >= 0)
                _viewModel.ResultMode = selected;
        } catch (Exception exception) {
            _viewModel.ReportDialogFailure(exception);
        } finally {
            _viewModel.IsChoosing = false;
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e) {
        if (!await _viewModel.RefreshDeleteEligibilityAsync())
            return;
        bool confirmed;
        _viewModel.IsChoosing = true;
        try {
            confirmed = await AppDialogPage.ShowAsync(this, AppResources.DeleteManualGame,
                AppResources.DeleteManualGameQuestion, AppResources.DeleteManualGame, AppResources.Cancel, destructive: true);
        } catch (Exception exception) {
            _viewModel.ReportDialogFailure(exception);
            return;
        } finally {
            _viewModel.IsChoosing = false;
        }
        if (!confirmed || !await _viewModel.DeleteAsync())
            return;

        if (_viewModel.DeletionWarning is string warning) {
            _viewModel.IsChoosing = true;
            try {
                await AppDialogPage.ShowAsync(this, AppResources.DeleteManualGame, warning, AppResources.Ok);
            } catch (Exception exception) {
                _viewModel.ReportDialogFailure(exception);
                return;
            } finally {
                _viewModel.IsChoosing = false;
            }
        }

        await _viewModel.GoBackCommand.ExecuteAsync(null);
    }

    /// <summary>Uses the header departure command so saved settings stay frozen after navigation failure.</summary>
    protected override bool OnBackButtonPressed() {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}

using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Games.ManualGames.Features.Settings;

public partial class ManualGameEditorPage : ContentPage, IQueryAttributable
{
    private readonly ManualGameEditorViewModel _viewModel;
    private string? _gameKey;
    private bool _navigating;

    public ManualGameEditorPage(ManualGameEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParameters.GameKey, out object? key))
            _gameKey = key as string;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync(_gameKey);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_navigating || !_viewModel.CanInteract)
            return;
        if (await _viewModel.SaveAsync())
            await GoBackAsync();
    }

    private async void OnVisibilityToggled(object? sender, ToggledEventArgs e) =>
        await _viewModel.SetVisibilityAsync(e.Value);

    private async void OnResultModeClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanInteract)
            return;
        _viewModel.IsChoosing = true;
        try
        {
            int selected = await AppDialogPage.ChooseAsync(this, AppResources.ResultFromPoints,
                _viewModel.ResultChoices, _viewModel.ResultMode);
            if (selected >= 0)
                _viewModel.ResultMode = selected;
        }
        finally { _viewModel.IsChoosing = false; }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_navigating || !await _viewModel.RefreshDeleteEligibilityAsync())
            return;
        bool confirmed;
        _viewModel.IsChoosing = true;
        try
        {
            confirmed = await AppDialogPage.ShowAsync(this, AppResources.DeleteManualGame,
                AppResources.DeleteManualGameQuestion, AppResources.DeleteManualGame, AppResources.Cancel, destructive: true);
        }
        finally { _viewModel.IsChoosing = false; }
        if (confirmed && await _viewModel.DeleteAsync())
            await GoBackAsync();
    }

    private async Task GoBackAsync()
    {
        _navigating = true;
        try { await Shell.Current.GoToAsync("..", false); }
        finally { _navigating = false; }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_viewModel.CanInteract && !_navigating)
            await GoBackAsync();
    }

    protected override bool OnBackButtonPressed() => !_viewModel.CanInteract || _navigating || base.OnBackButtonPressed();
}

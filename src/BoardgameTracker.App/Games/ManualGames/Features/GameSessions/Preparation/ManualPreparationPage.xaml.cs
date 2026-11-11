using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Running;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Preparation;

public partial class ManualPreparationPage : ContentPage, IQueryAttributable
{
    private readonly ManualPreparationViewModel _viewModel;
    private readonly ILogger<ManualPreparationPage> _logger;
    private string _gameKey = string.Empty;
    private Guid _groupId;
    private bool _navigating;
    public ManualPreparationPage(ManualPreparationViewModel viewModel, ILogger<ManualPreparationPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParameters.GameKey, out object? key) && key is string gameKey)
            _gameKey = gameKey;
        if (query.TryGetValue(NavigationParameters.GroupId, out object? group) && group is Guid id)
            _groupId = id;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync(_gameKey, _groupId);
    }
    private void OnUpClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: ManualParticipantViewModel player })
            _viewModel.Move(player, -1);
    }
    private void OnDownClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: ManualParticipantViewModel player })
            _viewModel.Move(player, 1);
    }
    private void OnRotateClicked(object? sender, EventArgs e) => _viewModel.Rotate();
    private void OnRandomClicked(object? sender, EventArgs e) => _viewModel.RotateRandom();
    private void OnPaletteClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: ManualParticipantViewModel player })
            _viewModel.OpenColorPicker(player);
    }
    private void OnColorTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ManualColorOption option)
            _viewModel.SelectColor(option.Color);
    }
    private void OnClosePaletteClicked(object? sender, EventArgs e) => _viewModel.CloseColorPicker();
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsChoosingColor)
            _viewModel.CloseColorPicker();
        else if (_viewModel.CanInteract && !_navigating)
            await Shell.Current.GoToAsync("..", false);
    }
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsChoosingColor)
        {
            _viewModel.CloseColorPicker();
            return true;
        }
        return _navigating || _viewModel.IsBusy || base.OnBackButtonPressed();
    }
    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if (_navigating || !_viewModel.CanStart)
            return;
        Guid? id = await _viewModel.StartAsync();
        if (id is null)
            return;
        _navigating = true;
        try
        {
            if (Shell.Current is AppShell shell)
            {
                shell.CaptureGameSessionCaller(useRoot: true);
                shell.RememberGameSession(id.Value, _viewModel.GameKey);
            }
            await Shell.Current.GoToAsync(nameof(ManualRunningPage), false,
                new ShellNavigationQueryParameters { [NavigationParameters.GameSessionId] = id.Value });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open started manual session.");
            await AppDialogPage.ShowAsync(this, AppResources.RunningGame, AppResources.ManualSessionLoadFailed, AppResources.Ok);
        }
        finally { _navigating = false; }
    }
}

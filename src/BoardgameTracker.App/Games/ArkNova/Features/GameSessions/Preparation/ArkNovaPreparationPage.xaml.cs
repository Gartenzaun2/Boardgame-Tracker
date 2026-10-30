using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;

/// <summary>
/// MAUI view for Ark Nova preparation. It receives the group id through Shell,
/// binds screen state to the view model and performs concrete Shell navigation.
/// </summary>
public partial class ArkNovaPreparationPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaPreparationViewModel _viewModel;
    private Guid _groupId;

    public ArkNovaPreparationPage(ArkNovaPreparationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GroupId, out object? value)
            || value is not Guid groupId
            || groupId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Required navigation parameter '{NavigationParameters.GroupId}' is missing or invalid.");
        }

        _groupId = groupId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;

        if (_groupId != Guid.Empty && _viewModel.GroupId != _groupId)
        {
            await _viewModel.LoadAsync(_groupId);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private void OnMoveUpClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ArkNovaPreparationPlayerViewModel player })
            _viewModel.MovePlayer(player, -1);
    }

    private void OnMoveDownClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ArkNovaPreparationPlayerViewModel player })
            _viewModel.MovePlayer(player, 1);
    }

    private void OnDrawZooPlansClicked(object? sender, EventArgs e) =>
        _viewModel.DrawZooPlans();

    private async void OnRandomStartClicked(object? sender, EventArgs e)
    {
        RandomStartButton.IsEnabled = false;

        try
        {
            await Task.WhenAll(
                RandomStartButton.RotateToAsync(180, 180, Easing.CubicOut),
                RandomStartButton.ScaleToAsync(0.82, 180, Easing.CubicOut));

            _viewModel.RotateToRandomStartingPlayer();

            await Task.WhenAll(
                RandomStartButton.RotateToAsync(360, 180, Easing.CubicIn),
                RandomStartButton.ScaleToAsync(1.0, 180, Easing.CubicIn));
        }
        finally
        {
            RandomStartButton.Rotation = 0;
            RandomStartButton.Scale = 1;
            RandomStartButton.IsEnabled = true;
        }
    }

    private void OnRotateOrderClicked(object? sender, EventArgs e) =>
        _viewModel.RotateOrder();

    private void OnColorClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { BindingContext: ArkNovaPreparationPlayerViewModel player })
        {
            _viewModel.ToggleColorPicker(player);
        }
    }

    private void OnBlueColorClicked(object? sender, EventArgs e) =>
        SetColorFromButton(sender, PlayerColor.Blue);

    private void OnYellowColorClicked(object? sender, EventArgs e) =>
        SetColorFromButton(sender, PlayerColor.Yellow);

    private void OnRedColorClicked(object? sender, EventArgs e) =>
        SetColorFromButton(sender, PlayerColor.Red);

    private void OnBlackColorClicked(object? sender, EventArgs e) =>
        SetColorFromButton(sender, PlayerColor.Black);

    private void SetColorFromButton(object? sender, PlayerColor color)
    {
        if (sender is Button { BindingContext: ArkNovaPreparationPlayerViewModel player })
        {
            _viewModel.SetColor(player, color);
        }
    }

    private void OnZooPlanOption1Tapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ArkNovaPreparationPlayerViewModel player)
        {
            _viewModel.SelectZooPlan(player, 1);
        }
    }

    private void OnZooPlanOption2Tapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ArkNovaPreparationPlayerViewModel player)
        {
            _viewModel.SelectZooPlan(player, 2);
        }
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanStart)
        {
            string message = _viewModel.Players
                .Select(player => player.Color)
                .Distinct()
                .Count() != _viewModel.Players.Count
                ? AppResources.UniqueColorsRequired
                : AppResources.ZooPlanSelectionRequired;

            await AppDialogPage.ShowAsync(this,
                AppResources.StartValidationTitle,
                message,
                AppResources.Ok);
            return;
        }

        Guid? gameSessionId = await _viewModel.StartAsync();

        if (gameSessionId is null)
        {
            return;
        }

        if (Shell.Current is AppShell shell)
        {
            shell.RememberGameSession(gameSessionId.Value, ArkNovaGame.Key);
        }

        await Shell.Current.GoToAsync(
            nameof(ArkNovaRunningPage),
            new ShellNavigationQueryParameters
            {
                [NavigationParameters.GameSessionId] = gameSessionId.Value
            });
    }
}

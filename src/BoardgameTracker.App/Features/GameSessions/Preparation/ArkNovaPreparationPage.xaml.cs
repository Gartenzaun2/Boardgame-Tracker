using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Features.GameSessions.Preparation;

[QueryProperty(nameof(GroupId), "GroupId")]
public partial class ArkNovaPreparationPage : ContentPage
{
    private readonly ArkNovaPreparationViewModel _viewModel;
    private Guid _groupId;

    public ArkNovaPreparationPage(ArkNovaPreparationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public Guid GroupId
    {
        get => _groupId;
        set => _groupId = value;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_groupId != Guid.Empty && _viewModel.GroupId != _groupId)
        {
            await _viewModel.LoadAsync(_groupId);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private void OnDrawZooPlansClicked(object? sender, EventArgs e) =>
        _viewModel.DrawZooPlans();

    private void OnRandomStartClicked(object? sender, EventArgs e) =>
        _viewModel.RotateToRandomStartingPlayer();

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
        SetColorFromButton(sender, ArkNovaPlayerColor.Blue);

    private void OnYellowColorClicked(object? sender, EventArgs e) =>
        SetColorFromButton(sender, ArkNovaPlayerColor.Yellow);

    private void OnRedColorClicked(object? sender, EventArgs e) =>
        SetColorFromButton(sender, ArkNovaPlayerColor.Red);

    private void OnBlackColorClicked(object? sender, EventArgs e) =>
        SetColorFromButton(sender, ArkNovaPlayerColor.Black);

    private void SetColorFromButton(object? sender, ArkNovaPlayerColor color)
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
                ? "Jeder Spieler muss vor dem Start eine eigene Farbe haben."
                : "Jeder Spieler muss vor dem Start einen Zooplan ausgewählt haben.";

            await DisplayAlertAsync("Partie kann nicht gestartet werden", message, "OK");
            return;
        }

        await DisplayAlertAsync(
            "Vorbereitung vollständig",
            "Die Partievorbereitung ist vollständig.",
            "OK");
    }
}

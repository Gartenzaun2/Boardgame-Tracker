using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaPlayerScoringPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaPlayerScoringViewModel _viewModel;
    private readonly ILogger<ArkNovaPlayerScoringPage> _logger;
    private Guid _gameSessionId;
    private Guid _playerId;

    public ArkNovaPlayerScoringPage(ArkNovaPlayerScoringViewModel viewModel, ILogger<ArkNovaPlayerScoringPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? gameSessionValue)
            || gameSessionValue is not Guid gameSessionId || gameSessionId == Guid.Empty
            || !query.TryGetValue(NavigationParameters.PlayerId, out object? playerValue)
            || playerValue is not Guid playerId || playerId == Guid.Empty)
        {
            throw new InvalidOperationException("Required scoring navigation parameters are missing or invalid.");
        }
        _viewModel.Draft = query.TryGetValue(NavigationParameters.CorrectionDraft, out object? draft) ? draft as ArkNovaCorrectionDraft : null;
        _gameSessionId = gameSessionId;
        _playerId = playerId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        await _viewModel.LoadAsync(_gameSessionId, _playerId);
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this, AppResources.ScoringInputHelpTitle,
            $"{AppResources.NumberWheelHint}\n\n{AppResources.NumberWheelConfirmationHint}", AppResources.Ok);
    }

    private void OnReputationSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        // Binding updates only display the current value or suggestion; only a different value is input.
        if (BindingContext is ArkNovaPlayerScoringViewModel viewModel
            && Math.Abs(e.NewValue - viewModel.ReputationSliderValue) > 0.001)
            viewModel.ReputationSliderValue = e.NewValue;
    }

    private void OnReputationDragCompleted(object? sender, EventArgs e)
    {
        if (sender is not Slider slider)
            return;
        _viewModel.ReputationSliderValue = slider.Value;
        slider.Value = _viewModel.ReputationSliderValue;
    }

    private bool _saving;
    private async void OnDoneClicked(object? sender, EventArgs e) => await SaveAndCloseAsync(true);
    private async void OnBackClicked(object? sender, EventArgs e) => await SaveAndCloseAsync(false);

    private async Task SaveAndCloseAsync(bool confirmSuggestions)
    {
        if (_saving)
        {
            return;
        }
        _saving = true;
        ScoringInput.IsEnabled = false;
        bool persisted = false;
        try
        {
            if (confirmSuggestions)
                _viewModel.ConfirmSuggestedValues();

            if (!_viewModel.IsValid)
            {
                return;
            }
            await _viewModel.FlushAsync();
            persisted = true;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ark Nova player scoring failed. Persistence completed: {Persisted}.", persisted);
            await AppDialogPage.ShowAsync(this, AppResources.Scoring,
                persisted ? AppResources.NavigationAfterSaveFailed : AppResources.CorrectionFailed, AppResources.Ok);
        }
        finally
        {
            _saving = false;
            ScoringInput.IsEnabled = true;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_saving)
            Dispatcher.Dispatch(async () => await SaveAndCloseAsync(false));
        return true;
    }
}

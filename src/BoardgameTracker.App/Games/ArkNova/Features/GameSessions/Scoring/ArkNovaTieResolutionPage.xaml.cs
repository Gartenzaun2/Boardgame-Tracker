using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaTieResolutionPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaTieResolutionViewModel _viewModel;
    private readonly ILogger<ArkNovaTieResolutionPage> _logger;
    private Guid _gameSessionId;
    private bool _saving;

    public ArkNovaTieResolutionPage(ArkNovaTieResolutionViewModel viewModel, ILogger<ArkNovaTieResolutionPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? value)
            || value is not Guid gameSessionId || gameSessionId == Guid.Empty)
            throw new InvalidOperationException("Required gameSession navigation parameter is missing or invalid.");

        _viewModel.Draft = query.TryGetValue(NavigationParameters.CorrectionDraft, out object? draft) ? draft as ArkNovaHistoryCorrectionDraft : null;
        _gameSessionId = gameSessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        await _viewModel.LoadAsync(_gameSessionId);
    }

    private void OnPlacementClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ArkNovaPlacementOptionViewModel option })
            _viewModel.SelectPlacement(option.PlayerId, option.Placement);
    }

    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.ResolveTies, AppResources.ResolveTiesHint, AppResources.Ok);

    private async void OnFinishClicked(object? sender, EventArgs e)
    {
        if (_saving || !_viewModel.CanFinish)
            return;

        _saving = true;
        ResolutionContent.IsEnabled = false;
        bool persisted = false;
        try
        {
            await _viewModel.CompleteAsync();
            persisted = true;
            if (_viewModel.Draft is not null)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }
            await Shell.Current.GoToAsync(
                nameof(ArkNovaResultPage),
                new ShellNavigationQueryParameters { [NavigationParameters.GameSessionId] = _gameSessionId });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ark Nova tie completion failed. Persistence completed: {Persisted}.", persisted);
            await AppDialogPage.ShowAsync(this, AppResources.Scoring,
                persisted ? AppResources.NavigationAfterSaveFailed : AppResources.CorrectionFailed, AppResources.Ok);
        }
        finally
        {
            _saving = false;
            ResolutionContent.IsEnabled = true;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    protected override bool OnBackButtonPressed() => _saving || base.OnBackButtonPressed();
}

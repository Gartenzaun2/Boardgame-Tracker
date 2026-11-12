using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Scoring;

// The same concrete layout serves the points step or the single conditional decision step.
public partial class ManualScoringPage : ContentPage, IQueryAttributable
{
    private readonly ManualScoringViewModel _viewModel;
    private readonly ManualGameSessionNavigationHandler _navigation;
    private readonly ILogger<ManualScoringPage> _logger;
    private Guid _id;
    private bool _points;
    private bool _correction;
    private bool _navigating;
    public ManualScoringPage(ManualScoringViewModel viewModel, ManualGameSessionNavigationHandler navigation,
        ILogger<ManualScoringPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigation = navigation;
        _logger = logger;
        BindingContext = viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParameters.GameSessionId, out object? id) && id is Guid value)
            _id = value;
        _correction = query.TryGetValue(ManualGameSessionNavigationHandler.CorrectionParameter, out object? mode) && mode is true;
        _points = query.TryGetValue(ManualGameSessionNavigationHandler.PointsStepParameter, out object? step) && step is true;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
        {
            if (_correction)
                await _viewModel.LoadCorrectionAsync(_id);
            else
                await _viewModel.LoadAsync(_id, _points);
        }
    }
    private void OnPointsEntryReady(object? sender, EventArgs e)
    {
        if (sender is not Entry { BindingContext: ManualScoringPlayerViewModel player } entry)
            return;
#if ANDROID
        if (entry.Handler?.PlatformView is global::Android.Widget.EditText input)
        {
            input.KeyListener = global::Android.Text.Method.DigitsKeyListener.GetInstance(Java.Util.Locale.Root, player.AllowNegativePoints, false);
            input.InputType = global::Android.Text.InputTypes.ClassNumber
                | (player.AllowNegativePoints ? global::Android.Text.InputTypes.NumberFlagSigned : 0);
        }
#elif IOS
        if (entry.Handler?.PlatformView is UIKit.UITextField input)
            input.KeyboardType = player.AllowNegativePoints ? UIKit.UIKeyboardType.NumbersAndPunctuation : UIKit.UIKeyboardType.NumberPad;
#endif
    }
    private async void OnPointsTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_viewModel.IsBusy && sender is Entry { BindingContext: ManualScoringPlayerViewModel player })
        {
            player.PointsText = e.NewTextValue;
            await _viewModel.PointsChangedAsync();
        }
    }
    private async void OnWinnerChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox { BindingContext: ManualScoringPlayerViewModel player })
            await _viewModel.SelectWinnerAsync(player, e.Value);
    }
    private async void OnRankClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: ManualRankChoice choice })
            await choice.ChooseAsync();
    }
    private async void OnPrimaryClicked(object? sender, EventArgs e)
    {
        if (_navigating || !_viewModel.CanSubmit)
            return;
        ManualSubmitResult? result = await _viewModel.SubmitAsync();
        if (result is null || _correction && result == ManualSubmitResult.Decision)
            return;
        _navigating = true;
        try
        {
            if (_correction)
                await _navigation.ReturnFromCorrectionAsync();
            else
                await _navigation.ContinueAsync(_id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not navigate after manual scoring.");
            await AppDialogPage.ShowAsync(this, AppResources.Scoring, AppResources.ScoringLoadFailed, AppResources.Ok);
        }
        finally
        {
            _navigating = false;
        }
    }
    private async Task BackAsync()
    {
        if (_navigating || !_viewModel.CanInteract || !_correction && !await _viewModel.SaveDraftAsync())
            return;
        if (_correction && _viewModel.TryReturnToCorrectionPoints())
            return;
        _navigating = true;
        try
        {
            if (_correction)
            {
                _viewModel.CancelCorrection();
                await _navigation.ReturnFromCorrectionAsync();
            }
            else if (!_points && _viewModel.HasPointsStep)
            {
                var stack = Navigation.NavigationStack;
                if (stack.Count > 1 && stack[^2] is ManualScoringPage previous && previous._points)
                    await Shell.Current.GoToAsync("..", false);
                else
                {
                    await Shell.Current.GoToAsync(ManualGameSessionNavigationHandler.PointsRoute, false, new ShellNavigationQueryParameters
                        { [NavigationParameters.GameSessionId] = _id, [ManualGameSessionNavigationHandler.PointsStepParameter] = true });
                    Navigation.RemovePage(this);
                }
            }
            else await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();
        }
        finally
        {
            _navigating = false;
        }
    }
    private async void OnBackClicked(object? sender, EventArgs e) => await BackAsync();
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.CanInteract && !_navigating)
            Dispatcher.Dispatch(async () => await BackAsync());
        return true;
    }
    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        if (_navigating || !_viewModel.CanInteract)
            return;
        if (_correction)
        {
            _navigating = true;
            try
            {
                _viewModel.CancelCorrection();
                await _navigation.ReturnFromCorrectionAsync();
            }
            finally { _navigating = false; }
            return;
        }
        bool confirmed;
        _viewModel.IsConfirming = true;
        try
        {
            confirmed = await AppDialogPage.ShowAsync(this, AppResources.DiscardScoringTitle, AppResources.DiscardScoringMessage, AppResources.Discard, AppResources.Cancel, destructive: true);
        }
        finally
        {
            _viewModel.IsConfirming = false;
        }
        if (!confirmed)
            return;
        _navigating = true;
        try
        {
            if (await _viewModel.DiscardAsync(confirmed))
                await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not discard manual scoring.");
            await AppDialogPage.ShowAsync(this, AppResources.DiscardScoringTitle, AppResources.DiscardScoringFailed, AppResources.Ok);
        }
        finally
        {
            _navigating = false;
        }
    }
}

using BoardgameTracker.App.Features.GameSessions.Result;
using BoardgameTracker.App.Features.Games.ArkNova;
using BoardgameTracker.App.Features.Games.ArkNova.Settings;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.Scoring;

namespace BoardgameTracker.App.Features.GameSessions.Scoring;

public partial class ArkNovaScoringPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaScoringViewModel _viewModel;
    private Guid _sessionId;

    public ArkNovaScoringPage(ArkNovaScoringViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.SessionId, out object? value)
            || value is not Guid sessionId || sessionId == Guid.Empty)
        {
            throw new InvalidOperationException($"Required navigation parameter '{NavigationParameters.SessionId}' is missing or invalid.");
        }

        _sessionId = sessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_sessionId != Guid.Empty)
        {
            await _viewModel.LoadAsync(_sessionId);
        }
    }

    private async void OnPlayerTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ArkNovaScoringPlayerViewModel player || !_viewModel.TryBeginNavigation())
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(
                nameof(ArkNovaPlayerScoringPage),
                new ShellNavigationQueryParameters
                {
                    [NavigationParameters.SessionId] = _sessionId,
                    [NavigationParameters.PersonId] = player.PersonId
                });
        }
        finally
        {
            _viewModel.EndNavigation();
        }
    }

    private void OnAddSpecialCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Guid personId)
        {
            _viewModel.OpenSpecialCardPicker(personId);
        }
    }

    private async void OnSpecialCardOptionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ArkNovaSpecialCardOptionViewModel option
            || _viewModel.SpecialCardTargetPersonId is not Guid personId)
        {
            return;
        }

        if (option.IsAssignedToTarget)
        {
            await _viewModel.RemoveSpecialCardAsync(personId, option.Card);
            return;
        }

        if (option.IsAssignedToOtherPlayer)
        {
            bool confirmed = await DisplayAlertAsync(
                AppResources.SpecialCardMultipleTitle,
                string.Format(
                    AppResources.SpecialCardMultipleMessageFormat,
                    ArkNovaSpecialCardPresentation.GetName(option.Card)),
                AppResources.Yes,
                AppResources.Cancel);

            if (!confirmed)
            {
                return;
            }
        }

        await _viewModel.AssignSpecialCardAsync(personId, option.Card);
    }

    private void OnCloseSpecialCardPickerClicked(object? sender, EventArgs e) =>
        _viewModel.CloseSpecialCardPicker();

    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        ArkNovaCompletionPlan? plan = await _viewModel.PrepareCompletionAsync();
        if (plan is null) return;

        string route = plan.HasTies ? nameof(ArkNovaTieResolutionPage) : nameof(ArkNovaResultPage);
        if (!plan.HasTies)
        {
            await _viewModel.CompleteWithoutTieAsync(plan.DefaultPlacements);
        }

        await Shell.Current.GoToAsync(
            route,
            new ShellNavigationQueryParameters { [NavigationParameters.SessionId] = _sessionId });
    }

    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync(
            AppResources.DiscardScoringTitle,
            AppResources.DiscardScoringMessage,
            AppResources.Discard,
            AppResources.Cancel);

        if (confirmed && await _viewModel.DiscardAsync())
        {
            await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");
        }
    }

    private async void OnCameraClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Fotoerkennung", "Der Kamera-Einstieg ist vorbereitet. Die Bilderkennung folgt im nächsten Feature-Slice.", AppResources.Ok);

    private async void OnSettingsClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage));

    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () => await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}"));
        return true;
    }
}

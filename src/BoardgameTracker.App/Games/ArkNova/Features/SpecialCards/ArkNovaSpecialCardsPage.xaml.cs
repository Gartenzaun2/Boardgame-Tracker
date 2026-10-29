using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;

namespace BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;

public partial class ArkNovaSpecialCardsPage : ContentPage
{
    private readonly ArkNovaSpecialCardsViewModel _viewModel;
    private bool _navigating;
    public ArkNovaSpecialCardsPage(ArkNovaSpecialCardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        await _viewModel.LoadAsync();
    }
    private async void OnBackClicked(object? sender, EventArgs e) => await NavigateAsync(null, true);
    private async void OnNewClicked(object? sender, EventArgs e) => await NavigateAsync(null, false);
    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ArkNovaSpecialCardData card })
            await NavigateAsync(card.Id, false);
    }
    private async Task NavigateAsync(Guid? id, bool back)
    {
        if (_navigating || _viewModel.IsBusy)
            return;
        _navigating = true;
        try
        {
            if (back)
                await Shell.Current.GoToAsync("..");
            else
                await Shell.Current.GoToAsync(nameof(ArkNovaSpecialCardEditorPage),
                    new ShellNavigationQueryParameters { [NavigationParameters.SpecialCardId] = id! });
        }
        finally
        {
            _navigating = false;
        }
    }
    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.SpecialCardManagementTitle,
            AppResources.SpecialCardUsageHint, AppResources.Ok);
}

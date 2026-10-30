using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.EditPlayer;

public partial class EditPlayerPage : ContentPage, IQueryAttributable
{
    private readonly EditPlayerViewModel _viewModel;
    private readonly ILogger<EditPlayerPage> _logger;
    private Guid _playerId;
    private bool _saving;

    public EditPlayerPage(EditPlayerViewModel viewModel, ILogger<EditPlayerPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.PlayerId, out object? value) || value is not Guid id || id == Guid.Empty)
            throw new InvalidOperationException("Required player navigation parameter is missing or invalid.");
        _playerId = id;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync(_playerId);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_saving)
            return;
        _saving = true;
        EditorContent.IsEnabled = false;
        try
        {
            if (await _viewModel.SaveAsync())
                await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not leave player editor after saving.");
            await AppDialogPage.ShowAsync(this, AppResources.EditPlayer, AppResources.PlayerNavigationFailed, AppResources.Ok);
        }
        finally
        {
            _saving = false;
            EditorContent.IsEnabled = true;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_saving)
            await Shell.Current.GoToAsync("..");
    }
    protected override bool OnBackButtonPressed() => _saving || base.OnBackButtonPressed();
}

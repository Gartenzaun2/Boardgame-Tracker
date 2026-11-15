using System.Globalization;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Settings.Navigation;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.EditPlayer;

public partial class EditPlayerPage : ContentPage, IQueryAttributable {
    private readonly EditPlayerViewModel _viewModel;
    private readonly ILogger<EditPlayerPage> _logger;
    private Guid _playerId;
    private bool _isSavingOrNavigating;

    public EditPlayerPage(EditPlayerViewModel viewModel, ILogger<EditPlayerPage> logger) {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (query.TryGetValue(NavigationParameters.NewPlayer, out object? create) && create is true) {
            _viewModel.InitializeNew();
            return;
        }
        if (!query.TryGetValue(NavigationParameters.PlayerId, out object? value) || value is not Guid id || id == Guid.Empty)
            throw new InvalidOperationException("Required player navigation parameter is missing or invalid.");
        _playerId = id;
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this) && !_viewModel.IsNew)
            await _viewModel.LoadAsync(_playerId);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
        => await SaveAsync(false);

    private async void OnAddAnotherClicked(object? sender, EventArgs e)
        => await SaveAsync(true);

    private async Task SaveAsync(bool addAnother) {
        if (_isSavingOrNavigating)
            return;
        _isSavingOrNavigating = true;
        EditorContent.IsEnabled = false;
        try {
            if (addAnother) {
                await _viewModel.SaveAndAddAnotherAsync();
                return;
            }

            if (!await _viewModel.SaveAsync())
                return;

            if (_viewModel.IsNew && Shell.Current is AppShell shell)
                await shell.NavigateToMainSectionAsync(AppMainSection.Participants);
            else
                await Shell.Current.GoToAsync("..", false);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Could not leave player editor after saving.");
            try {
                await AppDialogPage.ShowAsync(this, AppResources.EditPlayer, AppResources.PlayerEditorNavigationFailed, AppResources.Ok);
            }
            catch (Exception dialogException) {
                _logger.LogError(dialogException, "Could not show player editor navigation error.");
            }
        }
        finally {
            _isSavingOrNavigating = false;
            EditorContent.IsEnabled = true;
        }
    }

    // Keep separate entry points: the header awaits Shell navigation, while system back can defer to MAUI.
    private async void OnBackClicked(object? sender, EventArgs e) {
        if (_isSavingOrNavigating)
            return;
        if (_viewModel.IsSaveCompleted)
            await SaveAsync(false);
        else
            await Shell.Current.GoToAsync("..", false);
    }

    /// <summary>
    /// Handles MAUI system back independently of the header button, protecting saves and retrying departure after completion.
    /// </summary>
    protected override bool OnBackButtonPressed() {
        // Keep the editor open until both persistence and navigation have finished.
        if (_isSavingOrNavigating)
            return true;
        if (_viewModel.IsSaveCompleted) {
            // A completed save only needs another attempt to leave; it must not write again.
            // This callback returns synchronously, so dispatch the asynchronous navigation attempt.
            Dispatcher.Dispatch(async () => await SaveAsync(false));
            return true;
        }
        // Unsaved editors retain MAUI's default system-back behavior.
        return base.OnBackButtonPressed();
    }
}

public sealed class PlayerEditorBoolNegationConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag ? !flag : BindableProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag ? !flag : BindableProperty.UnsetValue;
}

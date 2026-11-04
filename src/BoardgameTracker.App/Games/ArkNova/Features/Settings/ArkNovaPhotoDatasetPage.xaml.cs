using BoardgameTracker.Application.Games.ArkNova.PhotoDataset;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

public partial class ArkNovaPhotoDatasetPage : ContentPage
{
    private readonly ArkNovaPhotoDataset _dataset;
    private readonly ILogger<ArkNovaPhotoDatasetPage> _logger;
    private bool _busy;
    private bool _loading;

    public ArkNovaPhotoDatasetPage(ArkNovaPhotoDataset dataset, ILogger<ArkNovaPhotoDatasetPage> logger)
    {
        InitializeComponent();
        _dataset = dataset;
        _logger = logger;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await RunAsync(RefreshAsync);
    }

    private async Task RefreshAsync()
    {
        ArkNovaDatasetSummary summary = await _dataset.GetSummaryAsync();
        _loading = true;
        try { CollectionSwitch.IsToggled = summary.Enabled; }
        finally { _loading = false; }
        SummaryLabel.Text = string.Format(AppResources.PhotoDatasetSummary, summary.Count, summary.Bytes / 1048576d);
        ExportButton.IsEnabled = DeleteButton.IsEnabled = summary.Count > 0;
    }

    private async void OnCollectionToggled(object? sender, ToggledEventArgs e)
    {
        if (_loading || _busy)
            return;
        await RunAsync(async () =>
        {
            ArkNovaDatasetSummary summary = await _dataset.GetSummaryAsync();
            if (!e.Value && summary.Count > 0)
            {
                int choice = await AppDialogPage.ChooseAsync(this, AppResources.PhotoDatasetDisable,
                    [AppResources.PhotoDatasetKeep, AppResources.PhotoDatasetDelete], -1);
                if (choice < 0)
                {
                    await RefreshAsync();
                    return;
                }
                await _dataset.SetEnabledAsync(false);
                if (choice == 1)
                    await _dataset.DeleteSamplesAsync();
            }
            else
                await _dataset.SetEnabledAsync(e.Value);
            await RefreshAsync();
        });
    }

    private async void OnDeleteClicked(object? sender, EventArgs e) => await RunAsync(async () =>
    {
        if (await AppDialogPage.ShowAsync(this, AppResources.PhotoDatasetTitle, AppResources.PhotoDatasetDeleteQuestion,
            AppResources.PhotoDatasetDelete, AppResources.Cancel, destructive: true))
            await _dataset.DeleteSamplesAsync();
        await RefreshAsync();
    });

    private async void OnExportClicked(object? sender, EventArgs e) => await RunAsync(async () =>
    {
        // Only this explicit user action offers the copy to a system share target.
        string path = Path.Combine(FileSystem.CacheDirectory, $"ark-nova-dataset-{Guid.NewGuid():N}.zip");
        await using (Stream output = File.Create(path))
            await _dataset.ExportAsync(output);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = AppResources.PhotoDatasetExport,
            File = new ShareFile(path, "application/zip")
        });
    });

    private async Task RunAsync(Func<Task> action)
    {
        if (_busy)
            return;
        _busy = true;
        Actions.IsEnabled = false;
        ErrorLabel.IsVisible = false;
        try { await action(); }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Ark Nova dataset settings operation failed.");
            ErrorLabel.Text = AppResources.PhotoDatasetFailed;
            ErrorLabel.IsVisible = true;
            // A failed preference write must not leave the switch claiming a new persisted state.
            try { await RefreshAsync(); }
            catch (Exception refreshException) { _logger.LogWarning(refreshException, "Dataset summary unavailable."); }
        }
        finally
        {
            Actions.IsEnabled = true;
            _busy = false;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_busy)
            await Shell.Current.GoToAsync("..", false);
    }

    protected override bool OnBackButtonPressed() => _busy || base.OnBackButtonPressed();
}

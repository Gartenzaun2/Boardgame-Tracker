using BoardgameTracker.App.Features.History.Export;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.History;

public partial class HistoryPage
{
    private bool StartTransfer()
    {
        if (_transferring || _loading || _navigating || _choosing)
            return false;
        _transferring = true;
        BusyOverlay.IsVisible = true;
        return true;
    }
    private async void OnExportClicked(object? sender, EventArgs e)
    {
        if (!HistoryExportFlow.CanExport(_visibleIds) || !StartTransfer())
            return;
        try
        {
            await _exportFlow.RunAsync(_visibleIds, AppInfo.Current.VersionString,
                targets => AppDialogPage.ChooseAsync(this, AppResources.TransferExportTitle,
                    targets.Select(target => target.DisplayName).ToArray(), -1, AppResources.Cancel));
        }
        catch (Exception exception)
        {
            await ShowTransferErrorAsync(exception);
        }
        finally
        {
            _transferring = false;
            BusyOverlay.IsVisible = false;
        }
    }
    private async void OnImportClicked(object? sender, EventArgs e)
    {
        if (!StartTransfer())
            return;
        bool committed = false;
        try
        {
            FileResult? selected = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = AppResources.TransferPickFile,
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = ["application/json", "application/octet-stream", "text/*"],
                    [DevicePlatform.iOS] = ["public.data"],
                    [DevicePlatform.WinUI] = [".bgt"]
                })
            });
            if (selected is null)
                return;
            await using Stream stream = await selected.OpenReadAsync();
            ImportPlan plan = await _transfer.AnalyzeAsync(stream);
            List<GroupResolution> resolutions = [];
            while (plan.Conflicts.Length != 0)
            {
                GroupConflict conflict = plan.Conflicts[0];
                string mappingText = string.Join(Environment.NewLine, conflict.Imported.GroupPlayers.Select(player =>
                {
                    BgtPlayer imported = plan.Source.Players.Single(candidate => candidate.Id == player.PlayerId);
                    return conflict.FixedMappings.TryGetValue(player.PlayerId, out Guid localId)
                        ? $"{imported.Name} → {plan.Snapshot.Players.Single(candidate => candidate.Id == localId).Name}" : imported.Name;
                }));
                int choice = await AppDialogPage.ChooseAsync(this,
                    string.Format(AppResources.TransferResolveGroup, conflict.Imported.Name, mappingText),
                    [AppResources.TransferMerge, AppResources.TransferSplit], -1);
                if (choice < 0)
                    return;
                Dictionary<Guid, Guid> mappings = [];
                if (choice == 0)
                {
                    HashSet<Guid> used = conflict.FixedMappings.Values.ToHashSet();
                    foreach (BgtGroupPlayer player in conflict.Imported.GroupPlayers.Where(player => !conflict.FixedMappings.ContainsKey(player.PlayerId)))
                    {
                        LocalPlayer[] candidates = plan.Snapshot.Players.Where(candidate => conflict.Local.PlayerIds.Contains(candidate.Id) && !used.Contains(candidate.Id)).ToArray();
                        BgtPlayer imported = plan.Source.Players.Single(candidate => candidate.Id == player.PlayerId);
                        int picked = await AppDialogPage.ChooseAsync(this, string.Format(AppResources.TransferMapPlayer, imported.Name), candidates.Select(candidate => candidate.Name).ToArray(), -1);
                        if (picked < 0)
                            return;
                        mappings.Add(imported.Id, candidates[picked].Id);
                        used.Add(candidates[picked].Id);
                    }
                }
                resolutions.Add(new(conflict.Imported.Id, choice == 1, mappings));
                plan = ImportPlanner.Analyze(plan.Source, plan.Snapshot, resolutions);
            }
            TransferPreview preview = await _transfer.PreviewAsync(plan);
            string summary = string.Format(AppResources.TransferPreviewFormat,
                plan.GameSessions.Length, plan.Skipped, preview.GamesExisting,
                plan.Players.Count(player => !player.IsNew && !player.IsManual), plan.Players.Count(player => player.IsManual), plan.Players.Count(player => player.IsNew),
                plan.Groups.Count(group => !group.IsNew), plan.Groups.Count(group => group.IsNew), preview.ReferencesExisting, preview.ReferencesNew);
            summary += Environment.NewLine + string.Join(Environment.NewLine, plan.Groups.Where(group => group.IsNew).Select(group => group.Name));
            if (plan.GameSessions.Length == 0)
            {
                await AppDialogPage.ShowAsync(this, AppResources.TransferPreview, summary, AppResources.Ok);
                return;
            }
            if (!await AppDialogPage.ShowAsync(this, AppResources.TransferPreview, summary, AppResources.TransferCommit, AppResources.Cancel))
                return;
            await _transfer.CommitAsync(plan);
            committed = true;
            await AppDialogPage.ShowAsync(this, AppResources.TransferImport, AppResources.TransferSuccess, AppResources.Ok);
        }
        catch (Exception exception)
        {
            await ShowTransferErrorAsync(exception);
        }
        finally
        {
            _transferring = false;
            BusyOverlay.IsVisible = false;
            if (committed)
                await ReloadAsync();
        }
    }
    private async Task ShowTransferErrorAsync(Exception exception)
    {
        _logger.LogError(exception, "History transfer failed.");
        string message = exception is InvalidDataException { Message: "UpdateRequired" }
            ? AppResources.TransferUpdateRequired : AppResources.TransferFailed;
        await AppDialogPage.ShowAsync(this, AppResources.History, message, AppResources.Ok);
    }
    protected override bool OnBackButtonPressed() => _transferring || base.OnBackButtonPressed();
}

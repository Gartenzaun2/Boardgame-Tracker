using BoardgameTracker.App.Features.History.Export;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.History.Transfer;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.History;

public partial class HistoryPage {
    private bool StartTransfer() {
        if (_isTransferring || _isLoading || _isNavigating || _isFilterSelectionOpen)
            return false;

        ErrorLabel.IsVisible = false;
        _isTransferring = true;
        BusyOverlay.IsVisible = true;
        return true;
    }

    private async void OnExportClicked(object? sender, EventArgs e) {
        if (!HistoryExportFlow.CanExport(_visibleIds) || !StartTransfer())
            return;

        try {
            await _exportFlow.RunAsync(_visibleIds, AppInfo.Current.VersionString,
                targets => AppDialogPage.ChooseAsync(this, AppResources.TransferExportTitle,
                    targets.Select(target => target.DisplayName).ToArray(), -1, AppResources.Cancel));
        } catch (Exception exception) {
            await ShowTransferErrorAsync(exception);
        } finally {
            _isTransferring = false;
            BusyOverlay.IsVisible = false;
        }
    }

    private async void OnImportClicked(object? sender, EventArgs e) {
        if (!StartTransfer())
            return;

        bool committed = false;
        string? fallbackMessage = null;

        try {
            FileResult? selected = await FilePicker.Default.PickAsync(new PickOptions {
                PickerTitle = AppResources.TransferPickFile,
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> {
                    [DevicePlatform.Android] = ["application/json", "application/octet-stream", "text/*"],
                    [DevicePlatform.iOS] = ["public.data"],
                    [DevicePlatform.WinUI] = [".bgt"]
                })
            });
            if (selected is null)
                return;

            await using Stream stream = await selected.OpenReadAsync();
            ImportPlan? plan = await _transfer.AnalyzeAsync(stream);
            plan = await ResolveImportGroupsAsync(plan);
            if (plan is null)
                return;

            plan = await ResolveImportGamesAsync(plan);
            if (plan is null || !await ConfirmImportAsync(plan))
                return;

            await _transfer.CommitAsync(plan);
            committed = true;
            await AppDialogPage.ShowAsync(this, AppResources.TransferImport, AppResources.TransferSuccess, AppResources.Ok);
        } catch (Exception exception) {
            fallbackMessage = await ShowTransferErrorAsync(exception, committed);
        } finally {
            _isTransferring = false;
            BusyOverlay.IsVisible = false;
            if (committed) {
                await ReloadAsync();
                // Reload clears transient errors; a failed result dialog must remain visible afterwards.
                if (fallbackMessage is not null) {
                    ErrorLabel.Text = fallbackMessage;
                    ErrorLabel.IsVisible = true;
                }
            }
        }
    }

    /// <returns>The resolved plan, or null when group resolution or player mapping is cancelled.</returns>
    private async Task<ImportPlan?> ResolveImportGroupsAsync(ImportPlan plan) {
        List<GroupResolution> resolutions = [];
        while (plan.Conflicts.Length != 0) {
            GroupConflict conflict = plan.Conflicts[0];
            string mappingText = string.Join(Environment.NewLine, conflict.Imported.GroupPlayers.Select(player => {
                BgtPlayer imported = plan.Source.Players.Single(candidate => candidate.Id == player.PlayerId);
                return conflict.FixedMappings.TryGetValue(player.PlayerId, out Guid localId)
                    ? $"{imported.Name} → {plan.Snapshot.Players.Single(candidate => candidate.Id == localId).Name}" : imported.Name;
            }));
            int choice = await AppDialogPage.ChooseAsync(this,
                string.Format(AppResources.TransferResolveGroup, conflict.Imported.Name, mappingText),
                [AppResources.TransferMerge, AppResources.TransferSplit], -1);
            if (choice < 0)
                return null;

            Dictionary<Guid, Guid> mappings = [];
            if (choice == 0) {
                HashSet<Guid> used = conflict.FixedMappings.Values.ToHashSet();
                foreach (BgtGroupPlayer player in conflict.Imported.GroupPlayers.Where(player => !conflict.FixedMappings.ContainsKey(player.PlayerId))) {
                    LocalPlayer[] candidates = plan.Snapshot.Players.Where(candidate => conflict.Local.PlayerIds.Contains(candidate.Id) && !used.Contains(candidate.Id)).ToArray();
                    BgtPlayer imported = plan.Source.Players.Single(candidate => candidate.Id == player.PlayerId);
                    int picked = await AppDialogPage.ChooseAsync(this, string.Format(AppResources.TransferMapPlayer, imported.Name), candidates.Select(candidate => candidate.Name).ToArray(), -1);
                    if (picked < 0)
                        return null;
                    mappings.Add(imported.Id, candidates[picked].Id);
                    used.Add(candidates[picked].Id);
                }
            }

            resolutions.Add(new(conflict.Imported.Id, choice == 1, mappings));
            plan = _transfer.Resolve(plan, resolutions);
        }

        return plan;
    }

    /// <returns>The resolved plan, or null when game resolution is cancelled.</returns>
    private async Task<ImportPlan?> ResolveImportGamesAsync(ImportPlan plan) {
        List<GameResolution> gameResolutions = [];
        while (plan.GameConflicts.Length != 0) {
            GameConflict conflict = plan.GameConflicts[0];
            string[] options = conflict.Candidates.Select(candidate => candidate.Name).Append(AppResources.TransferNewGame).ToArray();
            int picked = await AppDialogPage.ChooseAsync(this, string.Format(AppResources.TransferResolveGame, conflict.Name), options, -1);
            if (picked < 0)
                return null;
            gameResolutions.Add(new(conflict.SourceId, picked < conflict.Candidates.Length ? conflict.Candidates[picked].Id : null));
            plan = _transfer.Resolve(plan, games: gameResolutions);
        }

        return plan;
    }

    /// <returns>True only when importable sessions exist and the user confirms committing them.</returns>
    private async Task<bool> ConfirmImportAsync(ImportPlan plan) {
        TransferPreview preview = await _transfer.PreviewAsync(plan);
        string summary = string.Format(AppResources.TransferPreviewFormat,
            plan.GameSessions.Length, plan.Skipped, preview.GamesExisting,
            plan.Players.Count(player => !player.IsNew && !player.IsManual), plan.Players.Count(player => player.IsManual), plan.Players.Count(player => player.IsNew),
            plan.Groups.Count(group => !group.IsNew), plan.Groups.Count(group => group.IsNew), preview.ReferencesExisting, preview.ReferencesNew);
        summary += Environment.NewLine + string.Join(Environment.NewLine, plan.Groups.Where(group => group.IsNew).Select(group => group.Name));
        summary += Environment.NewLine + string.Join(Environment.NewLine, plan.Games.Select(game =>
            string.Format(game.IsNew ? AppResources.TransferGameCreated : AppResources.TransferGameMatched,
                game.SourceName, game.Name)));
        if (plan.GameSessions.Length == 0) {
            await AppDialogPage.ShowAsync(this, AppResources.TransferPreview, summary, AppResources.Ok);
            return false;
        }

        return await AppDialogPage.ShowAsync(this, AppResources.TransferPreview, summary, AppResources.TransferCommit, AppResources.Cancel);
    }

    /// <returns>The inline fallback message when modal presentation fails; otherwise null.</returns>
    private async Task<string?> ShowTransferErrorAsync(Exception exception, bool importCommitted = false) {
        if (importCommitted)
            _logger.LogError(exception, "Could not present the completed import result.");
        else
            _logger.LogError(exception, "History transfer failed.");

        string message = importCommitted ? AppResources.TransferResultPresentationFailed
            : exception is InvalidDataException { Message: "UpdateRequired" } ? AppResources.TransferUpdateRequired : AppResources.TransferFailed;
        try {
            await AppDialogPage.ShowAsync(this, AppResources.History, message, AppResources.Ok);
            return null;
        } catch (Exception presentationException) {
            _logger.LogError(presentationException, "Could not show history transfer error.");
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
            return message;
        }
    }

    /// <summary>Prevents system back from interrupting a transfer or its conflict-resolution dialogs.</summary>
    protected override bool OnBackButtonPressed()
        => _isTransferring || _isLoading || _isNavigating || _isFilterSelectionOpen || base.OnBackButtonPressed();
}

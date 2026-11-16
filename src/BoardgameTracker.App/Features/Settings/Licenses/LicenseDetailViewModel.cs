using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.Licenses;

public partial class LicenseDetailViewModel : ObservableObject {
    private readonly LicenseTextService _licenses;
    private readonly ILogger<LicenseDetailViewModel> _logger;

    public LicenseDetailViewModel(LicenseTextService licenses, ILogger<LicenseDetailViewModel> logger) {
        _licenses = licenses;
        _logger = logger;
    }

    /// <summary>Remember only a successfully loaded component so failed or cancelled reads can be retried without losing the displayed identity.</summary>
    private string? _loadedComponentId;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => ErrorMessage is not null;

    [ObservableProperty]
    public partial string DisplayName { get; private set; } = AppResources.OpenSourceLicenses;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVersion))]
    public partial string VersionText { get; private set; } = string.Empty;
    public bool HasVersion => VersionText.Length > 0;

    [ObservableProperty]
    public partial string LicenseDisplayName { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSpdx))]
    public partial string SpdxText { get; private set; } = string.Empty;
    public bool HasSpdx => SpdxText.Length > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttribution))]
    public partial string AttributionText { get; private set; } = string.Empty;
    public bool HasAttribution => AttributionText.Length > 0;

    [ObservableProperty]
    public partial string LicenseText { get; private set; } = string.Empty;

    public async Task LoadAsync(string componentId, CancellationToken cancellationToken = default) {
        if (IsBusy || _loadedComponentId == componentId)
            return;

        IsBusy = true;
        ErrorMessage = null;
        LicenseText = string.Empty;
        _loadedComponentId = null;
        DisplayName = AppResources.OpenSourceLicenses;
        VersionText = string.Empty;
        LicenseDisplayName = string.Empty;
        SpdxText = string.Empty;
        AttributionText = string.Empty;

        try {
            LicenseContent content = await _licenses.LoadLicenseAsync(componentId, cancellationToken);
            DisplayName = content.Component.DisplayName;
            VersionText = string.IsNullOrWhiteSpace(content.Component.Version) ? string.Empty
                : string.Format(AppResources.LicenseVersionFormat, content.Component.Version);
            LicenseDisplayName = content.Component.LicenseDisplayName;
            SpdxText = string.IsNullOrWhiteSpace(content.Component.SpdxIdentifier) ? string.Empty
                : string.Format(AppResources.LicenseSpdxFormat, content.Component.SpdxIdentifier);
            AttributionText = content.Component.AttributionText ?? string.Empty;
            LicenseText = content.Text;
            _loadedComponentId = componentId;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // A cancelled read has not published a loaded ID, so the next appearance may retry.
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load packaged license text for {ComponentId}.", componentId);
            ErrorMessage = AppResources.LicensesLoadFailed;
        } finally {
            IsBusy = false;
        }
    }
}

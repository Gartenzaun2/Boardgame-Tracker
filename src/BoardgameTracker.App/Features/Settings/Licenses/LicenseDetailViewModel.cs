using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.Licenses;

public partial class LicenseDetailViewModel(LicenseTextService licenses, ILogger<LicenseDetailViewModel> logger) : ObservableObject
{
    private string? _loadedId;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }
    public bool IsIdle => !IsBusy;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => ErrorMessage is not null;
    [ObservableProperty] public partial string DisplayName { get; private set; } = AppResources.OpenSourceLicenses;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVersion))]
    public partial string VersionText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string LicenseDisplayName { get; private set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSpdx))]
    public partial string SpdxText { get; private set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAttribution))]
    public partial string AttributionText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string LicenseText { get; private set; } = string.Empty;
    public bool HasVersion => VersionText.Length > 0;
    public bool HasSpdx => SpdxText.Length > 0;
    public bool HasAttribution => AttributionText.Length > 0;

    public async Task LoadAsync(string componentId, CancellationToken cancellationToken = default)
    {
        if (IsBusy || _loadedId == componentId)
            return;
        IsBusy = true;
        ErrorMessage = null;
        LicenseText = string.Empty;
        _loadedId = null;
        DisplayName = AppResources.OpenSourceLicenses;
        VersionText = string.Empty;
        LicenseDisplayName = string.Empty;
        SpdxText = string.Empty;
        AttributionText = string.Empty;
        try
        {
            LicenseContent content = await licenses.LoadLicenseAsync(componentId, cancellationToken);
            DisplayName = content.Component.DisplayName;
            VersionText = string.IsNullOrWhiteSpace(content.Component.Version) ? string.Empty
                : string.Format(AppResources.LicenseVersionFormat, content.Component.Version);
            LicenseDisplayName = content.Component.LicenseDisplayName;
            SpdxText = string.IsNullOrWhiteSpace(content.Component.SpdxIdentifier) ? string.Empty
                : string.Format(AppResources.LicenseSpdxFormat, content.Component.SpdxIdentifier);
            AttributionText = content.Component.AttributionText ?? string.Empty;
            LicenseText = content.Text;
            _loadedId = componentId;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load packaged license text for {ComponentId}.", componentId);
            ErrorMessage = AppResources.LicensesLoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

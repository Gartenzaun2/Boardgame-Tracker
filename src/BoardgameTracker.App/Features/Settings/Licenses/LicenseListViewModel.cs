using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.Licenses;

public partial class LicenseListViewModel : ObservableObject {
    private readonly LicenseTextService _licenses;
    private readonly ILogger<LicenseListViewModel> _logger;

    public LicenseListViewModel(LicenseTextService licenses, ILogger<LicenseListViewModel> logger) {
        _licenses = licenses;
        _logger = logger;
    }

    /// <summary>Retain a successful catalog load across navigation and modal dismissal; cancelled reads remain retryable.</summary>
    private bool _isInitialLoadCompleted;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => ErrorMessage is not null;

    [ObservableProperty]
    public partial string IjgAttribution { get; private set; } = string.Empty;

    public ObservableCollection<ThirdPartyLicense> Components { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        if (IsBusy || _isInitialLoadCompleted)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try {
            IReadOnlyList<ThirdPartyLicense> catalog = await _licenses.LoadCatalogAsync(cancellationToken);
            string attribution = catalog.Single(item => item.Id == "independent-jpeg-group").AttributionText
                ?? throw new InvalidDataException("Required IJG attribution is missing.");
            Components.Clear();
            foreach (ThirdPartyLicense component in catalog)
                Components.Add(component);
            IjgAttribution = attribution;
            _isInitialLoadCompleted = true;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Leaving/cancelling a load is expected; keep the catalog eligible for a later retry.
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load the packaged third-party license catalog.");
            ErrorMessage = AppResources.LicensesLoadFailed;
        } finally {
            IsBusy = false;
        }
    }
}

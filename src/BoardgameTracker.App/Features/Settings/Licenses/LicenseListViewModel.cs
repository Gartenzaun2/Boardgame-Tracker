using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.Licenses;

public partial class LicenseListViewModel(LicenseTextService licenses, ILogger<LicenseListViewModel> logger) : ObservableObject
{
    public ObservableCollection<ThirdPartyLicense> Components { get; } = [];
    private bool _loaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }
    public bool IsIdle => !IsBusy;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => ErrorMessage is not null;
    [ObservableProperty] public partial string IjgAttribution { get; private set; } = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || _loaded)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            IReadOnlyList<ThirdPartyLicense> catalog = await licenses.LoadCatalogAsync(cancellationToken);
            string attribution = catalog.Single(item => item.Id == "independent-jpeg-group").AttributionText
                ?? throw new InvalidDataException("Required IJG attribution is missing.");
            Components.Clear();
            foreach (ThirdPartyLicense component in catalog)
                Components.Add(component);
            IjgAttribution = attribution;
            _loaded = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load the packaged third-party license catalog.");
            ErrorMessage = AppResources.LicensesLoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

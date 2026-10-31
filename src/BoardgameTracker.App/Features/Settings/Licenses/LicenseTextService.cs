using System.Text;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.Settings.Licenses;

/// <summary>Host boundary for offline app-package reads; no network or storage permissions.</summary>
public interface ILicenseAssetReader
{
    Task<string> ReadTextAsync(string assetPath, CancellationToken cancellationToken = default);
}

public sealed class LicenseTextService(ILicenseAssetReader assets)
{
    public async Task<IReadOnlyList<ThirdPartyLicense>> LoadCatalogAsync(CancellationToken cancellationToken = default)
    {
        string json = await assets.ReadTextAsync(ThirdPartyLicenseCatalog.AssetPath, cancellationToken);
        return ThirdPartyLicenseCatalog.Parse(json).Components;
    }

    public async Task<LicenseContent> LoadLicenseAsync(string componentId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ThirdPartyLicense> catalog = await LoadCatalogAsync(cancellationToken);
        ThirdPartyLicense component = catalog.SingleOrDefault(item => item.Id == componentId)
            ?? throw new InvalidDataException("Unknown license component.");
        StringBuilder text = new();
        foreach (string path in component.AllAssetPaths.Distinct(StringComparer.Ordinal))
        {
            string document = await assets.ReadTextAsync(path, cancellationToken);
            if (string.IsNullOrWhiteSpace(document))
                throw new InvalidDataException($"License asset is empty: {path}");
            if (text.Length > 0)
                text.AppendLine().AppendLine().AppendLine(AppResources.LicenseAdditionalNotices).AppendLine();
            text.Append(document);
        }
        return new(component, text.ToString());
    }
}

public sealed record LicenseContent(ThirdPartyLicense Component, string Text);

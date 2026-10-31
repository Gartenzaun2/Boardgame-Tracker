using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoardgameTracker.App.Features.Settings.Licenses;

/// <summary>Offline package manifest. Texts have one canonical asset even when several packages share it.</summary>
public sealed record ThirdPartyLicenseCatalog(IReadOnlyList<ThirdPartyLicense> Components)
{
    public const string AssetPath = "ThirdPartyLicenses/catalog.json";

    public static ThirdPartyLicenseCatalog Parse(string json)
    {
        ThirdPartyLicenseCatalog catalog = JsonSerializer.Deserialize(json, LicenseJsonContext.Default.ThirdPartyLicenseCatalog)
            ?? throw new InvalidDataException("License catalog is empty.");
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (ThirdPartyLicense component in catalog.Components)
        {
            if (string.IsNullOrWhiteSpace(component.Id) || !ids.Add(component.Id)
                || string.IsNullOrWhiteSpace(component.DisplayName) || string.IsNullOrWhiteSpace(component.LicenseDisplayName))
                throw new InvalidDataException("License catalog contains an invalid or duplicate component.");
            foreach (string path in component.AllAssetPaths)
                if (string.IsNullOrWhiteSpace(path) || path.Contains("..", StringComparison.Ordinal)
                    || !(path.StartsWith("ThirdPartyLicenses/texts/", StringComparison.Ordinal)
                         || path.StartsWith("ArkNovaVision/licenses/", StringComparison.Ordinal)))
                    throw new InvalidDataException("License catalog contains an invalid package asset path.");
        }
        return catalog with { Components = Array.AsReadOnly(catalog.Components.ToArray()) };
    }
}

public sealed record ThirdPartyLicense(string Id, string DisplayName, string? Version, string LicenseDisplayName,
    string? SpdxIdentifier, string AssetPath, string? AttributionText, IReadOnlyList<string> AdditionalAssetPaths)
{
    public string DisplayTitle => string.IsNullOrWhiteSpace(Version) ? DisplayName : $"{DisplayName} {Version}";
    public string OverviewText => $"{DisplayTitle}. {LicenseDisplayName}";
    public IEnumerable<string> AllAssetPaths => new[] { AssetPath }.Concat(AdditionalAssetPaths);
}

// Metadata generation keeps catalog loading compatible with trimming and iOS AOT.
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ThirdPartyLicenseCatalog))]
internal partial class LicenseJsonContext : JsonSerializerContext;

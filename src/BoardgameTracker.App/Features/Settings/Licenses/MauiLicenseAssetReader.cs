namespace BoardgameTracker.App.Features.Settings.Licenses;

public sealed class MauiLicenseAssetReader : ILicenseAssetReader
{
    public async Task<string> ReadTextAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        using Stream stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}

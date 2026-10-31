using System.Xml.Linq;
using BoardgameTracker.App.Features.Settings.Licenses;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class LicensePresentationTests
{
    private const string IjgNotice = "This software is based in part on the work of the Independent JPEG Group.";
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CatalogIncludesTheCanonicalNativeComponentsAndLibraryGroups()
    {
        LicenseTextService service = new(new PackagedAssets());
        IReadOnlyList<ThirdPartyLicense> catalog = await service.LoadCatalogAsync(Token);
        foreach (string id in new[] { "opencv", "kleidicv", "android-cpu-features", "carotene", "openjpeg", "libjpeg-turbo", "independent-jpeg-group", "libpng" })
            Assert.Contains(catalog, component => component.Id == id && component.AssetPath.StartsWith("ArkNovaVision/licenses/", StringComparison.Ordinal));
        Assert.Equal("4.13.0", catalog.Single(component => component.Id == "opencv").Version);
        Assert.Contains(catalog, component => component.Id == "CommunityToolkit.Mvvm");
        Assert.Contains(catalog, component => component.Id == "dotnet-maui");
        Assert.Contains(catalog, component => component.Id == "entity-framework-core");
        Assert.DoesNotContain(catalog, component => component.Id.Contains("xunit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(catalog, component => component.Id.Contains("OpenCvSharp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(catalog, component => component.Id == "Microsoft.Extensions.Logging.Debug");
        Assert.Equal(catalog.Count, catalog.Select(component => component.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task EveryCatalogTextLoadsOfflineFromExactlyOnePackagedResource()
    {
        PackagedAssets assets = new();
        LicenseTextService service = new(assets);
        IReadOnlyList<ThirdPartyLicense> catalog = await service.LoadCatalogAsync(Token);
        string[] paths = catalog.SelectMany(component => component.AllAssetPaths).Distinct(StringComparer.Ordinal).ToArray();
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (string path in paths)
            Assert.Equal(1, typeof(LicensePresentationTests).Assembly.GetManifestResourceNames().Count(name => name == "LicenseAssets." + path));
        foreach (string name in typeof(LicensePresentationTests).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("LicenseAssets.", StringComparison.Ordinal) && name.EndsWith(".txt", StringComparison.Ordinal)))
            Assert.Contains(name["LicenseAssets.".Length..], paths);
        foreach (ThirdPartyLicense component in catalog)
        {
            LicenseContent content = await service.LoadLicenseAsync(component.Id, Token);
            Assert.Equal(component.Id, content.Component.Id);
            Assert.False(string.IsNullOrWhiteSpace(content.Text));
        }
        // This reader only accepts assembly resources; no network or device file system exists in the test.
        Assert.All(assets.Requested, path => Assert.True(path.StartsWith("ThirdPartyLicenses/", StringComparison.Ordinal)
            || path.StartsWith("ArkNovaVision/licenses/", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task IjgAttributionIsVisibleInOverviewAndJpegDetailsWithTheFullIjgText()
    {
        LicenseTextService service = new(new PackagedAssets());
        LicenseListViewModel list = new(service, NullLogger<LicenseListViewModel>.Instance);
        await list.LoadAsync(Token);
        Assert.Equal(IjgNotice, list.IjgAttribution);
        LicenseDetailViewModel detail = new(service, NullLogger<LicenseDetailViewModel>.Instance);
        await detail.LoadAsync("libjpeg-turbo", Token);
        Assert.Equal(IjgNotice, detail.AttributionText);
        Assert.Contains("Independent JPEG Group", detail.LicenseText);
        Assert.Contains("LEGAL ISSUES", detail.LicenseText);
        Assert.False(detail.HasError);
    }

    [Fact]
    public async Task MissingTextIsLoggedAndShownAsAFriendlyRetryableFailure()
    {
        PackagedAssets assets = new() { Missing = "ArkNovaVision/licenses/OpenCV.txt" };
        CaptureLogger<LicenseDetailViewModel> logger = new();
        LicenseDetailViewModel detail = new(new(assets), logger);
        await detail.LoadAsync("opencv", Token);
        Assert.True(detail.HasError);
        Assert.Equal(AppResources.LicensesLoadFailed, detail.ErrorMessage);
        Assert.Empty(detail.LicenseText);
        Assert.True(detail.IsIdle);
        Assert.IsType<FileNotFoundException>(Assert.Single(logger.Errors));
        Assert.DoesNotContain("OpenCV.txt", detail.ErrorMessage);
        assets.Missing = null;
        await detail.LoadAsync("opencv", Token);
        Assert.False(detail.HasError);
        Assert.Contains("Apache License", detail.LicenseText);
        Assert.Equal("OpenCV", detail.DisplayName);
        Assert.Equal("Version 4.13.0", detail.VersionText);
        Assert.True(detail.HasSpdx);
    }

    [Fact]
    public async Task MissingCatalogDoesNotCrashAndCanBeRetried()
    {
        PackagedAssets assets = new() { Missing = ThirdPartyLicenseCatalog.AssetPath };
        CaptureLogger<LicenseListViewModel> logger = new();
        LicenseListViewModel list = new(new(assets), logger);
        await list.LoadAsync(Token);
        Assert.True(list.HasError);
        Assert.Empty(list.Components);
        Assert.Single(logger.Errors);
        assets.Missing = null;
        await list.LoadAsync(Token);
        Assert.False(list.HasError);
        int count = list.Components.Count;
        int reads = assets.Requested.Count;
        await list.LoadAsync(Token);
        Assert.Equal(count, list.Components.Count);
        Assert.Equal(reads, assets.Requested.Count);
    }

    [Fact]
    public async Task MissingAdditionalNoticeDoesNotExposePartialLicenseContent()
    {
        PackagedAssets assets = new() { Missing = "ArkNovaVision/licenses/IndependentJPEGGroup.txt" };
        CaptureLogger<LicenseDetailViewModel> logger = new();
        LicenseDetailViewModel detail = new(new(assets), logger);
        await detail.LoadAsync("libjpeg-turbo", Token);
        Assert.True(detail.HasError);
        Assert.Empty(detail.LicenseText);
        Assert.Single(logger.Errors);
    }

    [Fact]
    public async Task UnknownNavigationIdDoesNotShowAStaleLicense()
    {
        LicenseDetailViewModel detail = new(new(new PackagedAssets()), NullLogger<LicenseDetailViewModel>.Instance);
        await detail.LoadAsync("opencv", Token);
        await detail.LoadAsync("missing-component", Token);
        Assert.True(detail.HasError);
        Assert.Empty(detail.LicenseText);
        Assert.Empty(detail.AttributionText);
        Assert.Equal(AppResources.OpenSourceLicenses, detail.DisplayName);
        await detail.LoadAsync("opencv", Token);
        Assert.False(detail.HasError);
        Assert.Equal("OpenCV", detail.DisplayName);
    }

    [Fact]
    public async Task BothPagesUseTypedBindingsExistingThemeTokensAndSharedNavigationKey()
    {
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2009/xaml";
        foreach (string name in new[] { "LicenseListPage.xaml", "LicenseDetailPage.xaml" })
        {
            string text = await ReadResourceAsync("LicenseNavigation." + name);
            XDocument document = XDocument.Parse(text);
            Assert.NotNull(document.Root!.Attribute(xaml + "DataType"));
            Assert.DoesNotContain("WebView", text);
            Assert.Contains("{StaticResource HeaderDivider}", text);
            Assert.Contains("{Binding IsIdle}", text);
            Assert.Contains("{DynamicResource BusyOverlay}", text);
            Assert.Contains("icon_back.png", text);
        }
        string shell = await ReadResourceAsync("LicenseNavigation.AppShell.xaml.cs");
        Assert.Contains("Routing.RegisterRoute(nameof(LicenseListPage)", shell);
        Assert.Contains("Routing.RegisterRoute(nameof(LicenseDetailPage)", shell);
        string navigation = await ReadResourceAsync("LicenseNavigation.NavigationParameters.cs");
        Assert.Contains("LicenseComponentId = nameof(LicenseComponentId)", navigation);
        string sender = await ReadFeatureSourceAsync("LicenseListPage.xaml.cs");
        string receiver = await ReadFeatureSourceAsync("LicenseDetailPage.xaml.cs");
        Assert.Contains("[NavigationParameters.LicenseComponentId] = id", sender);
        Assert.Contains("TryGetValue(NavigationParameters.LicenseComponentId", receiver);
    }

    [Fact]
    public void CatalogRejectsDuplicateIdentityAndEscapingAssetPaths()
    {
        const string valid = "{\"id\":\"one\",\"displayName\":\"One\",\"licenseDisplayName\":\"MIT\",\"assetPath\":\"ThirdPartyLicenses/texts/one.txt\",\"additionalAssetPaths\":[]}";
        Assert.Throws<InvalidDataException>(() => ThirdPartyLicenseCatalog.Parse("{\"components\":[" + valid + "," + valid + "]}"));
        Assert.Throws<InvalidDataException>(() => ThirdPartyLicenseCatalog.Parse("{\"components\":[" + valid.Replace("texts/one", "../one", StringComparison.Ordinal) + "]}"));
    }

    private static async Task<string> ReadResourceAsync(string name)
    {
        using Stream stream = typeof(LicensePresentationTests).Assembly.GetManifestResourceStream(name)
            ?? throw new FileNotFoundException("Missing packaged test resource.", name);
        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync(Token);
    }

    private static Task<string> ReadFeatureSourceAsync(string fileName)
    {
        string resource = Assert.Single(typeof(LicensePresentationTests).Assembly.GetManifestResourceNames(),
            name => name.StartsWith("PresentationBoundary.", StringComparison.Ordinal) && name.EndsWith(fileName, StringComparison.Ordinal));
        return ReadResourceAsync(resource);
    }

    private sealed class PackagedAssets : ILicenseAssetReader
    {
        public string? Missing { get; set; }
        public List<string> Requested { get; } = [];
        public Task<string> ReadTextAsync(string assetPath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requested.Add(assetPath);
            if (assetPath == Missing)
                throw new FileNotFoundException("Injected missing package asset.", assetPath);
            return ReadResourceAsync("LicenseAssets." + assetPath);
        }
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<Exception?> Errors { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
                Errors.Add(exception);
        }
    }
}

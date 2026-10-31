using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ArkNova.Content;

/// <summary>Resolves physical game names from user content and the shipped catalog.</summary>
public sealed class ArkNovaContent(GameContentLanguagePreference preference, ArkNovaContentCatalog catalog)
{
    public GameContentLanguages Languages => catalog.Languages;

    public string ResolveName(string resourceType, string objectId, string? userName = null) =>
        catalog.ResolveName(resourceType, objectId, userName, preference.Get(Languages),
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    public async Task<IReadOnlyList<ArkNovaSpecialCardData>> ReadCardCatalogAsync(
        BoardgameTrackerDbContext db, bool includeArchived, CancellationToken cancellationToken)
    {
        var cards = await db.ArkNovaSpecialCards.AsNoTracking().Where(card => includeArchived || !card.IsArchived)
            .Select(card => new { card.Id, card.Symbol1, card.Symbol2, card.Symbol3, card.IsArchived })
            .ToArrayAsync(cancellationToken);
        Dictionary<string, string> names = await ReadUserNamesAsync(db, ArkNovaUserContent.SpecialCard, cancellationToken);
        return cards.Select(card => new ArkNovaSpecialCardData(card.Id,
                ResolveName(ArkNovaUserContent.SpecialCard, card.Id.ToString(), names.GetValueOrDefault(card.Id.ToString())),
                card.Symbol1, card.Symbol2, card.Symbol3, card.IsArchived))
            .OrderBy(card => card.Name, StringComparer.CurrentCulture).ThenBy(card => card.Id).ToArray();
    }

    public async Task<IReadOnlyDictionary<int, string>> ReadZooPlanNamesAsync(
        BoardgameTrackerDbContext db, CancellationToken cancellationToken)
    {
        Dictionary<string, string> names = await ReadUserNamesAsync(db, ArkNovaUserContent.ZooPlan, cancellationToken);
        return Enumerable.Range(1, 14).ToDictionary(number => number, number =>
            ResolveName(ArkNovaUserContent.ZooPlan, number.ToString(CultureInfo.InvariantCulture),
                names.GetValueOrDefault(number.ToString(CultureInfo.InvariantCulture))));
    }

    private static Task<Dictionary<string, string>> ReadUserNamesAsync(
        BoardgameTrackerDbContext db, string resourceType, CancellationToken cancellationToken) =>
        db.ArkNovaUserContent.AsNoTracking().Where(content => content.ResourceType == resourceType)
            .ToDictionaryAsync(content => content.ObjectId, content => content.Name, cancellationToken);
}

// A small Ark Nova catalog, not a registry or engine for hypothetical future modules.
public sealed class ArkNovaContentCatalog
{
    private readonly Dictionary<(string Type, string Id), Dictionary<string, string>> _names = [];
    public GameContentLanguages Languages { get; }

    public static ArkNovaContentCatalog LoadBuiltIn()
    {
        using Stream stream = typeof(ArkNovaContentCatalog).Assembly.GetManifestResourceStream(
            "BoardgameTracker.Application.Games.ArkNova.Content.content.json")
            ?? throw new InvalidDataException("The Ark Nova content file is missing.");
        return new(stream);
    }

    public ArkNovaContentCatalog(Stream stream)
    {
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;
        string defaultLanguage = root.GetProperty("defaultLanguage").GetString()!;
        string[] supportedLanguages = root.GetProperty("supportedLanguages").EnumerateArray()
            .Select(language => language.GetString()!).ToArray();
        if (string.IsNullOrWhiteSpace(defaultLanguage) || !supportedLanguages.Contains(defaultLanguage)
            || supportedLanguages.Any(language => language.Length != 2 || language.Any(character => character is < 'a' or > 'z'))
            || supportedLanguages.Distinct(StringComparer.Ordinal).Count() != supportedLanguages.Length)
            throw new InvalidDataException("Invalid Ark Nova content languages.");
        Languages = new(ArkNovaGame.Key, defaultLanguage, Array.AsReadOnly(supportedLanguages));
        ReadNames(root.GetProperty("specialCards"), ArkNovaUserContent.SpecialCard);
        ReadNames(root.GetProperty("zooPlans"), ArkNovaUserContent.ZooPlan);
    }

    private void ReadNames(JsonElement objects, string type)
    {
        foreach (JsonProperty item in objects.EnumerateObject())
        {
            Dictionary<string, string> translations = [];
            foreach (JsonProperty name in item.Value.GetProperty("name").EnumerateObject())
            {
                if (!Languages.SupportedLanguages.Contains(name.Name) || string.IsNullOrWhiteSpace(name.Value.GetString()))
                    throw new InvalidDataException("Invalid Ark Nova content name.");
                translations.Add(name.Name, name.Value.GetString()!);
            }
            _names.Add((type, item.Name), translations);
        }
    }

    public string ResolveName(string resourceType, string objectId, string? userName, string selectedLanguage, string appLanguage)
    {
        if (userName is not null)
            return userName;
        if (_names.TryGetValue((resourceType, objectId), out Dictionary<string, string>? translations))
        {
            foreach (string language in new[] { selectedLanguage, appLanguage, Languages.DefaultContentLanguage }.Distinct(StringComparer.Ordinal))
            {
                if (Languages.SupportedLanguages.Contains(language) && translations.TryGetValue(language, out string? name))
                    return name;
            }
        }
        // Stable diagnostics make missing shipped resources visible during development.
        string missing = $"[missing:ark-nova/{resourceType}/{objectId}/name]";
        Debug.WriteLine(missing);
        return missing;
    }
}

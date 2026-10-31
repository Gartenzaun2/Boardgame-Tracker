using System.Globalization;
using System.Text;
using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.Content;

public sealed class ArkNovaContentTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly string CardId = ArkNovaSpecialCardDefaults.GoldenEagle.ToString();
    private static ArkNovaContentCatalog Catalog() => new(new MemoryStream(Encoding.UTF8.GetBytes("""
        {"defaultLanguage":"de","supportedLanguages":["de","en","fr"],
         "specialCards":{"CARD_ID":{"name":{"de":"German test name","en":"English test name"}}},
         "zooPlans":{"1":{"name":{"de":"German test plan","en":"English test plan"}}}}
        """.Replace("CARD_ID", CardId))));

    [Theory]
    [InlineData("Custom", "en", "de", "Custom")]
    [InlineData(null, "en", "de", "English test name")]
    [InlineData(null, "fr", "en", "English test name")]
    [InlineData(null, "fr", "it", "German test name")]
    [InlineData(null, "it", "it", "German test name")]
    public void NamesFollowTheSingleFallbackChain(string? user, string selected, string app, string expected)
    {
        Assert.Equal(expected, Catalog().ResolveName(ArkNovaUserContent.SpecialCard, CardId, user, selected, app));
    }

    [Fact]
    public void MissingContentNeverUsesAnArbitraryTranslation()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("""
            {"defaultLanguage":"de","supportedLanguages":["de","en","fr"],
             "specialCards":{},"zooPlans":{"1":{"name":{"en":"Only test English"}}}}
            """));
        ArkNovaContentCatalog catalog = new(stream);
        Assert.Equal("[missing:ark-nova/zoo-plan/1/name]", catalog.ResolveName("zoo-plan", "1", null, "fr", "it"));
        Assert.Equal("[missing:ark-nova/special-card/unknown/name]", catalog.ResolveName("special-card", "unknown", null, "en", "de"));
    }

    [Fact]
    public void ShippedContentContainsExactlyKnownIdsNumbersAndGermanDefaults()
    {
        ArkNovaContentCatalog catalog = ArkNovaContentCatalog.LoadBuiltIn();
        Assert.Equal("de", catalog.Languages.DefaultContentLanguage);
        Assert.Equal(new[] { "de" }, catalog.Languages.SupportedLanguages);
        using Stream stream = typeof(ArkNovaContentCatalog).Assembly.GetManifestResourceStream(
            "BoardgameTracker.Application.Games.ArkNova.Content.content.json")!;
        using System.Text.Json.JsonDocument json = System.Text.Json.JsonDocument.Parse(stream);
        Assert.Equal(ArkNovaSpecialCardDefaults.Create().Select(card => card.Id).Order(),
            json.RootElement.GetProperty("specialCards").EnumerateObject().Select(card => Guid.Parse(card.Name)).Order());
        Assert.Equal(Enumerable.Range(1, 14), json.RootElement.GetProperty("zooPlans").EnumerateObject().Select(plan => int.Parse(plan.Name)).Order());
        foreach (ArkNovaSpecialCard card in ArkNovaSpecialCardDefaults.Create())
            Assert.DoesNotContain("[missing:", catalog.ResolveName("special-card", card.Id.ToString(), null, "de", "de"));
        foreach (int number in Enumerable.Range(1, 14))
            Assert.DoesNotContain("[missing:", catalog.ResolveName("zoo-plan", number.ToString(), null, "de", "de"));
        Assert.Equal("Steinadler", catalog.ResolveName("special-card", CardId, null, "de", "de"));
        Assert.Equal("Aussichtsplattform", catalog.ResolveName("zoo-plan", "1", null, "de", "de"));
    }

    [Fact]
    public void InvalidDefaultLanguageIsRejected()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("""
            {"defaultLanguage":"de","supportedLanguages":["en"],"specialCards":{},"zooPlans":{}}
            """));
        Assert.Throws<InvalidDataException>(() => new ArkNovaContentCatalog(stream));
    }

    [Fact]
    public async Task SymbolOnlyEditDoesNotCreateUserContent_AndLanguageSwitchDoesNotChangeDomain()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        TestGameContentLanguageStore store = new();
        GameContentLanguagePreference preference = new(store);
        ArkNovaContent content = new(preference, Catalog());
        preference.Save(content.Languages, "en");
        ArkNovaSpecialCardUseCase cards = new(database, content);
        Guid id = Guid.Parse(CardId);
        await cards.UpdateAsync(id, "English test name", ArkNovaCardSymbol.Science, null, null, Token);
        await cards.SetArchivedAsync(id, true, Token);
        preference.Save(content.Languages, "de");
        ArkNovaSpecialCardData german = (await cards.ListAsync(true, Token)).Single(card => card.Id == id);
        Assert.Equal("German test name", german.Name);
        Assert.Equal(ArkNovaCardSymbol.Science, german.Symbol1);
        Assert.True(german.IsArchived);
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            Assert.Empty(await db.ArkNovaUserContent.ToArrayAsync(Token));
            Assert.Null(db.Model.FindEntityType(typeof(ArkNovaSpecialCard))!.FindProperty("Name"));
        }
        await cards.UpdateAsync(id, "  My eagle  ", german.Symbol1, null, null, Token);
        preference.Save(content.Languages, "en");
        Assert.Equal("My eagle", (await cards.ListAsync(true, Token)).Single(card => card.Id == id).Name);
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            ArkNovaUserContent user = await db.ArkNovaUserContent.SingleAsync(Token);
            Assert.Equal(CardId, user.ObjectId);
            Assert.Equal("My eagle", user.Name);
            db.Remove(user); // Removing only the user name restores the content chain, not symbols/archive state.
            await db.SaveChangesAsync(Token);
        }
        ArkNovaSpecialCardData restored = (await cards.ListAsync(true, Token)).Single(card => card.Id == id);
        Assert.Equal("English test name", restored.Name);
        Assert.Equal(ArkNovaCardSymbol.Science, restored.Symbol1);
        Assert.True(restored.IsArchived);
    }

    [Fact]
    public async Task CustomCardsAndZooNamesUsePersistentLanguageIndependentUserContent()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameContentLanguagePreference preference = new(new TestGameContentLanguageStore());
        ArkNovaContent content = new(preference, Catalog());
        ArkNovaSpecialCardUseCase cards = new(database, content);
        Guid id = await cards.CreateAsync("  Custom card  ", null, null, null, Token);
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            Assert.Equal("Custom card", (await db.ArkNovaUserContent.SingleAsync(Token)).Name);
            db.ArkNovaUserContent.Add(new("zoo-plan", "1", "Custom plan"));
            await db.SaveChangesAsync(Token);
        }
        preference.Save(content.Languages, "en");
        Assert.Equal("Custom card", (await cards.ListAsync(cancellationToken: Token)).Single(card => card.Id == id).Name);
        await using BoardgameTrackerDbContext fresh = database.CreateDbContext();
        Assert.Equal("Custom plan", (await content.ReadZooPlanNamesAsync(fresh, Token))[1]);
        Assert.Equal(1, (await fresh.ArkNovaZooPlans.SingleAsync(plan => plan.Number == 1, Token)).Number);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void InvalidUserNamesAreRejected(string? name) => Assert.ThrowsAny<ArgumentException>(() =>
        new ArkNovaUserContent("special-card", CardId, name!));

    [Fact]
    public void UserNameLengthLimitIsPreserved()
    {
        Assert.Equal(120, new ArkNovaUserContent("special-card", CardId, new string('x', 120)).Name.Length);
        Assert.Throws<ArgumentException>(() => new ArkNovaUserContent("special-card", CardId, new string('x', 121)));
    }
}

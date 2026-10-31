using System.Globalization;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Application.Tests.Persistence;

namespace BoardgameTracker.Application.Tests.Settings;

public sealed class GameContentLanguageTests
{
    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("de-DE", "de")]
    [InlineData("it-IT", "de")]
    public void InitialSelectionUsesSupportedAppLanguageOrDefaultAndIsThenStored(string appCulture, string expected)
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(appCulture);
            TestGameContentLanguageStore store = new();
            GameContentLanguagePreference preference = new(store);
            GameContentLanguages game = new("test-game", "de", ["de", "en"]);
            Assert.Equal(expected, preference.Get(game));
            Assert.Equal(expected, store.Get(game.GameKey));
            Assert.Equal(appCulture, CultureInfo.CurrentUICulture.Name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(expected == "de" ? "en" : "de");
            Assert.Equal(expected, new GameContentLanguagePreference(store).Get(game));
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void GamesAreIndependentAndSavingNeverChangesAppCulture()
    {
        TestGameContentLanguageStore store = new();
        GameContentLanguagePreference preference = new(store);
        GameContentLanguages first = new("first", "de", ["de", "en"]);
        GameContentLanguages second = new("second", "de", ["de", "en"]);
        CultureInfo before = CultureInfo.CurrentUICulture;
        preference.Save(first, "en");
        preference.Save(second, "de");
        Assert.Equal("en", preference.Get(first));
        Assert.Equal("de", preference.Get(second));
        Assert.Same(before, CultureInfo.CurrentUICulture);
        Assert.Throws<ArgumentException>(() => preference.Save(first, "user"));
        Assert.Throws<ArgumentException>(() => preference.Save(first, "it"));
        store.Save(first.GameKey, "invalid");
        Assert.Contains(preference.Get(first), first.SupportedLanguages);
    }
}

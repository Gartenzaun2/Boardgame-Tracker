using System.Globalization;

namespace BoardgameTracker.Application.Settings;

// This port is a native Preferences boundary, not a database abstraction.
public interface IGameContentLanguageStore
{
    string? Get(string gameKey);
    void Save(string gameKey, string language);
}

public sealed record GameContentLanguages(string GameKey, string DefaultContentLanguage, IReadOnlyList<string> SupportedLanguages);

public sealed class GameContentLanguagePreference(IGameContentLanguageStore store)
{
    public string Get(GameContentLanguages game)
    {
        string? stored = store.Get(game.GameKey);
        if (stored is not null && game.SupportedLanguages.Contains(stored, StringComparer.Ordinal))
            return stored;
        string appLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string initial = game.SupportedLanguages.Contains(appLanguage, StringComparer.Ordinal)
            ? appLanguage : game.DefaultContentLanguage;
        Save(game, initial);
        return initial;
    }

    public void Save(GameContentLanguages game, string language)
    {
        if (!game.SupportedLanguages.Contains(language, StringComparer.Ordinal))
            throw new ArgumentException("Unsupported game content language.", nameof(language));
        store.Save(game.GameKey, language);
    }
}

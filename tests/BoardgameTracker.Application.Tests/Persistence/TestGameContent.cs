using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Application.Settings;

namespace BoardgameTracker.Application.Tests.Persistence;

internal sealed class TestGameContentLanguageStore : IGameContentLanguageStore
{
    private readonly Dictionary<string, string> _languages = [];
    public string? Get(string gameKey) => _languages.GetValueOrDefault(gameKey);
    public void Save(string gameKey, string language) => _languages[gameKey] = language;
}

internal static class TestGameContent
{
    public static ArkNovaContent Create() => new(new(new TestGameContentLanguageStore()), ArkNovaContentCatalog.LoadBuiltIn());
}

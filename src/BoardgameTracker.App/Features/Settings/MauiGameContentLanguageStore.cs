using BoardgameTracker.Application.Settings;
using Microsoft.Maui.Storage;

namespace BoardgameTracker.App.Features.Settings;

public sealed class MauiGameContentLanguageStore : IGameContentLanguageStore
{
    private static string Key(string gameKey) => $"Game.{gameKey}.ContentLanguage";
    public string? Get(string gameKey) => Preferences.Default.ContainsKey(Key(gameKey))
        ? Preferences.Default.Get(Key(gameKey), string.Empty) : null;
    public void Save(string gameKey, string language) => Preferences.Default.Set(Key(gameKey), language);
}

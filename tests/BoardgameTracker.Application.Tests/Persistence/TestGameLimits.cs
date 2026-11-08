global using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Tests.Persistence;

internal static class TestGameLimits
{
    public static GamePlayerLimitCatalog Catalog { get; } = new([
        ArkNovaGame.PlayerLimit,
        .. new[] { "other", "other-game", "test-game", "test-first", "test-second", "second-game", "new-game", "another-game" }
            .Select(key => new GamePlayerLimit(key, 8))]);
}

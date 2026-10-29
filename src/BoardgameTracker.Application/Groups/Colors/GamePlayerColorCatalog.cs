using BoardgameTracker.Core.Games;

namespace BoardgameTracker.Application.Groups.Colors;

public sealed class GamePlayerColorCatalog(IEnumerable<GamePlayerColorRules> rules)
{
    private readonly IReadOnlyDictionary<string, GamePlayerColorRules> _rules = rules.ToDictionary(rule => rule.GameKey);
    public GamePlayerColorRules Get(string? gameKey) => gameKey is not null && _rules.TryGetValue(gameKey, out var rules)
        ? rules : new(gameKey ?? string.Empty, []);
}

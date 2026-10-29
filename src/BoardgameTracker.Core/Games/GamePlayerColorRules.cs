using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Games;

/// <summary>A game's allowed subset of the shared semantic colors. Empty means no player colors.</summary>
public sealed class GamePlayerColorRules(string gameKey, IReadOnlyList<PlayerColor> allowedColors)
{
    public string GameKey { get; } = gameKey;
    public IReadOnlyList<PlayerColor> AllowedColors { get; } = Array.AsReadOnly(allowedColors.ToArray());
    public bool Allows(PlayerColor color) => AllowedColors.Contains(color);

    public void Validate(IEnumerable<PlayerColor?> selection)
    {
        PlayerColor[] assigned = [.. selection.OfType<PlayerColor>()];
        if (assigned.Any(color => !Allows(color)))
            throw new ArgumentOutOfRangeException(nameof(selection), "A selected player color is not allowed for this game.");
        if (assigned.Distinct().Count() != assigned.Length)
            throw new InvalidOperationException("Group default colors must be unique within the game.");
    }
}

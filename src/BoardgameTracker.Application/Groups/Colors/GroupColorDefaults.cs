using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Groups.Colors;

public sealed record GroupColorDefaults(string GameKey, IReadOnlyDictionary<Guid, PlayerColor?> Colors);

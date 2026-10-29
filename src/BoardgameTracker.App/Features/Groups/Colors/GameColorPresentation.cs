namespace BoardgameTracker.App.Features.Groups.Colors;

// The composition root supplies localized game names; group views know no game modules.
public sealed record GameColorPresentation(string GameKey, Func<string> Name);

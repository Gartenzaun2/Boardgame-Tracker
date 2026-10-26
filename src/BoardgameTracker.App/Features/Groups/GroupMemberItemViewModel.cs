namespace BoardgameTracker.App.Features.Groups;

public sealed record GroupMemberItemViewModel(
    Guid? PersonId,
    string Name,
    bool IsGuest);

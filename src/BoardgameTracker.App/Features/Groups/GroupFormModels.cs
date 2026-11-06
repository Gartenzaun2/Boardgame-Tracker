using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.Groups;

public sealed partial class GroupMemberDraft(Guid? playerId, string name, bool isGuest) : ObservableObject
{
    public Guid? PlayerId { get; } = playerId;
    public string Name { get; } = name;
    public bool IsGuest { get; } = isGuest;
    [ObservableProperty] public partial int Position { get; set; }
    [ObservableProperty] public partial bool CanMoveUp { get; set; }
    [ObservableProperty] public partial bool CanMoveDown { get; set; }

    public static void UpdatePositions(IReadOnlyList<GroupMemberDraft> players)
    {
        for (int index = 0; index < players.Count; index++)
        {
            players[index].Position = index + 1;
            players[index].CanMoveUp = index > 0;
            players[index].CanMoveDown = index < players.Count - 1;
        }
    }
}

public sealed class GroupGameSelection(Guid id, string name, bool isSelected, bool canRemove = true, bool isVisible = true) : ObservableObject
{
    private bool _isSelected = isSelected;
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public bool IsVisible { get; } = isVisible;
    public bool CanChange { get; } = !isSelected || canRemove;
    public bool IsLocked => !CanChange;
    public string LockReason => AppResources.GroupGameLocked;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (CanChange)
                SetProperty(ref _isSelected, value);
        }
    }
}

public sealed record DuplicateGroupRow(Guid Id, string Name, string Members, bool IsArchived)
{
    public string ActionText => IsArchived ? AppResources.ReactivateGroup : AppResources.ReuseGroup;
}

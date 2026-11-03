using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.Groups;

public sealed record GroupMemberDraft(Guid? PlayerId, string Name, bool IsGuest);

public sealed class GroupGameSelection(Guid id, string name, bool isSelected, bool canRemove = true) : ObservableObject
{
    private bool _isSelected = isSelected;
    public Guid Id { get; } = id;
    public string Name { get; } = name;
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

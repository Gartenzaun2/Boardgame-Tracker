using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Settings.Navigation;

namespace BoardgameTracker.Application.Tests.Presentation;

internal sealed class EditorNavigationFake : INavigationService {
    public List<(string Route, AppMainSection? Section)> Calls { get; } = [];
    public bool FailNavigation { get; set; }
    public Exception NavigationFailure { get; } = new InvalidOperationException("Navigation unavailable");
    public Task NavigationGate { get; set; } = Task.CompletedTask;
    public TaskCompletionSource NavigationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Action? OnNavigating { get; set; }

    public Func<GroupSaveOutcome, Task>? OnGroupCreationCompleted { get; set; }
    public Func<Task>? OnGroupDuplicatesRequested { get; set; }
    public List<GroupSaveOutcome> GroupOutcomes { get; } = [];
    public Task ShowGroupDuplicatesAsync(CreateGroupViewModel draft)
        => OnGroupDuplicatesRequested?.Invoke() ?? Task.CompletedTask;
    public async Task CompleteGroupCreationAsync(CreateGroupViewModel draft, GroupSaveOutcome outcome) {
        GroupOutcomes.Add(outcome);
        if (OnGroupCreationCompleted is not null)
            await OnGroupCreationCompleted(outcome);
        else
            await NavigateAsync("group-completed", null);
    }

    public Task OpenGroupCreationAsync(string gameKey) => NavigateAsync("create-group:" + gameKey, null);

    public Task GoBackAsync() => NavigateAsync("..", null);

    public Task NavigateToMainSectionAsync(AppMainSection section) => NavigateAsync("main", section);

    private async Task NavigateAsync(string route, AppMainSection? section) {
        Calls.Add((route, section));
        OnNavigating?.Invoke();
        NavigationStarted.TrySetResult();
        await NavigationGate;
        if (FailNavigation)
            throw NavigationFailure;
    }
}

using BoardgameTracker.App;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.GameSessions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class GroupCreationNavigationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ManagementCompletion_ReturnsPastExactlyTheCompletedFormPages(bool fromDuplicates) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        AppShell shell = MainSectionNavigationTests.CreateShell(new(), db);
        Shell.Current = shell;
        await shell.NavigateToMainSectionAsync(AppMainSection.Participants);
        ShellNavigationService navigation = new([]);
        CreateGroupViewModel draft = Draft(db, navigation);
        var stack = shell.CurrentItem.CurrentItem.Navigation.NavigationStackPages;
        Page caller = stack[0];
        stack.Add(new CreateGroupPage { ViewModel = draft });
        if (fromDuplicates)
            stack.Add(new DuplicateGroupPage { ViewModel = draft });
        await navigation.CompleteGroupCreationAsync(draft, new(db.Group.Id, null, fromDuplicates));
        Assert.Same(caller, Assert.Single(stack));
        Assert.Equal(fromDuplicates ? "../.." : "..", shell.Calls[^1].Route);
        Assert.False(shell.Animations[^1]);
    }

    [Fact]
    public async Task PlayCompletion_OnlyRemovesThisDraftAfterTheHandlerOpensPreparation() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        AppShell shell = MainSectionNavigationTests.CreateShell(new(), db);
        Shell.Current = shell;
        PreparationHandler handler = new(shell);
        ShellNavigationService navigation = new([handler]);
        CreateGroupViewModel draft = Draft(db, navigation);
        CreateGroupViewModel otherDraft = Draft(db, navigation);
        var stack = shell.CurrentItem.CurrentItem.Navigation.NavigationStackPages;
        CreateGroupPage creation = new() { ViewModel = draft };
        DuplicateGroupPage duplicates = new() { ViewModel = draft };
        CreateGroupPage unrelated = new() { ViewModel = otherDraft };
        stack.AddRange([unrelated, creation, duplicates]);
        GroupSaveOutcome outcome = new(db.Group.Id, "ark-nova", true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => navigation.CompleteGroupCreationAsync(draft, outcome));
        Assert.Contains(creation, stack);
        Assert.Contains(duplicates, stack);
        Assert.Contains(unrelated, stack);
        handler.Fail = false;
        await navigation.CompleteGroupCreationAsync(draft, outcome);
        Assert.DoesNotContain(creation, stack);
        Assert.DoesNotContain(duplicates, stack);
        Assert.Contains(unrelated, stack);
        Assert.Same(handler.Preparation, stack[^1]);
        Assert.Equal(new[] { ("ark-nova", db.Group.Id), ("ark-nova", db.Group.Id) }, handler.Attempts);
    }

    [Fact]
    public async Task OpeningDraftAndDuplicates_ForwardsTheExactContextWithoutRoutePassThrough() {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        AppShell shell = MainSectionNavigationTests.CreateShell(new(), db);
        Shell.Current = shell;
        ShellNavigationService navigation = new([]);
        CreateGroupViewModel draft = Draft(db, navigation);
        await navigation.OpenGroupCreationAsync("selected-game");
        Assert.Equal(nameof(CreateGroupPage), shell.Calls[^1].Route);
        Assert.Equal("selected-game", shell.Calls[^1].Parameters[NavigationParameters.GameKey]);
        await navigation.ShowGroupDuplicatesAsync(draft);
        Assert.Equal(nameof(DuplicateGroupPage), shell.Calls[^1].Route);
        Assert.Same(draft, shell.Calls[^1].Parameters[NavigationParameters.GroupDraft]);
        Assert.All(shell.Animations, animation => Assert.False(animation));
    }

    private static CreateGroupViewModel Draft(SliceDatabase db, INavigationService navigation)
        => new(new(db), new(db), new(db), new(db, new TestGameEnabledStore()), navigation, NullLogger<CreateGroupViewModel>.Instance);

    private sealed class PreparationHandler(AppShell shell) : IGameSessionNavigationHandler {
        public bool Fail { get; set; } = true;
        public Page Preparation { get; } = new();
        public List<(string Key, Guid GroupId)> Attempts { get; } = [];
        public bool CanHandle(string gameKey) => gameKey == "ark-nova";
        public string DisplayName => "Game";
        public string BadgeText => "G";
        public Task OpenGameSessionAsync(Guid gameSessionId) => Task.CompletedTask;
        public Task OpenGroupAsync(string gameKey, Guid groupId, Guid? openGameSessionId, GameSessionStatus? status) {
            Attempts.Add((gameKey, groupId));
            Assert.Null(openGameSessionId);
            Assert.Null(status);
            if (Fail)
                throw new InvalidOperationException("Preparation unavailable");
            shell.CurrentItem.CurrentItem.Navigation.NavigationStackPages.Add(Preparation);
            return Task.CompletedTask;
        }
    }
}

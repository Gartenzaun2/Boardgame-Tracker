using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.People;

namespace BoardgameTracker.Application.Tests.Groups.CreateGroup;

public sealed class CreateGroupUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesGroupWithExistingAndNewPeopleInRequestedOrder()
    {
        Guid existingPersonId = Guid.NewGuid();
        FakeCreateGroupDataAccess dataAccess = new(new HashSet<Guid> { existingPersonId });
        CreateGroupUseCase useCase = new(dataAccess);

        Guid groupId = await useCase.ExecuteAsync(new CreateGroupCommand(
            "Freitagabend",
            false,
            [
                new CreateGroupMemberInput(existingPersonId, "Anna", false),
                new CreateGroupMemberInput(null, "Ben", true)
            ]));

        Assert.NotEqual(Guid.Empty, groupId);
        Assert.NotNull(dataAccess.SavedGroup);
        Assert.Single(dataAccess.SavedNewPeople);
        Assert.True(dataAccess.SavedNewPeople.Single().IsGuest);

        Group savedGroup = dataAccess.SavedGroup!;
        Assert.Collection(
            savedGroup.Members.OrderBy(member => member.DefaultOrder),
            first => Assert.Equal(existingPersonId, first.PersonId),
            second => Assert.Equal(dataAccess.SavedNewPeople.Single().Id, second.PersonId));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInactiveOrMissingExistingPerson()
    {
        Guid personId = Guid.NewGuid();
        FakeCreateGroupDataAccess dataAccess = new(new HashSet<Guid>());
        CreateGroupUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new CreateGroupCommand(
                "Freitagabend",
                false,
                [new CreateGroupMemberInput(personId, "Anna", false)])));
    }

    private sealed class FakeCreateGroupDataAccess(IReadOnlySet<Guid> activeIds)
        : ICreateGroupDataAccess
    {
        public Group? SavedGroup { get; private set; }
        public IReadOnlyCollection<Person> SavedNewPeople { get; private set; } = [];

        public Task<IReadOnlyList<SelectablePerson>> GetSelectablePeopleAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SelectablePerson>>([]);

        public Task<IReadOnlySet<Guid>> GetActivePersonIdsAsync(
            IReadOnlyCollection<Guid> personIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(activeIds);

        public Task SaveAsync(
            Group group,
            IReadOnlyCollection<Person> newPeople,
            CancellationToken cancellationToken = default)
        {
            SavedGroup = group;
            SavedNewPeople = newPeople;
            return Task.CompletedTask;
        }
    }
}

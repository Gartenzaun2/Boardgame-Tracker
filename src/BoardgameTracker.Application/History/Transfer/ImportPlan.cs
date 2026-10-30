using System.Text.Json;

namespace BoardgameTracker.Application.History.Transfer;

public sealed record LocalPlayer(Guid Id, string Name);
public sealed record LocalGroup(Guid Id, string Name, Guid[] PlayerIds);
public sealed record TransferSnapshot(LocalPlayer[] Players, LocalGroup[] Groups, Guid[] GameSessionIds, IReadOnlyDictionary<string, JsonElement>? ModuleState = null);
public sealed record GroupResolution(Guid GroupId, bool Split, IReadOnlyDictionary<Guid, Guid> PlayerMappings);
public sealed record GroupConflict(BgtGroup Imported, LocalGroup Local, IReadOnlyDictionary<Guid, Guid> FixedMappings);
public sealed record PlannedPlayer(BgtPlayer Imported, Guid LocalId, bool IsNew, bool IsManual);
public sealed record PlannedGroup(BgtGroup Imported, Guid LocalId, string Name, bool IsNew);
public sealed record ImportPlan(BgtFile Source, TransferSnapshot Snapshot, GroupResolution[] Resolutions,
    PlannedPlayer[] Players, PlannedGroup[] Groups, BgtGameSession[] GameSessions, int Skipped, GroupConflict[] Conflicts)
{
    public bool CanCommit => Conflicts.Length == 0;
}

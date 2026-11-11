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
    public GameResolution[] GameResolutions { get; init; } = [];
    public PlannedGame[] Games { get; init; } = [];
    public GameConflict[] GameConflicts { get; init; } = [];
    public bool CanCommit => Conflicts.Length == 0 && GameConflicts.Length == 0;
}

public sealed record GameResolution(Guid SourceId, Guid? LocalId);
public sealed record GameCandidate(Guid Id, string Name);
public sealed record GameConflict(Guid SourceId, string Name, GameCandidate[] Candidates);
public sealed record PlannedGame(Guid SourceId, Guid? LocalId, string Name, string SourceName)
{
    public bool IsNew => LocalId is null;
}

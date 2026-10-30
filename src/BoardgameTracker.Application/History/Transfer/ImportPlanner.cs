using System.Text.Json;
using BoardgameTracker.Core.Names;

namespace BoardgameTracker.Application.History.Transfer;

public sealed record LocalPlayer(Guid Id, string Name);
public sealed record LocalGroup(Guid Id, string Name, Guid[] PlayerIds);
public sealed record TransferSnapshot(LocalPlayer[] Players, LocalGroup[] Groups, Guid[] GameSessionIds, IReadOnlyDictionary<string, System.Text.Json.JsonElement>? ModuleState = null);
public sealed record GroupResolution(Guid GroupId, bool Split, IReadOnlyDictionary<Guid, Guid> PlayerMappings);
public sealed record GroupConflict(BgtGroup Imported, LocalGroup Local, IReadOnlyDictionary<Guid, Guid> FixedMappings);
public sealed record PlannedPlayer(BgtPlayer Imported, Guid LocalId, bool IsNew, bool IsManual);
public sealed record PlannedGroup(BgtGroup Imported, Guid LocalId, string Name, bool IsNew);
public sealed record ImportPlan(BgtFile Source, TransferSnapshot Snapshot, GroupResolution[] Resolutions,
    PlannedPlayer[] Players, PlannedGroup[] Groups, BgtGameSession[] GameSessions, int Skipped, GroupConflict[] Conflicts)
{
    public bool CanCommit => Conflicts.Length == 0;
}

public static class ImportPlanner
{
    public static ImportPlan Analyze(BgtFile file, TransferSnapshot local, IReadOnlyList<GroupResolution>? resolutions = null)
    {
        GroupResolution[] decisions = resolutions?.ToArray() ?? [];
        BgtGameSession[] gameSessions = file.GameSessions.Where(gameSession => !local.GameSessionIds.Contains(gameSession.Id)).ToArray();
        HashSet<Guid> neededGroups = gameSessions.Select(gameSession => gameSession.GroupId).ToHashSet();
        BgtGroup[] groups = file.Groups.Where(group => neededGroups.Contains(group.Id)).OrderBy(group => group.Id).ToArray();
        HashSet<Guid> neededPlayers = groups.SelectMany(group => group.GroupPlayers.Select(player => player.PlayerId)).ToHashSet();
        BgtPlayer[] players = file.Players.Where(player => neededPlayers.Contains(player.Id)).OrderBy(player => player.Id).ToArray();
        Dictionary<Guid, Guid> mappings = [];
        HashSet<Guid> manual = [];
        foreach (BgtPlayer player in players)
        {
            LocalPlayer? match = local.Players.SingleOrDefault(candidate => candidate.Id == player.Id)
                ?? local.Players.SingleOrDefault(candidate => EntityName.Equals(candidate.Name, player.Name));
            if (match is not null)
                AddMapping(mappings, player.Id, match.Id);
        }
        foreach (GroupResolution decision in decisions)
        {
            BgtGroup imported = groups.Single(group => group.Id == decision.GroupId);
            LocalGroup? target = Candidate(imported, local);
            if (decision.Split)
            {
                if (target?.Id == imported.Id || decision.PlayerMappings.Count != 0)
                    throw new InvalidDataException("InvalidResolution");
                continue;
            }
            if (target is null || target.PlayerIds.Length != imported.GroupPlayers.Length)
                throw new InvalidDataException("InvalidResolution");
            foreach (KeyValuePair<Guid, Guid> mapping in decision.PlayerMappings)
            {
                if (!imported.GroupPlayers.Any(player => player.PlayerId == mapping.Key) || !target.PlayerIds.Contains(mapping.Value))
                    throw new InvalidDataException("InvalidResolution");
                AddMapping(mappings, mapping.Key, mapping.Value);
                manual.Add(mapping.Key);
            }
            if (!imported.GroupPlayers.All(player => mappings.ContainsKey(player.PlayerId))
                || !target.PlayerIds.ToHashSet().SetEquals(imported.GroupPlayers.Select(player => mappings[player.PlayerId])))
                throw new InvalidDataException("InvalidResolution");
        }
        List<PlannedGroup> plannedGroups = [];
        List<GroupConflict> conflicts = [];
        HashSet<string> names = new(local.Groups.Select(group => group.Name.Trim()), EntityName.Comparer);
        foreach (BgtGroup group in groups)
        {
            LocalGroup? target = Candidate(group, local);
            bool split = decisions.Any(decision => decision.GroupId == group.Id && decision.Split);
            Guid[] known = group.GroupPlayers.Where(player => mappings.ContainsKey(player.PlayerId)).Select(player => mappings[player.PlayerId]).ToArray();
            bool possible = target is not null && target.PlayerIds.Length == group.GroupPlayers.Length
                && known.All(target.PlayerIds.Contains)
                && target.PlayerIds.Except(known).All(id => !mappings.Values.Contains(id));
            bool complete = possible && known.Length == group.GroupPlayers.Length;
            if (target?.Id == group.Id && !possible)
                throw new InvalidDataException("GroupIdentityConflict");
            if (!split && complete)
                plannedGroups.Add(new(group, target!.Id, target.Name, false));
            else if (!split && possible)
                conflicts.Add(new(group, target!, new Dictionary<Guid, Guid>(mappings)));
            else
            {
                string name = group.Name.Trim();
                if (names.Contains(name))
                {
                    string basis = name;
                    int suffix = 1;
                    do
                    {
                        string tail = suffix == 1 ? " Import" : $" Import {suffix}";
                        name = basis[..Math.Min(basis.Length, 100 - tail.Length)] + tail;
                        suffix++;
                    } while (names.Contains(name));
                }
                names.Add(name);
                plannedGroups.Add(new(group, group.Id, name, true));
            }
        }
        PlannedPlayer[] plannedPlayers = players.Select(player => new PlannedPlayer(player,
            mappings.GetValueOrDefault(player.Id, player.Id), !mappings.ContainsKey(player.Id), manual.Contains(player.Id))).ToArray();
        return new(file, local, decisions, plannedPlayers, plannedGroups.ToArray(), gameSessions,
            file.GameSessions.Length - gameSessions.Length, conflicts.ToArray());
    }

    private static LocalGroup? Candidate(BgtGroup group, TransferSnapshot local) =>
        local.Groups.SingleOrDefault(candidate => candidate.Id == group.Id)
        ?? local.Groups.SingleOrDefault(candidate => EntityName.Equals(candidate.Name, group.Name) && candidate.PlayerIds.Length == group.GroupPlayers.Length);

    private static void AddMapping(Dictionary<Guid, Guid> mappings, Guid importedId, Guid localId)
    {
        if (mappings.TryGetValue(importedId, out Guid existing) && existing != localId
            || mappings.Any(mapping => mapping.Key != importedId && mapping.Value == localId))
            throw new InvalidDataException("PlayerMappingConflict");
        mappings[importedId] = localId;
    }
}

public sealed record TransferPreview(int GamesExisting, int ReferencesExisting, int ReferencesNew);

public interface IHistoryTransferStore
{
    Task<BgtFile> ExportAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default);
    Task<TransferSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default);
    Task<TransferPreview> PreviewAsync(ImportPlan plan, CancellationToken cancellationToken = default);
    Task CommitAsync(ImportPlan plan, CancellationToken cancellationToken = default);
}

public sealed class HistoryTransferUseCase(IHistoryTransferStore store, BgtFormat format)
{
    public async Task<BgtFile> ExportAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default)
    {
        BgtFile file = await store.ExportAsync(gameSessionIds, appVersion, cancellationToken);
        format.Validate(file);
        return file;
    }
    public async Task<BgtExportContent> CreateExportContentAsync(IReadOnlyCollection<Guid> gameSessionIds, string appVersion, CancellationToken cancellationToken = default)
    {
        BgtFile file = await ExportAsync(gameSessionIds, appVersion, cancellationToken);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(file, BgtFormat.JsonOptions);
        if (bytes.Length > BgtFormat.MaximumBytes)
            throw new InvalidDataException("SizeLimit");
        return new($"boardgame-tracker-{Guid.CreateVersion7()}.bgt", bytes);
    }
    public async Task<ImportPlan> AnalyzeAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        BgtFile file = await format.ReadAsync(stream, cancellationToken);
        return ImportPlanner.Analyze(file, await store.ReadSnapshotAsync(cancellationToken));
    }
    public Task<TransferPreview> PreviewAsync(ImportPlan plan, CancellationToken cancellationToken = default) => store.PreviewAsync(plan, cancellationToken);
    public Task CommitAsync(ImportPlan plan, CancellationToken cancellationToken = default)
    {
        format.Validate(plan.Source);
        if (!plan.CanCommit)
            throw new InvalidOperationException("UnresolvedImport");
        return store.CommitAsync(plan, cancellationToken);
    }
}

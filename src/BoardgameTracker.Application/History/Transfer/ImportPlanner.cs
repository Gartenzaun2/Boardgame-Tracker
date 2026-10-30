using BoardgameTracker.Core.Names;

namespace BoardgameTracker.Application.History.Transfer;

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
        // Resolve stable player identities first, apply decisions globally, then evaluate all
        // groups again. A mapping chosen for one group constrains every other group in the file.
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

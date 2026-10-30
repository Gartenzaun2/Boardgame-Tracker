using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoardgameTracker.Core.Names;

namespace BoardgameTracker.Application.History.Transfer;

public sealed record BgtFile(int FormatVersion, string AppVersion, DateTimeOffset ExportedAtUtc,
    BgtGame[] Games, BgtPlayer[] Players, BgtGroup[] Groups, BgtGameSession[] GameSessions);
public sealed record BgtGame(Guid Id, string Key, int SchemaVersion, JsonElement Data);
public sealed record BgtPlayer(Guid Id, string Name, bool IsGuest, bool IsArchived, DateTimeOffset CreatedAt);
public sealed record BgtGroup(Guid Id, string Name, bool IsTemporary, bool IsArchived, BgtGroupPlayer[] GroupPlayers);
public sealed record BgtGroupPlayer(Guid PlayerId, int DefaultOrder);
public sealed record BgtGameSession(Guid Id, Guid GameId, Guid GroupId, DateTimeOffset StartedAt,
    DateTimeOffset EndedAt, DateTimeOffset ModifiedAt, BgtGameSessionPlayer[] Players, JsonElement Data);
public sealed record BgtGameSessionPlayer(Guid PlayerId, int Order, int Placement);

public interface IGameTransferSchema
{
    string GameKey { get; }
    int SchemaVersion { get; }
    void Validate(BgtGame game, IReadOnlyList<BgtGameSession> gameSessions);
}

public sealed class BgtFormat(IEnumerable<IGameTransferSchema> schemas)
{
    public const int MaximumBytes = 16 * 1024 * 1024;
    public const int MaximumItems = 10000;
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        MaxDepth = 32,
        WriteIndented = true
    };

    public async Task<BgtFile> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) != 0)
        {
            if (buffer.Length + count > MaximumBytes)
                throw new InvalidDataException("SizeLimit");
            await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }
        string json = new UTF8Encoding(false, true).GetString(buffer.ToArray());
        using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
        CheckProperties(document.RootElement);
        if (!document.RootElement.TryGetProperty("formatVersion", out JsonElement version) || !version.TryGetInt32(out int value) || value != 1)
            throw new InvalidDataException("UpdateRequired");
        BgtFile file = JsonSerializer.Deserialize<BgtFile>(json, JsonOptions) ?? throw new InvalidDataException("InvalidFile");
        Validate(file);
        return file;
    }

    private static void CheckProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException("DuplicateProperty");
                CheckProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            if (element.GetArrayLength() > MaximumItems)
                throw new InvalidDataException("CollectionLimit");
            foreach (JsonElement child in element.EnumerateArray())
                CheckProperties(child);
        }
        else if (element.ValueKind == JsonValueKind.String && element.GetString()!.Length > 1000)
            throw new InvalidDataException("StringLimit");
    }

    public void Validate(BgtFile file)
    {
        if (file.FormatVersion != 1)
            throw new InvalidDataException("UpdateRequired");
        if (file.AppVersion is null || file.AppVersion.Length > 100)
            throw new InvalidDataException("InvalidMetadata");
        CheckIds(file.Games.Select(game => game.Id));
        CheckIds(file.Players.Select(player => player.Id));
        CheckIds(file.Groups.Select(group => group.Id));
        CheckIds(file.GameSessions.Select(gameSession => gameSession.Id));
        CheckNames(file.Players.Select(player => player.Name));
        CheckNames(file.Groups.Select(group => group.Name));
        if (file.Games.Select(game => game.Key).Distinct(StringComparer.Ordinal).Count() != file.Games.Length)
            throw new InvalidDataException("DuplicateGame");
        Dictionary<Guid, BgtPlayer> players = file.Players.ToDictionary(player => player.Id);
        Dictionary<Guid, BgtGroup> groups = file.Groups.ToDictionary(group => group.Id);
        Dictionary<Guid, BgtGame> games = file.Games.ToDictionary(game => game.Id);
        foreach (BgtGroup group in file.Groups)
        {
            CheckOrder(group.GroupPlayers.Select(player => player.DefaultOrder));
            CheckIds(group.GroupPlayers.Select(player => player.PlayerId));
            if (group.GroupPlayers.Any(player => !players.ContainsKey(player.PlayerId)))
                throw new InvalidDataException("MissingPlayer");
        }
        foreach (BgtGameSession gameSession in file.GameSessions)
        {
            if (!games.ContainsKey(gameSession.GameId) || !groups.TryGetValue(gameSession.GroupId, out BgtGroup? group))
                throw new InvalidDataException("MissingDependency");
            CheckOrder(gameSession.Players.Select(player => player.Order));
            CheckIds(gameSession.Players.Select(player => player.PlayerId));
            if (!gameSession.Players.Select(player => player.PlayerId).ToHashSet().SetEquals(group.GroupPlayers.Select(player => player.PlayerId))
                || gameSession.Players.Any(player => player.Placement < 1 || player.Placement > gameSession.Players.Length)
                || gameSession.EndedAt < gameSession.StartedAt || gameSession.ModifiedAt < gameSession.EndedAt)
                throw new InvalidDataException("InvalidGameSession");
        }
        foreach (BgtGame game in file.Games)
        {
            IGameTransferSchema schema = schemas.SingleOrDefault(schema => schema.GameKey == game.Key)
                ?? throw new InvalidDataException("UpdateRequired");
            if (game.SchemaVersion != schema.SchemaVersion)
                throw new InvalidDataException("UpdateRequired");
            schema.Validate(game, file.GameSessions.Where(gameSession => gameSession.GameId == game.Id).ToArray());
        }
    }

    public static void CheckIds(IEnumerable<Guid> values)
    {
        Guid[] ids = values.ToArray();
        if (ids.Length > MaximumItems || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new InvalidDataException("InvalidIds");
    }
    public static void CheckOrder(IEnumerable<int> values)
    {
        int[] order = values.Order().ToArray();
        if (order.Length == 0 || !order.SequenceEqual(Enumerable.Range(1, order.Length)))
            throw new InvalidDataException("InvalidOrder");
    }
    private static void CheckNames(IEnumerable<string> values)
    {
        string[] names = values.Select(EntityName.Normalize).ToArray();
        if (names.Distinct(EntityName.Comparer).Count() != names.Length)
            throw new InvalidDataException("DuplicateName");
    }
}

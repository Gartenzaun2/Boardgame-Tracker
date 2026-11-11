using System.Text.Json;
using System.Text.Json.Serialization;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ManualGames.History.Transfer;

public sealed record ManualTransferGame(string Name, string BadgeText, int MinPlayers, int MaxPlayers,
    bool CapturePoints, bool AllowNegativePoints, bool CaptureWinner, bool CapturePlacements, bool AllowTies,
    bool ResultFromPoints, PointsDirection? PointsDirection, bool CapturePlayerOrder, bool CapturePlayerColors)
{
    [JsonIgnore]
    public ManualGameOptions Options => new(CapturePoints, AllowNegativePoints, CaptureWinner, CapturePlacements, AllowTies,
        ResultFromPoints, PointsDirection, CapturePlayerOrder, CapturePlayerColors);
}
public sealed record ManualTransferSession(string PlacementKind, ManualTransferPlayer[] Players);
public sealed record ManualTransferPlayer(Guid PlayerId, int? Points, PlayerColor? Color);

public sealed class ManualTransferSchema : IGameTransferSchema
{
    public const string ModuleKey = "manual";
    public string GameKey => ModuleKey;
    public bool CanHandle(string key) => ManualGame.MatchesKey(key);
    public int SchemaVersion => 1;
    public static T Read<T>(JsonElement data) => data.Deserialize<T>(BgtFormat.JsonOptions) ?? throw new InvalidDataException("InvalidManualData");
    public static string Kind(ManualHistoricalPlacements placements) => placements switch
    {
        ManualHistoricalPlacements.None => "none",
        ManualHistoricalPlacements.Winners => "winners",
        ManualHistoricalPlacements.Full => "placements",
        _ => throw new InvalidDataException("InvalidPlacementKind")
    };
    public static ScoringRulesSnapshot ImportedSnapshot(ManualTransferSession data, IReadOnlyList<BgtGameSessionPlayer> players)
    {
        if (data.PlacementKind is not ("none" or "winners" or "placements"))
            throw new InvalidDataException("InvalidPlacementKind");
        bool points = data.Players.All(player => player.Points.HasValue);
        bool winner = data.PlacementKind != "none";
        bool ties = players.Where(player => player.Placement.HasValue).GroupBy(player => player.Placement).Any(group => group.Count() > 1);
        return new(points, data.Players.Any(player => player.Points < 0), winner,
            data.PlacementKind == "placements", winner && ties, false, null);
    }
    public void Validate(BgtGame game, IReadOnlyList<BgtGameSession> sessions)
    {
        ManualTransferGame data = Read<ManualTransferGame>(game.Data);
        Game metadata = new(game.Key, data.Name, data.BadgeText, data.MinPlayers, data.MaxPlayers);
        ManualGame options = new(metadata, data.Options);
        ManualGameOptions normalized = new(options.CapturePoints, options.AllowNegativePoints, options.CaptureWinner, options.CapturePlacements,
            options.AllowTies, options.ResultFromPoints, options.PointsDirection, options.CapturePlayerOrder, options.CapturePlayerColors);
        if (normalized != data.Options)
            throw new InvalidDataException("InvalidManualConfiguration");
        foreach (BgtGameSession session in sessions)
        {
            ManualTransferSession result = Read<ManualTransferSession>(session.Data);
            BgtFormat.CheckIds(result.Players.Select(player => player.PlayerId));
            if (result.Players.Length is < 1 or > 12 || !result.Players.Select(player => player.PlayerId).ToHashSet().SetEquals(session.Players.Select(player => player.PlayerId)))
                throw new InvalidDataException("InvalidManualParticipants");
            PlayerColor?[] colors = result.Players.Select(player => player.Color).ToArray();
            if (colors.Any(color => color.HasValue) && (colors.Any(color => color is null)
                || colors.Distinct().Count() != colors.Length || colors.Any(color => !Enum.IsDefined(color!.Value))))
                throw new InvalidDataException("InvalidManualColors");
            ScoringRulesSnapshot snapshot = ImportedSnapshot(result, session.Players);
            ManualScoreInput[] values = result.Players.Select(player => new ManualScoreInput(player.PlayerId, player.Points,
                session.Players.Single(participant => participant.PlayerId == player.PlayerId).Placement)).ToArray();
            ManualHistoricalResult form = ManualHistoricalResult.FromValues(values, snapshot);
            if (Kind(form.Placements) != result.PlacementKind)
                throw new InvalidDataException("InvalidPlacementKind");
        }
    }
}

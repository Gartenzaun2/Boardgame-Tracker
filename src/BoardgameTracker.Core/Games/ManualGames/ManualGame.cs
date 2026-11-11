namespace BoardgameTracker.Core.Games.ManualGames;

public enum PointsDirection { Highest = 1, Lowest = 2 }

public sealed record ManualGameOptions(
    bool CapturePoints = false, bool AllowNegativePoints = false,
    bool CaptureWinner = false, bool CapturePlacements = false, bool AllowTies = false,
    bool ResultFromPoints = false, PointsDirection? PointsDirection = null,
    bool CapturePlayerOrder = false, bool CapturePlayerColors = false);

/// <summary>One manual game's capture configuration; shared metadata remains on Game.</summary>
public sealed class ManualGame
{
    private ManualGame() { }

    public ManualGame(Game game, ManualGameOptions options)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (!MatchesKey(game.Key))
            throw new ArgumentException("A manual game must use the reserved key namespace.", nameof(game));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(game.MaxPlayers, 12);
        GameId = game.Id;
        SetConfiguration(options);
    }

    public static string CreateKey() => "manual:" + Guid.NewGuid().ToString("D");
    public static bool MatchesKey(string key) => key.StartsWith("manual:", StringComparison.Ordinal)
        && Guid.TryParseExact(key[7..], "D", out Guid id) && id != Guid.Empty;

    public Guid GameId { get; private set; }
    public bool CapturePoints { get; private set; }
    public bool AllowNegativePoints { get; private set; }
    public bool CaptureWinner { get; private set; }
    public bool CapturePlacements { get; private set; }
    public bool AllowTies { get; private set; }
    public bool ResultFromPoints { get; private set; }
    public PointsDirection? PointsDirection { get; private set; }
    public bool CapturePlayerOrder { get; private set; }
    public bool CapturePlayerColors { get; private set; }

    public void SetConfiguration(ManualGameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        bool automatic = options.ResultFromPoints && options.CapturePoints && options.CaptureWinner;
        if (automatic && (options.PointsDirection is null || !Enum.IsDefined(options.PointsDirection.Value)))
            throw new ArgumentException("Automatic results require a valid points direction.", nameof(options));
        CapturePoints = options.CapturePoints;
        AllowNegativePoints = options.CapturePoints && options.AllowNegativePoints;
        CaptureWinner = options.CaptureWinner;
        CapturePlacements = options.CaptureWinner && options.CapturePlacements;
        AllowTies = options.CaptureWinner && options.AllowTies;
        ResultFromPoints = automatic;
        PointsDirection = automatic ? options.PointsDirection : null;
        CapturePlayerOrder = options.CapturePlayerOrder;
        CapturePlayerColors = options.CapturePlayerColors;
    }

    public void UpdateMetadata(Game game, string name, string badgeText, int minPlayers, int maxPlayers)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (game.Id != GameId || !MatchesKey(game.Key))
            throw new ArgumentException("Metadata must belong to this manual game.", nameof(game));
        if (maxPlayers != game.MaxPlayers)
            throw new InvalidOperationException("The maximum player count cannot change after creation.");
        game.SetMetadata(name, badgeText, minPlayers, maxPlayers);
    }
}

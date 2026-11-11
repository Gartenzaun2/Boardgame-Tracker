using System.Text.Json;
using System.Text.Json.Nodes;
using BoardgameTracker.Application.Games.ArkNova.History.Transfer;
using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.Application.Games.ManualGames.History.Transfer;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Presentation;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.History.Transfer;

public sealed class ManualTransferTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static BgtFormat Format => new([new ArkNovaTransferSchema(), new ManualTransferSchema()]);
    private static HistoryTransferPersistence Store(SliceDatabase db) => new(db, [new ArkNovaTransferPersistence(), new ManualTransferPersistence()]);
    private static JsonElement Json<T>(T value) => JsonSerializer.SerializeToElement(value, BgtFormat.JsonOptions);
    private static ManualTransferGame Config(string name = "Manual") => new(name, "TAG", 1, 12, false, false, false, false, false, false, null, false, false);
    private static BgtFile File(string kind = "placements", bool points = true, bool colors = true, bool order = true, bool allFirst = false, string name = "Manual")
    {
        BgtPlayer[] players = [new(Guid.NewGuid(), "P1", false, false, DateTimeOffset.UtcNow), new(Guid.NewGuid(), "P2", false, false, DateTimeOffset.UtcNow)];
        BgtGroup group = new(Guid.NewGuid(), "Imported group", false, false, players.Select((player, index) => new BgtGroupPlayer(player.Id, index + 1)).ToArray());
        BgtGame game = new(Guid.NewGuid(), ManualGame.CreateKey(), 1, Json(Config(name)));
        BgtGameSession session = new(Guid.NewGuid(), game.Id, group.Id, DateTimeOffset.UtcNow.AddHours(-3), DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1),
            players.Select((player, index) => new BgtGameSessionPlayer(player.Id, order ? index + 1 : null,
                kind == "none" ? null : allFirst || index == 0 ? 1 : kind == "winners" ? null : 2)).ToArray(),
            Json(new ManualTransferSession(kind, players.Select((player, index) => new ManualTransferPlayer(player.Id, points ? -index : null, colors ? (PlayerColor)(index + 1) : null)).ToArray())));
        return new(1, "test", DateTimeOffset.UtcNow, [game], players, [group], [session]);
    }
    private static async Task<ImportPlan> AnalyzeAsync(SliceDatabase db, BgtFile file) => await new HistoryTransferUseCase(Store(db), Format)
        .AnalyzeAsync(new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(file, BgtFormat.JsonOptions)), Token);

    [Theory]
    [InlineData("none", false, false, false)]
    [InlineData("none", true, true, true)]
    [InlineData("winners", false, true, false)]
    [InlineData("winners", true, false, true)]
    [InlineData("placements", false, false, true)]
    [InlineData("placements", true, true, true)]
    public async Task Roundtrip_PreservesEveryHistoricalFormRegardlessOfCurrentConfiguration(string kind, bool points, bool colors, bool order)
    {
        BgtFile input = File(kind, points, colors, order);
        await using SliceDatabase source = await SliceDatabase.CreateAsync();
        ImportPlan plan = await AnalyzeAsync(source, input);
        Assert.True(plan.CanCommit);
        Assert.True(Assert.Single(plan.Games).IsNew);
        await Store(source).PreviewAsync(plan, Token);
        await Store(source).CommitAsync(plan, Token);
        BgtFile output = await new HistoryTransferUseCase(Store(source), Format).ExportAsync([input.GameSessions[0].Id], "test", Token);
        Assert.NotEqual(input.Games[0].Id, output.Games[0].Id);
        Assert.NotEqual(input.Games[0].Key, output.Games[0].Key);
        Assert.Equal(1, output.FormatVersion);
        Assert.Equal(1, output.Games[0].SchemaVersion);
        Assert.Equal(kind, ManualTransferSchema.Read<ManualTransferSession>(output.GameSessions[0].Data).PlacementKind);
        Assert.Equal(input.GameSessions[0].Players, output.GameSessions[0].Players.OrderBy(player => player.Order ?? (player.PlayerId == input.Players[0].Id ? 1 : 2)));
        Assert.Equal(ManualTransferSchema.Read<ManualTransferSession>(input.GameSessions[0].Data).Players.OrderBy(player => player.PlayerId),
            ManualTransferSchema.Read<ManualTransferSession>(output.GameSessions[0].Data).Players.OrderBy(player => player.PlayerId));
        await using SliceDatabase target = await SliceDatabase.CreateAsync();
        ImportPlan second = await AnalyzeAsync(target, output);
        await Store(target).CommitAsync(second, Token);
        ManualScoringData result = (await new ManualScoringQuery(target).GetAsync(input.GameSessions[0].Id, Token))!;
        Assert.False(result.Session.ScoringRules.ResultFromPoints);
        Assert.Null(result.Session.ScoringRules.PointsDirection);
        Assert.Equal(points, result.Session.ScoringRules.CapturePoints);
        Assert.Equal(kind == "placements", result.Session.ScoringRules.CapturePlacements);
        Assert.Equal(input.GameSessions[0].Players.Select(player => player.Placement), result.Values.Select(player => player.Placement));
        Assert.Equal(input.GameSessions[0].EndedAt, result.Session.EndedAt);
        await using BoardgameTrackerDbContext db = target.CreateDbContext();
        Assert.Empty(await db.GroupGamePlayerColors.ToArrayAsync(Token));
        ImportPlan repeat = await AnalyzeAsync(target, output);
        Assert.Empty(repeat.Games);
        Assert.Empty(repeat.GameConflicts);
        Assert.Empty(repeat.Players);
        Assert.Empty(repeat.Groups);
        Assert.Equal(1, repeat.Skipped);
    }

    [Theory]
    [InlineData("winners", false)]
    [InlineData("placements", true)]
    public async Task AllFirstPlace_ExplicitKindSurvivesMatchedImportCorrectionAndReexport(string kind, bool full)
    {
        BgtFile file = File(kind, allFirst: true);
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        Guid local = await new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(new(null, "MANUAL", "LOCAL", 1, 1, new()), Token);
        ImportPlan unresolved = await AnalyzeAsync(db, file);
        Assert.False(unresolved.CanCommit);
        Assert.Equal(local, Assert.Single(Assert.Single(unresolved.GameConflicts).Candidates).Id);
        HistoryTransferUseCase transfer = new(Store(db), Format);
        ImportPlan plan = transfer.Resolve(unresolved, games: [new(file.Games[0].Id, local)]);
        await transfer.CommitAsync(plan, Token);
        ManualScoringData result = (await new ManualScoringQuery(db).GetAsync(file.GameSessions[0].Id, Token))!;
        Assert.Equal(full, result.Session.ScoringRules.CapturePlacements);
        await new ManualHistoryCorrectionUseCase(db).SaveCorrectionAsync(result.Session.SessionId, result.Values, Token);
        BgtFile output = await transfer.ExportAsync([result.Session.SessionId], "test", Token);
        Assert.Equal(kind, ManualTransferSchema.Read<ManualTransferSession>(output.GameSessions[0].Data).PlacementKind);
        await using BoardgameTrackerDbContext context = db.CreateDbContext();
        Game game = await context.Games.SingleAsync(game => game.Id == local, Token);
        Assert.Equal("MANUAL", game.Name);
        Assert.Equal("LOCAL", game.BadgeText);
        Assert.Equal(1, game.MaxPlayers);
        Assert.False((await context.ManualGames.SingleAsync(Token)).CapturePoints);
        Assert.Empty(await context.GroupGames.Where(assignment => assignment.GameId == local).ToArrayAsync(Token));
    }

    [Theory]
    [InlineData("placementKind-missing")]
    [InlineData("placementKind-null")]
    [InlineData("placementKind-unknown")]
    [InlineData("placementKind-mismatch")]
    [InlineData("points-missing")]
    [InlineData("points-partial")]
    [InlineData("points-overflow")]
    [InlineData("points-fraction")]
    [InlineData("color-missing")]
    [InlineData("color-partial")]
    [InlineData("color-duplicate")]
    [InlineData("color-invalid")]
    [InlineData("extra-session")]
    [InlineData("extra-game")]
    [InlineData("missing-game-field")]
    [InlineData("wrong-version")]
    [InlineData("wrong-format")]
    [InlineData("unknown-key")]
    [InlineData("mixed-order")]
    [InlineData("missing-placement")]
    [InlineData("invalid-placement")]
    [InlineData("partial-placements")]
    [InlineData("foreign-player")]
    [InlineData("duplicate-player")]
    [InlineData("inconsistent-options")]
    public async Task StrictReading_RejectsInvalidPayloadBeforeAnyWrites(string invalid)
    {
        JsonNode file = JsonNode.Parse(JsonSerializer.Serialize(File(), BgtFormat.JsonOptions))!;
        JsonObject game = file["games"]![0]!["data"]!.AsObject();
        JsonObject session = file["gameSessions"]![0]!["data"]!.AsObject();
        JsonObject player = session["players"]![0]!.AsObject();
        JsonObject general = file["gameSessions"]![0]!["players"]![0]!.AsObject();
        switch (invalid)
        {
            case "placementKind-missing": session.Remove("placementKind"); break;
            case "placementKind-null": session["placementKind"] = null; break;
            case "placementKind-unknown": session["placementKind"] = "Placements"; break;
            case "placementKind-mismatch": session["placementKind"] = "none"; break;
            case "points-missing": player.Remove("points"); break;
            case "points-partial": player["points"] = null; break;
            case "points-overflow": player["points"] = 2147483648L; break;
            case "points-fraction": player["points"] = 1.5; break;
            case "color-missing": player.Remove("color"); break;
            case "color-partial": player["color"] = null; break;
            case "color-duplicate": player["color"] = 2; break;
            case "color-invalid": player["color"] = 99; break;
            case "extra-session": session["unknown"] = 1; break;
            case "extra-game": game["unknown"] = 1; break;
            case "missing-game-field": game.Remove("captureWinner"); break;
            case "wrong-version": file["games"]![0]!["schemaVersion"] = 2; break;
            case "wrong-format": file["formatVersion"] = 2; break;
            case "unknown-key": file["games"]![0]!["key"] = "manual:invalid"; break;
            case "mixed-order": general["order"] = null; break;
            case "missing-placement": general.Remove("placement"); break;
            case "invalid-placement": general["placement"] = 3; break;
            case "partial-placements": general["placement"] = null; break;
            case "foreign-player": player["playerId"] = Guid.NewGuid(); break;
            case "duplicate-player": player["playerId"] = session["players"]![1]!["playerId"]!.DeepClone(); break;
            case "inconsistent-options": game["allowNegativePoints"] = true; break;
        }
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => new HistoryTransferUseCase(Store(db), Format).AnalyzeAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(file.ToJsonString())), Token));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessions.ToArrayAsync(Token));
    }

    [Fact]
    public async Task NewInsteadOfMatch_GeneratesDeterministicNamesAndSeparateLocalIdentities()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        string name = new('N', 100);
        Guid local = await new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(new(null, name, "L", 1, 12, new()), Token);
        BgtFile file = File(name: name);
        BgtGame second = file.Games[0] with { Id = Guid.NewGuid(), Key = ManualGame.CreateKey() };
        file = file with { Games = [file.Games[0], second], GameSessions = [file.GameSessions[0], file.GameSessions[0] with { Id = Guid.NewGuid(), GameId = second.Id }] };
        ImportPlan plan = await AnalyzeAsync(db, file);
        Assert.Equal(2, plan.GameConflicts.Length);
        HistoryTransferUseCase transfer = new(Store(db), Format);
        Assert.Throws<InvalidDataException>(() => transfer.Resolve(plan, games: file.Games.Select(game => new GameResolution(game.Id, local)).ToArray()));
        plan = transfer.Resolve(plan, games: file.Games.Select(game => new GameResolution(game.Id, null)).ToArray());
        Assert.Equal(new[] { name[..88] + "-Importiert1", name[..88] + "-Importiert2" }, plan.Games.Select(game => game.Name));
        await transfer.PreviewAsync(plan, Token);
        await using (BoardgameTrackerDbContext before = db.CreateDbContext())
            Assert.Single(await before.ManualGames.ToArrayAsync(Token));
        await transfer.CommitAsync(plan, Token);
        await using BoardgameTrackerDbContext after = db.CreateDbContext();
        Assert.Equal(3, await after.ManualGames.CountAsync(Token));
        Assert.Equal(2, await after.GroupGames.Where(assignment => assignment.GroupId == file.Groups[0].Id).CountAsync(Token));
    }

    [Fact]
    public async Task SameSourceKeyAndId_NeverImplicitlyMatch_AndIntegratedNamesAreNotCandidates()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        Guid id = await new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(new(null, "Local", "L", 1, 12, new()), Token);
        await using BoardgameTrackerDbContext context = db.CreateDbContext();
        Game local = await context.Games.SingleAsync(game => game.Id == id, Token);
        BgtFile file = File(name: "Different");
        file = file with { Games = [file.Games[0] with { Id = id, Key = local.Key }], GameSessions = [file.GameSessions[0] with { GameId = id }] };
        Assert.True(Assert.Single((await AnalyzeAsync(db, file)).Games).IsNew);
        Game integrated = await context.Games.SingleAsync(game => game.Id == db.GameId, Token);
        Assert.Empty((await AnalyzeAsync(db, File(name: integrated.Name))).GameConflicts);
    }

    [Fact]
    public async Task CommitRechecksConfigurationAndRollsBackEveryModuleRowOnFailure()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        BgtFile file = File();
        ImportPlan plan = await AnalyzeAsync(db, file);
        await using (BoardgameTrackerDbContext context = db.CreateDbContext())
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectManualImport BEFORE INSERT ON ManualGameSessions BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => Store(db).CommitAsync(plan, Token));
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
        {
            Assert.Empty(await verify.ManualGames.ToArrayAsync(Token));
            Assert.Empty(await verify.GameSessions.ToArrayAsync(Token));
            Assert.Empty(await verify.GameSessionPlayers.ToArrayAsync(Token));
            Assert.Empty(await verify.GameSessionPlayerColors.ToArrayAsync(Token));
            Assert.Equal(2, await verify.Players.CountAsync(Token));
            Assert.Single(await verify.Groups.ToArrayAsync(Token));
            await verify.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectManualImport;", Token);
        }
        await new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(new(null, "Manual", "L", 1, 12, new()), Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store(db).CommitAsync(plan, Token));
    }

    [Fact]
    public async Task MultipleSessionsOfOneGame_ExportOnceAndExcludeOpenOrUnselectedSessions()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        BgtFile file = File();
        file = file with { GameSessions = [file.GameSessions[0], file.GameSessions[0] with { Id = Guid.NewGuid() }] };
        await Store(db).CommitAsync(await AnalyzeAsync(db, file), Token);
        BgtFile output = await new HistoryTransferUseCase(Store(db), Format).ExportAsync(file.GameSessions.Select(session => session.Id).ToArray(), "test", Token);
        Assert.Single(output.Games);
        Assert.Equal(2, output.GameSessions.Length);
        BgtFile selected = await Store(db).ExportAsync([file.GameSessions[0].Id], "test", Token);
        Assert.Single(selected.GameSessions);
        await using BoardgameTrackerDbContext context = db.CreateDbContext();
        GameSession running = new(output.Games[0].Id, file.Groups[0].Id, DateTimeOffset.UtcNow, file.Players.Select(player => player.Id).ToArray());
        context.GameSessions.Add(running);
        await context.SaveChangesAsync(Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store(db).ExportAsync([running.Id], "test", Token));
    }
    [Fact]
    public async Task MixedGames_KeepAllModulesDistinctAndPruneKnownGameDependencies()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        BgtFile manual = File("winners", allFirst: true);
        BgtGame other = new(Guid.NewGuid(), ManualGame.CreateKey(), 1, Json(Config("Other")));
        BgtGame ark = new(Guid.NewGuid(), "ark-nova", 3, Json(new ArkNovaTransferReferences([])));
        BgtGameSession arkSession = manual.GameSessions[0] with
        {
            Id = Guid.NewGuid(), GameId = ark.Id,
            Players = manual.GameSessions[0].Players.Select((player, index) => player with { Placement = index + 1 }).ToArray(),
            Data = Json(new ArkNovaTransferGameSession(manual.Players.Select((player, index) => new ArkNovaTransferPlayer(player.Id,
                (PlayerColor)(index + 1), index + 1, 20 - index, 5, null, [])).ToArray()))
        };
        BgtFile file = manual with { Games = [manual.Games[0], other, ark], GameSessions = [manual.GameSessions[0], manual.GameSessions[0] with { Id = Guid.NewGuid(), GameId = other.Id }, arkSession] };
        await Store(db).CommitAsync(await AnalyzeAsync(db, file), Token);
        BgtFile export = await new HistoryTransferUseCase(Store(db), Format).ExportAsync(file.GameSessions.Select(session => session.Id).ToArray(), "test", Token);
        Assert.Equal(3, export.Games.Length);
        Assert.Equal(3, export.GameSessions.Length);
        await using SliceDatabase target = await SliceDatabase.CreateAsync();
        await Store(target).CommitAsync(await AnalyzeAsync(target, export), Token);
        await using BoardgameTrackerDbContext context = target.CreateDbContext();
        Assert.Equal(2, await context.ManualGameSessions.CountAsync(Token));
        Assert.Equal(2, await context.ArkNovaGameSessionPlayers.CountAsync(Token));
        Assert.Equal(3, await context.GroupGames.Where(assignment => assignment.GroupId == file.Groups[0].Id).CountAsync(Token));
        ImportPlan repeat = await AnalyzeAsync(target, export);
        Assert.Empty(repeat.Games);
        Assert.Empty(repeat.GameConflicts);
        Assert.Equal(3, repeat.Skipped);
        JsonNode invalid = JsonNode.Parse(JsonSerializer.Serialize(export, BgtFormat.JsonOptions))!;
        int arkIndex = Array.FindIndex(export.GameSessions, session => session.GameId == export.Games.Single(game => game.Key == "ark-nova").Id);
        invalid["gameSessions"]![arkIndex]!["players"]![0]!["placement"] = null;
        await Assert.ThrowsAnyAsync<Exception>(() => Format.ReadAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalid.ToJsonString())), Token));
    }

    [Fact]
    public async Task ExistingGroup_KeepsRemovedAssignmentsAndDefaults_AndChangedMatchingRequiresReanalysis()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        BgtFile source = File("none", colors: false, order: false);
        source = source with
        {
            Players = [source.Players[0] with { Name = db.First.Name }, source.Players[1] with { Name = db.Second.Name }],
            Groups = [source.Groups[0] with { Name = db.Group.Name }]
        };
        Guid game = await new ManualGameUseCase(db, new TestGameActivationStore()).SaveAsync(new(null, "Manual", "L", 1, 12, new()), Token);
        HistoryTransferUseCase transfer = new(Store(db), Format);
        ImportPlan initial = await AnalyzeAsync(db, source);
        ImportPlan plan = transfer.Resolve(initial, games: [new(source.Games[0].Id, game)]);
        Assert.False(Assert.Single(plan.Groups).IsNew);
        await using (BoardgameTrackerDbContext context = db.CreateDbContext())
        {
            (await context.ManualGames.SingleAsync(Token)).SetConfiguration(new(CapturePoints: true));
            await context.SaveChangesAsync(Token);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => transfer.CommitAsync(plan, Token));
        initial = await AnalyzeAsync(db, source);
        plan = transfer.Resolve(initial, games: [new(source.Games[0].Id, game)]);
        await transfer.CommitAsync(plan, Token);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Empty(await verify.GroupGames.Where(assignment => assignment.GameId == game).ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGamePlayerColors.ToArrayAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
    }

}

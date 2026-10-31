using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ArkNova.PhotoRecognition;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class PhotoRecognitionTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(ArkNovaPhotoSource.Camera)]
    [InlineData(ArkNovaPhotoSource.Library)]
    public async Task CameraAndPickerUseTheSameFrozenAnalyzer(ArkNovaPhotoSource source)
    {
        Input input = new(); Engine engine = new();
        ArkNovaPhotoReviewFlow flow = new(input, new(engine));
        ArkNovaPhotoRecognitionResult? result = await flow.AnalyzeAsync(source, Token);
        Assert.Equal(source, input.Source);
        Assert.Equal(1, engine.Calls);
        Assert.True(result!.Registration.Success);
        Assert.Equal(63, result.Colors["red"].Tracks["attractiveness"].Value);
        Assert.Equal("High", result.Colors["red"].Tracks["attractiveness"].Confidence);
        Assert.Equal("Uncertain", result.Colors["red"].Tracks["conservation"].Confidence);
    }

    [Fact]
    public async Task PickerCancellationDoesNotAnalyzeOrCreateAProposal()
    {
        Engine engine = new();
        ArkNovaPhotoReviewFlow flow = new(new Input { Cancel = true }, new(engine));
        Assert.Null(await flow.AnalyzeAsync(ArkNovaPhotoSource.Library, Token));
        Assert.Equal(0, engine.Calls);
    }

    [Fact]
    public async Task FailedRegistrationDoesNotExposeSyntheticScores()
    {
        Engine engine = new() { Registered = false };
        ArkNovaPhotoRecognitionResult result = await new ArkNovaPhotoRecognition(engine).AnalyzeAsync(new byte[] { 1 }, Token);
        Assert.False(result.Registration.Success);
        Assert.Empty(result.Colors);
    }

    [Fact]
    public async Task EditableProposalCannotAutosaveAndConfirmationPersistsTheCorrection()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var session = await database.AddSessionAsync(scoring: true);
        await using (var context = database.CreateDbContext())
        {
            ArkNovaGameSessionPlayer player = new(session.Id, database.First.Id, PlayerColor.Red, 1, 2, 1);
            player.SetScoreValues(70, 20, 8);
            context.Add(player); await context.SaveChangesAsync(Token);
        }
        ArkNovaPhotoRecognitionResult result = await new ArkNovaPhotoRecognition(new Engine()).AnalyzeAsync(new byte[] { 1 }, Token);
        ArkNovaPhotoReviewPlayer review = new(database.First.Id, "Anna", 70, 20, 8, true, result.Colors["red"]);
        Assert.Equal("63", review.AttractivenessText);
        review.AttractivenessText = "67";
        await Task.Delay(600, Token); // Past the existing manual editor's autosave debounce.
        await using (var context = database.CreateDbContext())
            Assert.Equal(70, (await context.ArkNovaGameSessionPlayers.SingleAsync(Token)).Attractiveness);
        Assert.True(review.TryRead(out ArkNovaConfirmedPhotoScore? confirmed));
        ArkNovaScoringUseCase useCase = new(database, new ArkNovaGameSessionResultQuery(database, TestGameContent.Create()));
        await useCase.SaveConfirmedPhotoScoresAsync(session.Id, [confirmed!], Token);
        await using (var context = database.CreateDbContext())
            Assert.Equal(67, (await context.ArkNovaGameSessionPlayers.SingleAsync(Token)).Attractiveness);
    }

    [Fact]
    public async Task InvalidConfirmedPlayerRollsBackTheWholeBatch()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var session = await database.AddSessionAsync(scoring: true);
        await using (var context = database.CreateDbContext())
        {
            ArkNovaGameSessionPlayer first = new(session.Id, database.First.Id, PlayerColor.Red, 1, 2, 1);
            ArkNovaGameSessionPlayer second = new(session.Id, database.Second.Id, PlayerColor.Blue, 2, 3, 2);
            first.SetScoreValues(70, 20, 8); second.SetScoreValues(60, 19, 7);
            context.AddRange(first, second); await context.SaveChangesAsync(Token);
        }
        ArkNovaScoringUseCase useCase = new(database, new(database, TestGameContent.Create()));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => useCase.SaveConfirmedPhotoScoresAsync(session.Id,
            [new(database.First.Id, 99, 20, 8), new(database.Second.Id, 60, 99, 7)], Token));
        await using var check = database.CreateDbContext();
        Assert.Equal(70, (await check.ArkNovaGameSessionPlayers.SingleAsync(player => player.PlayerId == database.First.Id, Token)).Attractiveness);
    }

    [Theory]
    [InlineData("High", "High", "✓ Sicher")]
    [InlineData("Medium", "Medium", "! Prüfen")]
    [InlineData("unexpected", "Uncertain", "? Unsicher")]
    public void PresentationPreservesKnownConfidenceAndTreatsUnknownAsUncertain(string input, string expected, string status)
    {
        FieldPrediction prediction = new(63, input, input, null, null, .9, .3, .4, .5, 1, 1, .4, [], 64, .1, 0, 0);
        ArkNovaPhotoReviewPlayer review = new(Guid.NewGuid(), "Anna", null, null, null, true, new(new() { ["attractiveness"] = prediction }));
        Assert.Equal(expected, review.AttractivenessConfidence);
        Assert.Equal(status, review.AttractivenessStatus);
        review.AttractivenessText = "114";
        Assert.False(review.TryRead(out _));
    }

    private sealed class Input : IArkNovaPhotoInput
    {
        public bool Cancel { get; init; }
        public ArkNovaPhotoSource Source { get; private set; }
        public Task<byte[]?> ReadAsync(ArkNovaPhotoSource source, CancellationToken cancellationToken = default)
        {
            Source = source;
            return Task.FromResult(Cancel ? null : new byte[] { 1, 2, 3 });
        }
    }
    private sealed class Engine : IArkNovaVisionEngine
    {
        public int Calls { get; private set; }
        public bool Registered { get; init; } = true;
        public Task<ArkNovaVisionEvidence> AnalyzeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
        {
            Calls++;
            List<RankingObservation> fields = [];
            foreach ((string track, int value) in new[] { ("attractiveness", 63), ("conservation", 21), ("reputation", 11) })
            {
                fields.Add(new("red", track, value, .4, .5, .5, .5, .5, 1, 1, .4, .5, .5));
                fields.Add(new("red", track, value+1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0));
            }
            return Task.FromResult(new ArkNovaVisionEvidence(new(Registered, 100, 80, 1, 1, 1, []), fields));
        }
    }
}

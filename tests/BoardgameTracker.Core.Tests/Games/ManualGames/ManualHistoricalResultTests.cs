using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;

namespace BoardgameTracker.Core.Tests.Games.ManualGames;

public sealed class ManualHistoricalResultTests
{
    private static ScoringRulesSnapshot Snapshot(bool full = false) => new(false, false, full, full, false, false, null);
    private static ManualScoreInput[] Values(int?[] points, int?[] places) => points.Select((value, index) => new ManualScoreInput(Guid.NewGuid(), value, places[index])).ToArray();

    [Theory]
    [InlineData(false, ManualHistoricalPlacements.None)]
    [InlineData(true, ManualHistoricalPlacements.None)]
    [InlineData(false, ManualHistoricalPlacements.Winners)]
    [InlineData(true, ManualHistoricalPlacements.Winners)]
    [InlineData(false, ManualHistoricalPlacements.Full)]
    [InlineData(true, ManualHistoricalPlacements.Full)]
    public void RecordedValues_DefineTheFormRegardlessOfDisabledSnapshotOptions(bool points, ManualHistoricalPlacements placements)
    {
        int?[] ranks = placements switch
        {
            ManualHistoricalPlacements.Winners => [1, 1, null],
            ManualHistoricalPlacements.Full => [1, 1, 3],
            _ => [null, null, null]
        };
        ManualScoreInput[] values = Values(points ? [0, -2, int.MinValue] : [null, null, null], ranks);
        ManualHistoricalResult form = ManualHistoricalResult.FromValues(values, Snapshot());
        Assert.Equal(new(points, placements), form);
        Assert.Equal(points || placements != ManualHistoricalPlacements.None, form.CanCorrect);
        if (form.CanCorrect)
            form.ValidateCorrection(values, Snapshot());
        else
            Assert.Throws<ArgumentException>(() => form.ValidateCorrection(values, Snapshot()));
    }

    [Theory]
    [InlineData(false, ManualHistoricalPlacements.Winners)]
    [InlineData(true, ManualHistoricalPlacements.Full)]
    public void AllFirstPlaces_UseOnlyOriginalCapturePlacementsToDisambiguate(bool full, ManualHistoricalPlacements expected)
    {
        ManualScoreInput[] values = Values([null, null, null], [1, 1, 1]);
        ScoringRulesSnapshot snapshot = Snapshot(full);
        ManualHistoricalResult form = ManualHistoricalResult.FromValues(values, snapshot);
        Assert.Equal(expected, form.Placements);
        form.ValidateCorrection(values, snapshot);
        ManualScoreInput[] changed = values.Select((value, index) => value with { Placement = index == 0 ? 1 : full ? index + 1 : null }).ToArray();
        form.ValidateCorrection(changed, snapshot);
        Assert.Equal(expected, ManualHistoricalResult.FromValues(changed, snapshot).Placements);
    }

    [Fact]
    public void Corrections_CannotAddRemoveOrConvertRecordedResultKinds()
    {
        ScoringRulesSnapshot snapshot = Snapshot();
        ManualScoreInput[] winners = Values([null, null], [1, null]);
        ManualHistoricalResult form = ManualHistoricalResult.FromValues(winners, snapshot);
        Assert.Throws<ArgumentException>(() => form.ValidateCorrection(winners.Select(player => player with { Points = 0 }).ToArray(), snapshot));
        Assert.Throws<ArgumentException>(() => form.ValidateCorrection(winners.Select(player => player with { Placement = null }).ToArray(), snapshot));
        Assert.Throws<ArgumentException>(() => form.ValidateCorrection([winners[0], winners[1] with { Placement = 2 }], snapshot));
        ManualScoreInput[] full = Values([0, -1, 4], [1, 2, 3]);
        ManualHistoricalResult complete = ManualHistoricalResult.FromValues(full, snapshot);
        Assert.Throws<ArgumentException>(() => complete.ValidateCorrection([full[0], full[1] with { Points = null }, full[2]], snapshot));
        Assert.Throws<ArgumentException>(() => complete.ValidateCorrection(full.Select(player => player with { Placement = 1 }).ToArray(), snapshot));
        // An explicit all-winners result cannot turn into full placements on reload either.
        ScoringRulesSnapshot fullSnapshot = Snapshot(true);
        ManualHistoricalResult winnerForm = ManualHistoricalResult.FromValues(winners, fullSnapshot);
        Assert.Throws<ArgumentException>(() => winnerForm.ValidateCorrection(winners.Select(player => player with { Placement = 1 }).ToArray(), fullSnapshot));
    }

    [Fact]
    public void HistoricalRanks_AcceptSharedRanksAndGapsWithoutDensifyingOrApplyingNewSessionRules()
    {
        ManualScoreInput[] values = Values([0, -1, 4, 8], [1, 1, 3, 4]);
        ManualHistoricalResult form = ManualHistoricalResult.FromValues(values, Snapshot());
        form.ValidateCorrection(values, Snapshot());
        form.ValidateCorrection([values[0], values[1] with { Placement = 3 }, values[2], values[3]], Snapshot());
        Assert.Throws<ArgumentException>(() => form.ValidateCorrection([values[0], values[1] with { Placement = null }, values[2], values[3]], Snapshot()));
        Assert.Throws<ArgumentException>(() => form.ValidateCorrection([values[0], values[1] with { Placement = 5 }, values[2], values[3]], Snapshot()));
    }

    [Fact]
    public void InvalidHistoricalData_IsNotInterpretedAsAnotherResultKind()
    {
        Assert.Throws<ArgumentException>(() => ManualHistoricalResult.FromValues(Values([0, null], [null, null]), Snapshot()));
        Assert.Throws<ArgumentException>(() => ManualHistoricalResult.FromValues(Values([null, null], [2, null]), Snapshot()));
        Assert.Throws<ArgumentException>(() => ManualHistoricalResult.FromValues(Values([null, null], [2, 2]), Snapshot()));
    }

    [Fact]
    public void CorrectionTimestamp_ChangesOnlyModifiedAtOnCompletedSessions()
    {
        DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(-2);
        GameSession session = new(Guid.NewGuid(), Guid.NewGuid(), start, [Guid.NewGuid()]);
        Assert.Throws<InvalidOperationException>(() => session.RecordHistoricalCorrection(start));
        session.BeginScoring(start.AddHours(1));
        Assert.Throws<InvalidOperationException>(() => session.RecordHistoricalCorrection(start.AddHours(1)));
        session.Complete(start.AddMinutes(90));
        session.RecordHistoricalCorrection(start.AddHours(2));
        Assert.Equal(GameSessionStatus.Completed, session.Status);
        Assert.Equal(start, session.StartedAt);
        Assert.Equal(start.AddHours(1), session.EndedAt);
        Assert.Equal(start.AddHours(2), session.ModifiedAt);
    }
}

using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;
using BoardgameTracker.App.Games.ArkNova.PhotoRecognition;
using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

/// <summary>Both user acquisition routes invoke exactly the same analyzer. Cancellation produces no proposal.</summary>
public sealed class ArkNovaPhotoReviewFlow(IArkNovaPhotoInput input, ArkNovaPhotoRecognition recognition)
{
    public async Task<ArkNovaPhotoRecognitionResult?> AnalyzeAsync(ArkNovaPhotoSource source, CancellationToken cancellationToken = default)
    {
        byte[]? image = await input.ReadAsync(source, cancellationToken);
        return image is null ? null : await recognition.AnalyzeAsync(image, cancellationToken);
    }
}

/// <summary>Isolated editable proposal. It has no autosave, database, or live manual-scoring bindings.</summary>
public partial class ArkNovaPhotoReviewPlayer : ObservableObject
{
    public Guid PlayerId { get; }
    public string Name { get; }
    public bool ShowReputation { get; }
    private readonly int? _previousReputation;
    [ObservableProperty] public partial string AttractivenessText { get; set; }
    [ObservableProperty] public partial string ConservationText { get; set; }
    [ObservableProperty] public partial string ReputationText { get; set; }
    public string AttractivenessConfidence { get; }
    public string ConservationConfidence { get; }
    public string ReputationConfidence { get; }
    public string AttractivenessStatus => Status(AttractivenessConfidence);
    public string ConservationStatus => Status(ConservationConfidence);
    public string ReputationStatus => Status(ReputationConfidence);

    public ArkNovaPhotoReviewPlayer(Guid playerId, string name, int? attractiveness, int? conservation, int? reputation,
        bool showReputation, ColorScores? proposal)
    {
        PlayerId = playerId;
        Name = name;
        ShowReputation = showReputation;
        _previousReputation = reputation;
        FieldPrediction? a = proposal?.Tracks.GetValueOrDefault("attractiveness");
        FieldPrediction? c = proposal?.Tracks.GetValueOrDefault("conservation");
        FieldPrediction? r = proposal?.Tracks.GetValueOrDefault("reputation");
        AttractivenessText = (a?.Value ?? attractiveness)?.ToString() ?? string.Empty;
        ConservationText = (c?.Value ?? conservation)?.ToString() ?? string.Empty;
        ReputationText = (r?.Value ?? reputation)?.ToString() ?? string.Empty;
        AttractivenessConfidence = Confidence(a);
        ConservationConfidence = Confidence(c);
        ReputationConfidence = Confidence(r);
    }

    public bool TryRead(out ArkNovaConfirmedPhotoScore? score)
    {
        score = null;
        if (!Read(AttractivenessText, 0, 113, out int? a) || !Read(ConservationText, 0, 41, out int? c)
            || !Read(ReputationText, 1, 15, out int? r))
            return false;
        score = new(PlayerId, a, c, ShowReputation ? r : _previousReputation);
        return true;
    }
    private static bool Read(string text, int min, int max, out int? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (!int.TryParse(text, out int value) || value < min || value > max)
            return false;
        result = value;
        return true;
    }
    private static string Confidence(FieldPrediction? value) => value?.Value is not null && value.Confidence is "High" or "Medium"
        ? value.Confidence : "Uncertain";
    private static string Status(string confidence) => confidence switch
    {
        "High" => AppResources.PhotoConfidenceHigh,
        "Medium" => AppResources.PhotoConfidenceMedium,
        _ => AppResources.PhotoConfidenceUncertain
    };
}

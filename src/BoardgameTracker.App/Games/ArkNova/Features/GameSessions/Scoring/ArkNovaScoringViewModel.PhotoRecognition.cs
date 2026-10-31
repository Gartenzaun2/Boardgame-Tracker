using System.Collections.ObjectModel;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;
using BoardgameTracker.App.Games.ArkNova.PhotoRecognition;
using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaScoringViewModel
{
    public ObservableCollection<ArkNovaPhotoReviewPlayer> PhotoPlayers { get; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScoringContentEnabled))]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    public partial bool IsPhotoSourceVisible { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScoringContentEnabled))]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    public partial bool IsPhotoReviewVisible { get; private set; }
    [ObservableProperty] public partial string? PhotoError { get; private set; }

    public void OpenPhotoSource()
    {
        if (IsScoringContentEnabled && IsLiveScoring)
            IsPhotoSourceVisible = true;
    }
    public void CancelPhotoReview()
    {
        if (!IsIdle)
            return;
        IsPhotoSourceVisible = false;
        IsPhotoReviewVisible = false;
        PhotoPlayers.Clear();
        PhotoError = null;
    }
    public async Task AnalyzePhotoAsync(ArkNovaPhotoSource source, CancellationToken cancellationToken = default)
    {
        if (!IsIdle || !IsLiveScoring || !IsPhotoSourceVisible)
            return;
        IsPhotoSourceVisible = false;
        IsBusy = true;
        ErrorMessage = null;
        PhotoError = null;
        try
        {
            ArkNovaPhotoRecognitionResult? result = await photoReview.AnalyzeAsync(source, cancellationToken);
            if (result is null)
                return;
            if (!result.Registration.Success)
            {
                ErrorMessage = AppResources.PhotoBoardNotRecognized;
                return;
            }
            PhotoPlayers.Clear();
            foreach (ArkNovaScoringPlayerViewModel player in Players)
            {
                ColorScores? scores = result.Colors.GetValueOrDefault(player.Color.ToString().ToLowerInvariant());
                PhotoPlayers.Add(new(player.PlayerId, player.Name, player.Attractiveness, player.Conservation, player.Reputation,
                    player.ShowReputation, scores));
            }
            IsPhotoReviewVisible = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ark Nova photo acquisition/analysis failed for {GameSessionId}.", GameSessionId);
            ErrorMessage = AppResources.PhotoAnalysisFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ConfirmPhotoReviewAsync(CancellationToken cancellationToken = default)
    {
        if (!IsIdle || !IsLiveScoring || !IsPhotoReviewVisible)
            return;
        List<ArkNovaConfirmedPhotoScore> confirmed = [];
        foreach (ArkNovaPhotoReviewPlayer player in PhotoPlayers)
        {
            if (!player.TryRead(out ArkNovaConfirmedPhotoScore? score))
            {
                PhotoError = AppResources.PhotoValuesInvalid;
                return;
            }
            confirmed.Add(score!);
        }
        IsSaving = true;
        PhotoError = null;
        try
        {
            await scoringUseCase.SaveConfirmedPhotoScoresAsync(GameSessionId, confirmed, cancellationToken);
            IsPhotoReviewVisible = false;
            PhotoPlayers.Clear();
            await LoadAsync(GameSessionId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ark Nova confirmed photo scores could not be saved.");
            PhotoError = AppResources.CorrectionFailed;
        }
        finally
        {
            IsSaving = false;
        }
    }
}

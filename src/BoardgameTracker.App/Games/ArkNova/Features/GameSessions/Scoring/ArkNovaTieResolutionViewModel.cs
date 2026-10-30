using System.Collections.ObjectModel;
using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaTieResolutionViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    ArkNovaCorrectionUseCase correction) : ObservableObject
{
    public ArkNovaCorrectionDraft? Draft { get; set; }
    private readonly Dictionary<Guid, int> _scores = [];
    private readonly Dictionary<Guid, int> _placements = [];
    private readonly Dictionary<Guid, string> _names = [];
    private readonly Dictionary<Guid, PlayerColor?> _colors = [];
    private IReadOnlyList<ArkNovaTieBlock> _tieBlocks = [];
    private bool _loaded;
    private bool _completed;

    public ObservableCollection<ArkNovaPlacementPlayerViewModel> Players { get; } = [];
    public ObservableCollection<ArkNovaPlacementGroupViewModel> PlacementGroups { get; } = [];
    [ObservableProperty] public partial Guid GameSessionId { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFinish))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFinish))]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string? ValidationMessage { get; private set; }
    public bool HasValidationMessage => ValidationMessage is not null;
    public bool CanFinish => !IsBusy && !_completed && Players.Count > 0 && !HasValidationMessage;

    public async Task LoadAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        if (IsBusy || (_loaded && GameSessionId == gameSessionId))
            return;

        IsBusy = true;
        try
        {
            ArkNovaScoringGameSessionData gameSession = Draft?.Data ?? await scoringUseCase.LoadAsync(gameSessionId, cancellationToken)
                ?? throw new InvalidOperationException("The scoring GameSession does not exist.");
            if (Draft is null && gameSession.Status == GameSessionStatus.Completed)
            {
                Draft = await correction.BeginAsync(gameSessionId, cancellationToken);
                gameSession = Draft.Data;
            }
            ArkNovaCompletionPlan plan = Draft is not null
                ? correction.Prepare(Draft)
                : await scoringUseCase.PrepareCompletionAsync(gameSessionId, cancellationToken);

            GameSessionId = gameSessionId;
            _scores.Clear();
            _placements.Clear();
            _names.Clear();
            _colors.Clear();
            foreach (ArkNovaScoringPlayerData player in gameSession.Players)
            {
                _scores[player.PlayerId] = plan.Scores[player.PlayerId];
                _placements[player.PlayerId] = plan.DefaultPlacements[player.PlayerId];
                _names[player.PlayerId] = player.Name;
                _colors[player.PlayerId] = player.Color;
            }

            _tieBlocks = ArkNovaTieBlock.Create(_scores, _placements);
            foreach (ArkNovaTieBlock block in _tieBlocks)
                CopyPlacements(block);

            _completed = false;
            RefreshPlayers();
            _loaded = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectPlacement(Guid playerId, int placement)
    {
        if (IsBusy || _completed)
            return;

        ArkNovaTieBlock? block = _tieBlocks.FirstOrDefault(candidate => candidate.PlayerIds.Contains(playerId));
        if (block is null || !block.AvailablePlacements.Contains(placement))
            return;

        block.SelectPlacement(playerId, placement);
        CopyPlacements(block);
        RefreshPlayers();
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (!CanFinish)
            return;

        IsBusy = true;
        try
        {
            ArkNovaPlacementRules.ValidateResolution(_scores, _placements);
            if (Draft is not null)
                await correction.SaveAsync(Draft, _placements, cancellationToken);
            else
                await scoringUseCase.CompleteAsync(GameSessionId, _placements, DateTimeOffset.UtcNow, cancellationToken);

            _completed = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CopyPlacements(ArkNovaTieBlock block)
    {
        foreach (KeyValuePair<Guid, int> player in block.Placements)
            _placements[player.Key] = player.Value;
    }

    private void RefreshPlayers()
    {
        Players.Clear();
        foreach (Guid playerId in _scores.Keys.OrderByDescending(id => _scores[id]))
        {
            ArkNovaTieBlock? block = _tieBlocks.FirstOrDefault(candidate => candidate.PlayerIds.Contains(playerId));
            ArkNovaPlacementOptionViewModel[] options = block is null ? [] : block.AvailablePlacements
                .Select(placement => new ArkNovaPlacementOptionViewModel(playerId, placement, placement == _placements[playerId])).ToArray();
            Players.Add(new(playerId, _names[playerId], _scores[playerId], _placements[playerId], options, _colors[playerId]));
        }
        PlacementGroups.Clear();
        for (int placement = 1; placement <= Players.Count; placement++)
            PlacementGroups.Add(new(placement, Players.Where(player => player.Placement == placement).ToArray()));
        try
        {
            ArkNovaPlacementRules.ValidateResolution(_scores, _placements);
            ValidationMessage = null;
        }
        catch (ArgumentException)
        {
            ValidationMessage = AppResources.TieInvalid;
        }
        OnPropertyChanged(nameof(CanFinish));
    }
}

public sealed record ArkNovaPlacementPlayerViewModel(
    Guid PlayerId, string Name, int Score, int Placement, IReadOnlyList<ArkNovaPlacementOptionViewModel> PlacementOptions, PlayerColor? Color)
{
    public bool HasColor => Color is not null;
    public PlayerColorStyle? ColorStyle => Color is PlayerColor color ? PlayerColorPalette.Get(color) : null;
    public string ScoreText => string.Format(AppResources.PointsFormat, Score);
    public bool HasPlacementOptions => PlacementOptions.Count > 0;
}

public sealed record ArkNovaPlacementGroupViewModel(int Placement, IReadOnlyList<ArkNovaPlacementPlayerViewModel> Players)
{
    public string Title => string.Format(AppResources.PlaceFormat, Placement);
}

public sealed record ArkNovaPlacementOptionViewModel(Guid PlayerId, int Placement, bool IsSelected)
{
    public string Text => Placement.ToString();
    public string Description => string.Format(AppResources.PlaceFormat, Placement);
}

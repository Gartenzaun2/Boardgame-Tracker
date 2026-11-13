using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.Settings;

public partial class ManualGameEditorViewModel(ManualGameUseCase useCase, GameActivationUseCase activation,
    ILogger<ManualGameEditorViewModel> logger) : ObservableObject
{
    private Guid? _gameId;
    private string? _gameKey;
    private bool _loaded;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    public partial bool IsChoosing { get; set; }
    public bool CanInteract => !IsBusy && !IsChoosing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(CanEditMaximum))]
    [NotifyPropertyChangedFor(nameof(HeaderBadge))]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    public partial bool IsEditing { get; private set; }
    public bool CanEditMaximum => !IsEditing;
    public string Title => IsEditing ? AppResources.Settings : AppResources.CreateManualGame;
    public string HeaderBadge => IsEditing ? BadgeText : string.Empty;
    [ObservableProperty] public partial string GameName { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderBadge))]
    public partial string BadgeText { get; set; } = string.Empty;
    [ObservableProperty] public partial string MinimumPlayers { get; set; } = "1";
    [ObservableProperty] public partial string MaximumPlayers { get; set; } = "4";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDeriveResult))]
    public partial bool CapturePoints { get; set; }
    [ObservableProperty] public partial bool AllowNegativePoints { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDeriveResult))]
    public partial bool CaptureWinner { get; set; }
    [ObservableProperty] public partial bool CapturePlacements { get; set; }
    [ObservableProperty] public partial bool AllowTies { get; set; }
    [ObservableProperty] public partial bool CapturePlayerOrder { get; set; }
    [ObservableProperty] public partial bool CapturePlayerColors { get; set; }
    public bool CanDeriveResult => CapturePoints && CaptureWinner;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultModeText))]
    public partial int ResultMode { get; set; }
    public string ResultModeText => ResultChoices[Math.Clamp(ResultMode, 0, 2)];
    public IReadOnlyList<string> ResultChoices => [AppResources.ManualResult, AppResources.HighestPointsWin, AppResources.LowestPointsWin];
    [ObservableProperty] public partial bool IsVisibleGame { get; private set; } = true;
    [ObservableProperty] public partial bool VisibilityLocked { get; private set; }
    public bool CanChangeVisibility => !VisibilityLocked;
    partial void OnVisibilityLockedChanged(bool value) => OnPropertyChanged(nameof(CanChangeVisibility));
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    public partial bool HasSessions { get; private set; }
    public bool CanDelete => IsEditing && !HasSessions;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string? DeletionWarning { get; private set; }

    public async Task LoadAsync(string? gameKey, CancellationToken cancellationToken = default)
    {
        if (_loaded || IsBusy)
            return;
        if (gameKey is null)
        {
            _loaded = true;
            return;
        }
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            ManualGameDetails details = await useCase.GetAsync(gameKey, cancellationToken)
                ?? throw new ManualGameValidationException(ManualGameError.NotFound);
            _gameId = details.Id;
            _gameKey = details.Key;
            IsEditing = true;
            GameName = details.Name;
            BadgeText = details.BadgeText;
            MinimumPlayers = details.MinPlayers.ToString();
            MaximumPlayers = details.MaxPlayers.ToString();
            CapturePoints = details.Options.CapturePoints;
            AllowNegativePoints = details.Options.AllowNegativePoints;
            CaptureWinner = details.Options.CaptureWinner;
            CapturePlacements = details.Options.CapturePlacements;
            AllowTies = details.Options.AllowTies;
            CapturePlayerOrder = details.Options.CapturePlayerOrder;
            CapturePlayerColors = details.Options.CapturePlayerColors;
            ResultMode = details.Options.ResultFromPoints ? details.Options.PointsDirection == PointsDirection.Highest ? 1 : 2 : 0;
            HasSessions = details.HasSessions;
            GameActivationState visibility = (await activation.GetGamesAsync(cancellationToken)).Single(game => game.GameKey == gameKey);
            IsVisibleGame = visibility.IsEnabled;
            VisibilityLocked = visibility.HasOpenSessions;
            _loaded = true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load manual game settings.");
            ErrorMessage = AppResources.ManualLoadFailed;
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (!CanInteract || !_loaded)
            return false;
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(GameName) || GameName.Trim().Length > 100 || string.IsNullOrWhiteSpace(BadgeText)
            || !int.TryParse(MinimumPlayers, out int min) || !int.TryParse(MaximumPlayers, out int max)
            || min < 1 || max < min || max > 12)
        {
            ErrorMessage = AppResources.ManualFieldsInvalid;
            return false;
        }
        IsBusy = true;
        try
        {
            ManualGameOptions options = new(CapturePoints, AllowNegativePoints, CaptureWinner, CapturePlacements, AllowTies,
                ResultMode != 0, ResultMode == 1 ? PointsDirection.Highest : ResultMode == 2 ? PointsDirection.Lowest : null,
                CapturePlayerOrder, CapturePlayerColors);
            _gameId = await useCase.SaveAsync(new(_gameId, GameName, BadgeText, min, max, options), cancellationToken);
            return true;
        }
        catch (ManualGameValidationException exception)
        {
            ErrorMessage = ValidationMessage(exception.Error);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save manual game.");
            ErrorMessage = AppResources.ManualSaveFailed;
            return false;
        }
        finally { IsBusy = false; }
    }

    public async Task SetVisibilityAsync(bool visible, CancellationToken cancellationToken = default)
    {
        if (!CanInteract || _gameKey is null || IsVisibleGame == visible)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            GameActivationState state = await activation.SetEnabledAsync(_gameKey, visible, cancellationToken);
            IsVisibleGame = state.IsEnabled;
            VisibilityLocked = state.HasOpenSessions;
        }
        catch (GameActivationBlockedException)
        {
            IsVisibleGame = true;
            VisibilityLocked = true;
            ErrorMessage = AppResources.GameDisableBlocked;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not change manual game visibility.");
            ErrorMessage = AppResources.GameActivationSaveFailed;
        }
        finally
        {
            OnPropertyChanged(nameof(IsVisibleGame));
            IsBusy = false;
        }
    }

    public async Task<bool> RefreshDeleteEligibilityAsync(CancellationToken cancellationToken = default)
    {
        if (!CanInteract || _gameKey is null)
            return false;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            ManualGameDetails? details = await useCase.GetAsync(_gameKey, cancellationToken);
            if (details is null)
                return false;
            HasSessions = details.HasSessions;
            return CanDelete;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not check manual game deletion.");
            ErrorMessage = AppResources.ManualDeleteFailed;
            return false;
        }
        finally { IsBusy = false; }
    }

    public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || _gameId is not Guid id)
            return false;
        IsBusy = true;
        ErrorMessage = null;
        DeletionWarning = null;
        try
        {
            await useCase.DeleteAsync(id, cancellationToken);
            return true;
        }
        catch (ManualGamePreferenceCleanupException exception)
        {
            logger.LogWarning(exception, "Manual game {GameId} was deleted, but visibility cleanup failed.", exception.GameId);
            DeletionWarning = AppResources.ManualPreferenceCleanupFailed;
            return true;
        }
        catch (ManualGameValidationException exception)
        {
            if (exception.Error == ManualGameError.HasSessions)
                HasSessions = true;
            ErrorMessage = ValidationMessage(exception.Error);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not delete manual game.");
            ErrorMessage = AppResources.ManualDeleteFailed;
            return false;
        }
        finally { IsBusy = false; }
    }

    private static string ValidationMessage(ManualGameError error) => error switch
    {
        ManualGameError.DuplicateName => AppResources.ManualDuplicateName,
        ManualGameError.MaximumLocked => AppResources.ManualMaximumLocked,
        ManualGameError.AssignedGroups => AppResources.ManualAssignedGroups,
        ManualGameError.HasSessions => AppResources.ManualDeleteBlocked,
        ManualGameError.BadgeTooLong => AppResources.ManualBadgeTooLong,
        _ => AppResources.ManualLoadFailed
    };
}

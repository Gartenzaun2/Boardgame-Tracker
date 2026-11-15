using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Players;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.EditPlayer;

public partial class EditPlayerViewModel : ObservableObject {
    private readonly EditPlayerUseCase _useCase;
    private readonly CreatePlayerUseCase _createPlayer;
    private readonly ILogger<EditPlayerViewModel> _logger;

    public EditPlayerViewModel(EditPlayerUseCase useCase, CreatePlayerUseCase createPlayer, ILogger<EditPlayerViewModel> logger) {
        _useCase = useCase;
        _createPlayer = createPlayer;
        _logger = logger;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    public partial bool IsBusy { get; private set; }

    /// <summary>
    /// Set to true after a successful save to preserve the completed state if navigation fails.
    /// This prevents further editing or saving while the dialog remains open,
    /// allowing the user to retry leaving without modifying the saved player.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit), nameof(SaveText))]
    public partial bool IsSaveCompleted { get; private set; }

    public bool CanEdit => !IsBusy && !IsSaveCompleted;

    [ObservableProperty]
    public partial Guid PlayerId { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsNew { get; private set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsGuest { get; set; }

    [ObservableProperty]
    public partial bool IsArchived { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => ErrorMessage is not null;

    #region Localized UI Text

    public string Title => IsNew ? AppResources.AddPlayer : AppResources.EditPlayer;
    public string SaveText => IsSaveCompleted ? AppResources.Done : AppResources.Save;

    #endregion

    public void InitializeNew() {
        PlayerId = Guid.Empty;
        IsSaveCompleted = false;
        Name = string.Empty;
        IsGuest = false;
        IsArchived = false;
        ErrorMessage = null;
        IsNew = true;
    }

    public async Task LoadAsync(Guid playerId, CancellationToken cancellationToken = default) {
        if (IsBusy || PlayerId == playerId)
            return;

        IsBusy = true;
        try {
            Player player = await _useCase.GetAsync(playerId, cancellationToken)
                ?? throw new InvalidOperationException("The player does not exist.");
            Name = player.Name;
            IsArchived = player.IsArchived;
            PlayerId = playerId;
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Could not load player for editing.");
            ErrorMessage = AppResources.PlayersLoadFailed;
        }
        finally {
            IsBusy = false;
        }
    }

    /// <returns>True after a new player was saved and the form reset; false for an existing player or when saving was blocked or failed.</returns>
    public async Task<bool> SaveAndAddAnotherAsync(CancellationToken cancellationToken = default) {
        if (!IsNew || !await SaveAsync(cancellationToken))
            return false;

        InitializeNew();
        return true;
    }

    /// <returns>
    /// True if the player was saved successfully or a previous save already completed;
    /// false when busy, input or editor state is invalid, or persistence fails.
    /// </returns>
    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default) {
        if (IsSaveCompleted)
            return true;
        if (IsBusy)
            return false;

        if (!IsNew && PlayerId == Guid.Empty) {
            ErrorMessage = AppResources.PlayerSaveFailed;
            _logger.LogError("Could not save an existing player because the editor has no player ID.");
            return false;
        }

        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name)) {
            ErrorMessage = AppResources.PlayerNameRequired;
            return false;
        }
        if (Name.Trim().Length > 100) {
            ErrorMessage = AppResources.PlayerNameTooLong;
            return false;
        }

        IsBusy = true;
        try {
            if (IsNew) {
                CreatePlayerResult result = await _createPlayer.CreatePlayerAsync(Name, IsGuest, cancellationToken);
                PlayerId = result.Id;
            }
            else
                await _useCase.UpdatePlayerAsync(PlayerId, Name, IsArchived, cancellationToken);

            IsSaveCompleted = true;
            return true;
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Could not save player.");
            ErrorMessage = AppResources.PlayerSaveFailed;
            return false;
        }
        finally {
            IsBusy = false;
        }
    }
}

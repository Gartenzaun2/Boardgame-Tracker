using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Players;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.EditPlayer;

public partial class EditPlayerViewModel : ObservableObject {
    private readonly EditPlayerUseCase _editPlayerUseCase;
    private readonly CreatePlayerUseCase _createPlayerUseCase;
    private readonly INavigationService _navigationService;
    private readonly ILogger<EditPlayerViewModel> _logger;

    public EditPlayerViewModel(EditPlayerUseCase editPlayerUseCase, CreatePlayerUseCase createPlayerUseCase, INavigationService navigationService, ILogger<EditPlayerViewModel> logger) {
        _editPlayerUseCase = editPlayerUseCase;
        _createPlayerUseCase = createPlayerUseCase;
        _navigationService = navigationService;
        _logger = logger;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(SaveAndAddAnotherCommand), nameof(GoBackCommand))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    public partial bool IsBusy { get; private set; }
    private bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Set to true after a successful save to preserve the completed state if navigation fails.
    /// This prevents further editing or saving while the dialog remains open,
    /// allowing the user to retry leaving without modifying the saved player.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit), nameof(SaveText))]
    public partial bool IsSaveCompleted { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyCanExecuteChangedFor(nameof(SaveAndAddAnotherCommand))]
    public partial bool IsCreateMode { get; private set; }

    public bool CanEdit => IsNotBusy && !IsSaveCompleted;
    public bool CanSaveAndAddAnother => IsNotBusy && IsCreateMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => ErrorMessage is not null;

    [ObservableProperty]
    public partial Guid PlayerId { get; private set; }

    [ObservableProperty]
    public partial string PlayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPlayerGuest { get; set; }

    [ObservableProperty]
    public partial bool IsPlayerArchived { get; set; }

    #region Localized UI Text

    public string Title => IsCreateMode ? AppResources.AddPlayer : AppResources.EditPlayer;
    public string SaveText => IsSaveCompleted ? AppResources.Done : AppResources.Save;

    #endregion

    public void InitializeNew() {
        if (!IsNotBusy)
            return;

        ResetNewPlayer();
    }

    private void ResetNewPlayer() {
        PlayerId = Guid.Empty;
        IsSaveCompleted = false;
        PlayerName = string.Empty;
        IsPlayerGuest = false;
        IsPlayerArchived = false;
        ErrorMessage = null;
        IsCreateMode = true;
    }

    public async Task LoadAsync(Guid playerId, CancellationToken cancellationToken = default) {
        if (!IsNotBusy || PlayerId == playerId)
            return;

        try {
            IsBusy = true;
            ErrorMessage = null;

            Player player = await _editPlayerUseCase.GetAsync(playerId, cancellationToken) ?? throw new InvalidOperationException("The player does not exist.");
            PlayerName = player.Name;
            IsPlayerArchived = player.IsArchived;
            IsCreateMode = false;
            IsSaveCompleted = false;
            PlayerId = playerId;
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load player for editing.");
            ErrorMessage = AppResources.PlayersLoadFailed;
        } finally {
            IsBusy = false;
        }
    }

    /// <returns>True after a new player was saved and the form reset; false for an existing player or when saving was blocked or failed.</returns>
    [RelayCommand(CanExecute = nameof(CanSaveAndAddAnother))]
    public async Task<bool> SaveAndAddAnotherAsync(CancellationToken cancellationToken = default) {
        if (!CanSaveAndAddAnother)
            return false;

        try {
            IsBusy = true;
            ErrorMessage = null;

            if (!IsSaveCompleted && !await SavePlayerAsync(cancellationToken))
                return false;

            ResetNewPlayer();
            return true;
        } finally {
            IsBusy = false;
        }
    }

    /// <returns>
    /// True after saving and navigation succeed; false when busy, validation,
    /// persistence or navigation fails. A navigation retry skips the completed write.
    /// </returns>
    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default) {
        if (!IsNotBusy)
            return false;

        try {
            IsBusy = true;
            ErrorMessage = null;

            if (!IsSaveCompleted && !await SavePlayerAsync(cancellationToken))
                return false;

            return await NavigateFromEditorAsync();
        } finally {
            IsBusy = false;
        }
    }

    /// <returns>True only after persistence succeeds; false for invalid input, invalid editor state or a failed write.</returns>
    private async Task<bool> SavePlayerAsync(CancellationToken cancellationToken) {
        if (!IsCreateMode && PlayerId == Guid.Empty) {
            ErrorMessage = AppResources.PlayerSaveFailed;
            _logger.LogError("Could not save an existing player because the editor has no player ID.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(PlayerName)) {
            ErrorMessage = AppResources.PlayerNameRequired;
            return false;
        }
        if (PlayerName.Trim().Length > 100) {
            ErrorMessage = AppResources.PlayerNameTooLong;
            return false;
        }

        try {
            if (IsCreateMode) {
                CreatePlayerResult result = await _createPlayerUseCase.CreatePlayerAsync(PlayerName, IsPlayerGuest, cancellationToken);
                PlayerId = result.Id;
            } else
                await _editPlayerUseCase.UpdatePlayerAsync(PlayerId, PlayerName, IsPlayerArchived, cancellationToken);

            IsSaveCompleted = true;
            return true;
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not save player.");
            ErrorMessage = AppResources.PlayerSaveFailed;
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task GoBackAsync() {
        if (!IsNotBusy)
            return;

        try {
            IsBusy = true;
            ErrorMessage = null;

            await NavigateFromEditorAsync();
        } finally {
            IsBusy = false;
        }
    }

    /// <returns>True when navigation succeeds; otherwise false with an inline error.</returns>
    private async Task<bool> NavigateFromEditorAsync() {
        try {
            if (IsSaveCompleted && IsCreateMode)
                await _navigationService.NavigateToMainSectionAsync(AppMainSection.Participants);
            else
                await _navigationService.GoBackAsync();

            return true;
        } catch (Exception exception) when (IsSaveCompleted) {
            _logger.LogError(exception, "Could not leave player editor after saving.");
            ErrorMessage = AppResources.PlayerEditorNavigationFailed;
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not leave player editor.");
            ErrorMessage = AppResources.NavigationFailed;
        }
        return false;
    }
}

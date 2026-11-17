using System.Collections.ObjectModel;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.MyGames;

public partial class MyGamesViewModel : ObservableObject {
    private readonly GamesQuery _gamesQuery;
    private readonly SetGameEnabledUseCase _setGameEnabledUseCase;
    private readonly IEnumerable<IGameSettingsNavigation> _settings;
    private readonly INavigationService _navigationService;
    private readonly ILogger<MyGamesViewModel> _logger;

    public MyGamesViewModel(GamesQuery gamesQuery, SetGameEnabledUseCase setGameEnabledUseCase, IEnumerable<IGameSettingsNavigation> settings, INavigationService navigationService, ILogger<MyGamesViewModel> logger) {
        _gamesQuery = gamesQuery;
        _setGameEnabledUseCase = setGameEnabledUseCase;
        _settings = settings;
        _navigationService = navigationService;
        _logger = logger;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    public partial bool IsBusy { get; private set; }
    private bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ObservableCollection<GameSettingsRow> Games { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default) {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try {
            IReadOnlyList<GameInfo> games = await _gamesQuery.GetAllAsync(cancellationToken);
            Games.Clear();
            foreach (GameInfo game in games) {
                GameEnabledState state = new(game.Key, game.IsEnabled, game.HasOpenSessions, game.Name);
                Games.Add(new(game.Name, state, _settings.SingleOrDefault(entry => entry.CanHandle(game.Key))?.KindText));
            }
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not load games.");
            ErrorMessage = AppResources.MyGamesLoadFailed;
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync(GameSettingsRow? row) {
        if (!IsNotBusy || row is null || !Games.Contains(row))
            return;

        IsBusy = true;
        ErrorMessage = null;

        try {
            IGameSettingsNavigation? settings = _settings.SingleOrDefault(entry => entry.CanHandle(row.GameKey));
            if (settings is null)
                ErrorMessage = AppResources.GameSettingsUnavailable;
            else
                await settings.OpenAsync(row.GameKey);
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not open game settings.");
            ErrorMessage = AppResources.NavigationFailed;
        } finally {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task GoBackAsync() {
        if (!IsNotBusy)
            return;

        IsBusy = true;
        ErrorMessage = null;

        try {
            await _navigationService.GoBackAsync();
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not leave game management.");
            ErrorMessage = AppResources.NavigationFailed;
        } finally {
            IsBusy = false;
        }
    }

    public async Task SetEnabledAsync(GameSettingsRow row, bool enabled, CancellationToken cancellationToken = default) {
        if (row.IsEnabled == enabled)
            return;
        if (IsBusy || !Games.Contains(row) || !row.CanToggle) {
            row.RefreshBindings();
            return;
        }
        IsBusy = true;
        ErrorMessage = null;

        try {
            row.Apply(await _setGameEnabledUseCase.SetEnabledAsync(row.GameKey, enabled, cancellationToken));
        } catch (GameEnablementBlockedException) {
            row.Apply(new(row.GameKey, true, true, row.Name));
            ErrorMessage = AppResources.GameDisableBlocked;
        } catch (Exception exception) {
            _logger.LogError(exception, "Could not change enablement for game {GameKey}.", row.GameKey);
            ErrorMessage = AppResources.GameActivationSaveFailed;
        } finally {
            row.RefreshBindings();
            IsBusy = false;
        }
    }
}

public sealed class GameSettingsRow(string name, GameEnabledState state, string? kindText = null) : ObservableObject {
    private GameEnabledState _state = state;

    public string? KindText { get; } = kindText;
    public bool HasKind => !string.IsNullOrWhiteSpace(KindText);
    public string GameKey => _state.GameKey;
    public string Name { get; } = name;
    public bool IsEnabled => _state.IsEnabled;
    public bool IsLocked => _state.HasOpenSessions;
    public bool CanToggle => !IsLocked;
    #region Localized UI Text

    public string StateText => IsEnabled ? AppResources.GameEnabled : AppResources.GameDisabled;

    #endregion

    public void Apply(GameEnabledState state) {
        _state = state;
        RefreshBindings();
    }

    public void RefreshBindings() {
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(CanToggle));
        OnPropertyChanged(nameof(StateText));
    }
}

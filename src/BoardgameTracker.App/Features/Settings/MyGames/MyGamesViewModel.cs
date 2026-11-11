using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Settings.MyGames;

public partial class MyGamesViewModel(GameActivationUseCase useCase,
    ILogger<MyGamesViewModel> logger) : ObservableObject
{
    public ObservableCollection<GameActivationRow> Games { get; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool IsIdle => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            IReadOnlyList<GameActivationState> states = await useCase.GetGamesAsync(cancellationToken);
            Games.Clear();
            foreach (GameActivationState state in states)
            {
                Games.Add(new(state.Name, state));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load game activation settings.");
            ErrorMessage = AppResources.MyGamesLoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SetEnabledAsync(GameActivationRow row, bool enabled, CancellationToken cancellationToken = default)
    {
        if (row.IsEnabled == enabled)
            return;
        if (IsBusy || !Games.Contains(row) || !row.CanToggle)
        {
            row.RefreshBindings();
            return;
        }
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            row.Apply(await useCase.SetEnabledAsync(row.GameKey, enabled, cancellationToken));
        }
        catch (GameActivationBlockedException)
        {
            row.Apply(new(row.GameKey, true, true, row.Name));
            ErrorMessage = AppResources.GameDisableBlocked;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not change activation for game {GameKey}.", row.GameKey);
            ErrorMessage = AppResources.GameActivationSaveFailed;
        }
        finally
        {
            row.RefreshBindings();
            IsBusy = false;
        }
    }
}

public sealed class GameActivationRow(string name, GameActivationState state) : ObservableObject
{
    private GameActivationState _state = state;
    public string GameKey => _state.GameKey;
    public string Name { get; } = name;
    public bool IsEnabled => _state.IsEnabled;
    public bool IsLocked => _state.HasOpenSessions;
    public bool CanToggle => !IsLocked;
    public string StateText => IsEnabled ? AppResources.GameEnabled : AppResources.GameDisabled;
    public void Apply(GameActivationState state)
    {
        _state = state;
        RefreshBindings();
    }
    public void RefreshBindings()
    {
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(CanToggle));
        OnPropertyChanged(nameof(StateText));
    }
}

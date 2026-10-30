using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Players.EditPlayer;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.EditPlayer;

public partial class EditPlayerViewModel(EditPlayerUseCase useCase, ILogger<EditPlayerViewModel> logger) : ObservableObject
{
    private Guid _playerId;
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsArchived { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => ErrorMessage is not null;
    public bool IsIdle => !IsBusy;

    public async Task LoadAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        if (IsBusy || _playerId == playerId)
            return;
        IsBusy = true;
        try
        {
            Player player = await useCase.GetAsync(playerId, cancellationToken)
                ?? throw new InvalidOperationException("The player does not exist.");
            Name = player.Name;
            IsArchived = !player.IsActive;
            _playerId = playerId;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load player for editing.");
            ErrorMessage = AppResources.PlayersLoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy || _playerId == Guid.Empty)
            return false;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await useCase.ExecuteAsync(_playerId, Name, IsArchived, cancellationToken);
            return true;
        }
        catch (ArgumentException)
        {
            ErrorMessage = AppResources.PlayerNameRequired;
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save player.");
            ErrorMessage = AppResources.PlayerSaveFailed;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

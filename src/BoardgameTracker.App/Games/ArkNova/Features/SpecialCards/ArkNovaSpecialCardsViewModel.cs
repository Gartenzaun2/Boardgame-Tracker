using System.Collections.ObjectModel;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.App.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;

public partial class ArkNovaSpecialCardsViewModel(ArkNovaSpecialCardUseCase useCase, ILogger<ArkNovaSpecialCardsViewModel> logger) : ObservableObject
{
    public ObservableCollection<ArkNovaSpecialCardListItem> Cards { get; } = [];
    [ObservableProperty] public partial bool ShowArchived { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool IsIdle => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    partial void OnShowArchivedChanged(bool value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            IReadOnlyList<ArkNovaSpecialCardData> cards = await useCase.ListAsync(ShowArchived);
            Cards.Clear();
            foreach (ArkNovaSpecialCardData card in cards)
                Cards.Add(new(card));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load special cards.");
            ErrorMessage = AppResources.SpecialCardOperationFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
    public async Task ToggleArchiveAsync(ArkNovaSpecialCardData card)
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await useCase.SetArchivedAsync(card.Id, !card.IsArchived);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not archive/unarchive special card {SpecialCardId}.", card.Id);
            ErrorMessage = AppResources.SpecialCardOperationFailed;
        }
        finally
        {
            IsBusy = false;
        }
        if (!HasError)
            await LoadAsync();
    }
}

public sealed record ArkNovaSpecialCardListItem(ArkNovaSpecialCardData Card)
{
    public bool IsArchived => Card.IsArchived;
    public string ArchiveAction => IsArchived ? AppResources.UnarchiveCard : AppResources.ArchiveCard;
}

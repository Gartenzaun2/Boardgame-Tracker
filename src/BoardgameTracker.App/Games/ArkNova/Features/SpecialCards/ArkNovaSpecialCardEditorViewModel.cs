using BoardgameTracker.Application.Games.ArkNova.Content;
using System.Collections.ObjectModel;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;

public partial class ArkNovaSpecialCardEditorViewModel(ArkNovaSpecialCardUseCase useCase, ILogger<ArkNovaSpecialCardEditorViewModel> logger) : ObservableObject
{
    private Guid? _id;
    private bool _originalIsArchived;
    private readonly List<ArkNovaCardSymbol> _symbols = [];
    private int _position;
    public ObservableCollection<ArkNovaSymbolOption> SymbolOptions { get; } = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(Preview))]
    public partial string NameText { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsEditorContentEnabled))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditorContentEnabled))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial bool IsSymbolPickerVisible { get; private set; }
    public bool IsIdle => !IsBusy;
    public bool IsEditorContentEnabled => IsIdle && !IsSymbolPickerVisible;
    public bool IsEditing => _id.HasValue;
    [ObservableProperty] public partial bool IsArchived { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool CanSave => IsEditorContentEnabled && !string.IsNullOrWhiteSpace(NameText) && NameText.Trim().Length <= ArkNovaUserContent.MaximumNameLength;
    public string Title => _id.HasValue ? AppResources.EditSpecialCard : AppResources.NewSpecialCard;
    public ArkNovaSpecialCardData Preview => new(_id ?? Guid.Empty, string.IsNullOrWhiteSpace(NameText) ? AppResources.NewSpecialCard : NameText,
        _symbols.Count > 0 ? _symbols[0] : null, _symbols.Count > 1 ? _symbols[1] : null, _symbols.Count > 2 ? _symbols[2] : null, false);
    public string Symbol1Name => SlotName(0);
    public string Symbol2Name => SlotName(1);
    public string Symbol3Name => SlotName(2);
    public string? Symbol1Image => SlotImage(0);
    public string? Symbol2Image => SlotImage(1);
    public string? Symbol3Image => SlotImage(2);
    public bool HasSymbol1 => _symbols.Count > 0;
    public bool HasSymbol2 => _symbols.Count > 1;
    public bool HasSymbol3 => _symbols.Count > 2;
    public bool CanChooseSymbol2 => _symbols.Count >= 1;
    public bool CanChooseSymbol3 => _symbols.Count >= 2;

    public async Task LoadAsync(Guid? id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            _id = id;
            _symbols.Clear();
            ArkNovaSpecialCardData? card = id.HasValue ? (await useCase.ListAsync(true)).Single(c => c.Id == id.Value) : null;
            NameText = card?.Name ?? string.Empty;
            IsArchived = _originalIsArchived = card?.IsArchived ?? false;
            if (card is not null)
                _symbols.AddRange(card.Symbols);
            RefreshSymbols();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(IsEditing));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load special card {SpecialCardId}.", id);
            ErrorMessage = AppResources.SpecialCardOperationFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }
    public void OpenSymbolPicker(int position)
    {
        if (position < 0 || position > _symbols.Count || position > 2 || !IsEditorContentEnabled)
            return;
        _position = position;
        ArkNovaCardSymbol? selected = position < _symbols.Count ? _symbols[position] : null;
        SymbolOptions.Clear();
        SymbolOptions.Add(new(null, selected is null));
        foreach (ArkNovaCardSymbol symbol in Enum.GetValues<ArkNovaCardSymbol>())
            SymbolOptions.Add(new(symbol, selected == symbol));
        IsSymbolPickerVisible = true;
    }
    public void CloseSymbolPicker() => IsSymbolPickerVisible = false;
    public void SelectSymbol(ArkNovaCardSymbol? symbol)
    {
        if (!IsSymbolPickerVisible || !IsIdle)
            return;
        if (symbol.HasValue)
        {
            if (_position == _symbols.Count)
                _symbols.Add(symbol.Value);
            else
                _symbols[_position] = symbol.Value;
        }
        else if (_position < _symbols.Count)
            _symbols.RemoveAt(_position);
        CloseSymbolPicker();
        RefreshSymbols();
    }
    public async Task<bool> SaveAsync()
    {
        if (!IsEditorContentEnabled)
            return false;
        if (!CanSave)
        {
            ErrorMessage = AppResources.SpecialCardNameValidation;
            return false;
        }
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            ArkNovaSpecialCardData values = Preview;
            if (_id is Guid id)
            {
                await useCase.UpdateAsync(id, NameText, values.Symbol1, values.Symbol2, values.Symbol3);
                if (IsArchived != _originalIsArchived)
                {
                    await useCase.SetArchivedAsync(id, IsArchived);
                    _originalIsArchived = IsArchived;
                }
            }
            else
                await useCase.CreateAsync(NameText, values.Symbol1, values.Symbol2, values.Symbol3);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save special card {SpecialCardId}.", _id);
            ErrorMessage = AppResources.SpecialCardOperationFailed;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
    private string SlotName(int index) => string.Format(AppResources.SymbolPositionFormat, index + 1)
        + ": " + (_symbols.Count > index ? ArkNovaCardSymbolPresentation.Name(_symbols[index]) : AppResources.NoCardSymbol);
    private string? SlotImage(int index) => _symbols.Count > index ? ArkNovaCardSymbolPresentation.Image(_symbols[index]) : null;
    private void RefreshSymbols()
    {
        foreach (string property in new[] { nameof(Preview), nameof(Symbol1Name), nameof(Symbol2Name), nameof(Symbol3Name),
            nameof(Symbol1Image), nameof(Symbol2Image), nameof(Symbol3Image), nameof(HasSymbol1), nameof(HasSymbol2), nameof(HasSymbol3),
            nameof(CanChooseSymbol2), nameof(CanChooseSymbol3) })
            OnPropertyChanged(property);
    }
}

public sealed record ArkNovaSymbolOption(ArkNovaCardSymbol? Symbol, bool IsSelected = false)
{
    public string Name => Symbol is ArkNovaCardSymbol value ? ArkNovaCardSymbolPresentation.Name(value) : AppResources.NoCardSymbol;
    public string? Image => Symbol is ArkNovaCardSymbol value ? ArkNovaCardSymbolPresentation.Image(value) : null;
    public bool HasImage => Symbol.HasValue;
}

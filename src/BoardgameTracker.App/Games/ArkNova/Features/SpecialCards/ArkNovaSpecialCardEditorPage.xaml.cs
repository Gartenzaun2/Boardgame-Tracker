using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;

public partial class ArkNovaSpecialCardEditorPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaSpecialCardEditorViewModel _viewModel;
    private Guid? _id;
    private bool _leaving;
    public ArkNovaSpecialCardEditorPage(ArkNovaSpecialCardEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = null;
        if (query.TryGetValue(NavigationParameters.SpecialCardId, out object? value) && value is not null)
        {
            if (value is not Guid id || id == Guid.Empty)
                throw new InvalidOperationException("Invalid special-card navigation ID.");
            _id = id;
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_id);
    }
    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_leaving || !_viewModel.IsEditorContentEnabled)
            return;
        _leaving = true;
        try
        {
            if (await _viewModel.SaveAsync())
                await Shell.Current.GoToAsync("..");
        }
        finally
        {
            _leaving = false;
        }
    }
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_leaving || !_viewModel.IsEditorContentEnabled)
            return;
        _leaving = true;
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            _leaving = false;
        }
    }
    private void OnSymbolClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string position } && int.TryParse(position, out int index))
            _viewModel.OpenSymbolPicker(index);
    }
    private void OnSymbolOptionTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ArkNovaSymbolOption option)
            _viewModel.SelectSymbol(option.Symbol);
    }
    private void OnCloseSymbolPickerClicked(object? sender, EventArgs e) => _viewModel.CloseSymbolPicker();
    protected override bool OnBackButtonPressed()
    {
        if (_leaving || _viewModel.IsBusy)
            return true;
        if (!_viewModel.IsSymbolPickerVisible)
            return base.OnBackButtonPressed();
        _viewModel.CloseSymbolPicker();
        return true;
    }
}

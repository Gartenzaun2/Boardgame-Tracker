using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

public partial class DuplicateGroupPage : ContentPage, IQueryAttributable
{
    public CreateGroupViewModel ViewModel { get; private set; } = null!;
    public DuplicateGroupPage() => InitializeComponent();
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GroupDraft, out object? value) || value is not CreateGroupViewModel draft)
            throw new InvalidOperationException("The group draft is required.");
        ViewModel = draft;
        BindingContext = draft;
    }
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (ViewModel.IsBusy)
            return;
        ViewModel.CancelDuplicates();
        await Shell.Current.GoToAsync("..", false);
    }
    protected override bool OnBackButtonPressed()
    {
        if (ViewModel.IsBusy)
            return true;
        ViewModel.CancelDuplicates();
        return base.OnBackButtonPressed();
    }
}

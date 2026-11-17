using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

public partial class DuplicateGroupPage : ContentPage, IQueryAttributable {
    public DuplicateGroupPage()
        => InitializeComponent();

    public CreateGroupViewModel ViewModel { get; private set; } = null!;

    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (!query.TryGetValue(NavigationParameters.GroupDraft, out object? value) || value is not CreateGroupViewModel draft)
            throw new InvalidOperationException("The group draft is required.");

        ViewModel = draft;
        BindingContext = draft;
    }

    /// <summary>Uses the header's cancellation/retry flow so system back cannot discard a completed write.</summary>
    protected override bool OnBackButtonPressed() {
        ViewModel.ReturnToDraftCommand.Execute(null);
        return true;
    }
}

using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Features.Groups.ListGroups;

namespace BoardgameTracker.App.Features.GameSessions.Scoring;

/// <summary>
/// Placeholder MAUI view for the Ark Nova scoring slice. It currently receives the
/// persisted session id so the scoring workflow can be implemented without changing navigation.
/// </summary>
public partial class ArkNovaScoringPage : ContentPage, IQueryAttributable
{
    public Guid SessionId { get; private set; }

    public ArkNovaScoringPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.SessionId, out object? value)
            || value is not Guid sessionId
            || sessionId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Required navigation parameter '{NavigationParameters.SessionId}' is missing or invalid.");
        }

        SessionId = sessionId;
    }

    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}");

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () =>
            await Shell.Current.GoToAsync($"//{nameof(GroupListPage)}"));
        return true;
    }

}

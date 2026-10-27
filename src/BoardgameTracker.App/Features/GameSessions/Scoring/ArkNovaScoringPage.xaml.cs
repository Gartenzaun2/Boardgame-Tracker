namespace BoardgameTracker.App.Features.GameSessions.Scoring;

[QueryProperty(nameof(SessionId), "SessionId")]
public partial class ArkNovaScoringPage : ContentPage
{
    public ArkNovaScoringPage()
    {
        InitializeComponent();
    }

    public Guid SessionId { get; set; }
}

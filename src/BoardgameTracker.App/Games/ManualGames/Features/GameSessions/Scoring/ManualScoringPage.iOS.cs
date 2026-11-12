namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Scoring;

public partial class ManualScoringPage
{
    private static partial void ConfigurePointsKeyboard(Entry entry, bool allowNegativePoints)
    {
        if (entry.Handler?.PlatformView is UIKit.UITextField input)
            input.KeyboardType = allowNegativePoints ? UIKit.UIKeyboardType.NumbersAndPunctuation : UIKit.UIKeyboardType.NumberPad;
    }
}

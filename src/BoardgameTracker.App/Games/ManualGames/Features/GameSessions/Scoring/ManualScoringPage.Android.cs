namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Scoring;

public partial class ManualScoringPage
{
    private static partial void ConfigurePointsKeyboard(Entry entry, bool allowNegativePoints)
    {
        if (entry.Handler?.PlatformView is global::Android.Widget.EditText input)
        {
            input.KeyListener = global::Android.Text.Method.DigitsKeyListener.GetInstance(Java.Util.Locale.Root, allowNegativePoints, false);
            input.InputType = global::Android.Text.InputTypes.ClassNumber
                | (allowNegativePoints ? global::Android.Text.InputTypes.NumberFlagSigned : 0);
        }
    }
}

using Foundation;

namespace BoardgameTracker.App;

/// <summary>
/// iOS application delegate that hands platform startup to the shared MAUI composition root.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

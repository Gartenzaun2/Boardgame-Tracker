using Android.App;
using Android.Runtime;

namespace BoardgameTracker.App;

/// <summary>
/// Android application host that creates the shared MAUI application.
/// </summary>
[Application]
public class MainApplication(nint handle, JniHandleOwnership ownership)
    : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

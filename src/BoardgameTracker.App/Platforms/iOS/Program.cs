using UIKit;

namespace BoardgameTracker.App;

/// <summary>
/// Native iOS process entry point for the MAUI application.
/// </summary>
public static class Program
{
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}

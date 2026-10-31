using Foundation;
using UIKit;

internal static class Program
{
    public static void Main(string[] args) => UIApplication.Main(args, null, typeof(ProbeAppDelegate));
}

[Register(nameof(ProbeAppDelegate))]
public sealed class ProbeAppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds) { RootViewController = new UIViewController() };
        Window.MakeKeyAndVisible();
        return true;
    }

    public override async void OnActivated(UIApplication application)
    {
        if (_started) return;
        _started = true;
        try
        {
            string report = await ProbeCheck.RunAsync(name => File.OpenRead(Path.Combine(NSBundle.MainBundle.BundlePath, name)));
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "probe.json"), report);
            Console.WriteLine(report);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
        }
    }

    private bool _started;
}

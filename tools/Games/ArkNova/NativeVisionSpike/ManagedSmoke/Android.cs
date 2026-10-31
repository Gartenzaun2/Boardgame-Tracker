using Android.App;
using Android.OS;
using Android.Util;

[Activity(Label = "Ark Nova Native Spike", MainLauncher = true, Exported = true)]
public sealed class ProbeActivity : Activity
{
    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        try
        {
            string report = await ProbeCheck.RunAsync(name => Assets!.Open(name));
            File.WriteAllText(Path.Combine(FilesDir!.AbsolutePath, "probe.json"), report);
            Log.Info("ArkNovaVisionSpike", report);
        }
        catch (Exception exception)
        {
            Log.Error("ArkNovaVisionSpike", exception.ToString());
        }
        finally
        {
            Finish();
        }
    }
}

using Android.Content;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace BoardgameTracker.App;

/// <summary>
/// Android launcher activity for the single-project MAUI application.
/// </summary>
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private ActivityResultLauncher? _exportLauncher;
    private TaskCompletionSource<Android.Net.Uri?>? _pendingExport;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _exportLauncher = RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), new ExportDocumentCallback(this));
    }

    public async Task<Android.Net.Uri?> CreateExportDocumentAsync(string fileName)
    {
        if (_pendingExport is not null)
            throw new InvalidOperationException("A document picker is already open.");
        TaskCompletionSource<Android.Net.Uri?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingExport = completion;
        try
        {
            using Intent intent = new(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Intent.ExtraTitle, fileName);
            (_exportLauncher ?? throw new InvalidOperationException("Document picker is not registered.")).Launch(intent);
            return await completion.Task;
        }
        finally
        {
            _pendingExport = null;
        }
    }

    protected override void OnDestroy()
    {
        _pendingExport?.TrySetCanceled();
        base.OnDestroy();
    }

    private sealed class ExportDocumentCallback(MainActivity owner) : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object? value)
        {
            if (value is ActivityResult result)
                owner._pendingExport?.TrySetResult((int)result.ResultCode == (int)Result.Ok ? result.Data?.Data : null);
        }
    }
}

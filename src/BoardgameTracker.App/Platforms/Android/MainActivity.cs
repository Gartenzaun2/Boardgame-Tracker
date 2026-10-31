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
    private ActivityResultLauncher? _photoLauncher;
    private TaskCompletionSource<bool>? _pendingPhoto;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _exportLauncher = RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), new ExportDocumentCallback(this));
        _photoLauncher = RegisterForActivityResult(new ActivityResultContracts.TakePicture(), new CapturePhotoCallback(this));
    }

    public async Task<byte[]?> CaptureArkNovaPhotoAsync(CancellationToken cancellationToken)
    {
        if (_pendingPhoto is not null)
            throw new InvalidOperationException("A camera capture is already open.");
        PermissionStatus permission = await Permissions.RequestAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
            throw new PermissionException("Camera permission denied.");
        string path = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString("N") + ".jpg");
        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingPhoto = completion;
        try
        {
            using Java.IO.File file = new(path);
            using Android.Net.Uri uri = AndroidX.Core.Content.FileProvider.GetUriForFile(this, PackageName + ".fileProvider", file)
                ?? throw new InvalidOperationException("Camera file URI unavailable.");
            // The system camera receives a grant for this one cache file. Unlike
            // MAUI's pre-33 capture helper, no storage/library permission is needed.
            (_photoLauncher ?? throw new InvalidOperationException("Camera is not registered.")).Launch(uri);
            bool captured = await completion.Task;
            cancellationToken.ThrowIfCancellationRequested();
            return captured ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
        }
        finally
        {
            _pendingPhoto = null;
            if (File.Exists(path))
                File.Delete(path);
        }
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
        _pendingPhoto?.TrySetCanceled();
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

    private sealed class CapturePhotoCallback(MainActivity owner) : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object? value)
        {
            owner._pendingPhoto?.TrySetResult(value is Java.Lang.Boolean result && result.BooleanValue());
        }
    }
}

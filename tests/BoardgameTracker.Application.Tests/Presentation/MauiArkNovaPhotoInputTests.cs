using BoardgameTracker.App.Games.ArkNova.PhotoRecognition;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class MauiArkNovaPhotoInputTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly byte[] Jpeg = [0xff, 0xd8, 0xff, 1];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LibraryRead_ReturnsPhotoAndClosesStreamBeforeOptionalResult(bool disposable)
    {
        List<string> events = [];
        using TrackedStream stream = new(Jpeg, events);
        FileResult result = CreateResult(disposable, () => Task.FromResult<Stream>(stream), events);
        Picker picker = new(result);

        byte[]? bytes = await new MauiArkNovaPhotoInput(picker).ReadAsync(ArkNovaPhotoSource.Library, Token);

        Assert.Equal(Jpeg, bytes);
        Assert.Equal(disposable ? ["stream", "result"] : new[] { "stream" }, events);
        Assert.Equal(1, picker.LibraryCalls);
        Assert.Equal(0, picker.CameraCalls);
        Assert.Equal(1, picker.SelectionLimit);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LibraryReadFailure_PropagatesAndReleasesOwnedResources(bool failsWhileReading)
    {
        List<string> events = [];
        IOException failure = new("Photo read failed.");
        using TrackedStream stream = new(Jpeg, events) { ReadFailure = failure };
        DisposableResult result = new(() => failsWhileReading
            ? Task.FromResult<Stream>(stream) : Task.FromException<Stream>(failure), events);

        IOException actual = await Assert.ThrowsAsync<IOException>(() =>
            new MauiArkNovaPhotoInput(new Picker(result)).ReadAsync(ArkNovaPhotoSource.Library, Token));

        Assert.Same(failure, actual);
        Assert.Equal(failsWhileReading ? ["stream", "result"] : new[] { "result" }, events);
    }

    [Fact]
    public async Task CancellationAfterSelection_ReleasesResultWithoutOpeningStream()
    {
        List<string> events = [];
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        int opens = 0;
        DisposableResult result = new(() =>
        {
            opens++;
            return Task.FromResult<Stream>(Stream.Null);
        }, events);
        Picker picker = new(result) { Selected = cancellation.Cancel };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new MauiArkNovaPhotoInput(picker).ReadAsync(ArkNovaPhotoSource.Library, cancellation.Token));

        Assert.Equal(0, opens);
        Assert.Equal(new[] { "result" }, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingRead_KeepsResultAliveUntilSuccessOrCancellation(bool cancel)
    {
        List<string> events = [];
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using TrackedStream stream = new(Jpeg, events) { PauseRead = true };
        DisposableResult result = new(() => Task.FromResult<Stream>(stream), events);
        Task<byte[]?> read = new MauiArkNovaPhotoInput(new Picker(result))
            .ReadAsync(ArkNovaPhotoSource.Library, cancellation.Token);
        await stream.ReadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(read.IsCompleted);
        Assert.Empty(events);

        if (cancel)
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
        }
        else
        {
            stream.ContinueRead.SetResult();
            Assert.Equal(Jpeg, await read);
        }

        Assert.Equal(new[] { "stream", "result" }, events);
    }

    [Fact]
    public async Task UnsupportedEncoding_ClosesStreamAndReleasesResultOnProcessingFailure()
    {
        List<string> events = [];
        using TrackedStream stream = new([1, 2, 3], events);
        DisposableResult result = new(() => Task.FromResult<Stream>(stream), events);

        // The desktop test target has no native codec; its unsupported-format path
        // verifies unwinding after the read, without pretending to execute UIKit.
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new MauiArkNovaPhotoInput(new Picker(result)).ReadAsync(ArkNovaPhotoSource.Library, Token));

        Assert.Equal(new[] { "stream", "result" }, events);
    }

    [Fact]
    public async Task CameraRead_PreservesCapturePathAndDoesNotDisposeForeignPickerResult()
    {
        List<string> events = [];
        using TrackedStream stream = new(Jpeg, events);
        DisposableResult result = new(() => Task.FromResult<Stream>(stream), events);
        Picker picker = new(result);

        Assert.Equal(Jpeg, await new MauiArkNovaPhotoInput(picker).ReadAsync(ArkNovaPhotoSource.Camera, Token));

        Assert.Equal(new[] { "stream" }, events);
        Assert.Equal(1, picker.CameraCalls);
        Assert.Equal(0, picker.LibraryCalls);
    }

    [Theory]
    [InlineData(ArkNovaPhotoSource.Library)]
    [InlineData(ArkNovaPhotoSource.Camera)]
    public async Task CancelledSelection_ReturnsNoPhoto(ArkNovaPhotoSource source)
        => Assert.Null(await new MauiArkNovaPhotoInput(new Picker(null)).ReadAsync(source, Token));

    private static FileResult CreateResult(bool disposable, Func<Task<Stream>> open, List<string> events)
        => disposable ? new DisposableResult(open, events) : new FileResult(open);

    private sealed class DisposableResult(Func<Task<Stream>> open, List<string> events) : FileResult(open), IDisposable
    {
        public void Dispose() => events.Add("result");
    }

    private sealed class Picker(FileResult? result) : IMediaPicker
    {
        public bool IsCaptureSupported => true;
        public Action? Selected { get; init; }
        public int LibraryCalls { get; private set; }
        public int CameraCalls { get; private set; }
        public int SelectionLimit { get; private set; }
        public Task<FileResult?> CapturePhotoAsync(MediaPickerOptions options)
        {
            CameraCalls++;
            Selected?.Invoke();
            return Task.FromResult(result);
        }
        public Task<List<FileResult>> PickPhotosAsync(MediaPickerOptions options)
        {
            LibraryCalls++;
            SelectionLimit = options.SelectionLimit;
            Selected?.Invoke();
            return Task.FromResult<List<FileResult>>(result is null ? [] : [result]);
        }
    }

    private sealed class TrackedStream(byte[] bytes, List<string> events) : MemoryStream(bytes)
    {
        public Exception? ReadFailure { get; init; }
        public bool PauseRead { get; init; }
        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ContinueRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ReadStarted.TrySetResult();
            if (PauseRead)
                await ContinueRead.Task.WaitAsync(cancellationToken);
            if (ReadFailure is not null)
                throw ReadFailure;
            await base.CopyToAsync(destination, bufferSize, cancellationToken);
        }
        protected override void Dispose(bool disposing)
        {
            events.Add("stream");
            base.Dispose(disposing);
        }
    }
}

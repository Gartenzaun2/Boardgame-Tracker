using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;
using Microsoft.Win32.SafeHandles;

namespace BoardgameTracker.App.Games.ArkNova.PhotoRecognition;

/// <summary>One serialized native context; SafeHandle owns destruction, buffers are borrowed only while pinned.</summary>
public sealed partial class NativeArkNovaVisionEngine(Func<string, Task<byte[]>> readAsset) : IArkNovaVisionEngine, IDisposable
{
#if IOS
    private const string Library = "__Internal";
#else
    private const string Library = "arknova_vision";
#endif
    private static readonly string[] Tracks = ["attractiveness", "conservation", "reputation"];
    private static readonly string[] Colors = ["red", "blue", "yellow", "black"];
    private readonly SemaphoreSlim _gate = new(1, 1);
    private EngineHandle? _handle;
    private int _fieldCount;
    private bool _disposed;

    public async Task<ArkNovaVisionEvidence> AnalyzeAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_handle is null)
            {
                byte[] reference = await readAsset("ArkNovaVision/board_reference_master_clean.png").ConfigureAwait(false);
                byte[] geometry = await readAsset("ArkNovaVision/scoring_regions_v2.json").ConfigureAwait(false);
                _handle = await Task.Run(() => Create(reference, geometry), cancellationToken).ConfigureAwait(false);
            }
            ArkNovaVisionEvidence result = await Task.Run(() => Analyze(image), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private unsafe EngineHandle Create(byte[] reference, byte[] geometry)
    {
        using JsonDocument json = JsonDocument.Parse(geometry);
        List<NativeField> fields = [];
        List<NativePoint> points = [];
        for (int track = 0; track < Tracks.Length; track++)
            foreach (JsonElement field in json.RootElement.GetProperty("tracks").GetProperty(Tracks[track]).GetProperty("fields").EnumerateArray())
            {
                JsonElement polygon = field.GetProperty("polygon");
                fields.Add(new(track, field.GetProperty("value").GetInt32(), points.Count, polygon.GetArrayLength()));
                foreach (JsonElement point in polygon.EnumerateArray())
                    points.Add(new((int)Math.Round(point[0].GetDouble() * .5), (int)Math.Round(point[1].GetDouble() * .5)));
            }
        NativeField[] fieldArray = fields.ToArray();
        NativePoint[] pointArray = points.ToArray();
        fixed (byte* data = reference)
        fixed (NativeField* fieldData = fieldArray)
        fixed (NativePoint* pointData = pointArray)
        {
            int status = CreateEngine(data, (ulong)reference.Length, fieldData, fieldArray.Length, pointData, pointArray.Length, out nint handle);
            if (status != 0 || handle == 0)
                throw new InvalidOperationException($"Native Ark Nova initialization failed ({status}).");
            _fieldCount = fieldArray.Length;
            return new(handle);
        }
    }

    private unsafe ArkNovaVisionEvidence Analyze(ReadOnlyMemory<byte> bytes)
    {
        using System.Buffers.MemoryHandle memory = bytes.Pin();
        NativeEvidence[] output = new NativeEvidence[_fieldCount * 4];
        fixed (NativeEvidence* evidence = output)
        {
            int status = AnalyzeEngine(_handle!, (byte*)memory.Pointer, (ulong)bytes.Length, evidence, output.Length, out NativeResult result, null, 0);
            if (status is not (0 or 3))
                throw new InvalidOperationException($"Native Ark Nova analysis failed ({status}).");
            double[] homography = [result.H00, result.H01, result.H02, result.H10, result.H11, result.H12, result.H20, result.H21, result.H22];
            ArkNovaVisionRegistration registration = new(status == 0 && result.Success != 0, result.Matches, result.Inliers,
                result.ReprojectionP95 > 0 ? result.ReprojectionP95 : null, result.MinimumVisibility, result.Spread, homography);
            if (!registration.Success)
                return new(registration, []);
            if (result.EvidenceCount != output.Length || result.Width != 2000 || result.Height != 1400)
                throw new InvalidDataException("Native Ark Nova result layout/reference mismatch.");
            RankingObservation[] fields = output.Select(field => new RankingObservation(Colors[field.Color], Tracks[field.Track], field.Value,
                field.Full, field.Core, field.Difference, field.CoreDifference, field.Grown, field.Visibility, field.TrackVisibility,
                field.Full, field.Core, field.Grown)).ToArray();
            return new(registration, fields);
        }
    }

    public void Dispose()
    {
        _gate.Wait();
        try
        {
            if (_disposed)
                return;
            _disposed = true;
            _handle?.Dispose();
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class EngineHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public EngineHandle() : base(true) { }
        public EngineHandle(nint value) : base(true) => SetHandle(value);
        protected override bool ReleaseHandle()
        {
            DestroyEngine(handle);
            return true;
        }
    }
    [LibraryImport(Library, EntryPoint = "anv_engine_create")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int CreateEngine(byte* reference, ulong length, NativeField* fields, int fieldCount, NativePoint* points, int pointCount, out nint handle);
    [LibraryImport(Library, EntryPoint = "anv_engine_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void DestroyEngine(nint handle);
    [LibraryImport(Library, EntryPoint = "anv_engine_analyze")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int AnalyzeEngine(EngineHandle handle, byte* image, ulong length, NativeEvidence* evidence, int capacity,
        out NativeResult result, byte* warp, ulong warpCapacity);
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativePoint(int X, int Y);
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativeField(int Track, int Value, int FirstVertex, int VertexCount);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeEvidence
    {
        public int Color, Track, Value, Reserved;
        public double Full, Core, Difference, CoreDifference, Grown, Visibility, TrackVisibility;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeResult
    {
        public int Success, Matches, Inliers, Width, Height, EvidenceCount;
        public double ReprojectionP95, MinimumVisibility, Spread;
        public double H00, H01, H02, H10, H11, H12, H20, H21, H22;
    }
}

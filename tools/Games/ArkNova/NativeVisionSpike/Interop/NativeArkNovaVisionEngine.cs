using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BoardgameTracker.Tools.Games.ArkNova.NativeVisionSpike;

/// <summary>One synchronous native call per image pair, source-generated interop, no native-owned memory.</summary>
public sealed partial class NativeArkNovaVisionEngine : IArkNovaVisionEngine
{
#if IOS
    private const string Library = "__Internal";
#else
    private const string Library = "arknova_vision";
#endif

    public unsafe ArkNovaRegistrationProbe ProbeRegistration(ArkNovaVisionImage reference, ArkNovaVisionImage photo)
    {
        reference.Validate();
        photo.Validate();
        if (AbiVersion() != 1)
            throw new InvalidOperationException("Unsupported native vision ABI.");
        int width = checked((int)Math.Round(reference.Width * 0.5, MidpointRounding.ToEven));
        int height = checked((int)Math.Round(reference.Height * 0.5, MidpointRounding.ToEven));
        byte[] warp = new byte[checked(width * height * 3)];
        using MemoryHandle referenceMemory = reference.Pixels.Pin();
        using MemoryHandle photoMemory = photo.Pixels.Pin();
        NativeImage referenceInput = new(referenceMemory.Pointer, reference);
        NativeImage photoInput = new(photoMemory.Pointer, photo);
        fixed (byte* output = warp)
        {
            ArkNovaVisionStatus status = Probe(in referenceInput, in photoInput, out NativeProbe result, output, (ulong)warp.Length);
            if (status != ArkNovaVisionStatus.Success)
                throw new ArkNovaVisionException(status);
            if (result.WarpWidth != width || result.WarpHeight != height || result.WarpBytes != (ulong)warp.Length)
                throw new InvalidOperationException("Native vision ABI output size mismatch.");
            return new(result.Matches, result.Inliers, result.ReprojectionP95, width, height, warp);
        }
    }

    [LibraryImport(Library, EntryPoint = "anv_abi_version")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int AbiVersion();

    [LibraryImport(Library, EntryPoint = "anv_probe")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial ArkNovaVisionStatus Probe(in NativeImage reference, in NativeImage photo,
        out NativeProbe result, byte* warp, ulong capacity);

    [StructLayout(LayoutKind.Sequential)]
    private readonly unsafe struct NativeImage(void* pointer, ArkNovaVisionImage image)
    {
        public readonly void* Data = pointer;
        public readonly ulong Length = (ulong)image.Pixels.Length;
        public readonly int Width = image.Width;
        public readonly int Height = image.Height;
        public readonly int Stride = image.Stride;
        public readonly int Reserved = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeProbe
    {
        public int Matches, Inliers, WarpWidth, WarpHeight;
        public double ReprojectionP95;
        public double H00, H01, H02, H10, H11, H12, H20, H21, H22;
        public ulong WarpBytes;
    }
}

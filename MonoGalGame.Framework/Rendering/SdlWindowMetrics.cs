using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace MonoGalGame.Framework.Rendering;

/// <summary>DesktopGL's actual surface size, independent of delayed or missing SDL resize events.</summary>
internal static unsafe partial class SdlWindowMetrics
{
    // Keep the native library reference alive for the lifetime of these cached function pointers.
    private static readonly IntPtr Library = NativeLibrary.Load(
        OperatingSystem.IsWindows() ? "SDL2.dll" : OperatingSystem.IsMacOS() ? "libSDL2-2.0.0.dylib" : "libSDL2-2.0.so.0",
        typeof(Game).Assembly, null);
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int*, int*, void> ReadWindow =
        (delegate* unmanaged[Cdecl]<IntPtr, int*, int*, void>)NativeLibrary.GetExport(Library, "SDL_GetWindowSize");
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int*, int*, void> ReadDrawable =
        (delegate* unmanaged[Cdecl]<IntPtr, int*, int*, void>)NativeLibrary.GetExport(Library, "SDL_GL_GetDrawableSize");
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int> GetDisplayIndex =
        (delegate* unmanaged[Cdecl]<IntPtr, int>)NativeLibrary.GetExport(Library, "SDL_GetWindowDisplayIndex");
    private static readonly delegate* unmanaged[Cdecl]<int, NativeBounds*, int> GetDisplayBounds =
        (delegate* unmanaged[Cdecl]<int, NativeBounds*, int>)NativeLibrary.GetExport(Library, "SDL_GetDisplayUsableBounds");

    public static Point WindowSize(IntPtr handle) => Read(ReadWindow, handle);
    public static Point DrawableSize(IntPtr handle) => Read(ReadDrawable, handle);

    public static Point InitialWindowSize(IntPtr handle, Point logicalSize, bool scaleToDpi)
    {
        if (!scaleToDpi) return logicalSize;
        var scale = OperatingSystem.IsWindows() ? Math.Max(1, GetDpiForSystem() / 96f) : 1;
        var displayIndex = GetDisplayIndex(handle);
        NativeBounds bounds;
        if (GetDisplayBounds(displayIndex, &bounds) != 0) return new((int)(logicalSize.X * scale), (int)(logicalSize.Y * scale));
        // Keep room for the native title bar and a margin, while preserving the apparent window size at 200% DPI.
        var maximum = new Point(Math.Max(1, bounds.Width - (int)(32 * scale)), Math.Max(1, bounds.Height - (int)(64 * scale)));
        return PresentationLayout.FitInitialWindow(logicalSize, scale, maximum);
    }

    private static Point Read(delegate* unmanaged[Cdecl]<IntPtr, int*, int*, void> function, IntPtr handle)
    {
        int width, height;
        function(handle, &width, &height);
        return new(width, height);
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativeBounds { public int X, Y, Width, Height; }
    [LibraryImport("user32.dll")] private static partial uint GetDpiForSystem();
}

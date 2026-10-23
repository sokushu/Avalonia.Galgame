using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using MonoGalGame.Framework.Hosting;
using MonoGalGame.Framework.Fonts;

namespace MonoGalGame.Demo;

// Read the actual SDL/OS state: checking GameWindow.Title alone misses creation-time encoding bugs.
internal static class DisplayProbe
{
    private static readonly IntPtr Library = NativeLibrary.Load(
        OperatingSystem.IsWindows() ? "SDL2.dll" : OperatingSystem.IsMacOS() ? "libSDL2-2.0.0.dylib" : "libSDL2-2.0.so.0",
        typeof(Game).Assembly, null);

    // Concrete delegate types let Native AOT generate each interop stub ahead of time.
    private static readonly GetTitle ReadTitle = Marshal.GetDelegateForFunctionPointer<GetTitle>(NativeLibrary.GetExport(Library, "SDL_GetWindowTitle"));
    private static readonly GetSize ReadWindowSize = Marshal.GetDelegateForFunctionPointer<GetSize>(NativeLibrary.GetExport(Library, "SDL_GetWindowSize"));
    private static readonly GetSize ReadDrawableSize = Marshal.GetDelegateForFunctionPointer<GetSize>(NativeLibrary.GetExport(Library, "SDL_GL_GetDrawableSize"));
    private static readonly SetSize SetWindowSize = Marshal.GetDelegateForFunctionPointer<SetSize>(NativeLibrary.GetExport(Library, "SDL_SetWindowSize"));
    private static readonly GetVersion ReadVersion = Marshal.GetDelegateForFunctionPointer<GetVersion>(NativeLibrary.GetExport(Library, "SDL_GetVersion"));
    private static readonly GetWindowInfo ReadWindowInfo = Marshal.GetDelegateForFunctionPointer<GetWindowInfo>(NativeLibrary.GetExport(Library, "SDL_GetWindowWMInfo"));

    public static DisplaySnapshot Inspect(GalgameGame game, DynamicFont? font = null)
    {
        var title = Marshal.PtrToStringUTF8(ReadTitle(game.Window.Handle)) ?? "";
        ReadWindowSize(game.Window.Handle, out var width, out var height);
        ReadDrawableSize(game.Window.Handle, out var drawableWidth, out var drawableHeight);
        var nativeTitle = title;
        int? awareness = null;
        uint? dpi = null;
        if (OperatingSystem.IsWindows())
        {
            ReadVersion(out var version);
            var info = new WindowInfo { Major = version.Major, Minor = version.Minor, Patch = version.Patch };
            if (ReadWindowInfo(game.Window.Handle, ref info) != 0 && info.Subsystem == 1)
            {
                var text = new StringBuilder(1024);
                GetWindowTextW(info.Window, text, text.Capacity);
                nativeTitle = text.ToString();
                awareness = GetAwarenessFromDpiAwarenessContext(GetWindowDpiAwarenessContext(info.Window));
                dpi = GetDpiForWindow(info.Window);
            }
        }
        return new(game.Window.Title, title, nativeTitle, title == game.Window.Title && nativeTitle == game.Window.Title,
            new(width, height), new(drawableWidth, drawableHeight),
            new(game.GraphicsDevice.PresentationParameters.BackBufferWidth, game.GraphicsDevice.PresentationParameters.BackBufferHeight),
            new(game.ContentPixelSize.X, game.ContentPixelSize.Y), game.RenderScale, font?.RenderScale, font?.AtlasPageCount, awareness, dpi);
    }

    public static void Resize(Game game, int width, int height) => SetWindowSize(game.Window.Handle, width, height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr GetTitle(IntPtr window);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GetSize(IntPtr window, out int width, out int height);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void SetSize(IntPtr window, int width, int height);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GetVersion(out SdlVersion version);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int GetWindowInfo(IntPtr window, ref WindowInfo info);
    [StructLayout(LayoutKind.Sequential)] private struct SdlVersion { public byte Major, Minor, Patch; }
    [StructLayout(LayoutKind.Explicit, Size = 72)]
    private struct WindowInfo
    {
        [FieldOffset(0)] public byte Major;
        [FieldOffset(1)] public byte Minor;
        [FieldOffset(2)] public byte Patch;
        [FieldOffset(4)] public int Subsystem;
        [FieldOffset(8)] public IntPtr Window;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(IntPtr window, StringBuilder text, int maximum);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr window);
    [DllImport("user32.dll")] private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
}

internal sealed record PixelSize(int Width, int Height);
internal sealed record DisplaySnapshot(string ManagedTitle, string SdlTitle, string NativeTitle, bool TitleMatches,
    PixelSize Window, PixelSize Drawable, PixelSize BackBuffer, PixelSize Content, float RenderScale,
    float? FontRasterScale, int? FontAtlasPages, int? WindowsDpiAwareness, uint? WindowDpi);

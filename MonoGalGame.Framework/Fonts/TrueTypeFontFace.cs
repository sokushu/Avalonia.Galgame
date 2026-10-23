using System.Text;
using StbTrueTypeSharp;
using static StbTrueTypeSharp.StbTrueType;

namespace MonoGalGame.Framework.Fonts;

public sealed record GlyphBitmap(int Width, int Height, int OffsetX, int OffsetY, float Advance, byte[] Alpha);

/// <summary>Only rasterization is delegated to stb; layout, caching and GPU rendering belong to this framework.</summary>
public sealed unsafe class TrueTypeFontFace : IDisposable
{
    private readonly stbtt_fontinfo _font;
    private bool _disposed;

    public TrueTypeFontFace(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 12) throw new InvalidDataException("Font file is too small.");
        fixed (byte* data = bytes)
        {
            var offset = stbtt_GetFontOffsetForIndex(data, 0);
            if (offset < 0) throw new InvalidDataException("Unsupported font file.");
            _font = CreateFont(bytes, offset) ?? throw new InvalidDataException("Cannot load this font.");
        }
    }

    public bool Contains(Rune rune)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return stbtt_FindGlyphIndex(_font, rune.Value) != 0;
    }

    public float GetAdvance(Rune rune, float pixelSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        int advance, bearing;
        stbtt_GetCodepointHMetrics(_font, rune.Value, &advance, &bearing);
        return advance * Scale(pixelSize);
    }

    public float GetAscent(float pixelSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        int ascent, descent, gap;
        stbtt_GetFontVMetrics(_font, &ascent, &descent, &gap);
        return ascent * Scale(pixelSize);
    }

    public GlyphBitmap Rasterize(Rune rune, float pixelSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var scale = Scale(pixelSize);
        int width, height, x, y;
        var bitmap = stbtt_GetCodepointBitmap(_font, scale, scale, rune.Value, &width, &height, &x, &y);
        try
        {
            var alpha = new byte[checked(width * height)];
            if (alpha.Length > 0)
            {
                if (bitmap == null) throw new InvalidDataException("Font rasterization failed.");
                new ReadOnlySpan<byte>(bitmap, alpha.Length).CopyTo(alpha);
            }
            return new(width, height, x, y, GetAdvance(rune, pixelSize), alpha);
        }
        finally { if (bitmap != null) stbtt_FreeBitmap(bitmap, null); }
    }

    private float Scale(float size)
    {
        if (!float.IsFinite(size) || size is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(size));
        return stbtt_ScaleForMappingEmToPixels(_font, size);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _font.Dispose();
        _disposed = true;
    }
}

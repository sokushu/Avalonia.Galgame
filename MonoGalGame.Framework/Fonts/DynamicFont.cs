using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGalGame.Core.Text;

namespace MonoGalGame.Framework.Fonts;

public interface IGameFont : ITextMetrics, IDisposable
{
    float Measure(string text, float size);
    void Draw(SpriteBatch batch, string text, Vector2 position, float size, Color color);
    void DrawPage(SpriteBatch batch, TextPage page, Vector2 position, float size, Color color, int visibleCount);
}

/// <summary>Optional capability for fonts that rasterize at the actual presentation pixel density.</summary>
public interface IRenderScaleAwareFont
{
    void SetRenderScale(float scale);
}

/// <summary>Lazy Unicode glyph atlas. Access on the game's graphics thread only.</summary>
public sealed class DynamicFont : IGameFont, IRenderScaleAwareFont
{
    private const int AtlasSize = 1024;
    private const int MaximumPages = 16;
    private readonly GraphicsDevice _device;
    private readonly List<TrueTypeFontFace> _faces = [];
    private readonly List<AtlasPage> _pages = [];
    private readonly Dictionary<(int Codepoint, int Size), CachedGlyph> _glyphs = [];
    private readonly Dictionary<(int Codepoint, int Size), float> _advances = [];
    private bool _disposed;
    public float RenderScale { get; private set; } = 1;
    public int CachedGlyphCount => _glyphs.Count;
    public int AtlasPageCount => _pages.Count;

    public DynamicFont(GraphicsDevice device, params string[] fontPaths)
    {
        _device = device;
        try
        {
            foreach (var path in fontPaths) _faces.Add(new(path));
            if (_faces.Count == 0) throw new ArgumentException("At least one font is required.", nameof(fontPaths));
        }
        catch { Dispose(); throw; }
    }

    public float GetAdvance(string textElement, float fontSize) => Measure(textElement, fontSize);

    public void SetRenderScale(float scale)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (Math.Abs(RenderScale - scale) < .00001f) return;
        // Release the old resolution, rather than accumulating a new atlas for every drag-resize step.
        foreach (var page in _pages) page.Texture.Dispose();
        _pages.Clear();
        _glyphs.Clear();
        RenderScale = scale;
        // Logical advance metrics stay unchanged, so resizing cannot change wrapping or pagination.
    }

    public float Measure(string text, float size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var pixels = PixelSize(size);
        var width = 0f;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsControl(rune) || rune.Value is 0xFE0E or 0xFE0F) continue;
            width += LogicalAdvance(rune, pixels);
        }
        return width;
    }

    public void Draw(SpriteBatch batch, string text, Vector2 position, float size, Color color)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var startX = position.X;
        foreach (var element in TextLayout.Elements(text.Replace("\r", "")))
        {
            if (element == "\n") { position.X = startX; position.Y += size * 1.55f; continue; }
            DrawElement(batch, element, position, size, color);
            position.X += Measure(element, size);
        }
    }

    public void DrawPage(SpriteBatch batch, TextPage page, Vector2 position, float size, Color color, int visibleCount)
    {
        foreach (var element in page.Elements)
        {
            if (element.Index >= visibleCount) break;
            DrawElement(batch, element.Text, position + new Vector2(element.X, element.Y), size, color);
        }
    }

    private void DrawElement(SpriteBatch batch, string element, Vector2 position, float size, Color color)
    {
        var logicalSize = PixelSize(size);
        var rasterSize = Math.Max(1, (int)MathF.Round(logicalSize * RenderScale));
        if (rasterSize > 1024) throw new ArgumentOutOfRangeException(nameof(size), "Physical font size exceeds 1024 pixels.");
        foreach (var rune in element.EnumerateRunes())
        {
            if (Rune.IsControl(rune) || rune.Value is 0xFE0E or 0xFE0F) continue;
            var glyph = GetGlyph(rune, rasterSize);
            if (glyph.Page is not null)
            {
                // One glyph texel maps to one display pixel. Snap the final origin, including the baseline.
                var pixelPosition = position * RenderScale + glyph.Offset;
                var snapped = new Vector2(MathF.Round(pixelPosition.X), MathF.Round(pixelPosition.Y)) / RenderScale;
                batch.Draw(glyph.Page.Texture, snapped, glyph.Bounds, color, 0, Vector2.Zero,
                    1 / RenderScale, SpriteEffects.None, 0);
            }
            position.X += LogicalAdvance(rune, logicalSize);
        }
    }

    private float LogicalAdvance(Rune rune, int size)
    {
        var key = (rune.Value, size);
        if (!_advances.TryGetValue(key, out var advance))
        {
            var (face, resolved) = Resolve(rune);
            _advances[key] = advance = face.GetAdvance(resolved, size);
        }
        return advance;
    }

    private CachedGlyph GetGlyph(Rune rune, int size)
    {
        var key = (rune.Value, size);
        if (_glyphs.TryGetValue(key, out var cached)) return cached;
        var (face, resolved) = Resolve(rune);
        var bitmap = face.Rasterize(resolved, size);
        var offset = new Vector2(bitmap.OffsetX, bitmap.OffsetY + _faces[0].GetAscent(size));
        if (bitmap.Width == 0 || bitmap.Height == 0)
            return _glyphs[key] = new(null, Rectangle.Empty, offset, bitmap.Advance);
        if (bitmap.Width + 4 > AtlasSize || bitmap.Height + 4 > AtlasSize)
            throw new InvalidOperationException("Glyph is too large for the font atlas.");
        AtlasPage? page = null;
        var bounds = Rectangle.Empty;
        foreach (var candidate in _pages)
            if (candidate.TryAllocate(bitmap.Width, bitmap.Height, out bounds)) { page = candidate; break; }
        if (page is null)
        {
            if (_pages.Count >= MaximumPages) throw new InvalidOperationException("Font atlas limit reached. Reduce font sizes or create another font instance.");
            page = new(_device);
            _pages.Add(page);
            page.TryAllocate(bitmap.Width, bitmap.Height, out bounds);
        }
        // Premultiplied coverage for MonoGame's standard AlphaBlend mode.
        var colors = new Color[bitmap.Alpha.Length];
        for (var i = 0; i < colors.Length; i++)
        {
            var alpha = bitmap.Alpha[i];
            colors[i] = new(alpha, alpha, alpha, alpha);
        }
        page.Texture.SetData(0, bounds, colors, 0, colors.Length);
        return _glyphs[key] = new(page, bounds, offset, bitmap.Advance);
    }

    private (TrueTypeFontFace Face, Rune Rune) Resolve(Rune rune)
    {
        foreach (var face in _faces) if (face.Contains(rune)) return (face, rune);
        return (_faces[0], new Rune('?'));
    }

    private static int PixelSize(float size)
    {
        if (!float.IsFinite(size) || size is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(size));
        return (int)MathF.Round(size);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var page in _pages) page.Texture.Dispose();
        foreach (var face in _faces) face.Dispose();
        _disposed = true;
    }

    private sealed record CachedGlyph(AtlasPage? Page, Rectangle Bounds, Vector2 Offset, float Advance);

    private sealed class AtlasPage
    {
        private int _x = 2, _y = 2, _rowHeight;
        public Texture2D Texture { get; }
        public AtlasPage(GraphicsDevice device)
        {
            Texture = new(device, AtlasSize, AtlasSize, false, SurfaceFormat.Color);
            Texture.SetData(new Color[AtlasSize * AtlasSize]);
        }
        public bool TryAllocate(int width, int height, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            var x = _x;
            var y = _y;
            var row = _rowHeight;
            if (x + width + 2 > AtlasSize) { x = 2; y += row + 2; row = 0; }
            if (y + height + 2 > AtlasSize) return false;
            bounds = new(x, y, width, height);
            _x = x + width + 2;
            _y = y;
            _rowHeight = Math.Max(row, height);
            return true;
        }
    }
}

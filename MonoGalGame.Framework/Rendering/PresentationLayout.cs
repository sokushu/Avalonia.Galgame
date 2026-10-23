using Microsoft.Xna.Framework;

namespace MonoGalGame.Framework.Rendering;

/// <summary>Design coordinates, actual framebuffer pixels and SDL mouse coordinates are separate spaces.</summary>
public readonly record struct PresentationLayout(Point BackBufferSize, Point DesignSize, Rectangle Viewport)
{
    public Vector2 Scale => new(Viewport.Width / (float)DesignSize.X, Viewport.Height / (float)DesignSize.Y);
    public Matrix Transform => Matrix.CreateScale(Scale.X, Scale.Y, 1);

    public static PresentationLayout Create(int width, int height, int designWidth = 1280, int designHeight = 720)
    {
        if (width < 1 || height < 1 || designWidth < 1 || designHeight < 1)
            throw new ArgumentOutOfRangeException(nameof(width));
        var scale = Math.Min(width / (float)designWidth, height / (float)designHeight);
        var contentWidth = Math.Clamp((int)MathF.Round(designWidth * scale), 1, width);
        var contentHeight = Math.Clamp((int)MathF.Round(designHeight * scale), 1, height);
        return new(new(width, height), new(designWidth, designHeight),
            new((width - contentWidth) / 2, (height - contentHeight) / 2, contentWidth, contentHeight));
    }

    public Vector2 ToBackBuffer(Point mouse, Point windowSize) => new(
        mouse.X * BackBufferSize.X / (float)Math.Max(1, windowSize.X),
        mouse.Y * BackBufferSize.Y / (float)Math.Max(1, windowSize.Y));

    public Vector2 ToDesign(Point mouse, Point windowSize)
    {
        var pixel = ToBackBuffer(mouse, windowSize);
        return new((pixel.X - Viewport.X) / Scale.X, (pixel.Y - Viewport.Y) / Scale.Y);
    }

    public bool Contains(Point mouse, Point windowSize) => Viewport.Contains(ToBackBuffer(mouse, windowSize));

    public static Point FitInitialWindow(Point logicalSize, float dpiScale, Point maximumClientSize)
    {
        if (logicalSize.X < 1 || logicalSize.Y < 1 || !float.IsFinite(dpiScale) || dpiScale <= 0 ||
            maximumClientSize.X < 1 || maximumClientSize.Y < 1) throw new ArgumentOutOfRangeException(nameof(logicalSize));
        var wanted = logicalSize.ToVector2() * dpiScale;
        var fit = Math.Min(1, Math.Min(maximumClientSize.X / wanted.X, maximumClientSize.Y / wanted.Y));
        return new(Math.Max(1, (int)MathF.Floor(wanted.X * fit)), Math.Max(1, (int)MathF.Floor(wanted.Y * fit)));
    }
}

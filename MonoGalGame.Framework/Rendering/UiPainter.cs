using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGalGame.Core.Text;
using MonoGalGame.Framework.Fonts;

namespace MonoGalGame.Framework.Rendering;

public sealed class UiPainter : IDisposable
{
    private readonly Texture2D _pixel;
    private readonly Texture2D _circle;
    private readonly SpriteBatch _batch;
    public IGameFont Font { get; }

    public UiPainter(GraphicsDevice device, SpriteBatch batch, IGameFont font)
    {
        _batch = batch;
        Font = font;
        _pixel = new(device, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _circle = new(device, 64, 64);
        var data = new Color[64 * 64];
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                var distance = Vector2.Distance(new(x + .5f, y + .5f), new(32));
                data[y * 64 + x] = Color.White * Math.Clamp(32 - distance, 0, 1);
            }
        _circle.SetData(data);
    }

    public void Fill(Rectangle rectangle, Color color) => _batch.Draw(_pixel, rectangle, color);
    public void Outline(Rectangle rectangle, Color color, int thickness = 1)
    {
        Fill(new(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        Fill(new(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        Fill(new(rectangle.X, rectangle.Y + thickness, thickness, rectangle.Height - thickness * 2), color);
        Fill(new(rectangle.Right - thickness, rectangle.Y + thickness, thickness, rectangle.Height - thickness * 2), color);
    }

    public void Gradient(Rectangle rectangle, Color from, Color to, bool horizontal = false)
    {
        var length = horizontal ? rectangle.Width : rectangle.Height;
        var steps = Math.Min(128, length);
        for (var i = 0; i < steps; i++)
        {
            var start = i * length / steps;
            var end = (i + 1) * length / steps;
            var strip = horizontal ? new Rectangle(rectangle.X + start, rectangle.Y, end - start, rectangle.Height) :
                new Rectangle(rectangle.X, rectangle.Y + start, rectangle.Width, end - start);
            Fill(strip, Color.Lerp(from, to, i / (float)Math.Max(1, steps - 1)));
        }
    }

    public void Line(Vector2 start, Vector2 end, Color color, float thickness = 1)
    {
        var delta = end - start;
        _batch.Draw(_pixel, start, null, color, MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
            new Vector2(delta.Length(), thickness), SpriteEffects.None, 0);
    }

    public void Diamond(Vector2 center, float size, Color color, float rotation = MathHelper.PiOver4) =>
        _batch.Draw(_pixel, center, null, color, rotation, new Vector2(.5f), new Vector2(size), SpriteEffects.None, 0);

    public float TrackedText(string text, float x, float y, float size, float spacing, Color color)
    {
        var origin = x;
        foreach (var element in TextLayout.Elements(text))
        {
            Text(element, x, y, size, color);
            x += Font.Measure(element, size) + spacing;
        }
        return x - origin;
    }

    public void VerticalText(string text, Rectangle bounds, float size, float spacing, Color color)
    {
        var elements = TextLayout.Elements(text).ToArray();
        var height = elements.Length * size + Math.Max(0, elements.Length - 1) * spacing;
        var y = bounds.Center.Y - height / 2;
        foreach (var element in elements)
        {
            Text(element, bounds.Center.X - Font.Measure(element, size) / 2, y, size, color);
            y += size + spacing;
        }
    }

    public void Circle(Vector2 center, float radius, Color color) =>
        _batch.Draw(_circle, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2)), color);

    public void Rounded(Rectangle rectangle, Color color, int radius = 10)
    {
        radius = Math.Min(radius, Math.Min(rectangle.Width, rectangle.Height) / 2);
        if (radius <= 0) { Fill(rectangle, color); return; }
        Fill(new(rectangle.X + radius, rectangle.Y, rectangle.Width - 2 * radius, rectangle.Height), color);
        Fill(new(rectangle.Left, rectangle.Y + radius, radius, rectangle.Height - 2 * radius), color);
        Fill(new(rectangle.Right - radius, rectangle.Y + radius, radius, rectangle.Height - 2 * radius), color);
        // Draw each pixel only once, so translucent panels do not have darker joins.
        _batch.Draw(_circle, new Rectangle(rectangle.Left, rectangle.Top, radius, radius), new Rectangle(0, 0, 32, 32), color);
        _batch.Draw(_circle, new Rectangle(rectangle.Right - radius, rectangle.Top, radius, radius), new Rectangle(32, 0, 32, 32), color);
        _batch.Draw(_circle, new Rectangle(rectangle.Left, rectangle.Bottom - radius, radius, radius), new Rectangle(0, 32, 32, 32), color);
        _batch.Draw(_circle, new Rectangle(rectangle.Right - radius, rectangle.Bottom - radius, radius, radius), new Rectangle(32, 32, 32, 32), color);
    }

    public void Text(string text, float x, float y, float size, Color color) => Font.Draw(_batch, text, new(x, y), size, color);
    public void Centered(string text, Rectangle bounds, float size, Color color) =>
        Text(text, bounds.Center.X - Font.Measure(text, size) / 2, bounds.Center.Y - size * .78f, size, color);

    public void Ellipsis(string text, float x, float y, float size, Color color, float maxWidth)
    {
        text = text.Replace("\r", "").Replace('\n', ' ');
        if (Font.Measure(text, size) > maxWidth)
        {
            var result = "";
            foreach (var element in TextLayout.Elements(text))
            {
                if (Font.Measure(result + element + "…", size) > maxWidth) break;
                result += element;
            }
            text = result + "…";
        }
        Text(text, x, y, size, color);
    }

    public void Cover(Texture2D texture, Rectangle bounds, Color color)
    {
        var scale = Math.Max((float)bounds.Width / texture.Width, (float)bounds.Height / texture.Height);
        var width = (int)(bounds.Width / scale);
        var height = (int)(bounds.Height / scale);
        _batch.Draw(texture, bounds, new Rectangle((texture.Width - width) / 2, (texture.Height - height) / 2, width, height), color);
    }

    public void Image(Texture2D texture, Rectangle bounds, Color color) => _batch.Draw(texture, bounds, color);
    public void Dispose() { _pixel.Dispose(); _circle.Dispose(); }
}

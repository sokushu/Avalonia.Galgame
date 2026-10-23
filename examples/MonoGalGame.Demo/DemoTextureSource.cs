using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGalGame.Core.Projects;
using MonoGalGame.Framework.Assets;

namespace MonoGalGame.Demo;

/// <summary>Loads the demo's file artwork, with procedural fallbacks for the original resource ids.</summary>
internal sealed class DemoTextureSource(GraphicsDevice device, GameProject project) : ITextureSource
{
    private readonly FileTextureSource _files = new(device, project);
    private readonly Dictionary<string, Texture2D> _cache = [];

    public Texture2D? Get(string id)
    {
        if (project.Textures.ContainsKey(id)) return _files.Get(id);
        if (_cache.TryGetValue(id, out var cached)) return cached;
        if (id is not ("campus" or "library" or "river" or "aoi")) return null;
        var image = id == "aoi" ? DrawCharacter() : DrawBackground(id);
        var texture = new Texture2D(device, image.Width, image.Height);
        texture.SetData(image.Pixels);
        _cache.Add(id, texture);
        return texture;
    }

    private static Illustration DrawBackground(string scene)
    {
        var p = new Illustration(1280, 720);
        var river = scene == "river";
        p.Gradient(river ? new(236, 198, 186) : new(186, 216, 209), new(250, 245, 223));
        p.Ellipse(1020, 170, 62, 62, new Color(255, 247, 214));
        p.Polygon(new Color(153, 183, 171), (0, 400), (130, 310), (310, 395), (560, 315), (820, 410), (1080, 330), (1280, 395), (1280, 720), (0, 720));
        if (river)
        {
            p.Rectangle(0, 415, 1280, 305, new Color(162, 192, 185));
            for (var i = 0; i < 20; i++) p.Rectangle(640 + (i * 71 % 400), 435 + i * 11, 120 + i % 3 * 65, 2, new Color(231, 223, 191));
            p.Polygon(new Color(201, 207, 173), (0, 720), (0, 410), (180, 400), (780, 720));
            p.Polygon(new Color(232, 219, 194), (90, 720), (80, 430), (135, 430), (490, 720));
            p.Rectangle(540, 390, 740, 13, new Color(130, 151, 143));
            for (var x = 570; x < 1280; x += 74) p.Rectangle(x, 355, 7, 79, new Color(121, 146, 137));
            p.Rectangle(540, 355, 740, 6, new Color(121, 146, 137));
        }
        else
        {
            p.Rectangle(0, 480, 1280, 240, new Color(210, 219, 185));
            p.Polygon(new Color(238, 228, 209), (330, 720), (570, 485), (720, 485), (1000, 720));
            p.Rectangle(205, 285, 620, 230, new Color(234, 229, 208));
            p.Rectangle(190, 270, 650, 17, new Color(135, 158, 149));
            p.Rectangle(205, 347, 620, 9, new Color(213, 209, 187));
            p.Rectangle(205, 425, 620, 9, new Color(213, 209, 187));
            for (var y = 302; y <= 442; y += 70)
                for (var x = 234; x < 800; x += 74)
                {
                    p.Rectangle(x, y, 47, 42, new Color(141, 174, 170));
                    p.Rectangle(x + 5, y + 4, 37, 33, new Color(181, 205, 191));
                    p.Rectangle(x + 23, y, 3, 42, new Color(236, 233, 215));
                }
            p.Rectangle(581, 438, 77, 77, new Color(130, 157, 149));
            p.Ellipse(618, 247, 25, 25, new Color(247, 246, 227));
            p.Line(618, 247, 618, 231, 3, new Color(126, 149, 139));
            p.Line(618, 247, 630, 253, 3, new Color(126, 149, 139));
            for (var x = 700; x < 1280; x += 49) p.Rectangle(x, 470, 7, 75, new Color(246, 243, 220));
            p.Rectangle(700, 485, 580, 7, new Color(246, 243, 220));
            p.Rectangle(700, 523, 580, 6, new Color(246, 243, 220));
        }
        // The cherry tree uses a deterministic seed, so screenshots remain reproducible.
        p.Polygon(new Color(129, 116, 100), (1030, 559), (1046, 237), (1062, 237), (1068, 559));
        p.Line(1049, 350, 939, 218, 15, new Color(129, 116, 100));
        p.Line(1055, 315, 1172, 185, 13, new Color(129, 116, 100));
        p.Line(1047, 290, 1011, 148, 10, new Color(129, 116, 100));
        var random = new Random(71);
        for (var i = 0; i < 95; i++)
        {
            var x = random.Next(835, 1270);
            var y = random.Next(35, 260);
            if (Math.Pow((x - 1060) / 237f, 2) + Math.Pow((y - 145) / 130f, 2) > 1) continue;
            var color = i % 3 == 0 ? new Color(231, 178, 183) : i % 3 == 1 ? new Color(249, 209, 208) : new Color(255, 226, 220);
            p.Ellipse(x, y, random.Next(25, 58), random.Next(24, 47), color);
        }
        for (var i = 0; i < 40; i++)
            p.Ellipse(random.Next(700, 1270), random.Next(550, 700), 4, 2, new Color(239, 198, 193));
        if (scene == "library")
        {
            // Large window onto the campus, flanked by warm timber bookshelves.
            p.Rectangle(0, 0, 1280, 80, new Color(195, 177, 147));
            p.Rectangle(0, 0, 175, 720, new Color(185, 165, 137));
            p.Rectangle(1095, 0, 185, 720, new Color(185, 165, 137));
            p.Rectangle(175, 80, 920, 12, new Color(244, 233, 206));
            p.Rectangle(620, 80, 12, 490, new Color(244, 233, 206));
            p.Rectangle(175, 570, 920, 16, new Color(244, 233, 206));
            for (var y = 145; y < 710; y += 155)
            {
                foreach (var origin in new[] { 15, 1110 })
                {
                    p.Rectangle(origin, y + 90, 148, 13, new Color(140, 119, 100));
                    for (var i = 0; i < 8; i++)
                    {
                        var color = i % 3 == 0 ? new Color(140, 164, 146) : i % 3 == 1 ? new Color(196, 149, 133) : new Color(225, 207, 171);
                        p.Rectangle(origin + i * 18, y + 10 + i % 3 * 10, 13, 80 - i % 3 * 10, color);
                    }
                }
            }
            p.Polygon(new Color(161, 137, 112), (200, 720), (350, 600), (980, 600), (1110, 720));
        }
        return p;
    }

    private static Illustration DrawCharacter()
    {
        var p = new Illustration(440, 690);
        var hair = new Color(84, 76, 73);
        var skin = new Color(251, 220, 196);
        var dark = new Color(65, 88, 84);
        p.Ellipse(220, 203, 96, 132, hair);
        p.Polygon(hair, (128, 191), (113, 389), (178, 357), (207, 238));
        p.Polygon(hair, (293, 168), (327, 379), (266, 363), (232, 231));
        p.Rectangle(201, 263, 38, 60, skin);
        p.Ellipse(220, 203, 72, 91, skin);
        p.Ellipse(148, 215, 12, 20, skin);
        p.Ellipse(292, 215, 12, 20, skin);
        p.Polygon(hair, (146, 204), (143, 139), (180, 107), (247, 109), (291, 152), (294, 206),
            (271, 164), (258, 182), (231, 144), (220, 176), (191, 145), (175, 185), (167, 162));
        p.Line(171, 205, 193, 200, 3, hair);
        p.Line(245, 200, 268, 205, 3, hair);
        p.Ellipse(183, 219, 8, 13, dark);
        p.Ellipse(256, 219, 8, 13, dark);
        p.Ellipse(181, 215, 3, 4, Color.White);
        p.Ellipse(254, 215, 3, 4, Color.White);
        p.Ellipse(164, 240, 13, 5, new Color(242, 183, 174));
        p.Ellipse(275, 240, 13, 5, new Color(242, 183, 174));
        p.Line(212, 261, 229, 261, 2, new Color(184, 124, 120));
        p.Polygon(new Color(235, 225, 199), (165, 300), (199, 289), (219, 322), (242, 289), (278, 302),
            (318, 459), (282, 471), (268, 377), (277, 525), (162, 525), (171, 379), (152, 473), (118, 461));
        p.Polygon(new Color(250, 247, 228), (176, 299), (200, 285), (220, 320), (201, 348));
        p.Polygon(new Color(250, 247, 228), (240, 285), (264, 300), (239, 348), (220, 320));
        p.Polygon(new Color(173, 89, 110), (220, 327), (190, 315), (194, 342), (220, 331), (246, 343), (250, 315));
        p.Polygon(new Color(173, 89, 110), (219, 330), (204, 375), (224, 363), (238, 375), (224, 330));
        p.Line(219, 383, 219, 515, 2, new Color(193, 177, 146));
        for (var y = 400; y < 520; y += 27) p.Ellipse(227, y, 2, 2, new Color(171, 151, 123));
        p.Polygon(dark, (164, 512), (276, 512), (300, 619), (142, 619));
        for (var x = 160; x < 289; x += 23) p.Line(x + 8, 526, x, 614, 2, new Color(103, 123, 111));
        p.Polygon(skin, (126, 444), (151, 448), (185, 409), (196, 421), (154, 484), (135, 479));
        p.Polygon(skin, (289, 446), (312, 441), (307, 478), (285, 484), (243, 426), (252, 413));
        p.Rectangle(176, 393, 89, 64, new Color(253, 248, 224));
        p.Line(177, 394, 220, 426, 2, new Color(211, 195, 172));
        p.Line(264, 394, 220, 426, 2, new Color(211, 195, 172));
        p.Ellipse(178, 427, 12, 16, skin);
        p.Ellipse(265, 432, 12, 16, skin);
        p.Polygon(skin, (168, 621), (203, 621), (200, 690), (172, 690));
        p.Polygon(skin, (239, 621), (274, 621), (268, 690), (239, 690));
        p.Ellipse(300, 184, 14, 18, new Color(173, 89, 110));
        p.Ellipse(300, 184, 5, 6, new Color(245, 225, 204));
        return p;
    }

    public void Dispose()
    {
        _files.Dispose();
        foreach (var texture in _cache.Values) texture.Dispose();
    }

    private sealed class Illustration(int width, int height)
    {
        // Generate source artwork at 4K (3x design coordinates), rather than magnifying 720p pixels.
        private const int ArtworkScale = 3;
        public int Width { get; } = width * ArtworkScale;
        public int Height { get; } = height * ArtworkScale;
        public Color[] Pixels { get; } = new Color[checked(width * height * ArtworkScale * ArtworkScale)];
        public void Rectangle(int x, int y, int w, int h, Color color)
            => FillPixels(x * ArtworkScale, y * ArtworkScale, w * ArtworkScale, h * ArtworkScale, color);

        private void FillPixels(int x, int y, int w, int h, Color color)
        {
            for (var py = Math.Max(0, y); py < Math.Min(Height, y + h); py++)
                for (var px = Math.Max(0, x); px < Math.Min(Width, x + w); px++) Pixels[py * Width + px] = color;
        }
        public void Ellipse(int cx, int cy, int rx, int ry, Color color)
            => EllipsePixels(cx * ArtworkScale, cy * ArtworkScale, rx * ArtworkScale, ry * ArtworkScale, color);

        private void EllipsePixels(int cx, int cy, int rx, int ry, Color color)
        {
            for (var y = Math.Max(0, cy - ry); y < Math.Min(Height, cy + ry + 1); y++)
                for (var x = Math.Max(0, cx - rx); x < Math.Min(Width, cx + rx + 1); x++)
                    if (Math.Pow((x - cx) / (double)rx, 2) + Math.Pow((y - cy) / (double)ry, 2) <= 1) Pixels[y * Width + x] = color;
        }
        public void Gradient(Color top, Color bottom)
        {
            for (var y = 0; y < Height; y++)
                Array.Fill(Pixels, Color.Lerp(top, bottom, y / (float)(Height - 1)), y * Width, Width);
        }
        public void Line(int x1, int y1, int x2, int y2, int thickness, Color color)
        {
            x1 *= ArtworkScale; y1 *= ArtworkScale; x2 *= ArtworkScale; y2 *= ArtworkScale; thickness *= ArtworkScale;
            var steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
            for (var i = 0; i <= steps; i++)
            {
                var t = steps == 0 ? 0 : i / (float)steps;
                EllipsePixels((int)MathHelper.Lerp(x1, x2, t), (int)MathHelper.Lerp(y1, y2, t), Math.Max(1, thickness / 2), Math.Max(1, thickness / 2), color);
            }
        }
        public void Polygon(Color color, params (int X, int Y)[] points)
        {
            points = points.Select(point => (point.X * ArtworkScale, point.Y * ArtworkScale)).ToArray();
            var intersections = new List<int>();
            for (var y = Math.Max(0, points.Min(p => p.Y)); y <= Math.Min(Height - 1, points.Max(p => p.Y)); y++)
            {
                intersections.Clear();
                for (var i = 0; i < points.Length; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Length];
                    if ((a.Y <= y && b.Y > y) || (b.Y <= y && a.Y > y))
                        intersections.Add(a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y));
                }
                intersections.Sort();
                for (var i = 0; i + 1 < intersections.Count; i += 2)
                    FillPixels(intersections[i], y, intersections[i + 1] - intersections[i] + 1, 1, color);
            }
        }
    }
}

using Microsoft.Xna.Framework;

namespace MonoGalGame.Framework.Hosting;

public partial class GalgameGame
{
    private void DrawGallery()
    {
        var artwork = Options.Theme.Artwork;
        Panel("作品画廊", "GALLERY / ROSES & MEMORIES", "留住一束光，也留住故事里的人。");
        if (artwork.Count == 0) return;
        _artworkIndex = Math.Clamp(_artworkIndex, 0, artwork.Count - 1);
        var selected = artwork[_artworkIndex];
        bool Available(GalgameArtwork item) => !item.RequiresReading || _readingProgress.Artwork.Contains(item.Texture);
        var available = Available(selected);
        var stage = new Rectangle(116, 205, 738, 398);
        _ui.Gradient(stage, new Color(236, 226, 209), Paper);
        if (!available)
        {
            _ui.Centered("尚未遇见的回忆", new(stage.X, stage.Y + 126, stage.Width, 52), 29, Ink);
            _ui.Centered("在故事中读到这一幕后，插画会自动收录。", new(stage.X, stage.Y + 194, stage.Width, 36), 16, Muted);
            _ui.Centered("MEMORIES WILL FIND THEIR PLACE", new(stage.X, stage.Y + 260, stage.Width, 30), 10, Accent);
        }
        else if (_textures.Get(selected.Texture) is { } image)
        {
            if (selected.IsPortrait)
            {
                _ui.Fill(new(stage.X + 22, stage.Y + 25, 2, stage.Height - 50), Border);
                _ui.TrackedText("CHARACTER PORTRAIT", stage.X + 38, stage.Y + 29, 10, 1.8f, Accent);
                _ui.Text(selected.Title, stage.X + 38, stage.Y + 76, 32, Ink);
                _ui.Text(selected.Caption, stage.X + 40, stage.Y + 129, 15, Muted);
                var description = MonoGalGame.Core.Text.TextLayout.Build(selected.Description, _font, 16, 270, 25, 8);
                _font.DrawPage(_batch, description[0], new(stage.X + 40, stage.Y + 184), 16, Ink * .8f, int.MaxValue);
                var height = stage.Height - 14;
                var width = (int)(height * image.Width / (float)image.Height);
                _ui.Image(image, new(stage.X + 355, stage.Y + 7, width, height), Color.White);
            }
            else _ui.Cover(image, stage, Color.White);
        }
        _ui.Outline(stage, Border);
        _ui.Ellipsis(available ? selected.Title : "未解锁的剧情插画", 119, 616, 18, Ink, 400);
        _ui.Ellipsis(available ? selected.Caption : "继续阅读，留住这一刻", 516, 620, 11, Accent, 340);

        var pageStart = _artworkIndex / 6 * 6;
        for (var index = pageStart; index < Math.Min(artwork.Count, pageStart + 6); index++)
        {
            var item = artwork[index];
            var unlocked = Available(item);
            var id = index;
            var row = new Rectangle(878, 205 + (index - pageStart) * 63, 285, 54);
            Button(row, "", () => _artworkIndex = id, primary: index == _artworkIndex);
            var preview = new Rectangle(row.X + 4, row.Y + 4, 78, 46);
            _ui.Fill(preview, Paper);
            if (!unlocked) _ui.Centered("?", preview, 20, Muted);
            else if (_textures.Get(item.Texture) is { } thumbnail)
            {
                if (item.IsPortrait)
                {
                    _ui.Fill(preview, Paper);
                    _ui.Cover(thumbnail, preview, Color.White, verticalFocus: 0);
                }
                else _ui.Cover(thumbnail, preview, Color.White);
            }
            _ui.Ellipsis(unlocked ? item.Title : "尚未遇见", row.X + 95, row.Y + 8, 15,
                index == _artworkIndex ? Color.White : Ink, 178);
            _ui.Ellipsis(unlocked ? item.Caption : "在故事中解锁", row.X + 95, row.Y + 32, 9,
                index == _artworkIndex ? Paper * .8f : Muted, 178);
        }
        Button(new(878, 594, 83, 37), "上一页", () => _artworkIndex = Math.Max(0, pageStart - 6),
            compact: true, enabled: pageStart > 0);
        _ui.Centered($"{_artworkIndex + 1:00} / {artwork.Count:00}", new(969, 594, 103, 37), 12, Accent);
        Button(new(1080, 594, 83, 37), "下一页", () => _artworkIndex = Math.Min(artwork.Count - 1, pageStart + 6),
            compact: true, enabled: pageStart + 6 < artwork.Count);
    }
}

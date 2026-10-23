using System.Text;
using MonoGalGame.Core.Projects;
using MonoGalGame.Core.Text;
using MonoGalGame.Framework.Fonts;
using MonoGalGame.Framework.Hosting;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class TextAndProjectTests
{
    private sealed class FixedMetrics : ITextMetrics
    {
        public float GetAdvance(string textElement, float fontSize) => 10;
    }

    [Fact]
    public void ChinesePunctuationDoesNotStartWrappedLines()
    {
        var pages = TextLayout.Build("春天到了，我们一起去看花。明年也一起，好吗？", new FixedMetrics(), 20, 50, 32, 20);
        foreach (var line in pages[0].Elements.GroupBy(element => element.Y))
        {
            Assert.DoesNotContain(line.First().Text, new[] { "，", "。", "？" });
            Assert.True(line.Last().X + 10 <= 50);
        }
    }

    [Fact]
    public void EnglishWordsAreKeptTogetherWhenTheyFit()
    {
        var pages = TextLayout.Build("春天 MonoGame 框架", new FixedMetrics(), 20, 100, 30, 10);
        var english = pages[0].Elements.Where(glyph => "MonoGame".Contains(glyph.Text, StringComparison.Ordinal)).ToArray();
        Assert.Equal(8, english.Length);
        Assert.Single(english.Select(glyph => glyph.Y).Distinct());
    }

    [Fact]
    public void GraphemeClustersSurviveLayoutAndTypewriterReveal()
    {
        const string text = "A\U00020000e\u0301👩‍💻";
        var page = TextLayout.Build(text, new FixedMetrics(), 20, 100, 30, 3)[0];
        Assert.Equal(4, page.ElementCount);
        Assert.Equal(text, string.Concat(page.Elements.Select(element => element.Text)));
        var writer = new Typewriter();
        writer.Reset(page.ElementCount);
        writer.Update(TimeSpan.FromSeconds(.5), 4);
        Assert.Equal(2, writer.VisibleCount);
        Assert.False(writer.IsComplete);
        writer.RevealAll();
        Assert.True(writer.IsComplete);
    }

    [Fact]
    public void PaginationPreservesContentAndExplicitEmptyLines()
    {
        var pages = TextLayout.Build(new string('春', 25), new FixedMetrics(), 20, 50, 30, 2);
        Assert.Equal(3, pages.Count);
        Assert.Equal(25, pages.Sum(page => page.ElementCount));
        Assert.Equal(3, TextLayout.Build("春\n\n天", new FixedMetrics(), 20, 50, 30, 10)[0].LineCount);
        Assert.Empty(TextLayout.Build("", new FixedMetrics(), 20, 50, 30, 2)[0].Elements);
    }

    [Fact]
    public void DemoFontRasterizesChineseWithUsefulCoverageAndAdvance()
    {
        using var face = new TrueTypeFontFace(Path.Combine(AppContext.BaseDirectory, "Game", "Fonts", "NotoSansSC-Regular.otf"));
        var rune = new Rune('春');
        Assert.True(face.Contains(rune));
        var small = face.Rasterize(rune, 24);
        var large = face.Rasterize(rune, 48);
        Assert.Contains(small.Alpha, alpha => alpha > 0);
        Assert.True(large.Width > small.Width);
        Assert.True(large.Advance > small.Advance);
        Assert.InRange(small.Advance, 23, 25);
        face.Dispose();
        Assert.Throws<ObjectDisposedException>(() => face.Contains(rune));
    }

    [Fact]
    public void GameProjectLoadsAssetsIndependentlyOfWorkingDirectory()
    {
        var project = GameProject.Load(Path.Combine(AppContext.BaseDirectory, "Game", "game.json"));
        Assert.Equal("spring-letters", project.Id);
        Assert.Equal("Spring Letters", project.TitleCaption);
        Assert.True(File.Exists(project.ResolvePath(project.Font)));
        Assert.All(project.Textures.Values, asset => Assert.True(File.Exists(project.ResolvePath(asset))));
        Assert.Equal("arrival", project.LoadStory().Start);
        Assert.Throws<InvalidDataException>(() => project.ResolvePath("../private.txt"));
        Assert.Throws<InvalidDataException>(() => project.ResolvePath(Path.GetTempPath()));
    }

    [Fact]
    public void BrokenSettingsFallBackAndOutOfRangeValuesAreClamped()
    {
        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "settings.json");
        File.WriteAllText(path, "broken");
        Assert.Equal(36, UserSettings.Load(path).TextSpeed);
        File.WriteAllText(path, """{"textSpeed":1000,"musicVolume":-1,"autoDelay":0}""");
        var settings = UserSettings.Load(path);
        Assert.Equal(100, settings.TextSpeed);
        Assert.Equal(0, settings.MusicVolume);
        Assert.Equal(.5f, settings.AutoDelay);
    }
}

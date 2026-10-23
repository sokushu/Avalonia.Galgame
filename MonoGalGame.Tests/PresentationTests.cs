using Microsoft.Xna.Framework;
using MonoGalGame.Framework.Rendering;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class PresentationTests
{
    [Theory]
    [InlineData(1280, 720, 1280, 720, 0, 0)]
    [InlineData(1920, 1080, 1920, 1080, 0, 0)]
    [InlineData(3840, 2160, 3840, 2160, 0, 0)]
    [InlineData(1024, 768, 1024, 576, 0, 96)]
    [InlineData(1600, 1000, 1600, 900, 0, 50)]
    [InlineData(720, 1280, 720, 405, 0, 437)]
    public void RenderAreaUsesActualDisplayPixels(int width, int height, int contentWidth, int contentHeight, int x, int y)
    {
        var layout = PresentationLayout.Create(width, height);
        Assert.Equal(new Rectangle(x, y, contentWidth, contentHeight), layout.Viewport);
        var bottomRight = Vector2.Transform(new(1280, 720), layout.Transform);
        Assert.InRange(Math.Abs(bottomRight.X - contentWidth), 0, .001f);
        Assert.InRange(Math.Abs(bottomRight.Y - contentHeight), 0, .001f);
    }

    [Fact]
    public void LetterboxedMouseStillHitsTheSameDesignControl()
    {
        var layout = PresentationLayout.Create(1024, 768);
        var point = layout.ToDesign(new(854, 287), new(1024, 768));
        Assert.True(new Rectangle(1046, 220, 44, 41).Contains(point));
        Assert.False(layout.Contains(new(854, 50), new(1024, 768)));
        Assert.True(layout.Contains(new(854, 287), new(1024, 768)));
    }

    [Fact]
    public void MouseCoordinatesCanDifferFromFramebufferPixels()
    {
        var layout = PresentationLayout.Create(3840, 2160);
        var point = layout.ToDesign(new(960, 540), new(1920, 1080));
        Assert.Equal(new Vector2(640, 360), point);
        Assert.True(layout.Contains(new(960, 540), new(1920, 1080)));
    }

    [Fact]
    public void HighDpiWindowKeepsItsApparentSizeWithoutExceedingTheWorkArea()
    {
        Assert.Equal(new Point(2560, 1440), PresentationLayout.FitInitialWindow(new(1280, 720), 2, new(3776, 1900)));
        var fitted = PresentationLayout.FitInitialWindow(new(1280, 720), 1.5f, new(1872, 920));
        Assert.InRange(fitted.X, 1600, 1872);
        Assert.InRange(fitted.Y, 1, 920);
        Assert.InRange(Math.Abs(fitted.X / (float)fitted.Y - 16f / 9), 0, .005f);
    }
}

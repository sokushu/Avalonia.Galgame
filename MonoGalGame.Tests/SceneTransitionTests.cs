using Microsoft.Xna.Framework;
using MonoGalGame.Framework.Rendering;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class SceneTransitionTests
{
    [Fact]
    public void ThirdActorArrivalInterpolatesTheExistingActorsPositionAndScale()
    {
        var stage = new PortraitStage();
        stage.Set([new("叶照", "ye", 420), new("唐见月", "tang", 878)]);
        stage.Update(1);
        stage.Set([new("叶照", "ye", 285, .9f), new("唐见月", "tang", 995, .9f), new("周素琴", "zhou", 640, .82f)]);
        stage.Update(.2f);
        var lead = Assert.Single(stage.Frames, frame => frame.Character == "叶照");
        Assert.Equal(1, lead.Opacity);
        Assert.InRange(lead.Center, 286, 419);
        Assert.InRange(lead.Scale, .94f, .96f);
        Assert.Equal(3, stage.Frames.Count());
        stage.Update(.2f);
        Assert.Equal(285, Assert.Single(stage.Frames, frame => frame.Character == "叶照").Center);
        Assert.Equal(.9f, Assert.Single(stage.Frames, frame => frame.Character == "叶照").Scale);
    }

    [Fact]
    public void PaintedNightBackgroundKeepsItsOwnLightAndStillBlendsTheRain()
    {
        var painted = SceneLighting.For("rain-night-painted", Color.White);
        Assert.Equal(Color.Transparent, painted.Overlay);
        Assert.Equal(1, painted.Rain);
        var day = SceneLighting.For("day", Color.White);
        Assert.Equal(.5f, SceneLighting.Lerp(painted, day, .5f).Rain);
        Assert.Equal(Color.Transparent, SceneLighting.For("cool-painted", Color.White).Overlay);
        Assert.Equal(new Color(217, 228, 240), SceneLighting.For("cool-painted", Color.White).PortraitTint);
    }

    [Fact]
    public void ChangingOneExpressionKeepsTheOtherActorOpaqueAndStill()
    {
        var stage = new PortraitStage();
        stage.Set([new("叶照", "ye", 420), new("唐见月", "tang", 878)]);
        stage.Update(1);
        stage.Set([new("叶照", "ye-serious", 420), new("唐见月", "tang", 878)]);
        stage.Update(.2f);
        var listener = Assert.Single(stage.Frames, frame => frame.Character == "唐见月");
        Assert.Equal(1, listener.Opacity);
        Assert.Equal(0, listener.OffsetY);
        Assert.Equal(878, listener.Center);
        Assert.Equal(2, stage.Frames.Count(frame => frame.Character == "叶照"));
    }

    [Fact]
    public void ActorMovesBetweenSoloAndDualPositionsWithoutDuplicatingOrDisappearing()
    {
        var stage = new PortraitStage();
        stage.Set([new("叶照", "ye", 818)]);
        stage.Update(1);
        stage.Set([new("叶照", "ye", 420), new("唐见月", "tang", 878)]);
        stage.Update(.2f);
        var ye = Assert.Single(stage.Frames, frame => frame.Character == "叶照");
        Assert.Equal(1, ye.Opacity);
        Assert.InRange(ye.Center, 421, 817);
        stage.Update(.2f);
        Assert.Equal(420, Assert.Single(stage.Frames, frame => frame.Character == "叶照").Center);
        stage.Set([new("叶照", "ye", 818)]);
        Assert.Contains(stage.Frames, frame => frame.Character == "唐见月" && frame.Opacity == 1);
        stage.Update(.4f);
        Assert.Equal("叶照", Assert.Single(stage.Frames).Character);
        Assert.Equal(818, Assert.Single(stage.Frames).Center);
    }

    [Fact]
    public void RapidRetargetAndRepeatedDialogueKeepTheVisibleBlend()
    {
        var stage = new PortraitStage();
        stage.Set([new("叶照", "smile", 818)]);
        stage.Update(1);
        stage.Set([new("叶照", "serious", 818)]);
        stage.Update(.2f);
        var before = stage.Frames.ToArray();
        stage.Set([new("叶照", "sad", 818)]);
        Assert.Equal(before, stage.Frames.ToArray());
        stage.Update(.1f);
        var progress = stage.Frames.ToArray();
        stage.Set([new("叶照", "sad", 818)]);
        Assert.Equal(progress, stage.Frames.ToArray());
        stage.Update(.3f);
        var final = Assert.Single(stage.Frames);
        Assert.Equal("sad", final.Texture);
        Assert.Equal(1, final.Opacity);
    }

    [Fact]
    public void CgEntryFadesTheExistingActorOutBeforeRemovingItsTrack()
    {
        var stage = new PortraitStage();
        stage.Set([new("叶照", "ye", 818)]);
        stage.Update(1);
        stage.Set([]);
        stage.Update(.2f);
        Assert.InRange(Assert.Single(stage.Frames).Opacity, .49f, .51f);
        stage.Update(.2f);
        Assert.Empty(stage.Frames);
        stage.Set([new("叶照", "ye", 818)]);
        stage.Update(.2f);
        Assert.InRange(Assert.Single(stage.Frames).OffsetY, 5.9f, 6.1f);
    }

    [Fact]
    public void NightAndRainBlendWithTheBackgroundInsteadOfChangingAtTheFirstFrame()
    {
        var day = SceneLighting.For("day", Color.White);
        var rain = SceneLighting.For("rain-night", Color.White);
        Assert.Equal(day, SceneLighting.Lerp(day, rain, 0));
        Assert.Equal(rain, SceneLighting.Lerp(day, rain, 1));
        var middle = SceneLighting.Lerp(day, rain, .5f);
        Assert.Equal(.5f, middle.Rain);
        Assert.InRange(middle.Overlay.A, 30, 33);
        Assert.InRange(middle.PortraitTint.R, 225, 230);
        Assert.Equal(0, SceneLighting.Lerp(rain, day, 1).Rain);
    }
}

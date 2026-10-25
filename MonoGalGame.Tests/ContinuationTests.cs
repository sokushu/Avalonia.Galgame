using MonoGalGame.Core.Storage;
using MonoGalGame.Core.Stories;
using MonoGalGame.Framework.Hosting;
using System.Text.Json.Nodes;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class ContinuationTests
{
    private static StoryDocument Story(string id = "continue-test") => StoryDocument.Parse(
        "{\"id\":\"" + id + "\",\"start\":\"line\",\"nodes\":{\"line\":{\"text\":\"继续读\",\"next\":\"end\"},\"end\":{\"kind\":\"End\"}}}");

    [Fact]
    public void ContinueIncludesQuickSaveAndSkipsCorruptOrIncompatibleNewerSaves()
    {
        using var folder = new TemporaryDirectory();
        var story = Story();
        var store = new SaveStore(folder.Path, "continue-test");
        var snapshot = new StoryPlayer(story).Capture();
        var epoch = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        AtomicJson.Write(Path.Combine(folder.Path, "slot-01.json"), new SaveRecord(1, "continue-test", epoch, snapshot));
        AtomicJson.Write(Path.Combine(folder.Path, "slot-00.json"), new SaveRecord(1, "continue-test", epoch.AddDays(1), snapshot));
        AtomicJson.Write(Path.Combine(folder.Path, "slot-02.json"), new SaveRecord(1, "continue-test", epoch.AddDays(2), snapshot with { StoryId = "different-story" }));
        File.WriteAllText(Path.Combine(folder.Path, "slot-03.json"), "{broken");
        Assert.Equal(0, store.FindLatest(story)!.Slot);
        Assert.Null(store.FindLatest(Story("new-story")));
    }

    [Fact]
    public void ReadingDiscoveriesSurviveRestartWithoutCrossingStories()
    {
        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "reading-progress.json");
        var progress = ReadingProgress.Load(path, "rose");
        Assert.True(progress.Observe("cg09"));
        Assert.False(progress.Observe("cg09"));
        Assert.True(progress.Observe("bg04", "ending-a"));
        progress.Save(path);
        var reloaded = ReadingProgress.Load(path, "rose");
        Assert.Contains("cg09", reloaded.Artwork);
        Assert.Contains("ending-a", reloaded.Endings);
        Assert.Empty(ReadingProgress.Load(path, "different-story").Artwork);
        File.WriteAllText(path, "{\"storyId\":\"rose\",\"artwork\":null}");
        Assert.Empty(ReadingProgress.Load(path, "rose").Artwork);
    }

    [Fact]
    public void DiscoveredCostumesIncludeAllPortraitLayersAndPersistWithPartialLegacyScenes()
    {
        using var folder = new TemporaryDirectory();
        var progress = ReadingProgress.Load(Path.Combine(folder.Path, "reading-progress.json"), "rose");
        Assert.True(progress.ObserveScene("winter-room", "late-tang", "new-coat", "visitor"));
        Assert.False(progress.ObserveScene("winter-room", "late-tang", "new-coat", "visitor"));
        Assert.True(progress.ObserveScene(null, "old-coat", null, ""));
        Assert.True(progress.ObserveScene(null, null, null, null, "ending-b"));
        progress.Save(Path.Combine(folder.Path, "reading-progress.json"));
        var restored = ReadingProgress.Load(Path.Combine(folder.Path, "reading-progress.json"), "rose");
        Assert.Equal(new[] { "late-tang", "new-coat", "old-coat", "visitor", "winter-room" }, restored.Artwork.Order());
        Assert.Contains("ending-b", restored.Endings);
    }

    [Fact]
    public void ExistingSaveWithoutTheThirdPortraitFieldStillContinues()
    {
        using var folder = new TemporaryDirectory();
        var story = Story();
        var store = new SaveStore(folder.Path, "continue-test");
        store.Save(1, new StoryPlayer(story).Capture());
        var path = Path.Combine(folder.Path, "slot-01.json");
        var oldSave = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.True(oldSave["snapshot"]!["scene"]!.AsObject().Remove("tertiaryPortrait"));
        File.WriteAllText(path, oldSave.ToJsonString());
        var latest = store.FindLatest(story);
        Assert.NotNull(latest);
        var player = new StoryPlayer(story);
        player.Restore(latest.Save!.Snapshot);
        Assert.Equal("", player.Scene.TertiaryPortrait);
    }

    [Fact]
    public void ThirdActorInheritsClearsRollsBackAndSurvivesSaving()
    {
        using var folder = new TemporaryDirectory();
        var story = StoryDocument.Parse("""
            {"id":"triple","start":"first","nodes":{
              "first":{"text":"三人同场","scene":{"portrait":"tang","secondaryPortrait":"ye","tertiaryPortrait":"aunt"},"next":"second"},
              "second":{"text":"她们说话","next":"third"},
              "third":{"text":"姨妈离开","scene":{"tertiaryPortrait":""},"next":"end"},
              "end":{"kind":"End"}}}
            """);
        var player = new StoryPlayer(story);
        player.Advance();
        Assert.Equal("aunt", player.Scene.TertiaryPortrait);
        var saves = new SaveStore(folder.Path, "triple");
        saves.Save(1, player.Capture());
        var restored = new StoryPlayer(story);
        restored.Restore(saves.Load(1).Snapshot);
        Assert.Equal(player.Scene, restored.Scene);
        restored.Advance();
        Assert.Equal("", restored.Scene.TertiaryPortrait);
        Assert.True(restored.Rollback());
        Assert.Equal("aunt", restored.Scene.TertiaryPortrait);
        Assert.Throws<InvalidDataException>(() => restored.Restore(restored.Capture() with { Scene = restored.Scene with { TertiaryPortrait = null! } }));
    }
}

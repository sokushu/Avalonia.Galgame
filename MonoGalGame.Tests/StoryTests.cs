using System.Text.Json;
using MonoGalGame.Core.Stories;
using MonoGalGame.Core.Storage;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class StoryTests
{
    private static StoryDocument Demo() => StoryDocument.Load(Path.Combine(AppContext.BaseDirectory, "Game", "Stories", "main.json"));

    [Theory]
    [InlineData(0, 0, "ending-letter", 3)]
    [InlineData(1, 0, "ending-letter", 4)]
    [InlineData(0, 1, "ending-promise", 1)]
    [InlineData(1, 1, "ending-promise", 2)]
    public void DemoRoutesRespectChoicesAndVariables(int destination, int reply, string ending, int affection)
    {
        var player = new StoryPlayer(Demo());
        ReachChoice(player);
        player.Choose(destination);
        ReachChoice(player);
        player.Choose(reply);
        while (!player.IsEnded) Assert.True(player.Advance());
        Assert.Equal(ending, player.CurrentNodeId);
        Assert.Equal(affection, player.Variables["affection"]);
    }

    [Fact]
    public void RollbackUndoesChoiceEffectsAndRestoresSceneAndHistory()
    {
        var player = new StoryPlayer(Demo());
        ReachChoice(player);
        var historyCount = player.History.Count;
        player.Choose(1);
        Assert.Equal("river", player.Scene.Background);
        Assert.Equal(2, player.Variables["affection"]);
        Assert.True(player.Rollback());
        Assert.Equal("destination", player.CurrentNodeId);
        Assert.Equal("campus", player.Scene.Background);
        Assert.False(player.Variables.ContainsKey("affection"));
        Assert.Equal(historyCount, player.History.Count);
        player.Choose(0);
        Assert.Equal(1, player.Variables["affection"]);
    }

    [Fact]
    public void SnapshotRestoreDoesNotRepeatNodeEffectsOrShareMutableState()
    {
        var story = StoryDocument.Parse("""
            {"id":"counter","start":"line","nodes":{
              "line":{"text":"你好","effects":[{"variable":"count","value":1,"operation":"Add"}],"next":"end"},
              "end":{"kind":"End"}}}
            """);
        var player = new StoryPlayer(story);
        var snapshot = player.Capture();
        player.Advance();
        player.Restore(snapshot);
        Assert.Equal(1, player.Variables["count"]);
        Assert.Single(player.History);
        snapshot.Variables["count"] = 99;
        snapshot.History.Clear();
        Assert.Equal(1, player.Variables["count"]);
        Assert.Single(player.History);
        Assert.False(player.CanRollback);
    }

    [Fact]
    public void AutomaticCycleFailsWithoutLosingCurrentProgress()
    {
        var story = StoryDocument.Parse("""
            {"id":"cycle","start":"line","nodes":{
              "line":{"text":"安全位置","next":"loop"},
              "loop":{"kind":"Set","effects":[{"variable":"count","value":1,"operation":"Add"}],"next":"loop"}}}
            """);
        var player = new StoryPlayer(story);
        Assert.Throws<InvalidDataException>(() => player.Advance());
        Assert.Equal("line", player.CurrentNodeId);
        Assert.Empty(player.Variables);
        Assert.Single(player.History);
    }

    [Fact]
    public void InvalidSnapshotIsRejectedBeforeMutatingPlayer()
    {
        var player = new StoryPlayer(Demo());
        var before = player.Capture();
        Assert.Throws<InvalidDataException>(() => player.Restore(before with { StoryId = "another-game" }));
        Assert.Equal(before.NodeId, player.CurrentNodeId);
        Assert.Throws<InvalidDataException>(() => player.Restore(before with { NodeId = "ending-check" }));
        Assert.Equal(before.NodeId, player.CurrentNodeId);
    }

    [Fact]
    public void ChoiceConditionsFilterOptionsAndEmptyChoicesFailTransactionally()
    {
        var story = StoryDocument.Parse("""
            {"id":"conditions","start":"line","nodes":{
              "line":{"text":"start","next":"choice"},
              "choice":{"kind":"Choice","choices":[{"text":"locked","next":"end","condition":{"variable":"key","value":1}}]},
              "end":{"kind":"End"}}}
            """);
        var player = new StoryPlayer(story);
        Assert.Throws<InvalidDataException>(() => player.Advance());
        Assert.Equal("line", player.CurrentNodeId);
        var valid = player.Capture() with { NodeId = "choice", Variables = new() { ["key"] = 1 } };
        player.Restore(valid);
        Assert.Single(player.AvailableChoices);
        Assert.Throws<InvalidDataException>(() => player.Restore(valid with { Variables = [] }));
        Assert.Single(player.AvailableChoices);
        player.Choose(0);
        Assert.True(player.IsEnded);
    }

    [Fact]
    public void InvalidTargetsAreReportedAtLoadTime()
    {
        Assert.Throws<InvalidDataException>(() => StoryDocument.Parse("""
            {"start":"a","nodes":{"a":{"text":"hello","next":"missing"}}}
            """));
    }

    [Fact]
    public void SaveRoundtripIsAtomicAndRejectsAnotherGame()
    {
        using var folder = new TemporaryDirectory();
        var store = new SaveStore(folder.Path, "demo");
        var player = new StoryPlayer(Demo());
        ReachChoice(player);
        store.Save(1, player.Capture());
        player.Choose(1);
        player.Restore(store.Load(1).Snapshot);
        Assert.Equal("destination", player.CurrentNodeId);
        store.Save(1, player.Capture());
        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));
        Assert.Throws<InvalidDataException>(() => new SaveStore(folder.Path, "other").Load(1));
        File.WriteAllText(System.IO.Path.Combine(folder.Path, "slot-02.json"), "{broken");
        var slots = store.ListSlots();
        Assert.NotNull(slots[0].Save);
        Assert.NotNull(slots[1].Error);
        Assert.Null(slots[2].Save);
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Load(-1));
    }

    private static void ReachChoice(StoryPlayer player)
    {
        for (var step = 0; step < 50 && player.CurrentNode.Kind == NodeKind.Dialogue; step++) player.Advance();
        Assert.Equal(NodeKind.Choice, player.CurrentNode.Kind);
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MonoGalGame.Tests", Guid.NewGuid().ToString("N"));
    public TemporaryDirectory() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, recursive: true);
}

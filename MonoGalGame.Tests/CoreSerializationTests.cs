using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using MonoGalGame.Core.Projects;
using MonoGalGame.Core.Storage;
using MonoGalGame.Core.Stories;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class CoreSerializationTests
{
    [Fact]
    public void GeneratedMetadataPreservesAllOmittedDocumentAndSnapshotInitializers()
    {
        var story = StoryDocument.Parse("""
            {"start":"choice","nodes":{
              "choice":{"kind":"Choice","choices":[{"text":"继续","next":"end"}]},
              "end":{"kind":"End"}}}
            """);
        Assert.Equal("story", story.Id);
        Assert.Equal("", story.Title);
        Assert.Equal("", story.Nodes["end"].Speaker);
        Assert.Equal("", story.Nodes["end"].Text);
        Assert.Empty(story.Nodes["end"].Effects);
        Assert.Empty(story.Nodes["end"].Choices);
        Assert.Empty(Assert.Single(story.Nodes["choice"].Choices).Effects);

        var snapshot = JsonSerializer.Deserialize("{}",
            (JsonTypeInfo<StorySnapshot>)StoryJson.Options.GetTypeInfo(typeof(StorySnapshot)))!;
        Assert.Equal(1, snapshot.Version);
        Assert.Equal("", snapshot.StoryId);
        Assert.Equal("", snapshot.NodeId);
        Assert.Equal(new SceneState(), snapshot.Scene);
        Assert.Empty(snapshot.Variables);
        Assert.Empty(snapshot.History);
        Assert.Empty(snapshot.ReadNodes);

        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "game.json");
        File.WriteAllText(path, "{}");
        var project = GameProject.Load(path);
        Assert.Equal("my-game", project.Id);
        Assert.Equal("My visual novel", project.Title);
        Assert.Equal("", project.Subtitle);
        Assert.Equal("", project.TitleCaption);
        Assert.Equal("Stories/main.json", project.Story);
        Assert.Equal("Fonts/NotoSansSC-Regular.otf", project.Font);
        Assert.Equal("", project.TitleBackground);
        Assert.Empty(project.Textures);
        Assert.Empty(project.Audio);
    }

    [Theory]
    [InlineData("speaker")]
    [InlineData("text")]
    [InlineData("effects")]
    [InlineData("choices")]
    public void ExplicitNullStoryMembersAreRejectedRatherThanReplacedWithDefaults(string property)
    {
        var json = "{\"start\":\"end\",\"nodes\":{\"end\":{\"kind\":\"End\",\"" + property + "\":null}}}";
        Assert.Throws<InvalidDataException>(() => StoryDocument.Parse(json));
    }

    [Theory]
    [InlineData("variables")]
    [InlineData("scene")]
    [InlineData("history")]
    [InlineData("readNodes")]
    public void ExplicitNullSnapshotMembersAreRejectedBeforeRestoringState(string property)
    {
        var story = StoryDocument.Parse("""{"start":"end","nodes":{"end":{"kind":"End"}}}""");
        var player = new StoryPlayer(story);
        var json = "{\"storyId\":\"story\",\"nodeId\":\"end\",\"" + property + "\":null}";
        var snapshot = JsonSerializer.Deserialize(json,
            (JsonTypeInfo<StorySnapshot>)StoryJson.Options.GetTypeInfo(typeof(StorySnapshot)))!;
        Assert.Throws<InvalidDataException>(() => player.Restore(snapshot));
        Assert.Equal("end", player.CurrentNodeId);
    }

    [Theory]
    [InlineData("subtitle")]
    [InlineData("titleCaption")]
    [InlineData("textures")]
    [InlineData("audio")]
    public void ExplicitNullProjectMembersAreRejectedRatherThanReplacedWithDefaults(string property)
    {
        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "game.json");
        File.WriteAllText(path, "{\"" + property + "\":null}");
        Assert.Throws<InvalidDataException>(() => GameProject.Load(path));
    }

    [Fact]
    public void GeneratedStoryMetadataPreservesCaseInsensitiveNamesEnumsAndRecordDefaults()
    {
        var story = StoryDocument.Parse("""
            {"ID":"中文剧情","START":"line","NODES":{
              "line":{"KIND":"dialogue","SPEAKER":"小春","TEXT":"你好，世界。🌸",
                "SCENE":{"BACKGROUND":"classroom"},
                "EFFECTS":[{"VARIABLE":"score","VALUE":1,"OPERATION":"aDd"}],"NEXT":"choice"},
              "choice":{"KIND":"choice","CHOICES":[{"TEXT":"继续","NEXT":"check",
                "CONDITION":{"VARIABLE":"score","VALUE":1,"COMPARISON":"greaterOrEqual"},
                "EFFECTS":[{"VARIABLE":"score","VALUE":2}]}]},
              "check":{"KIND":"branch","CONDITION":{"VARIABLE":"score","VALUE":2},
                "THEN":"end","ELSE":"other"},
              "end":{"KIND":"end"},"other":{"KIND":"end"}}}
            """);

        var player = new StoryPlayer(story);
        Assert.Equal("你好，世界。🌸", player.CurrentNode.Text);
        Assert.Equal(new SceneState("classroom"), player.Scene);
        Assert.Equal(1, player.Variables["score"]);
        Assert.True(player.Advance());
        Assert.Single(player.AvailableChoices);
        player.Choose(0);
        Assert.Equal("end", player.CurrentNodeId);
        Assert.Equal(2, player.Variables["score"]);
    }

    [Theory]
    [InlineData("{\"kind\":0}")]
    [InlineData("{\"kind\":\"0\"}")]
    [InlineData("{\"kind\":\"Unknown\"}")]
    [InlineData("{\"kind\":\"End\",\"condition\":{\"variable\":\"score\",\"value\":1,\"comparison\":0}}")]
    [InlineData("{\"kind\":\"End\",\"effects\":[{\"variable\":\"score\",\"value\":1,\"operation\":0}]}")]
    public void GeneratedStoryMetadataRejectsNumericAndUnknownEnumValues(string node)
    {
        var json = "{\"start\":\"line\",\"nodes\":{\"line\":" + node + "}}";
        Assert.Throws<JsonException>(() => StoryDocument.Parse(json));
    }

    [Fact]
    public void GeneratedSaveMetadataRoundtripsCompleteStateAndExistingSaveFormat()
    {
        using var folder = new TemporaryDirectory();
        var snapshot = new StorySnapshot
        {
            StoryId = "中文剧情",
            NodeId = "chapter-2",
            Variables = new() { ["好感度"] = 7, ["Score"] = 2, ["score"] = 3 },
            Scene = new("教室", "小春", "春日"),
            History = [new("chapter-1", "小春", "你好，世界。🌸")],
            ReadNodes = ["chapter-1", "chapter-2"]
        };
        var store = new SaveStore(folder.Path, "test-game");
        store.Save(0, snapshot);
        var saved = store.Load(0);

        Assert.Equal(1, saved.Version);
        Assert.Equal("test-game", saved.GameId);
        Assert.Equal(TimeSpan.Zero, saved.SavedAt.Offset);
        Assert.Equal(snapshot.StoryId, saved.Snapshot.StoryId);
        Assert.Equal(snapshot.NodeId, saved.Snapshot.NodeId);
        Assert.Equal(3, saved.Snapshot.Variables.Count);
        Assert.Equal(7, saved.Snapshot.Variables["好感度"]);
        Assert.Equal(2, saved.Snapshot.Variables["Score"]);
        Assert.Equal(3, saved.Snapshot.Variables["score"]);
        Assert.Equal(snapshot.Scene, saved.Snapshot.Scene);
        Assert.Equal(snapshot.History, saved.Snapshot.History);
        Assert.True(snapshot.ReadNodes.SetEquals(saved.Snapshot.ReadNodes));
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder.Path, "slot-00.json")));
        Assert.Equal("chapter-2", json.RootElement.GetProperty("snapshot").GetProperty("nodeId").GetString());

        File.WriteAllText(Path.Combine(folder.Path, "slot-01.json"), """
            {"VERSION":1,"GAMEID":"test-game","SAVEDAT":"2026-10-23T12:34:56+08:00",
             "SNAPSHOT":{"STORYID":"中文剧情","NODEID":"line","SCENE":{"BACKGROUND":"教室"},
             "HISTORY":[{"NODEID":"line","SPEAKER":"小春","TEXT":"你好"}],"READNODES":["line"]}}
            """);
        var legacy = store.Load(1);
        Assert.Equal(TimeSpan.FromHours(8), legacy.SavedAt.Offset);
        Assert.Equal(new SceneState("教室"), legacy.Snapshot.Scene);
        Assert.Empty(legacy.Snapshot.Variables);
        Assert.Equal("你好", Assert.Single(legacy.Snapshot.History).Text);
        Assert.Contains("line", legacy.Snapshot.ReadNodes);
    }

    [Fact]
    public void GeneratedProjectMetadataRetainsAssetMapsAndIgnoresRuntimeRoot()
    {
        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "game.json");
        AtomicJson.Write(path, new GameProject
        {
            Id = "test-game",
            Title = "春日来信",
            Textures = new() { ["教室"] = "Images/classroom.png" },
            Audio = new() { ["春日"] = "Audio/spring.wav" }
        });
        var project = GameProject.Load(path);
        Assert.Equal("春日来信", project.Title);
        Assert.Equal(Path.GetFullPath(folder.Path), project.RootDirectory);
        Assert.Equal("Images/classroom.png", project.Textures["教室"]);
        Assert.Equal("Audio/spring.wav", project.Audio["春日"]);
        AtomicJson.Write(path, project);
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.False(json.RootElement.TryGetProperty("rootDirectory", out _));
    }

    [Fact]
    public void AtomicWriterSupportsCustomGeneratedMetadataWithoutReflectionFallback()
    {
        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "custom.json");
        var value = new CustomSaveData("额外数据", 12);
        AtomicJson.Write(path, value, CoreSerializationTestContext.Default.CustomSaveData);
        var actual = JsonSerializer.Deserialize(File.ReadAllText(path), CoreSerializationTestContext.Default.CustomSaveData);
        Assert.Equal(value, actual);
        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));

        var unknownPath = Path.Combine(folder.Path, "unregistered", "custom.json");
        Assert.Throws<NotSupportedException>(() => AtomicJson.Write(unknownPath, value));
        Assert.False(Directory.Exists(Path.GetDirectoryName(unknownPath)));
    }
}

internal sealed record CustomSaveData(string Label, int Level);

[JsonSerializable(typeof(CustomSaveData))]
internal sealed partial class CoreSerializationTestContext : JsonSerializerContext;

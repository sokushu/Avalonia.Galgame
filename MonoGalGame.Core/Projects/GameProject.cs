using System.Text.Json;
using MonoGalGame.Core.Stories;

namespace MonoGalGame.Core.Projects;

public sealed class GameProject
{
    public string Id { get; set; } = "my-game";
    public string Title { get; set; } = "My visual novel";
    public string Subtitle { get; set; } = "";
    public string TitleCaption { get; set; } = "";
    public string Story { get; set; } = "Stories/main.json";
    public string Font { get; set; } = "Fonts/NotoSansSC-Regular.otf";
    public string TitleBackground { get; set; } = "";
    public Dictionary<string, string> Textures { get; set; } = [];
    public Dictionary<string, string> Audio { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public string RootDirectory { get; private set; } = "";

    public static GameProject Load(string path)
    {
        var project = JsonSerializer.Deserialize(File.ReadAllText(path), StoryJson.Context.GameProject)
            ?? throw new InvalidDataException("Game project is empty.");
        if (string.IsNullOrWhiteSpace(project.Id) || project.Id is "." or ".." ||
            project.Id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.')) ||
            string.IsNullOrWhiteSpace(project.Title) || project.Subtitle is null || project.TitleCaption is null ||
            project.Textures is null || project.Audio is null)
            throw new InvalidDataException("Game project needs an ASCII id, a title and valid asset maps.");
        project.RootDirectory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        project.ResolvePath(project.Story);
        project.ResolvePath(project.Font);
        foreach (var asset in project.Textures.Values.Concat(project.Audio.Values)) project.ResolvePath(asset);
        return project;
    }

    /// <summary>All file assets are relative to the game directory, independent of the working directory.</summary>
    public string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Asset paths must be relative to the game directory.");
        var fullPath = Path.GetFullPath(relativePath, RootDirectory);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!fullPath.StartsWith(RootDirectory + Path.DirectorySeparatorChar, comparison))
            throw new InvalidDataException($"Asset path escapes the game directory: '{relativePath}'.");
        return fullPath;
    }

    public StoryDocument LoadStory() => StoryDocument.Load(ResolvePath(Story));
}

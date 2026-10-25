using System.Text.Json;
using MonoGalGame.Core.Projects;
using MonoGalGame.Core.Storage;
using MonoGalGame.Core.Stories;

namespace MonoGalGame.Framework.Hosting;

public sealed record GalgameOptions(GameProject Project, StoryDocument Story)
{
    public string? DataDirectory { get; init; }
    public int WindowWidth { get; init; } = 1280;
    public int WindowHeight { get; init; } = 720;
    public bool ScaleWindowToDpi { get; init; } = true;
    public GalgameTheme Theme { get; init; } = new();
    public static GalgameOptions FromProject(string path)
    {
        var project = GameProject.Load(path);
        return new(project, project.LoadStory());
    }
}

public sealed class UserSettings
{
    public float TextSpeed { get; set; } = 36;
    public float AutoDelay { get; set; } = 2;
    public float MusicVolume { get; set; } = .55f;
    public float VoiceVolume { get; set; } = .8f;
    public bool Fullscreen { get; set; }

    public static UserSettings Load(string path)
    {
        try
        {
            var result = File.Exists(path)
                ? JsonSerializer.Deserialize(File.ReadAllText(path), FrameworkJsonContext.Default.UserSettings) ?? new() : new();
            result.TextSpeed = Clamp(result.TextSpeed, 10, 100, 36);
            result.AutoDelay = Clamp(result.AutoDelay, .5f, 5, 2);
            result.MusicVolume = Clamp(result.MusicVolume, 0, 1, .55f);
            result.VoiceVolume = Clamp(result.VoiceVolume, 0, 1, .8f);
            return result;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }

    private static float Clamp(float value, float min, float max, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    public void Save(string path) => AtomicJson.Write(path, this, FrameworkJsonContext.Default.UserSettings);
}

using System.Text.Json;
using MonoGalGame.Core.Storage;

namespace MonoGalGame.Framework.Hosting;

/// <summary>Gallery discoveries persist independently of rollback and individual save slots.</summary>
public sealed class ReadingProgress
{
    public string StoryId { get; set; } = "";
    public HashSet<string> Artwork { get; set; } = [];
    public HashSet<string> Endings { get; set; } = [];

    public static ReadingProgress Load(string path, string storyId)
    {
        try
        {
            if (File.Exists(path))
            {
                var value = JsonSerializer.Deserialize(File.ReadAllText(path), FrameworkJsonContext.Default.ReadingProgress);
                if (value?.StoryId == storyId && value.Artwork is not null && value.Endings is not null)
                    return value;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        return new() { StoryId = storyId };
    }

    public bool Observe(string background, string? ending = null)
    {
        var changed = !string.IsNullOrWhiteSpace(background) && Artwork.Add(background);
        if (!string.IsNullOrWhiteSpace(ending)) changed |= Endings.Add(ending);
        return changed;
    }

    public bool ObserveScene(string? background, string? portrait, string? secondaryPortrait,
        string? tertiaryPortrait, string? ending = null)
    {
        var changed = Observe(background ?? "", ending);
        changed |= Observe(portrait ?? "");
        changed |= Observe(secondaryPortrait ?? "");
        changed |= Observe(tertiaryPortrait ?? "");
        return changed;
    }

    public void Save(string path) => AtomicJson.Write(path, this, FrameworkJsonContext.Default.ReadingProgress);
}

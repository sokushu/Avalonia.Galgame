using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MonoGalGame.Core.Stories;

namespace MonoGalGame.Core.Storage;

public sealed record SaveRecord(int Version, string GameId, DateTimeOffset SavedAt, StorySnapshot Snapshot);
public sealed record SaveSlotInfo(int Slot, SaveRecord? Save, string? Error = null);

public sealed class SaveStore
{
    private readonly string _directory;
    private readonly string _gameId;
    public const int ManualSlotCount = 6;

    public SaveStore(string directory, string gameId)
    {
        _directory = Path.GetFullPath(directory);
        _gameId = gameId;
    }

    public bool Exists(int slot) => File.Exists(SlotPath(slot));

    public void Save(int slot, StorySnapshot snapshot)
    {
        var record = new SaveRecord(1, _gameId, DateTimeOffset.UtcNow, snapshot);
        AtomicJson.Write(SlotPath(slot), record, StoryJson.Context.SaveRecord);
    }

    public SaveRecord Load(int slot)
    {
        var save = JsonSerializer.Deserialize(File.ReadAllText(SlotPath(slot)), StoryJson.Context.SaveRecord);
        if (save is null || save.Version != 1 || save.GameId != _gameId || save.Snapshot is null)
            throw new InvalidDataException("This save belongs to another game or uses an unsupported version.");
        return save;
    }

    public IReadOnlyList<SaveSlotInfo> ListSlots() => Enumerable.Range(1, ManualSlotCount).Select(slot =>
    {
        try { return new SaveSlotInfo(slot, Exists(slot) ? Load(slot) : null); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
        { return new SaveSlotInfo(slot, null, ex.Message); }
    }).ToArray();

    // Slot 0 is reserved for quick save.
    private string SlotPath(int slot)
    {
        if (slot is < 0 or > ManualSlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(_directory, $"slot-{slot:D2}.json");
    }
}

public static class AtomicJson
{
    /// <summary>Writes a Core document or save using its generated JSON metadata.</summary>
    public static void Write<T>(string path, T value) =>
        Write(path, value, (JsonTypeInfo<T>)StoryJson.Options.GetTypeInfo(typeof(T)));

    /// <summary>Writes a custom type without reflection. Pass metadata from a generated JSON context.</summary>
    public static void Write<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, typeInfo);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

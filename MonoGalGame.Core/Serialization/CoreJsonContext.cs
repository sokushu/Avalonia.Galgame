using System.Text.Json.Serialization;
using MonoGalGame.Core.Projects;
using MonoGalGame.Core.Storage;
using MonoGalGame.Core.Stories;

namespace MonoGalGame.Core.Serialization;

// Generate every nested story and save type at build time; Native AOT cannot discover
// their constructors and properties through the reflection serializer at runtime.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(StoryDocument))]
[JsonSerializable(typeof(GameProject))]
[JsonSerializable(typeof(SaveRecord))]
internal sealed partial class CoreJsonContext : JsonSerializerContext;

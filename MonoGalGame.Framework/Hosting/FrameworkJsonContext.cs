using System.Text.Json.Serialization;

namespace MonoGalGame.Framework.Hosting;

// The metadata is compiled with the framework, so settings never need reflection at runtime.
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true, WriteIndented = true)]
[JsonSerializable(typeof(UserSettings))]
[JsonSerializable(typeof(ReadingProgress))]
internal sealed partial class FrameworkJsonContext : JsonSerializerContext;

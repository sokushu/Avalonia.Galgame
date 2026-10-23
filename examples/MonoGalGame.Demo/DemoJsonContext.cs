using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MonoGalGame.Demo;

internal sealed record RuntimeSnapshot(bool IsDynamicCodeSupported, bool IsDynamicCodeCompiled, bool HasManagedEntryAssembly)
{
    // PublishAot also disables these feature switches in ordinary managed build output.
    // An on-disk entry assembly distinguishes that output from the native executable.
    [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "An empty Assembly.Location is expected and intentionally inspected for this runtime diagnostic; no file is accessed through it.")]
    public static RuntimeSnapshot Current => new(RuntimeFeature.IsDynamicCodeSupported, RuntimeFeature.IsDynamicCodeCompiled,
        !string.IsNullOrEmpty(typeof(RuntimeSnapshot).Assembly.Location));

    public void RequireAot()
    {
        if (IsDynamicCodeSupported || IsDynamicCodeCompiled || HasManagedEntryAssembly)
            throw new InvalidOperationException("--require-aot requires the published Native AOT executable; a managed assembly or dynamic code runtime was detected.");
    }
}

internal sealed record SmokeResult(bool Passed, bool DisplayTest, RuntimeSnapshot Runtime, string? AudioError = null);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DisplaySnapshot))]
[JsonSerializable(typeof(SmokeResult))]
internal sealed partial class DemoJsonContext : JsonSerializerContext;

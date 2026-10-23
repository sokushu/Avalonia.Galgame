using MonoGalGame.Core.Projects;
using MonoGalGame.Core.Stories;
using MonoGalGame.Framework.Fonts;
using MonoGalGame.Framework.Hosting;
using MonoGalGame.Demo;

try
{
    // Native AOT otherwise uses the Windows console code page for redirected diagnostics.
    Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    string? Option(string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0) return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"{name} needs a value.");
        return args[index + 1];
    }
    var runtime = RuntimeSnapshot.Current;
    if (args.Contains("--require-aot")) runtime.RequireAot();
    var projectPath = Option("--game") ?? Path.Combine(AppContext.BaseDirectory, "Game", "game.json");
    var options = GalgameOptions.FromProject(projectPath) with { DataDirectory = Option("--data-dir") };
    if (args.Contains("--validate"))
    {
        var project = options.Project;
        using var font = new TrueTypeFontFace(project.ResolvePath(project.Font));
        if (!font.Contains(new System.Text.Rune('春')) || font.Rasterize(new System.Text.Rune('春'), 32).Alpha.All(alpha => alpha == 0))
            throw new InvalidDataException("Chinese font validation failed.");
        foreach (var path in project.Textures.Values.Concat(project.Audio.Values))
            if (!File.Exists(project.ResolvePath(path))) throw new FileNotFoundException(path);
        // Traverse both demo choices to verify the supplied content can reach an ending.
        if (project.Id == "spring-letters")
        {
            foreach (var route in new[] { 0, 1 })
            {
                var player = new StoryPlayer(options.Story);
                for (var step = 0; !player.IsEnded && step < 100; step++)
                {
                    if (player.CurrentNode.Kind == NodeKind.Choice) player.Choose(route % player.AvailableChoices.Count);
                    else player.Advance();
                }
                if (!player.IsEnded) throw new InvalidDataException("Demo route did not end.");
            }
        }
        Console.WriteLine($"Validated {project.Title}: {options.Story.Nodes.Count} nodes, Chinese glyphs and file assets.");
        Console.WriteLine($"Runtime: dynamic code supported={runtime.IsDynamicCodeSupported}, compiled={runtime.IsDynamicCodeCompiled}, managed entry assembly={runtime.HasManagedEntryAssembly}.");
        return 0;
    }
    var smokeDirectory = Option("--smoke-test");
    if (smokeDirectory is not null)
        options = options with
        {
            DataDirectory = Path.Combine(Path.GetFullPath(smokeDirectory), "player-data"),
            WindowWidth = 1024,
            WindowHeight = 768,
            ScaleWindowToDpi = false
        };
    if (smokeDirectory is not null)
    {
        new UserSettings().Save(Path.Combine(options.DataDirectory!, "settings.json"));
        File.WriteAllText(Path.Combine(Path.GetFullPath(smokeDirectory), "success.json"),
            System.Text.Json.JsonSerializer.Serialize(new SmokeResult(false, args.Contains("--display-test"), runtime), DemoJsonContext.Default.SmokeResult));
    }
    if (args.Contains("--display-test") && smokeDirectory is null)
        throw new ArgumentException("--display-test requires --smoke-test <directory>.");
    using var game = new DemoGame(options, smokeDirectory, args.Contains("--display-test"));
    game.Run();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

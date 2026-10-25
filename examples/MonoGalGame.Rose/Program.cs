using MonoGalGame.Framework.Hosting;
using MonoGalGame.Rose;

try
{
    Console.OutputEncoding = new System.Text.UTF8Encoding(false);
    string? Option(string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0) return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"{name} needs a value.");
        return args[index + 1];
    }
    // macOS bundles keep assets in Contents/Resources, beside Contents/MacOS.
    var bundledProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "Game", "game.json"));
    var defaultProject = OperatingSystem.IsMacOS() && File.Exists(bundledProject)
        ? bundledProject : Path.Combine(AppContext.BaseDirectory, "Game", "game.json");
    var options = GalgameOptions.FromProject(Option("--game") ?? defaultProject)
        with { DataDirectory = Option("--data-dir"), Theme = RoseTheme.Create(), WindowWidth = 1440, WindowHeight = 810 };
    if (args.Contains("--require-aot") && System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
        throw new InvalidOperationException("This check requires the Native AOT executable.");
    if (args.Contains("--validate"))
    {
        RoseValidation.Validate(options);
        return 0;
    }
    var smoke = Option("--smoke-test");
    string? expectedContinue = null;
    if (smoke is not null)
    {
        smoke = Path.GetFullPath(smoke);
        Directory.CreateDirectory(smoke);
        options = options with { DataDirectory = Path.Combine(smoke, "player-data"), WindowWidth = 1280,
            WindowHeight = 720, ScaleWindowToDpi = false };
        new UserSettings().Save(Path.Combine(options.DataDirectory, "settings.json"));
        var previousSession = new MonoGalGame.Core.Stories.StoryPlayer(options.Story);
        previousSession.Advance();
        previousSession.Advance();
        // Simulate an older save with valid progress but stale presentation data.
        var legacy = previousSession.Capture() with { Scene = new("bg09", "ye-zhao", "letter") };
        new MonoGalGame.Core.Storage.SaveStore(options.DataDirectory, options.Project.Id).Save(1, legacy);
        expectedContinue = previousSession.CurrentNodeId;
    }
    using var game = new RoseGame(options, smoke, expectedContinue);
    game.Run();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

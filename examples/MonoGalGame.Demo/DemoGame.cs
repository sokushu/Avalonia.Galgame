using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGalGame.Core.Stories;
using MonoGalGame.Framework.Assets;
using MonoGalGame.Framework.Audio;
using MonoGalGame.Framework.Hosting;
using MonoGalGame.Framework.Fonts;
using MonoGalGame.Framework.Rendering;

namespace MonoGalGame.Demo;

internal sealed class DemoGame(GalgameOptions options, string? smokeDirectory, bool displayTest = false) : GalgameGame(options)
{
    private int _frame;
    private DynamicFont? _diagnosticFont;
    private IAudioService? _diagnosticAudio;
    private float _logicalTextWidth;
    private bool _capturePending;
    protected override IGameFont CreateFont()
    {
        var font = base.CreateFont();
        _diagnosticFont = font as DynamicFont;
        _logicalTextWidth = font.Measure("春日来信 MonoGame", 26);
        return font;
    }
    protected override ITextureSource CreateTextureSource() => new DemoTextureSource(GraphicsDevice, Options.Project);
    protected override IAudioService CreateAudioService() => _diagnosticAudio = base.CreateAudioService();
    protected override void LoadContent()
    {
        base.LoadContent();
        if (smokeDirectory is not null) Settings.TextSpeed = 36;
    }
    protected override bool HasInputFocus => smokeDirectory is not null || base.HasInputFocus;
    protected override KeyboardState PollKeyboard() => smokeDirectory is null ? base.PollKeyboard() : _frame switch
    {
        24 => new(Keys.Enter), 34 or 39 or 44 or 109 => new(Keys.Space), 99 => new(Keys.D1),
        123 => new(Keys.F5), 200 => new(Keys.F9), 252 when displayTest => new(Keys.Enter),
        349 or 379 when displayTest => new(Keys.F11),
        436 or 438 or 440 when displayTest => new(Keys.Right),
        442 or 563 when displayTest => new(Keys.Enter),
        452 or 467 when displayTest => new(Keys.Escape),
        489 when displayTest => new(Keys.Space), _ => new()
    };
    protected override MouseState PollMouse()
    {
        if (smokeDirectory is null) return base.PollMouse();
        // 1024x768 letterboxed window: design (1068,239) maps to window (854,287).
        var position = displayTest ? _frame switch
        {
            454 => new Point(857, 478), // Vertical title menu: load.
            470 => new Point(761, 478), // Vertical title menu: start.
            550 => new Point(228, 332), // First save card: overwrite confirmation.
            _ => new Point(854, 287)
        } : new Point(854, 287);
        var pressed = _frame == 155 || displayTest && _frame is 454 or 470 or 550;
        return new MouseState(position.X, position.Y, 0, pressed ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }

    protected override void Update(GameTime gameTime)
    {
        // Fixed-step catch-up can run several updates without drawing. Do not skip a visual checkpoint.
        if (smokeDirectory is not null && _capturePending) return;
        base.Update(gameTime);
        if (smokeDirectory is null) return;
        _frame++;
        switch (_frame)
        {
            case 25:
                if (Screen != GameScreen.Reading) throw new InvalidOperationException("Keyboard start failed.");
                break;
            case 75:
                while (Player.CurrentNode.Kind == NodeKind.Dialogue) Player.Advance();
                if (Player.CurrentNode.Kind != NodeKind.Choice) throw new InvalidOperationException("Expected a choice.");
                break;
            case 100:
                if (Player.CurrentNodeId != "library-arrival") throw new InvalidOperationException("Keyboard choice failed.");
                break;
            case 125: SaveToSlot(1); OpenScreen(GameScreen.Save); break;
            case 150: CloseScreen(); OpenScreen(GameScreen.Settings); break;
            case 158:
                if (Settings.TextSpeed != 46) throw new InvalidOperationException("Letterboxed mouse hit testing failed.");
                break;
            case 175: CloseScreen(); OpenScreen(GameScreen.History); break;
            case 200:
                CloseScreen();
                Player.Advance();
                break;
            case 202:
                if (Player.CurrentNodeId != "library-arrival") throw new InvalidOperationException("Save/load roundtrip failed.");
                break;
            case 225:
                for (var step = 0; !Player.IsEnded && step < 100; step++)
                {
                    if (Player.CurrentNode.Kind == NodeKind.Choice) Player.Choose(0);
                    else Player.Advance();
                }
                if (!Player.IsEnded) throw new InvalidOperationException("Smoke route did not reach an ending.");
                break;
            case 250: if (!displayTest) CompleteSmoke(); break;
            case 255: DisplayProbe.Resize(this, 1920, 1080); break;
            case 285: DisplayProbe.Resize(this, 1600, 1000); break;
            case 315: DisplayProbe.Resize(this, 3840, 2160); break;
            case 405: DisplayProbe.Resize(this, 1024, 768); break;
            case 444:
                if (Screen != GameScreen.Settings) throw new InvalidOperationException("Vertical menu keyboard navigation failed.");
                break;
            case 456:
                if (Screen != GameScreen.Load) throw new InvalidOperationException($"Vertical load menu mouse hit testing failed: {Screen}.");
                break;
            case 472:
                if (Screen != GameScreen.Reading || Player.CurrentNodeId != "arrival")
                    throw new InvalidOperationException("Vertical start menu mouse hit testing failed.");
                break;
            case 480:
                while (Player.CurrentNode.Kind == NodeKind.Dialogue) Player.Advance();
                Player.Choose(1);
                Player.Advance();
                if (Player.CurrentNodeId != "river-talk") throw new InvalidOperationException("Expected the riverside portrait scene.");
                break;
            case 530: OpenScreen(GameScreen.Pause); break;
            case 545: CloseScreen(); OpenScreen(GameScreen.Save); break;
            case 565:
                if (Saves.Load(1).Snapshot.NodeId != "river-talk")
                    throw new InvalidOperationException("Save card overwrite confirmation failed.");
                break;
            case 575: CompleteSmoke(); break;
        }
        // Render controls before simulated clicks, including catch-up updates after PNG encoding.
        _capturePending = _frame is 20 or 70 or 95 or 120 or 145 or 170 or 195 or 245 ||
            (displayTest && _frame is 275 or 310 or 340 or 375 or 400 or 425 or 450 or 454 or 465 or 470 or 515 or 540 or 550 or 558);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        if (smokeDirectory is null || !_capturePending) return;
        if (_frame == 20) WriteDisplayReport("display");
        var name = _frame switch
        {
            20 => "01-title", 70 => "02-dialogue", 95 => "03-choices", 120 => "04-library",
            145 => "05-saves", 170 => "06-settings", 195 => "07-history", 245 => "08-ending", _ => null
        };
        if (name is not null) CaptureScreenshot(Path.Combine(smokeDirectory, name + ".png"));
        if (displayTest)
        {
            var displayName = _frame switch
            {
                275 => "09-native-1080p", 310 => "10-resize-letterbox", 340 => "11-native-4k",
                375 => "12-fullscreen", 400 => "13-windowed", 425 => "14-resized-back",
                450 => "15-title-menu-settings", 465 => "16-title-menu-load", 515 => "17-river-portrait",
                540 => "18-pause", 558 => "19-overwrite", _ => null
            };
            if (displayName is not null)
            {
                WriteDisplayReport(displayName);
                CaptureScreenshot(Path.Combine(smokeDirectory, displayName + ".png"));
            }
        }
        _capturePending = false;
    }

    private void WriteDisplayReport(string name)
    {
        Directory.CreateDirectory(smokeDirectory!);
        var report = DisplayProbe.Inspect(this, _diagnosticFont);
        File.WriteAllText(Path.Combine(smokeDirectory!, name + ".json"),
            System.Text.Json.JsonSerializer.Serialize(report, DemoJsonContext.Default.DisplaySnapshot));
        if (!report.TitleMatches || report.ManagedTitle != Options.Project.Title)
            throw new InvalidOperationException("Native window title did not preserve Chinese text.");
        if (OperatingSystem.IsWindows() && report.WindowsDpiAwareness != 2)
            throw new InvalidOperationException("Window is not per-monitor DPI aware.");
        if (report.BackBuffer != report.Drawable)
            throw new InvalidOperationException("Backbuffer does not match the actual drawable resolution.");
        var layout = PresentationLayout.Create(report.BackBuffer.Width, report.BackBuffer.Height);
        if (report.Content.Width != layout.Viewport.Width || report.Content.Height != layout.Viewport.Height)
            throw new InvalidOperationException("Rendering is not at native content resolution.");
        if (report.FontRasterScale is null || Math.Abs(report.FontRasterScale.Value - report.RenderScale) > .0001f)
            throw new InvalidOperationException("Font rasterization density does not match rendering density.");
        if (_diagnosticFont is not null && Math.Abs(_diagnosticFont.Measure("春日来信 MonoGame", 26) - _logicalTextWidth) > .001f)
            throw new InvalidOperationException("Resizing changed logical text metrics.");
    }

    private void CompleteSmoke()
    {
        File.WriteAllText(Path.Combine(smokeDirectory!, "success.json"),
            System.Text.Json.JsonSerializer.Serialize(new SmokeResult(true, displayTest, RuntimeSnapshot.Current, _diagnosticAudio?.LastError), DemoJsonContext.Default.SmokeResult));
        Console.WriteLine("Smoke test passed: native Chinese title, DPI, native rendering, keyboard, mouse, choices, saves and menus.");
        Exit();
    }
}

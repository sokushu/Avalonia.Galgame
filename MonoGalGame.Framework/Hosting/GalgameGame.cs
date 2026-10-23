using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGalGame.Core.Storage;
using MonoGalGame.Core.Stories;
using MonoGalGame.Core.Text;
using MonoGalGame.Framework.Assets;
using MonoGalGame.Framework.Audio;
using MonoGalGame.Framework.Fonts;
using MonoGalGame.Framework.Rendering;

namespace MonoGalGame.Framework.Hosting;

public enum GameScreen { Title, Reading, Pause, Save, Load, History, Settings }

/// <summary>A reusable desktop visual novel host. Game-specific content and assets live outside this assembly.</summary>
public partial class GalgameGame : Game
{
    public const int DesignWidth = 1280;
    public const int DesignHeight = 720;
    private readonly GraphicsDeviceManager _graphics;
    private readonly Stack<GameScreen> _navigation = [];
    private readonly List<UiButton> _buttons = [];
    private readonly Typewriter _writer = new();
    private readonly string _settingsPath;
    private SpriteBatch _batch = null!;
    private UiPainter _ui = null!;
    private IGameFont _font = null!;
    private ITextureSource _textures = null!;
    private IAudioService _audio = null!;
    private RenderTarget2D _canvas = null!;
    private KeyboardState _keyboard;
    private MouseState _mouse;
    private Vector2 _pointer;
    private Rectangle _viewport;
    private PresentationLayout _presentation;
    private Point _windowedSize;
    private Point _clientSize;
    private IntPtr _titledWindow;
    private IReadOnlyList<TextPage> _pages = [];
    private IReadOnlyList<SaveSlotInfo> _slots = [];
    private int _page, _focus, _choiceFocus, _historyOffset;
    private int? _overwriteSlot;
    private long _shownRevision = -1;
    private SceneState? _shownScene;
    private bool _hasSession, _hidden;
    private double _time, _autoElapsed, _skipElapsed, _toastUntil;
    private string _toast = "";
    private string? _lastAudioError;
    private float _transition;

    protected GalgameOptions Options { get; }
    public StoryPlayer Player { get; }
    public SaveStore Saves { get; }
    public UserSettings Settings { get; }
    public GameScreen Screen { get; private set; } = GameScreen.Title;
    public bool AutoAdvance { get; private set; }
    public string DataDirectory { get; }
    public Point ContentPixelSize => _canvas is null ? Point.Zero : new(_canvas.Width, _canvas.Height);
    public float RenderScale => Math.Min(_presentation.Scale.X, _presentation.Scale.Y);

    static GalgameGame()
    {
        // Game's base constructor initializes SDL. The hint must exist before that constructor runs.
        if (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("SDL_WINDOWS_DPI_AWARENESS") is null)
            Environment.SetEnvironmentVariable("SDL_WINDOWS_DPI_AWARENESS", "permonitorv2");
    }

    public GalgameGame(GalgameOptions options)
    {
        Options = options;
        Player = new(options.Story);
        DataDirectory = options.DataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonoGalGame", options.Project.Id);
        Saves = new(DataDirectory, options.Project.Id);
        _settingsPath = Path.Combine(DataDirectory, "settings.json");
        Settings = UserSettings.Load(_settingsPath);
        _windowedSize = SdlWindowMetrics.InitialWindowSize(Window.Handle, new(options.WindowWidth, options.WindowHeight), options.ScaleWindowToDpi);
        _graphics = new(this)
        {
            PreferredBackBufferWidth = _windowedSize.X,
            PreferredBackBufferHeight = _windowedSize.Y,
            SynchronizeWithVerticalRetrace = true,
            GraphicsProfile = GraphicsProfile.Reach,
            IsFullScreen = Settings.Fullscreen,
            HardwareModeSwitch = false
        };
        if (Settings.Fullscreen)
        {
            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = display.Width;
            _graphics.PreferredBackBufferHeight = display.Height;
        }
        IsMouseVisible = true;
        Window.Title = options.Project.Title;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (_, _) => UpdateViewport();
    }

    protected virtual IGameFont CreateFont() => new DynamicFont(GraphicsDevice, Options.Project.ResolvePath(Options.Project.Font));
    protected virtual ITextureSource CreateTextureSource() => new FileTextureSource(GraphicsDevice, Options.Project);
    protected virtual IAudioService CreateAudioService() => new MonoGameAudioService(Options.Project);
    protected virtual KeyboardState PollKeyboard() => Keyboard.GetState();
    protected virtual MouseState PollMouse() => Mouse.GetState();
    protected virtual bool HasInputFocus => IsActive;

    protected override void LoadContent()
    {
        _batch = new(GraphicsDevice);
        _font = CreateFont();
        _ui = new(GraphicsDevice, _batch, _font);
        _textures = CreateTextureSource();
        _audio = CreateAudioService();
        _audio.SetVolumes(Settings.MusicVolume, Settings.VoiceVolume);
        UpdateViewport();
        RefreshNativeTitle();
        SyncPresentation();
        base.LoadContent();
    }

    public void StartNewGame()
    {
        Player.Restart();
        _hasSession = true;
        AutoAdvance = false;
        _hidden = false;
        ChangeToReading();
        SyncPresentation();
    }

    public void AdvanceDialogue()
    {
        if (Screen != GameScreen.Reading || _hidden || Player.CurrentNode.Kind != NodeKind.Dialogue) return;
        if (!_writer.IsComplete) { _writer.RevealAll(); _autoElapsed = 0; return; }
        if (_page + 1 < _pages.Count)
        {
            _page++;
            _writer.Reset(_pages[_page].ElementCount);
            _autoElapsed = 0;
            return;
        }
        Player.Advance();
        SyncPresentation();
    }

    public void SelectChoice(int index)
    {
        if (Screen != GameScreen.Reading || Player.CurrentNode.Kind != NodeKind.Choice) return;
        Player.Choose(index);
        SyncPresentation();
        _focus = 0;
    }

    public void Rollback()
    {
        if (!Player.Rollback()) { Notify("已经是故事的开头了"); return; }
        AutoAdvance = false;
        ChangeToReading();
        SyncPresentation();
        _writer.RevealAll();
    }

    public void OpenScreen(GameScreen screen)
    {
        if (screen is GameScreen.Title or GameScreen.Reading) throw new ArgumentOutOfRangeException(nameof(screen));
        _navigation.Push(Screen);
        Screen = screen;
        _focus = 0;
        _hidden = false;
        _overwriteSlot = null;
        _buttons.Clear();
        if (screen is GameScreen.Save or GameScreen.Load) _slots = Saves.ListSlots();
        if (screen == GameScreen.History) _historyOffset = Math.Max(0, Player.History.Count - 4);
        _audio.StopVoice();
    }

    public void CloseScreen()
    {
        if (_overwriteSlot is not null) { _overwriteSlot = null; return; }
        Screen = _navigation.TryPop(out var previous) ? previous : (_hasSession ? GameScreen.Reading : GameScreen.Title);
        _focus = 0;
        _autoElapsed = 0;
        _buttons.Clear();
    }

    public void SaveToSlot(int slot)
    {
        if (!_hasSession) return;
        TryStorage(() =>
        {
            Saves.Save(slot, Player.Capture());
            _slots = Saves.ListSlots();
            Notify(slot == 0 ? "已快速保存" : $"已保存至位置 {slot:00}");
        });
        _overwriteSlot = null;
    }

    public void LoadFromSlot(int slot)
    {
        TryStorage(() =>
        {
            Player.Restore(Saves.Load(slot).Snapshot);
            _hasSession = true;
            AutoAdvance = false;
            ChangeToReading();
            SyncPresentation();
            Notify("已回到保存的时刻");
        });
    }

    private void ChangeToReading()
    {
        Screen = GameScreen.Reading;
        _navigation.Clear();
        _buttons.Clear();
        _focus = 0;
        _overwriteSlot = null;
    }

    private void ReturnToTitle()
    {
        Screen = GameScreen.Title;
        _navigation.Clear();
        _buttons.Clear();
        _focus = 0;
        AutoAdvance = false;
        _audio.PlayMusic("");
        _audio.StopVoice();
    }

    protected override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        _transition = Math.Min(1, _transition + (float)gameTime.ElapsedGameTime.TotalSeconds * 2.5f);
        var keyboard = PollKeyboard();
        var mouse = PollMouse();
        RefreshNativeTitle();
        UpdateViewport();
        _pointer = _presentation.ToDesign(mouse.Position, _clientSize);
        if (HasInputFocus)
        {
            HandleInput(keyboard, mouse);
            if (Screen == GameScreen.Reading && !_hidden)
            {
                var wasComplete = _writer.IsComplete;
                _writer.Update(gameTime.ElapsedGameTime, Settings.TextSpeed);
                if (AutoAdvance && wasComplete && Player.CurrentNode.Kind == NodeKind.Dialogue)
                {
                    _autoElapsed += gameTime.ElapsedGameTime.TotalSeconds;
                    if (_autoElapsed >= Settings.AutoDelay) AdvanceDialogue();
                }
                if (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl))
                {
                    _skipElapsed += gameTime.ElapsedGameTime.TotalSeconds;
                    if (_skipElapsed >= .10) { _skipElapsed = 0; _writer.RevealAll(); AdvanceDialogue(); }
                }
                else _skipElapsed = 0;
            }
        }
        _keyboard = keyboard;
        _mouse = mouse;
        SyncPresentation();
        if (_audio.LastError is { } error && error != _lastAudioError)
        { _lastAudioError = error; Notify("音频暂不可用，可以继续阅读"); }
        base.Update(gameTime);
    }

    private void HandleInput(KeyboardState keyboard, MouseState mouse)
    {
        bool Pressed(Keys key) => keyboard.IsKeyDown(key) && _keyboard.IsKeyUp(key);
        if (Pressed(Keys.F11)) ToggleFullscreen();
        if (Pressed(Keys.Escape))
        {
            if (_hidden) { _hidden = false; return; }
            if (Screen == GameScreen.Reading) OpenScreen(GameScreen.Pause);
            else if (Screen != GameScreen.Title) CloseScreen();
            return;
        }
        var clicked = mouse.LeftButton == ButtonState.Pressed && _mouse.LeftButton == ButtonState.Released;
        if (mouse.Position != _mouse.Position)
        {
            var focusableUnderPointer = _buttons.Where(button => button.Keyboard).ToArray();
            var hoveredIndex = Array.FindIndex(focusableUnderPointer, button => button.Bounds.Contains(_pointer));
            if (hoveredIndex >= 0) _focus = hoveredIndex;
            if (Screen == GameScreen.Reading && Player.CurrentNode.Kind == NodeKind.Choice)
            {
                var visible = Math.Min(Player.AvailableChoices.Count, 4);
                var first = Math.Clamp(_choiceFocus - visible + 1, 0, Math.Max(0, Player.AvailableChoices.Count - visible));
                var hoveredChoice = _buttons.Take(visible).ToList().FindIndex(button => button.Bounds.Contains(_pointer));
                if (hoveredChoice >= 0) _choiceFocus = first + hoveredChoice;
            }
        }
        if (_overwriteSlot is not null)
        {
            var modalButtons = _buttons.Where(button => button.Keyboard).ToArray();
            if (Pressed(Keys.Tab) || Pressed(Keys.Down) || Pressed(Keys.Up)) _focus = (_focus + 1) % 2;
            if (Pressed(Keys.Enter) && modalButtons.Length > 0) modalButtons[Math.Clamp(_focus, 0, modalButtons.Length - 1)].Action();
            if (clicked) _buttons.LastOrDefault(button => button.Bounds.Contains(_pointer))?.Action();
            return;
        }
        if (Screen == GameScreen.Reading)
        {
            if (Pressed(Keys.H)) _hidden = !_hidden;
            if (Pressed(Keys.F5)) { SaveToSlot(0); return; }
            if (Pressed(Keys.F9)) { LoadFromSlot(0); return; }
            if (Pressed(Keys.A)) { AutoAdvance = !AutoAdvance; _autoElapsed = 0; }
            if (Pressed(Keys.L)) { OpenScreen(GameScreen.History); return; }
            if (mouse.ScrollWheelValue > _mouse.ScrollWheelValue) { Rollback(); return; }
            if (_hidden) { if (clicked || Pressed(Keys.Space) || Pressed(Keys.Enter)) _hidden = false; return; }
            if (Player.CurrentNode.Kind == NodeKind.Dialogue && (Pressed(Keys.Space) || Pressed(Keys.Enter)))
            { AdvanceDialogue(); return; }
            if (Player.CurrentNode.Kind == NodeKind.Choice)
            {
                var count = Player.AvailableChoices.Count;
                if (Pressed(Keys.Down) || Pressed(Keys.Tab)) _choiceFocus = (_choiceFocus + 1) % count;
                if (Pressed(Keys.Up)) _choiceFocus = (_choiceFocus + count - 1) % count;
                if (Pressed(Keys.Enter) || Pressed(Keys.Space)) { SelectChoice(_choiceFocus); return; }
                for (var i = 0; i < Math.Min(9, Player.AvailableChoices.Count); i++)
                    if (Pressed(Keys.D1 + i)) { SelectChoice(i); return; }
            }
        }
        if (Screen == GameScreen.History && mouse.ScrollWheelValue != _mouse.ScrollWheelValue)
            _historyOffset = Math.Clamp(_historyOffset + (mouse.ScrollWheelValue > _mouse.ScrollWheelValue ? -1 : 1),
                0, Math.Max(0, Player.History.Count - 4));
        var focusable = _buttons.Where(button => button.Keyboard).ToArray();
        if (focusable.Length > 0)
        {
            if (Pressed(Keys.Down) || Pressed(Keys.Tab) || Screen == GameScreen.Title && Pressed(Keys.Right))
                _focus = (_focus + 1) % focusable.Length;
            if (Pressed(Keys.Up) || Screen == GameScreen.Title && Pressed(Keys.Left))
                _focus = (_focus + focusable.Length - 1) % focusable.Length;
            _focus = Math.Clamp(_focus, 0, focusable.Length - 1);
            if (Pressed(Keys.Enter) || (Screen != GameScreen.Reading && Pressed(Keys.Space)))
            { focusable[_focus].Action(); return; }
        }
        if (clicked && _presentation.Contains(mouse.Position, _clientSize))
        {
            var hit = _buttons.LastOrDefault(button => button.Bounds.Contains(_pointer));
            if (hit is not null) hit.Action();
            else if (Screen == GameScreen.Reading) AdvanceDialogue();
        }
        if (mouse.RightButton == ButtonState.Pressed && _mouse.RightButton == ButtonState.Released && Screen == GameScreen.Reading)
            OpenScreen(GameScreen.Pause);
    }

    private void SyncPresentation()
    {
        if (_shownRevision == Player.Revision) return;
        _shownRevision = Player.Revision;
        _pages = TextLayout.Build(Player.CurrentNode.Text, _font, DialogueSize, DialogueWidth, DialogueLineHeight, 3);
        _page = 0;
        _choiceFocus = 0;
        _writer.Reset(_pages[0].ElementCount);
        if (Player.CurrentNode.Kind != NodeKind.Dialogue) _writer.RevealAll();
        _autoElapsed = 0;
        if (_shownScene != Player.Scene) _transition = 0;
        _shownScene = Player.Scene;
        if (_hasSession)
        {
            _audio.PlayMusic(Player.Scene.Music);
            _audio.PlayVoice(Player.CurrentNode.Voice);
        }
    }

    private void UpdateViewport()
    {
        if (GraphicsDevice is null) return;
        _clientSize = SdlWindowMetrics.WindowSize(Window.Handle);
        var drawable = SdlWindowMetrics.DrawableSize(Window.Handle);
        var buffer = GraphicsDevice.PresentationParameters;
        var width = drawable.X > 0 ? drawable.X : buffer.BackBufferWidth;
        var height = drawable.Y > 0 ? drawable.Y : buffer.BackBufferHeight;
        if (width != buffer.BackBufferWidth || height != buffer.BackBufferHeight)
        {
            // This is the same OpenGL bookkeeping MonoGame performs in SDL's resize callback.
            // Querying the drawable also covers event loss after fullscreen and high-DPI pixel ratios.
            buffer.BackBufferWidth = width;
            buffer.BackBufferHeight = height;
            GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
            GraphicsDevice.ScissorRectangle = new(0, 0, width, height);
        }
        _presentation = PresentationLayout.Create(Math.Max(1, width), Math.Max(1, height), DesignWidth, DesignHeight);
        _viewport = _presentation.Viewport;
    }

    private void EnsureRenderTarget()
    {
        if (_canvas is not null && _canvas.Width == _viewport.Width && _canvas.Height == _viewport.Height) return;
        var replacement = new RenderTarget2D(GraphicsDevice, _viewport.Width, _viewport.Height, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        _canvas?.Dispose();
        _canvas = replacement;
        if (_font is IRenderScaleAwareFont scalable) scalable.SetRenderScale(RenderScale);
    }

    private void RefreshNativeTitle()
    {
        if (Window.Handle == _titledWindow) return;
        // MonoGame 3.8.6's SDL_CreateWindow binding marshals its title as ANSI on Windows,
        // while its title setter explicitly encodes UTF-8. Reapply after the GL window is created.
        var title = Window.Title;
        Window.Title = "";
        Window.Title = title;
        _titledWindow = Window.Handle;
    }

    protected override void Draw(GameTime gameTime)
    {
        UpdateViewport();
        EnsureRenderTarget();
        GraphicsDevice.SetRenderTarget(_canvas);
        GraphicsDevice.Clear(new Color(225, 233, 223));
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            transformMatrix: _presentation.Transform);
        _buttons.Clear();
        DrawScene();
        if (Screen == GameScreen.Title) DrawTitle();
        else if (Screen == GameScreen.Reading) DrawReading();
        else
        {
            _ui.Fill(new(0, 0, DesignWidth, DesignHeight), Ink * .32f);
            switch (Screen)
            {
                case GameScreen.Pause: DrawPause(); break;
                case GameScreen.Save:
                case GameScreen.Load: DrawSaves(); break;
                case GameScreen.Settings: DrawSettings(); break;
                case GameScreen.History: DrawHistory(); break;
            }
        }
        if (_overwriteSlot is { } slot) DrawOverwrite(slot);
        if (_time < _toastUntil)
        {
            var width = (int)_font.Measure(_toast, 19) + 52;
            var bounds = new Rectangle((DesignWidth - width) / 2, 22, width, 46);
            _ui.Fill(bounds, Ink * .94f);
            _ui.Fill(new(bounds.X, bounds.Y, 3, bounds.Height), Accent);
            _ui.Centered(_toast, bounds, 19, Color.White);
        }
        _batch.End();
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(new Color(25, 31, 31));
        // Content is already at framebuffer resolution; present it without resampling.
        _batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        _batch.Draw(_canvas, _viewport, Color.White);
        _batch.End();
        base.Draw(gameTime);
    }

    private void ToggleFullscreen()
    {
        if (!Settings.Fullscreen) _windowedSize = SdlWindowMetrics.WindowSize(Window.Handle);
        Settings.Fullscreen = !Settings.Fullscreen;
        _graphics.IsFullScreen = Settings.Fullscreen;
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.PreferredBackBufferWidth = Settings.Fullscreen ? display.Width : _windowedSize.X;
        _graphics.PreferredBackBufferHeight = Settings.Fullscreen ? display.Height : _windowedSize.Y;
        _graphics.ApplyChanges();
        UpdateViewport();
        _titledWindow = IntPtr.Zero;
        RefreshNativeTitle();
        PersistSettings();
    }

    private void PersistSettings() => TryStorage(() => Settings.Save(_settingsPath));
    private void TryStorage(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or ArgumentException)
        { Notify("操作未完成：" + ex.Message); }
    }

    protected void Notify(string message) { _toast = message; _toastUntil = _time + 3; }

    public void CaptureScreenshot(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = File.Create(path);
        _canvas.SaveAsPng(stream, _canvas.Width, _canvas.Height);
    }

    protected override void UnloadContent()
    {
        _audio?.Dispose();
        _textures?.Dispose();
        _ui?.Dispose();
        _font?.Dispose();
        _canvas?.Dispose();
        _batch?.Dispose();
        base.UnloadContent();
    }

    private sealed record UiButton(Rectangle Bounds, Action Action, bool Keyboard);
}

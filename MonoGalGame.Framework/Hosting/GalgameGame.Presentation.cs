using Microsoft.Xna.Framework;
using MonoGalGame.Core.Stories;
using MonoGalGame.Core.Text;

namespace MonoGalGame.Framework.Hosting;

public partial class GalgameGame
{
    private static readonly Color Ink = new(49, 65, 72);
    private static readonly Color Muted = new(123, 133, 137);
    private static readonly Color Accent = new(171, 94, 116);
    private static readonly Color Paper = new(255, 252, 247);
    private static readonly Color Border = new(222, 210, 205);
    private const float DialogueSize = 24;
    private const float DialogueWidth = 1108;
    private const float DialogueLineHeight = 37;

    private void DrawScene()
    {
        var background = Screen == GameScreen.Title || !_hasSession ? Options.Project.TitleBackground : Player.Scene.Background;
        if (_textures.Get(background) is { } texture) _ui.Cover(texture, new(0, 0, DesignWidth, DesignHeight), Color.White);
        else _ui.Gradient(new(0, 0, DesignWidth, DesignHeight), new(221, 235, 232), new(250, 233, 225));

        if (_hasSession && Screen != GameScreen.Title && _textures.Get(Player.Scene.Portrait) is { } portrait)
        {
            const int height = 710;
            var width = (int)(height * portrait.Width / (float)portrait.Height);
            var breathing = (int)(Math.Sin(_time * 1.5) * 1.2);
            _ui.Image(portrait, new(818 - width / 2, 32 + breathing + (int)((1 - _transition) * 12), width, height), Color.White * _transition);
        }

        // Organic scenery stays in the artwork; floating petals are small, straight-edged accents.
        for (var i = 0; i < 15; i++)
        {
            var x = (float)((i * 173 + _time * (9 + i % 4)) % 1360) - 40;
            var y = (float)((i * 89 + _time * (12 + i % 3)) % 790) - 35;
            _ui.Diamond(new(x, y), 2 + i % 3, new Color(255, 231, 233) * .62f, (float)_time * .2f + i);
        }
    }

    private void DrawTitle()
    {
        _ui.Gradient(new(0, 0, 620, DesignHeight), Paper * .82f, Color.Transparent, horizontal: true);
        _ui.Gradient(new(940, 0, 340, DesignHeight), Color.Transparent, Paper * .46f, horizontal: true);
        _ui.Gradient(new(0, 560, DesignWidth, 160), Color.Transparent, Paper * .72f);
        _ui.Outline(new(28, 26, 1224, 668), Color.White * .7f);
        Corner(new(28, 26), 1, 1);
        Corner(new(1252, 694), -1, -1);

        Envelope(new(63, 49, 24, 17), Accent * .9f);
        _ui.TrackedText("A LETTER FOR YOU", 100, 47, 12, 2.5f, Ink * .72f);
        _ui.TrackedText("VISUAL NOVEL", 1083, 47, 10, 2, Ink * .65f);

        _ui.Fill(new(80, 160, 2, 31), Accent);
        _ui.TrackedText("序章  /  在春天，与你相遇", 96, 167, 13, 1.5f, Ink * .78f);
        var titleSize = 78f;
        while (titleSize > 36 && _font.Measure(Options.Project.Title, titleSize) > 414) titleSize -= 2;
        _ui.Text(Options.Project.Title, 75, 213, titleSize, Ink);
        _ui.Fill(new(81, 325, 42, 2), Accent);
        if (!string.IsNullOrWhiteSpace(Options.Project.TitleCaption))
            _ui.TrackedText(Options.Project.TitleCaption.ToUpperInvariant(), 139, 319, 12, 3.3f, Accent);
        _ui.Ellipsis(Options.Project.Subtitle, 82, 369, 18, Ink * .82f, 400);
        _ui.Text("致  /  那个还没说出口的春天", 83, 414, 12, Muted);

        TitleMenu(new(927, 353, 49, 249), "开始故事", "START", "01", StartNewGame);
        TitleMenu(new(987, 353, 49, 249), "继续阅读", "RESUME", "02",
            () => { ChangeToReading(); _audio.PlayMusic(Player.Scene.Music); }, enabled: _hasSession);
        TitleMenu(new(1047, 353, 49, 249), "读取存档", "LOAD", "03", () => OpenScreen(GameScreen.Load));
        TitleMenu(new(1107, 353, 49, 249), "环境设置", "CONFIG", "04", () => OpenScreen(GameScreen.Settings));
        TitleMenu(new(1167, 353, 49, 249), "离开故事", "EXIT", "05", Exit);

        _ui.Fill(new(81, 602, 32, 1), Accent * .8f);
        _ui.Text("每个选择，都会成为故事的一部分。", 81, 618, 14, Ink * .8f);
        _ui.TrackedText("CLICK TO BEGIN YOUR STORY", 81, 651, 10, 1.7f, Muted);
        _ui.Text("← → / Tab 选择    Enter 确认", 1005, 655, 11, Ink * .72f);
    }

    private void Corner(Vector2 origin, int xDirection, int yDirection)
    {
        _ui.Line(origin, origin + new Vector2(38 * xDirection, 0), Accent * .72f);
        _ui.Line(origin, origin + new Vector2(0, 38 * yDirection), Accent * .72f);
    }

    private void Envelope(Rectangle bounds, Color color)
    {
        _ui.Outline(bounds, color);
        _ui.Line(new(bounds.Left, bounds.Top), new(bounds.Center.X, bounds.Center.Y + 2), color);
        _ui.Line(new(bounds.Right - 1, bounds.Top), new(bounds.Center.X, bounds.Center.Y + 2), color);
    }

    private void TitleMenu(Rectangle bounds, string label, string caption, string number, Action action, bool enabled = true)
    {
        var selected = enabled && _buttons.Count(button => button.Keyboard) == _focus;
        var offset = selected ? -5 : 0;
        var visual = new Rectangle(bounds.X, bounds.Y + offset, bounds.Width, bounds.Height);
        _ui.Fill(visual, selected ? Accent * .95f : Paper * (enabled ? .84f : .66f));
        _ui.Outline(visual, selected ? Color.White * .6f : Accent * .33f);
        _ui.Centered(number, new(visual.X, visual.Y + 15, visual.Width, 20), 11, selected ? Color.White * .75f : Accent);
        _ui.Fill(new(visual.X + 17, visual.Y + 44, 15, 1), selected ? Color.White * .5f : Border);
        _ui.VerticalText(label, new(visual.X, visual.Y + 60, visual.Width, 143), 22, 10, selected ? Color.White : enabled ? Ink : Muted * .55f);
        _ui.Centered(caption, new(visual.X, visual.Bottom - 31, visual.Width, 20), 9, selected ? Color.White * .85f : Muted);
        if (selected) _ui.Diamond(new(visual.Center.X, visual.Bottom + 16), 5, Accent);
        // The hit area includes the five-pixel hover lift so the selection never flickers at the edge.
        if (enabled) _buttons.Add(new(new(bounds.X, bounds.Y - 5, bounds.Width, bounds.Height + 5), action, true));
    }

    private void DrawReading()
    {
        if (_hidden) return;
        if (Player.IsEnded) { DrawEnding(); return; }

        var chapterWidth = Math.Min(450, (int)_font.Measure(Options.Story.Title, 16) + 50);
        _ui.Fill(new(40, 28, chapterWidth, 35), Paper * .88f);
        _ui.Fill(new(40, 28, 3, 35), Accent);
        _ui.Ellipsis(Options.Story.Title, 56, 33, 16, Ink, chapterWidth - 31);
        _ui.Text("STORY / 01", 1127, 37, 11, Ink * .8f);

        var dialogue = new Rectangle(40, 516, 1200, 159);
        _ui.Fill(dialogue, Paper * .96f);
        _ui.Outline(dialogue, Color.White * .88f);
        _ui.Outline(new(46, 522, 1188, 147), Border * .55f);
        _ui.Fill(new(40, 516, 1200, 2), Accent * .62f);
        var speaker = string.IsNullOrWhiteSpace(Player.CurrentNode.Speaker) ? "旁白" : Player.CurrentNode.Speaker;
        var nameWidth = Math.Max(142, (int)_font.Measure(speaker, 20) + 68);
        _ui.Fill(new(58, 474, nameWidth, 42), Ink * .95f);
        _ui.Fill(new(58, 474, 3, 42), Accent);
        _ui.Text(speaker, 81, 480, 20, Color.White);
        _ui.Diamond(new(58 + nameWidth - 21, 495), 4, new Color(236, 197, 204));
        _font.DrawPage(_batch, _pages[_page], new(83, 533), DialogueSize, Ink, _writer.VisibleCount);

        var hint = Player.CurrentNode.Kind == NodeKind.Choice ? "请选择你的心意" :
            _writer.IsComplete ? (_page + 1 < _pages.Count ? "点击翻页" : "点击继续") : "点击显示全文";
        _ui.Text(hint, 1093, 647, 12, Muted);
        _ui.Diamond(new(1212, 658 + (float)Math.Sin(_time * 3) * 2), 5, Accent * .8f);
        if (_pages.Count > 1) _ui.Text($"{_page + 1:00} / {_pages.Count:00}", 83, 648, 11, Muted);

        if (Player.CurrentNode.Kind == NodeKind.Choice) DrawChoices();

        _ui.Fill(new(40, 681, 1200, 31), Paper * .88f);
        _ui.Fill(new(40, 681, 1200, 1), Border * .7f);
        _ui.Text("Ctrl 快进  /  H 隐藏", 55, 687, 12, Ink * .72f);
        var x = 458;
        void Toolbar(string label, Action action, bool active = false)
        { LinkButton(new(x, 682, 110, 29), label, action, active); x += 111; }
        Toolbar("存档  SAVE", () => OpenScreen(GameScreen.Save));
        Toolbar("读档  LOAD", () => OpenScreen(GameScreen.Load));
        Toolbar("快存  Q.S", () => SaveToSlot(0));
        Toolbar("快读  Q.L", () => LoadFromSlot(0));
        Toolbar("自动  AUTO", () => { AutoAdvance = !AutoAdvance; _autoElapsed = 0; }, AutoAdvance);
        Toolbar("历史  LOG", () => OpenScreen(GameScreen.History));
        Toolbar("菜单  MENU", () => OpenScreen(GameScreen.Pause));
    }

    private void DrawChoices()
    {
        var choices = Player.AvailableChoices;
        var visibleChoices = Math.Min(choices.Count, 4);
        var first = Math.Clamp(_choiceFocus - visibleChoices + 1, 0, Math.Max(0, choices.Count - visibleChoices));
        var top = 278 - visibleChoices * 36;
        var caption = new Rectangle(120, top - 40, 600, 25);
        _ui.Fill(caption, Paper * .9f);
        _ui.Centered("—  选择接下来的故事  —", caption, 13, Accent);
        for (var i = first; i < first + visibleChoices; i++)
        {
            var index = i;
            var bounds = new Rectangle(120, top + (i - first) * 72, 600, 56);
            var selected = i == _choiceFocus;
            Button(bounds, "", () => SelectChoice(index), primary: selected, keyboard: false);
            _ui.Text($"{i + 1:00}", bounds.X + 19, bounds.Y + 15, 17, selected ? Color.White * .8f : Accent);
            _ui.Fill(new(bounds.X + 60, bounds.Y + 17, 1, 23), selected ? Color.White * .35f : Border);
            _ui.Ellipsis(choices[i].Text, bounds.X + 84, bounds.Y + 11, 22, selected ? Color.White : Ink, bounds.Width - 130);
            _ui.Diamond(new(bounds.Right - 23, bounds.Center.Y), 4, selected ? Color.White : Accent * .7f);
        }
    }

    private void LinkButton(Rectangle bounds, string label, Action action, bool active = false)
    {
        var hovered = bounds.Contains(_pointer);
        if (hovered || active)
        {
            _ui.Fill(bounds, Accent * .1f);
            _ui.Fill(new(bounds.X + 9, bounds.Bottom - 2, bounds.Width - 18, 2), Accent * .8f);
        }
        _ui.Centered(label, bounds, 12, active || hovered ? Accent : Ink * .86f);
        _buttons.Add(new(bounds, action, false));
    }

    private void DrawEnding()
    {
        _ui.Fill(new(0, 0, DesignWidth, DesignHeight), Paper * .62f);
        _ui.Fill(new(282, 140, 716, 444), Paper * .95f);
        _ui.Outline(new(290, 148, 700, 428), Border);
        Envelope(new(621, 185, 38, 26), Accent);
        _ui.Centered("E N D I N G  /  春 日 终 章", new(300, 236, 680, 30), 14, Accent);
        _ui.Centered(string.IsNullOrEmpty(Player.CurrentNode.Text) ? "故事，未完待续。" : Player.CurrentNode.Text,
            new(300, 294, 680, 58), 32, Ink);
        _ui.Centered("感谢你，走进这个春天。", new(300, 373, 680, 35), 18, Muted);
        Button(new(438, 446, 404, 45), "回到标题", ReturnToTitle, primary: true);
        Button(new(438, 507, 194, 38), "回看上一段", Rollback, compact: true, enabled: Player.CanRollback);
        Button(new(648, 507, 194, 38), "重新开始", StartNewGame, compact: true);
    }

    private void DrawPause()
    {
        _ui.Fill(new(416, 72, 448, 576), Paper * .97f);
        _ui.Outline(new(424, 80, 432, 560), Border);
        _ui.TrackedText("STORY MENU", 463, 106, 12, 2.5f, Accent);
        _ui.Text("故事暂停在这里", 460, 138, 27, Ink);
        _ui.Text("不用着急，我们还有整个春天。", 463, 184, 15, Muted);
        Button(new(460, 232, 360, 43), "继续阅读", CloseScreen, primary: true);
        Button(new(460, 287, 172, 43), "保存进度", () => OpenScreen(GameScreen.Save));
        Button(new(648, 287, 172, 43), "读取存档", () => OpenScreen(GameScreen.Load));
        Button(new(460, 342, 360, 43), "阅读历史", () => OpenScreen(GameScreen.History));
        Button(new(460, 397, 360, 43), "环境设置", () => OpenScreen(GameScreen.Settings));
        Button(new(460, 452, 360, 43), "回到标题", ReturnToTitle);
        _ui.Fill(new(462, 526, 356, 1), Border);
        _ui.Text("Esc 返回  /  F11 全屏  /  滚轮向上回退", 462, 553, 13, Muted);
        _ui.TrackedText("THE STORY WILL WAIT FOR YOU", 462, 599, 9, 1.4f, Muted * .8f);
    }

    private void Panel(string title, string caption, string description)
    {
        _ui.Fill(new(80, 54, 1120, 610), Paper * .98f);
        _ui.Outline(new(86, 60, 1108, 598), Border * .75f);
        _ui.Fill(new(80, 54, 1120, 2), Accent * .65f);
        _ui.Text(title, 116, 81, 31, Ink);
        _ui.TrackedText(caption, 367, 102, 12, 2, Accent);
        _ui.Text(description, 118, 141, 16, Muted);
        _ui.Fill(new(116, 184, 1048, 1), Border);
        Button(new(1060, 94, 104, 40), "返回  ESC", CloseScreen, compact: true);
    }

    private void DrawSaves()
    {
        var saving = Screen == GameScreen.Save;
        Panel(saving ? "保存这一刻" : "回到故事里", saving ? "SAVE / YOUR LETTERS" : "LOAD / YOUR LETTERS",
            saving ? "选择一个位置，收好此刻的对白与选择。" : "从上一次告别的地方，继续往下读。");
        foreach (var slot in _slots)
        {
            var bounds = new Rectangle(116 + (slot.Slot - 1) % 3 * 354, 205 + (slot.Slot - 1) / 3 * 198, 338, 181);
            var index = slot.Slot;
            var enabled = saving || slot.Save is not null;
            var selected = enabled && (bounds.Contains(_pointer) || _buttons.Count(button => button.Keyboard) == _focus);
            Button(bounds, "", () =>
            {
                if (!saving) LoadFromSlot(index);
                else if (Saves.Exists(index)) { _overwriteSlot = index; _focus = 0; _buttons.Clear(); }
                else SaveToSlot(index);
            }, enabled: enabled);

            var preview = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, 88);
            var scene = slot.Save?.Snapshot.Scene;
            if (_textures.Get(scene?.Background ?? Options.Project.TitleBackground) is { } background)
                _ui.Cover(background, preview, slot.Save is null ? Color.White * .34f : Color.White);
            if (slot.Save is null)
            {
                _ui.Fill(preview, Paper * .46f);
                Envelope(new(preview.Center.X - 14, preview.Center.Y - 10, 28, 20), Accent * .48f);
            }
            _ui.Fill(new(bounds.X + 9, bounds.Y + 9, 37, 23), Paper * .94f);
            _ui.Centered($"{slot.Slot:00}", new(bounds.X + 9, bounds.Y + 9, 37, 23), 12, Accent);
            var title = slot.Error is not null ? "存档无法读取" : slot.Save is null ? "等待一段故事" :
                slot.Save.Snapshot.History?.LastOrDefault()?.Speaker is { Length: > 0 } name ? name : "保存的故事";
            _ui.Ellipsis(title, bounds.X + 15, bounds.Y + 99, 18, slot.Save is null ? Muted : Ink, 306);
            var text = slot.Save?.Snapshot.History?.LastOrDefault()?.Text ?? (saving ? "点击保存此刻的心意" : "尚未写下的篇章");
            _ui.Ellipsis(text, bounds.X + 15, bounds.Y + 130, 14, Muted, 306);
            var timestamp = slot.Save?.SavedAt.ToLocalTime().ToString("yyyy.MM.dd  HH:mm") ?? "—  EMPTY LETTER  —";
            _ui.Text(timestamp, bounds.X + 15, bounds.Y + 157, 10, Muted);
            _ui.Outline(bounds, selected ? Accent : Border);
            if (selected) _ui.Fill(new(bounds.X, bounds.Bottom - 3, bounds.Width, 3), Accent);
        }
        _ui.Text("F5 快速保存  /  F9 快速读取", 118, 625, 13, Muted);
        _ui.Text("01 — 06  /  LETTER COLLECTION", 916, 626, 10, Accent * .8f);
    }

    private void DrawSettings()
    {
        Panel("环境设置", "CONFIG / YOUR PACE", "让文字、声音和画面，停在你喜欢的节奏。");
        SettingRow(220, "文字速度", $"{Settings.TextSpeed:0} 字 / 秒", () => Settings.TextSpeed = Math.Max(10, Settings.TextSpeed - 10),
            () => Settings.TextSpeed = Math.Min(100, Settings.TextSpeed + 10), (Settings.TextSpeed - 10) / 90);
        SettingRow(299, "自动等待", $"{Settings.AutoDelay:0.0} 秒", () => Settings.AutoDelay = Math.Max(.5f, Settings.AutoDelay - .5f),
            () => Settings.AutoDelay = Math.Min(5, Settings.AutoDelay + .5f), (Settings.AutoDelay - .5f) / 4.5f);
        SettingRow(378, "背景音乐", $"{Settings.MusicVolume:P0}", () => Settings.MusicVolume = Math.Max(0, Settings.MusicVolume - .1f),
            () => Settings.MusicVolume = Math.Min(1, Settings.MusicVolume + .1f), Settings.MusicVolume);
        SettingRow(457, "角色语音", $"{Settings.VoiceVolume:P0}", () => Settings.VoiceVolume = Math.Max(0, Settings.VoiceVolume - .1f),
            () => Settings.VoiceVolume = Math.Min(1, Settings.VoiceVolume + .1f), Settings.VoiceVolume);
        _ui.Text("显示模式", 127, 548, 21, Ink);
        _ui.Text("F11 切换", 390, 551, 16, Muted);
        Button(new(735, 544, 354, 42), Settings.Fullscreen ? "全屏模式" : "窗口模式", ToggleFullscreen);
        _ui.Text("设置会自动保存，按自己的节奏阅读。", 127, 626, 13, Muted);
    }

    private void SettingRow(int y, string label, string value, Action decrease, Action increase, float progress)
    {
        _ui.Text(label, 127, y, 21, Ink);
        _ui.Text(value, 390, y + 3, 17, Muted);
        _ui.Fill(new(626, y + 19, 325, 3), Border);
        var length = Math.Clamp((int)(325 * progress), 0, 325);
        _ui.Fill(new(626, y + 19, length, 3), Accent);
        _ui.Fill(new(622 + length, y + 14, 9, 13), Accent);
        _ui.Fill(new(127, y + 63, 963, 1), Border * .46f);
        void Change(Action change) { change(); _audio.SetVolumes(Settings.MusicVolume, Settings.VoiceVolume); PersistSettings(); }
        Button(new(990, y - 2, 44, 41), "−", () => Change(decrease), compact: true);
        Button(new(1046, y - 2, 44, 41), "+", () => Change(increase), compact: true);
    }

    private void DrawHistory()
    {
        Panel("读过的对白", "BACKLOG / OUR WORDS", "把那些来不及记住的话，再读一遍。");
        var entries = Player.History.Skip(_historyOffset).Take(4).ToArray();
        for (var i = 0; i < entries.Length; i++)
        {
            var y = 203 + i * 90;
            _ui.Text($"{_historyOffset + i + 1:000}", 128, y + 2, 11, Muted);
            _ui.Text(string.IsNullOrEmpty(entries[i].Speaker) ? "旁白" : entries[i].Speaker, 169, y, 17, Accent);
            _ui.Fill(new(251, y + 6, 1, 51), Border);
            var layout = TextLayout.Build(entries[i].Text, _font, 18, 869, 28, 2);
            _font.DrawPage(_batch, layout[0], new(278, y), 18, Ink, int.MaxValue);
            _ui.Fill(new(126, y + 78, 1020, 1), Border * .55f);
        }
        if (entries.Length == 0) _ui.Text("还没有读过的对白。", 126, 226, 21, Muted);
        Button(new(127, 594, 144, 37), "向前翻阅", () => _historyOffset = Math.Max(0, _historyOffset - 4), compact: true, enabled: _historyOffset > 0);
        Button(new(289, 594, 144, 37), "向后翻阅", () => _historyOffset = Math.Min(Math.Max(0, Player.History.Count - 4), _historyOffset + 4),
            compact: true, enabled: _historyOffset + 4 < Player.History.Count);
        _ui.Text($"共 {Player.History.Count} 段对白", 1000, 603, 14, Muted);
    }

    private void DrawOverwrite(int slot)
    {
        _buttons.Clear();
        _ui.Fill(new(0, 0, DesignWidth, DesignHeight), Ink * .5f);
        _ui.Fill(new(400, 228, 480, 250), Paper);
        _ui.Outline(new(407, 235, 466, 236), Border);
        _ui.TrackedText("CONFIRM / YOUR LETTER", 437, 252, 11, 1.8f, Accent);
        _ui.Text($"覆盖存档 {slot:00}？", 437, 286, 27, Ink);
        _ui.Text("这个位置的原有进度将被替换。", 437, 337, 18, Muted);
        Button(new(438, 407, 190, 42), "确认保存", () => SaveToSlot(slot), primary: true);
        Button(new(650, 407, 190, 42), "取消", () => { _overwriteSlot = null; _focus = 0; _buttons.Clear(); });
    }

    private void Button(Rectangle bounds, string label, Action action, bool primary = false,
        bool compact = false, bool enabled = true, bool keyboard = true)
    {
        var keyboardIndex = _buttons.Count(button => button.Keyboard);
        var selected = enabled && (bounds.Contains(_pointer) || keyboard && keyboardIndex == _focus);
        _ui.Fill(bounds, primary ? Accent * .96f : selected ? new Color(245, 234, 232) : Paper * .94f);
        _ui.Outline(bounds, primary ? Accent : selected ? Accent * .72f : Border * .85f);
        var textColor = primary ? Color.White : enabled ? Ink : Muted * .55f;
        if (compact) _ui.Centered(label, bounds, 14, textColor);
        else if (!string.IsNullOrEmpty(label))
        {
            _ui.Ellipsis(label, bounds.X + 20, bounds.Center.Y - 16, 20, textColor, bounds.Width - 53);
            _ui.Line(new(bounds.Right - 28, bounds.Center.Y), new(bounds.Right - 16, bounds.Center.Y), primary ? Color.White * .75f : Accent * .75f);
        }
        if (selected && !primary) _ui.Fill(new(bounds.X, bounds.Y, 3, bounds.Height), Accent);
        if (enabled) _buttons.Add(new(bounds, action, keyboard));
    }
}

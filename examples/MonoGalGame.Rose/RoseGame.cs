using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGalGame.Core.Stories;
using MonoGalGame.Framework.Hosting;

namespace MonoGalGame.Rose;

internal sealed class RoseGame(GalgameOptions options, string? smokeDirectory, string? expectedContinue = null) : GalgameGame(options)
{
    private int _frame;
    private bool _capturePending;
    private string _savedNode = "";
    private string? _captureName;
    private static readonly string[] StoryArt = ["cg01", "cg02", "cg03a", "cg03b", "cg04", "cg05", "cg06", "cg07", "cg08", "cg09", "cg10a", "cg10b", "cg10c"];
    protected override StorySnapshot PrepareLoadedSnapshot(StorySnapshot snapshot)
    {
        // Every Rose line has a complete authored scene. Keep node ids and choices
        // while updating older saves to the corrected cast, costumes and CGs.
        if (Options.Story.Nodes.TryGetValue(snapshot.NodeId, out var node) && node.Scene is { } scene)
            return snapshot with { Scene = new(scene.Background ?? snapshot.Scene.Background,
                scene.Portrait ?? snapshot.Scene.Portrait, scene.Music ?? snapshot.Scene.Music,
                scene.SecondaryPortrait ?? snapshot.Scene.SecondaryPortrait, scene.Chapter ?? snapshot.Scene.Chapter,
                scene.Location ?? snapshot.Scene.Location, scene.Time ?? snapshot.Scene.Time,
                scene.Viewpoint ?? snapshot.Scene.Viewpoint, scene.Tone ?? snapshot.Scene.Tone,
                scene.TertiaryPortrait ?? snapshot.Scene.TertiaryPortrait) };
        return snapshot;
    }
    protected override bool HasInputFocus => smokeDirectory is not null || base.HasInputFocus;
    protected override KeyboardState PollKeyboard() => smokeDirectory is null ? base.PollKeyboard() : _frame switch
    {
        24 => new(Keys.Right), 26 => new(Keys.Enter), 136 => new(Keys.D1), 186 => new(Keys.F5),
        206 => new(Keys.F9), 466 => new(Keys.Escape), _ => new()
    };
    protected override MouseState PollMouse()
    {
        if (smokeDirectory is null) return base.PollMouse();
        var portraitClick = _frame == 318;
        var lockedClick = _frame == 312;
        return new(portraitClick || lockedClick ? 1050 : 1190, portraitClick ? 290 : lockedClick ? 420 : 690, 0,
            portraitClick || lockedClick ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        if (smokeDirectory is not null) Settings.TextSpeed = 100;
    }

    protected override void Update(GameTime gameTime)
    {
        if (smokeDirectory is not null && _capturePending) return;
        // A deterministic clock exercises waiting/typing without making screenshot capture slow.
        base.Update(smokeDirectory is null ? gameTime : new GameTime(gameTime.TotalGameTime, TimeSpan.FromSeconds(.1)));
        if (smokeDirectory is null) return;
        _frame++;
        switch (_frame)
        {
            case 28:
                Require(Screen == GameScreen.Reading, "Vertical menu keyboard start failed.");
                Require(expectedContinue is null || Player.CurrentNodeId == expectedContinue, "Continue did not restore the previous session.");
                if (expectedContinue is not null && Options.Story.Nodes[expectedContinue].Scene is { } expectedScene)
                    Require(Player.Scene.Background == expectedScene.Background && Player.Scene.Portrait == expectedScene.Portrait &&
                        Player.Scene.Chapter == expectedScene.Chapter && Player.Scene.Time == expectedScene.Time,
                        "Older save did not refresh to the corrected scene.");
                break;
            case 76: Seek("纱S03", "唐见月"); break;
            case 110: SeekChoice("q1"); break;
            case 138: Require(Player.Variables.GetValueOrDefault("q1") == 1, "Numeric choice failed."); break;
            case 150: Seek("纱S05", "唐见月"); break;
            case 188: _savedNode = Player.CurrentNodeId; Player.Advance(); break;
            case 208: Require(Player.CurrentNodeId == _savedNode, "Quick save/load failed."); break;
            case 220: SaveToSlot(1); OpenScreen(GameScreen.Save); break;
            case 250: CloseScreen(); OpenScreen(GameScreen.Settings); break;
            case 280: CloseScreen(); OpenScreen(GameScreen.History); break;
            case 310: CloseScreen(); OpenScreen(GameScreen.Gallery); break;
            case 340: CloseScreen(); SeekChoice("q3"); break;
            case 370: SelectChoice(0); Finish(); break;
            case 400: SeekChoice("q3"); SelectChoice(1); Finish(); break;
            case 430: SeekChoice("q3"); SelectChoice(2); Finish(); break;
            case 462: StartNewGame(); break;
            case 468: Require(Screen == GameScreen.Pause, "Pause input failed."); break;
            case 1030: SeekArtwork("bg05-day"); break;
            case 1070: SeekArtwork("bg08-furnished"); break;
            case 1110: SeekText("C06", "信我写了", 2); break;
            case 1150: SeekText("C08", "寄走东西以后", 2); break;
            case 1190: SeekText("纱S07", "我拿着背景布过去"); break;
            case 1240: SeekText("纱S04", "胳膊抬一下"); break;
            case 1270: SeekText("A03", "第二天早上"); break;
            case 1300: SeekText("A03", "我们第三天就吵了一点"); break;
            case 1330: SeekArtwork("bg06-temporary"); break;
            case 1360: SeekText("纱S01", "别叫老师"); break;
            case 1385: Player.Advance(); break;
            case 1400: SeekText("纱S06", "见月，我给你拿了点"); break;
            case 1430: Player.Advance(); break;
            case 1460: SeekText("纱S06", "我跟见月说几句话"); break;
            case 1485:
                SaveToSlot(2);
                Require(Saves.Load(2).Snapshot.Scene.TertiaryPortrait == "zhou-suqin", "Third actor was not saved by the native serializer.");
                SeekText("纱S06", "我买了一瓶水");
                LoadFromSlot(2);
                Require(Player.Scene.TertiaryPortrait == "zhou-suqin", "Third actor was not restored.");
                break;
            case 1490: SeekText("纱S06", "我买了一瓶水"); break;
            case 1520: SeekText("纱S09", "这些怎么都收起来了"); break;
            case 1550: SeekText("A05", "地滑，慢点"); break;
            case 1580: SeekText("C05a", "姨妈气得走了", 2); break;
            case 1610: SeekArtwork("bg06-night"); break;
            case 1640: SeekArtwork("bg09-night"); break;
            case 1680: SeekText("纱S03", "饭后我抢着洗碗"); break;
            case 1710: SeekText("A05", "她走时仍不肯让我送"); break;
            case 1740: SeekText("纱S09", "说完我哭了"); break;
            case 1770: SeekArtwork("cg11"); break;
            case 1795: Player.Advance(); break;
            case 1820: SeekText("B02", "第三天早上，行李箱放到门边", 1); break;
            case 1850: SeekText("B02", "我点头。门铃响了一声", 1); break;
            case 1880: SeekText("B03", "我去拿水，再回来", 1); break;
            case 1910: SeekArtwork("bg03-night"); break;
            case 1940: SeekText("纱S08", "她没答。我不敢再等"); break;
            case 1980: SeekText("纱S07", "这里能做摄影"); break;
            case 2010: SeekText("A06", "你们两个这次都确定"); break;
            case 2040: SeekText("A06", "叶照，我喜欢你。我想和你一起生活"); break;
            case 2070: SeekText("纱S09", "那就先不用按原来拍"); break;
            case 2100: SeekText("A01", "第二天我和秦嘉聊工作"); break;
            case 2130: SeekText("A01", "我把文件带回房间"); break;
            case 2170: SeekText("纱S07", "屋顶接水的声音太密"); break;
            case 2200: SeekText("A03", "叶照给自己找了个位置"); break;
            case 2230:
                SaveToSlot(3);
                Require(Saves.Load(3).Snapshot.Scene.Background == "bg07-sorted-winter-night", "Sorted room was not saved.");
                SeekText("纱S04", "她上楼拿布");
                LoadFromSlot(3);
                Require(Player.Scene.Background == "bg07-sorted-winter-night", "Loaded scene lost the organized room state.");
                break;
            case 2260: SeekText("纱S09", "我给叶照打了电话"); break;
            case 2290: Player.Advance(); break;
            case 2340: SeekText("纱S05", "店里热得站不住"); break;
            case 2370: SeekArtwork("cg12"); break;
            case 2400: SeekText("纱S05", "她捏住我拿衣服的手"); break;
            case 2430: SeekText("A04", "新年第一场雨"); break;
            case 2460: SeekArtwork("cg13"); break;
            case 2485: Player.Advance(); break;
            case 2520: SeekText("A05", "她不说留，也没有立刻走"); break;
            case 2550: SeekText("A05", "她走时仍不肯让我送"); break;
            case 2580:
                SaveToSlot(4);
                Require(Saves.Load(4).Snapshot.Scene.Background == "bg07-repaired-night", "Repaired room was not saved.");
                SeekText("纱S04", "她上楼拿布");
                LoadFromSlot(4);
                Require(Player.Scene.Background == "bg07-repaired-night", "Loaded scene lost the repaired room state.");
                break;
            case 2610: SeekText("A04", "几天后，她用我的大桌裁好了"); break;
            case 2650: SeekText("纱S01", "叶老师，在里面"); break;
            case 2680: SeekText("纱S01", "实在对不住"); break;
            case 2710: SeekText("纱S02", "是不是很明显"); break;
            case 2740: SeekText("纱S02", "那我是不是该请你吃饭"); break;
            case 2770: SeekText("纱S02", "新人终于进场"); break;
            case 2800: SeekText("B03", "见月第一次到外地", 1); break;
            case 2830: SeekText("B03", "房间比她想象的小", 1); break;
            case 2860: SeekArtwork("cg14", 1); break;
            case 2885: Player.Advance(); break;
            case 2920: SeekText("B03", "晚上她睡着了", 1); break;
            case 2950:
                SaveToSlot(5);
                Require(Saves.Load(5).Snapshot.Scene.Background == "bg09-winter-night", "Visitor night was not saved.");
                SeekText("纱S04", "她上楼拿布");
                LoadFromSlot(5);
                Require(Player.Scene.Background == "bg09-winter-night" && Player.Scene.Portrait == "" && Player.Scene.SecondaryPortrait == "", "Visitor night restored an awake portrait.");
                break;
            case 2980: SeekText("B03", "她走后，抽屉少了一半", 1); break;
            case 3010: SeekArtwork("bg09-winter", 2); break;
            case 3040: SeekText("C08", "我收到那件外套", 2); break;
            case 3080: SeekText("C04", "三月她发了一张屋顶修后的照片", 2); break;
            case 3110: SeekText("C04", "五月余下那些日子都很好", 2); break;
            case 3140: SeekText("C04", "十二号那天可能去不了", 2); break;
            case 3170: SeekText("C04", "我挂断了电话。过了十分钟", 2); break;
            case 3200: SeekText("C05", "她打电话，我接了", 2); break;
            case 3230: SeekText("B07a", "我打过去。她接得很快", 1); break;
            case 3260: SeekText("B07a", "挂断之后，我没有轻松起来", 1); break;
            case 3290: SeekText("C06", "过几天，我们打了一次长电话", 2); break;
            case 3320: SeekText("C06", "十一月有一晚", 2); break;
            case 3350: SeekText("C07", "第二年春天，我换了住处", 2); break;
            case 3380: SeekText("C07", "有一晚下大雨", 2); break;
            case 3410:
                Player.Advance();
                Require(Player.Scene.Background == "bg11" && Player.Scene.Tone == "day", "Rain-to-day reading progression failed.");
                break;
            case 3440: SeekText("C07", "那晚我很想她", 2); break;
            case 3470:
                SaveToSlot(6);
                Require(Saves.Load(6).Snapshot.Scene.Background == "bg11-night", "New residence was not saved.");
                SeekText("纱S04", "她上楼拿布");
                LoadFromSlot(6);
                Require(Player.Scene.Background == "bg11-night" && Player.Scene.Location == "外地 · 新住处", "New residence was not restored.");
                break;
            case 3500: SeekText("C08", "我收到那件外套", 2); break;
            case 3530: SeekText("B08", "到门口之前", 1); break;
            case 3560: SeekText("B08", "她说完笑。我笑着点头", 1); break;
            case 3590: SeekText("B08", "取衣那天", 1); break;
            case 3620: SeekArtwork("cg10b", 1); break;
            case 3645:
                Player.Advance();
                Require(Player.Scene.Background == "bg03-reunion" && Player.Scene.SecondaryPortrait == "ye-zhao-reunion-wearing", "Fitting CG did not end before tea.");
                break;
            case 3675:
                SeekText("B08", "走到巷口", 1);
                Require(Player.Scene.Portrait == "ye-zhao-reunion-pocket" && Player.Scene.SecondaryPortrait == "", "Departure did not leave Ye alone with her hand in the new coat pocket.");
                break;
            case 3705: SeekText("C08", "冬天我整理抽屉", 2); break;
            case 3735: SeekText("C08", "我没有立即哭", 2); break;
            case 3760:
                for (var step = 0; Player.Scene.Background != "cg15" && step < 8; step++) Player.Advance();
                Require(Player.Scene.Background == "cg15", "Tang's explicit crying CG was not reached.");
                break;
            case 3785:
                SaveToSlot(6);
                Require(Saves.Load(6).Snapshot.Scene.Background == "cg15", "Solitary crying CG was not saved.");
                Player.Advance();
                Require(Player.Scene.Background == "bg04-winter" && Player.Scene.Portrait == "tang-jianyue-late-cardigan", "Crying CG continued into customer collection.");
                break;
            case 3815: SeekText("C08", "我把手伸进右内侧", 2); break;
            case 3845:
                SaveToSlot(6);
                Require(Saves.Load(6).Snapshot.Scene.Portrait == "ye-zhao-returned-coat-crying", "Returned coat pose was not saved.");
                Player.Advance();
                Player.Advance();
                LoadFromSlot(6);
                Require(Player.Scene.Portrait == "ye-zhao-returned-coat-crying" && Player.Scene.Background == "bg11", "Returned coat pose was not restored.");
                break;
            case 3875:
                Player.Advance();
                Player.Advance();
                Require(Player.Scene.Portrait == "ye-zhao-returned-coat-message", "Old coat crying did not end at the message.");
                break;
            case 3905:
                Player.Advance();
                Player.Advance();
                Require(Player.Scene.Portrait == "ye-zhao-residency-thoughtful", "Old coat stayed on after folding it away.");
                break;
            case 3935:
                Player.Advance();
                Require(Player.Scene.Background == "bg04-winter-night" && Player.Scene.Portrait == "tang-jianyue-late-cardigan", "Final winter shop viewpoint was not restored.");
                break;
            case 3965: SeekText("纱S05", "那以后，我学会了不借东西"); break;
            case 3995: SeekText("纱S05", "明天几点停"); break;
            case 4025: SeekText("A07", "春天我回雁汀"); break;
            case 4055: SeekText("B07", "旧备用相机也拿了", 1); break;
            case 4085: SeekText("B07", "我拖起箱子", 1); break;
            case 4115:
                Player.Advance();
                Require(Player.Scene.Background == "bg12" && Player.Scene.Portrait == "ye-zhao-old-coat-message", "Boarding retained the outdoor luggage pose.");
                break;
            case 4145:
                SaveToSlot(6);
                Require(Saves.Load(6).Snapshot.Scene.Background == "bg12", "Journey scene was not saved.");
                SeekText("纱S04", "她上楼拿布");
                LoadFromSlot(6);
                Require(Player.Scene.Background == "bg12" && Player.Scene.Portrait == "ye-zhao-old-coat-message", "Journey and old coat message were not restored.");
                break;
            case 4175: SeekText("C08", "我把手伸进右内侧", 2); break;
            case 4195:
                Player.Advance();
                Require(Player.Scene.Background == "cg16" && Player.Scene.Portrait == "", "Seated box-side CG was not reached.");
                break;
            case 4218:
                SaveToSlot(6);
                Require(Saves.Load(6).Snapshot.Scene.Background == "cg16", "Seated box-side CG was not saved.");
                LoadFromSlot(6);
                Require(Player.Scene.Background == "cg16", "Seated box-side CG was not restored.");
                break;
            case 4225:
                Player.Advance();
                Require(Player.Scene.Background == "bg11" && Player.Scene.Portrait == "ye-zhao-returned-coat-message", "Seated CG did not end at the phone message.");
                break;
            case 4255: SeekText("C05a", "我把消息打出来", 2); break;
            case 4285: SeekText("A07", "回店后，她给玫瑰换水"); break;
            case 4315:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-repaired-spring", "A-route ending stayed in the street instead of returning upstairs.");
                break;
            case 4345:
                Player.Advance();
                Require(Player.Scene.Background == "bg03-spring" && Player.Scene.Portrait == "ye-zhao" && Player.Scene.SecondaryPortrait == "", "The upstairs caller appeared physically at the shop door.");
                break;
            case 4375: SeekText("纱S06", "唐玉芬来的那天，我在厨房切黄瓜"); break;
            case 4405:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg18" && Player.Scene.Portrait == "", "Kitchen CG was not restored.");
                break;
            case 4415:
                Player.Advance();
                Require(Player.Scene.Background == "bg07" && Player.Scene.TertiaryPortrait == "tang-yufen", "Kitchen work did not end when the aunt spoke.");
                break;
            case 4445: SeekText("纱S06", "你一直在？"); break;
            case 4475:
                SeekText("A06", "正式签约那天");
                Require(Player.Scene.Background == "bg08-winter", "February signing retained autumn windows.");
                break;
            case 4505: SeekText("B06", "我们把话谈到很晚", 1); break;
            case 4535: SeekText("B06", "我哭了。她也哭", 1); break;
            case 4565:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg17", "Mutual tears CG was not restored.");
                break;
            case 4595: SeekText("B06", "我点头，说分别", 1); break;
            case 4625:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-sorted-winter" && Player.Scene.Tone == "day", "Parting CG did not return to the next morning.");
                break;
            case 4655:
                Player.Advance();
                Require(Player.Scene.Background == "bg04-winter" && Player.Scene.Portrait == "tang-jianyue-measuring" && Player.Scene.SecondaryPortrait == "ye-zhao-old-coat", "Final coat adjustment did not change room and costumes.");
                break;
            case 4685: SeekText("B06", "晚上我们出去走了一小段", 1); break;
            case 4715:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "bg05-winter-night" && Player.Scene.Portrait == "tang-jianyue-winter-empty", "Winter walk did not restore the night and empty hands.");
                break;
            case 4745: SeekText("B06", "我笑了一下，眼泪又掉", 1); break;
            case 4775:
                Player.Advance();
                Require(Player.Scene.Background == "cg21" && Player.Scene.Portrait == "", "Cold hands CG was not reached.");
                break;
            case 4805:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-sorted-winter" && Player.Scene.Tone == "day", "Cold hands CG leaked into packing the next day.");
                break;
            case 4835: SeekText("C05a", "唐玉芬又来了", 2); break;
            case 4865:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg19", "Ironing visit CG was not restored.");
                break;
            case 4895: SeekText("C05a", "我喜欢她", 2); break;
            case 4925:
                Player.Advance();
                Require(Player.Scene.Background == "cg22" && Player.Scene.Portrait == "" && Player.Scene.SecondaryPortrait == "", "The aunt did not remain seated after ironing stopped.");
                break;
            case 4935:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg22", "Seated aunt continuation CG was not restored.");
                break;
            case 4955: SeekText("C05a", "姨妈气得走了", 2); break;
            case 4985:
                Player.Advance();
                Require(Player.Scene.Portrait == "tang-jianyue-work-message" && Player.Scene.SecondaryPortrait == "", "Typing kept a sewing box or an absent visitor.");
                break;
            case 5015:
                Player.Advance(); Player.Advance();
                Require(Player.Scene.Portrait == "tang-jianyue-empty-thoughtful", "Phone remained in hand when the iron was switched off.");
                break;
            case 5045: Player.Advance(); break;
            case 5075:
                Player.Advance();
                Require(Player.Scene.Background == "cg20" && Player.Scene.Portrait == "" && Player.Scene.SecondaryPortrait == "", "Seated sewing did not clear the phone and absent aunt.");
                break;
            case 5105:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg20", "Solitary sewing CG was not restored.");
                break;
            case 5135:
                Player.Advance();
                Require(Player.Scene.Background == "bg09-summer-night" && Player.Scene.Portrait == "ye-zhao-residency-thoughtful", "Seated sewing leaked into the next chapter's viewpoint.");
                break;
            case 5165: SeekText("A03", "我抱着电脑下楼"); break;
            case 5195: SeekText("A03", "说出来的语气还是硬"); break;
            case 5225:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg25", "Midnight editing CG was not restored.");
                break;
            case 5255: SeekText("A03", "她笑了一下，困得眼睛只睁一半"); break;
            case 5285:
                Player.Advance();
                Require(Player.Scene.Background == "bg13" && Player.Scene.Portrait == "tang-jianyue-laundry" && Player.Scene.SecondaryPortrait == "ye-zhao-laundry" && Player.Scene.Tone == "day", "Midnight editing did not end at daytime balcony laundry.");
                break;
            case 5315:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "bg13" && Player.Scene.Portrait == "tang-jianyue-laundry" && Player.Scene.SecondaryPortrait == "ye-zhao-laundry", "Laundry actions and balcony were not restored.");
                break;
            case 5345:
                Player.Advance();
                Require(Player.Scene.Background == "bg04-winter" && Player.Scene.Portrait == "tang-jianyue-thoughtful", "Wet laundry remained in the workroom.");
                break;
            case 5375: SeekText("A03", "我把一格柜子清出来"); break;
            case 5405:
                Player.Advance();
                Require(Player.Scene.Background == "cg23" && Player.Scene.Portrait == "", "Old shirt floor scene was not reached.");
                break;
            case 5435:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg23", "Old shirt CG was not restored.");
                break;
            case 5465: SeekText("A03", "能放，不代表要一直放"); break;
            case 5495:
                Player.Advance();
                Require(Player.Scene.Background == "cg24" && Player.Scene.Portrait == "", "Folded shirt did not move to seated companionship.");
                break;
            case 5525:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg24", "Seated companionship CG was not restored.");
                break;
            case 5555: SeekText("A03", "那就坐一会儿"); break;
            case 5585:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-sorted-winter-night" && Player.Scene.Portrait == "tang-jianyue-home-thoughtful", "Floor scene did not end before dinner.");
                break;
            case 5615: SeekText("A05a", "几天后，秦嘉给我发来"); break;
            case 5645: SeekText("A05a", "她把衣服挂好"); break;
            case 5675: SeekText("A05a", "她拿着相机很小心"); break;
            case 5705:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg26", "Portrait photography CG was not restored.");
                break;
            case 5735: SeekText("A05a", "我看她，忍不住笑"); break;
            case 5765:
                Player.Advance();
                Require(Player.Scene.Background == "bg06-winter" && Player.Scene.SecondaryPortrait == "ye-zhao-home", "The photographed subject kept a camera before the timer.");
                break;
            case 5795: SeekText("A05a", "好了，去设"); break;
            case 5825:
                Player.Advance();
                Require(Player.Scene.Background == "cg27", "Second timer photo CG was not reached.");
                break;
            case 5855:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg27", "Timer photo CG was not restored.");
                break;
            case 5885:
                Player.Advance();
                Require(Player.Scene.Background == "bg03" && Player.Scene.SecondaryPortrait == "ye-zhao-home", "The printed photos stayed at the studio instead of returning to the shop.");
                break;
            case 5915: Player.Advance(); break;
            case 5945: SeekText("纱S09", "楼上修好了一次"); break;
            case 5975: SeekText("纱S09", "我给叶照打了电话"); break;
            case 6005:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-sorted-autumn" && Player.Scene.Portrait == "tang-jianyue-home-sore-wrist", "October sorting lost the season or injured wrist.");
                break;
            case 6035:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "bg07-sorted-autumn" && Player.Scene.Portrait == "tang-jianyue-home-sore-wrist", "Injured wrist and autumn room were not restored.");
                break;
            case 6065: SeekText("纱S09", "你是不是早就不想过以前"); break;
            case 6095:
                Player.Advance();
                Require(Player.Scene.Background == "cg28", "Taking out the old photographs did not start its CG.");
                break;
            case 6125:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg28", "Old photographs CG was not restored.");
                break;
            case 6155: SeekText("纱S09", "可是我也不能每开心一次"); break;
            case 6185:
                Player.Advance();
                Require(Player.Scene.Background == "cg29", "Explicit crying did not change to the tearful continuation.");
                break;
            case 6215:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg29", "Tearful photographs CG was not restored.");
                break;
            case 6245: Player.Advance(); break;
            case 6275:
                Player.Advance();
                Require(Player.Scene.Background == "cg07", "The original cabinet CG was not preserved after the visitor left.");
                break;
            case 6305: SeekText("A05", "她不说留，也没有立刻走"); break;
            case 6335:
                Player.Advance();
                Require(Player.Scene.Background == "cg30", "Dinner did not switch from standing portraits to its seated CG.");
                break;
            case 6365:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg30", "Three-person dinner CG was not restored.");
                break;
            case 6395: SeekText("A05", "饭快吃完"); break;
            case 6425:
                Player.Advance();
                Require(Player.Scene.Background == "cg31", "The answer about living together did not start its own CG.");
                break;
            case 6455:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg31", "Dinner answer CG was not restored.");
                break;
            case 6485: SeekText("A05", "不要了。锅里剩的"); break;
            case 6515:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-repaired-night" && Player.Scene.SecondaryPortrait == "ye-zhao-washing-thoughtful" && Player.Scene.TertiaryPortrait == "", "Dinner continued after the visitor left instead of switching to washing.");
                break;
            case 6545: SeekText("B07a", "我今天有点想你", 1); break;
            case 6575:
                Player.Advance();
                Require(Player.Scene.Background == "cg32" && Player.Scene.Portrait == "", "Sitting at the bedside did not start the solitary call CG.");
                break;
            case 6605:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg32", "Bedside call CG was not restored.");
                break;
            case 6635: SeekText("B07a", "我知道不是。我没想", 1); break;
            case 6665:
                Player.Advance();
                Require(Player.Scene.Background == "bg09-spring-night" && Player.Scene.Portrait == "ye-zhao-residency-phone-charging", "Picking up the cable did not end the call CG.");
                break;
            case 6695:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Portrait == "ye-zhao-residency-phone-charging", "Charging phone pose was not restored.");
                break;
            case 6725: SeekText("B07a", "快挂时", 1); break;
            case 6755:
                Player.Advance();
                Require(Player.Scene.Portrait == "ye-zhao-residency-thoughtful", "Phone was not put down after hanging up.");
                break;
            case 6785:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-sorted-night" && Player.Scene.Viewpoint == "唐见月", "The same-night cut did not return to Tang's lit room.");
                break;
            case 6815: Player.Advance(); break;
            case 6845:
                Player.Advance();
                Require(Player.Scene.Background == "cg33", "Folding the worn apron did not start its CG.");
                break;
            case 6875:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "cg33", "Apron folding CG was not restored.");
                break;
            case 6905:
                Player.Advance();
                Require(Player.Scene.Background == "bg07-lights-out" && Player.Scene.Portrait == "" && Player.Scene.Tone == "night-painted", "Switching off the light did not leave the empty room.");
                break;
            case 6935:
                SaveToSlot(6); LoadFromSlot(6);
                Require(Player.Scene.Background == "bg07-lights-out" && Player.Scene.Portrait == "", "Unlit empty room was not restored.");
                break;
            case 6965:
                Player.Advance();
                Require(Player.Scene.Background == "bg09" && Player.Scene.Viewpoint == "叶照" && Player.Scene.Time == "2031年 · 外地", "The year and viewpoint did not change after the unlit room.");
                break;
            case 6995:
                RoseValidation.Validate(Options);
                var collected = ReadingProgress.Load(Path.Combine(DataDirectory, "reading-progress.json"), Options.Story.Id);
                foreach (var pose in new[] { "ye-zhao-reunion-carrying", "ye-zhao-reunion-wearing", "ye-zhao-reunion-pocket",
                    "tang-jianyue-reunion", "tang-jianyue-late-cardigan", "ye-zhao-returned-coat-crying", "ye-zhao-returned-coat-message",
                    "ye-zhao-old-coat", "ye-zhao-old-coat-departing", "ye-zhao-old-coat-message",
                    "tang-jianyue-empty", "tang-jianyue-empty-thoughtful", "tang-jianyue-work-message", "tang-jianyue-winter-empty",
                    "ye-zhao-old-coat-tearful", "bg03-ironing", "bg05-winter-night", "bg08-winter", "cg17", "cg18", "cg19", "cg20", "cg21",
                    "cg22", "cg23", "cg24", "cg25", "cg26", "cg27", "bg13", "bg06-winter", "bg06-winter-night", "tang-jianyue-laundry", "ye-zhao-laundry",
                    "cg28", "cg29", "cg30", "cg31", "cg32", "cg33", "tang-jianyue-home-sore-wrist", "ye-zhao-residency-phone-charging", "bg07-autumn", "bg07-sorted-autumn", "bg07-lights-out" })
                    Require(collected.Artwork.Contains(pose), "A read costume remained locked in the gallery: " + pose);
                File.WriteAllText(Path.Combine(smokeDirectory, "success.json"), "{\"passed\":true,\"routes\":12,\"screenshots\":272,\"continue\":true,\"legacyScene\":true,\"thirdActorSave\":true,\"sortedRoomSave\":true,\"repairedRoomSave\":true,\"visitorNightSave\":true,\"newResidenceSave\":true,\"solitaryCgSave\":true,\"returnedCoatSave\":true,\"journeySave\":true,\"seatedCgSave\":true,\"kitchenCgSave\":true,\"mutualTearsCgSave\":true,\"winterWalkSave\":true,\"ironingVisitCgSave\":true,\"sewingCgSave\":true,\"seatedAuntCgSave\":true,\"editingCgSave\":true,\"laundrySave\":true,\"oldShirtCgSave\":true,\"seatedCompanionshipCgSave\":true,\"photographyCgSave\":true,\"timerCgSave\":true,\"injuredWristSave\":true,\"photographsCgSave\":true,\"cryingPhotographsCgSave\":true,\"dinnerCgSave\":true,\"dinnerAnswerCgSave\":true,\"bedsideCallCgSave\":true,\"chargingPhoneSave\":true,\"apronCgSave\":true,\"unlitRoomSave\":true,\"newArtworkUnlocked\":11,\"storyCg\":36,\"sprites\":56,\"backgrounds\":55}");
                Console.WriteLine("Rose visual smoke passed: title, dialogue, choices, save/load, settings, history, gallery, pause and all endings.");
                Exit();
                break;
        }
        _captureName = _frame switch
        {
            20 => "01-title", 65 => "02-opening", 102 => "03-boutique", 130 => "04-first-choice",
            180 => "05-summer", 240 => "06-save", 270 => "07-settings", 300 => "08-backlog",
            330 => "09-gallery", 360 => "10-route-choice", 395 => "11-ending-a",
            425 => "12-ending-b", 455 => "13-ending-c", 480 => "14-pause",
            314 => "33-gallery-locked", 374 => "34-ending-silence",
            1060 => "28-daytime-alley", 1100 => "29-furnished-studio", 1140 => "30-distance-call",
            1180 => "31-returned-coat-alone", 1220 => "32-rain-night", _ => null
        };
        _captureName ??= _frame switch
        {
            1260 => "35-shoulder-action", 1290 => "36-home-calm", 1320 => "37-home-difficult",
            1350 => "38-temporary-studio", 1380 => "39-entry-before", 1388 => "40-entry-middle",
            1392 => "41-entry-after", _ => null
        };
        _captureName ??= _frame switch
        {
            1420 => "42-aunt-arrives", 1433 => "43-aunt-expression-middle", 1450 => "44-aunt-stern",
            1480 => "45-mother-visits-three", 1510 => "46-waiting-without-visitors",
            1540 => "47-mother-and-tang", 1570 => "48-family-dinner-night", 1600 => "49-aunt-departed",
            1630 => "50-photo-studio-night", 1660 => "51-residency-night", _ => null
        };
        _captureName ??= _frame switch
        {
            1700 => "52-washing-banter", 1730 => "53-last-bowl", 1760 => "54-tang-tearful",
            1790 => "55-tears-and-towel-cg", 1798 => "56-cg-return-middle", 1815 => "57-after-crying",
            1840 => "58-departure-morning", 1870 => "59-towel-at-door", 1900 => "60-residency-visit",
            1930 => "61-boutique-night", 1960 => "62-workroom-night", _ => null
        };
        _captureName ??= _frame switch
        {
            2000 => "63-landlord-viewing", 2030 => "64-landlord-confirmation", 2060 => "65-private-confession",
            2090 => "66-qin-feedback", 2120 => "67-work-planning", 2150 => "68-back-in-private-room",
            2190 => "69-storm-dawn", 2220 => "70-organized-room-night", 2245 => "71-organized-save-restored",
            2280 => "72-before-sorting", 2293 => "73-sorting-transition", 2310 => "74-after-sorting", _ => null
        };
        _captureName ??= _frame switch
        {
            2360 => "75-hand-sewing", 2390 => "76-sewing-lesson-cg", 2420 => "77-summer-night-return",
            2450 => "78-january-leak", 2480 => "79-temporary-ironing-cg", 2488 => "80-ironing-return-middle",
            2505 => "81-temporary-night", 2540 => "82-repaired-day", 2570 => "83-repaired-night",
            2600 => "84-repaired-save-restored", 2630 => "85-temporary-later-day", _ => null
        };
        _captureName ??= _frame switch
        {
            2670 => "86-venue-guidance", 2700 => "87-manager-apology", 2730 => "88-bride-worried",
            2760 => "89-bride-relieved", 2790 => "90-wedding-entrance", 2820 => "91-visitor-arrival",
            2850 => "92-visitor-settled", 2880 => "93-visitor-handwork-cg", 2888 => "94-handwork-return-middle",
            2905 => "95-visitor-return-1530", 2940 => "96-visitor-third-night", 2970 => "97-visitor-night-restored",
            3000 => "98-visitor-departed", 3030 => "99-december-residency", 3060 => "100-later-winter-return", _ => null
        };
        _captureName ??= _frame switch
        {
            3100 => "101-march-spring", 3130 => "102-may-warm-call", 3160 => "103-june-tense-call",
            3190 => "104-june-hangup", 3220 => "105-summer-night-call", 3250 => "106-spring-night-call",
            3280 => "107-spring-hangup", 3310 => "108-august-argument", 3340 => "109-november-autumn",
            3370 => "110-moved-residence", 3400 => "111-new-home-rain", 3413 => "112-rain-return-middle",
            3430 => "113-new-home-after-rain", 3460 => "114-ticket-search-night", 3490 => "115-new-home-restored",
            3520 => "116-coat-at-new-home", _ => null
        };
        _captureName ??= _frame switch
        {
            3550 => "117-reunion-carrying-coat", 3580 => "118-reunion-wearing-coat", 3610 => "119-pickup-two-days-later",
            3640 => "120-reunion-fitting-cg", 3648 => "121-fitting-return-middle", 3665 => "122-tea-after-fitting",
            3695 => "123-departure-winter-alley", 3725 => "124-tang-winter-cardigan", 3755 => "125-before-tang-cries",
            3763 => "126-solitary-tears-middle", 3780 => "127-solitary-tears-cg", 3788 => "128-tears-return-middle",
            3805 => "129-work-after-washing-face", 3835 => "130-returned-coat-crying", 3865 => "131-returned-coat-restored",
            3878 => "132-tears-to-message-middle", 3895 => "133-thanks-for-pocket", 3908 => "134-coat-folded-middle",
            3925 => "135-coat-put-away", 3938 => "136-final-viewpoint-middle", 3955 => "137-final-winter-shop", _ => null
        };
        _captureName ??= _frame switch
        {
            3985 => "138-may-boutique-spring", 4015 => "139-june-boutique-summer", 4045 => "140-a-route-spring-return",
            4075 => "141-finished-old-coat", 4105 => "142-departing-with-tears-and-luggage", 4118 => "143-boarding-middle",
            4135 => "144-onboard-message", 4165 => "145-journey-restored", 4198 => "146-box-side-cg-middle",
            4215 => "147-box-side-tears-cg", 4228 => "148-box-side-return-middle", 4245 => "149-message-after-seated-cg",
            4275 => "150-june-shop-after-exhibition", 4305 => "151-a-ending-back-at-shop", 4335 => "152-a-ending-upstairs-spring",
            4365 => "153-a-ending-door-reply", _ => null
        };
        _captureName ??= _frame switch
        {
            4395 => "154-cucumber-preparation-cg", 4418 => "155-kitchen-return-middle", 4435 => "156-aunt-upstairs-after-cooking",
            4465 => "157-empty-hands-after-visitor", 4495 => "158-empty-hands-at-signing", 4525 => "159-parting-late-night",
            4555 => "160-mutual-tears-cg", 4585 => "161-mutual-tears-restored", 4615 => "162-parting-embrace",
            4628 => "163-parting-morning-middle", 4645 => "164-parting-next-morning", 4658 => "165-final-fitting-middle",
            4675 => "166-final-old-coat-adjustment", 4705 => "167-winter-night-walk", 4735 => "168-winter-walk-restored",
            4765 => "169-winter-walk-tearful", 4778 => "170-cold-hands-middle", 4795 => "171-cold-hands-cg",
            4808 => "172-packing-return-middle", 4825 => "173-packing-after-last-walk", 4855 => "174-ironing-visit-cg",
            4885 => "175-ironing-cg-restored", 4915 => "176-confession-during-ironing", 4928 => "177-iron-put-down-middle",
            4945 => "178-parked-iron-seated-aunt-cg", 4975 => "179-aunt-left-the-shop", 4988 => "180-typing-middle",
            5005 => "181-message-typed-and-deleted", 5018 => "182-put-phone-away-middle", 5035 => "183-iron-switched-off",
            5065 => "184-asking-if-she-is-busy", 5078 => "185-seated-sewing-middle", 5095 => "186-alone-sewing-at-door-cg",
            5125 => "187-sewing-cg-restored", 5138 => "188-sewing-viewpoint-return-middle", 5155 => "189-next-chapter-summer-night", _ => null
        };
        _captureName ??= _frame switch
        {
            5185 => "190-downstairs-before-editing", 5215 => "191-midnight-editing-cg", 5245 => "192-editing-cg-restored",
            5275 => "193-sleepy-companionship", 5288 => "194-laundry-daytime-middle", 5305 => "195-balcony-laundry",
            5335 => "196-laundry-restored", 5348 => "197-laundry-workroom-return-middle", 5365 => "198-tools-after-laundry",
            5395 => "199-before-old-shirt", 5408 => "200-old-shirt-middle", 5425 => "201-old-shirt-floor-cg",
            5455 => "202-old-shirt-restored", 5485 => "203-standing-by-the-door", 5498 => "204-seated-companionship-middle",
            5515 => "205-seated-companionship-cg", 5545 => "206-seated-cg-restored", 5575 => "207-memory-reaction-seated",
            5588 => "208-floor-dinner-return-middle", 5605 => "209-dinner-after-wardrobe", 5635 => "210-studio-winter-night",
            5665 => "211-studio-winter-day", 5695 => "212-portrait-photography-cg", 5725 => "213-camera-cg-restored",
            5755 => "214-waiting-to-be-photographed", 5768 => "215-camera-timer-return-middle", 5785 => "216-standing-before-timer",
            5815 => "217-second-timer-start", 5828 => "218-second-timer-middle", 5845 => "219-second-timer-photo-cg",
            5875 => "220-timer-photo-restored", 5888 => "221-returning-prints-middle", 5905 => "222-prints-back-at-shop",
            5935 => "223-photos-put-beside-old-photo", _ => null
        };
        _captureName ??= _frame switch
        {
            5965 => "224-injured-wrist-autumn", 5995 => "225-before-sorting-autumn", 6008 => "226-autumn-sorting-middle",
            6025 => "227-sorted-autumn-with-visitor", 6055 => "228-wrist-autumn-restored", 6085 => "229-before-photographs",
            6098 => "230-photographs-middle", 6115 => "231-old-photographs-cg", 6145 => "232-photographs-restored",
            6175 => "233-before-explicit-crying", 6188 => "234-photographs-crying-middle", 6205 => "235-crying-photographs-cg",
            6235 => "236-crying-photographs-restored", 6265 => "237-closing-album", 6278 => "238-cabinet-return-middle",
            6295 => "239-original-cabinet-cg", 6325 => "240-before-dinner", 6338 => "241-dinner-middle",
            6355 => "242-three-person-dinner-cg", 6385 => "243-dinner-restored", 6415 => "244-before-living-together-answer",
            6428 => "245-dinner-answer-middle", 6445 => "246-we-live-together-cg", 6475 => "247-dinner-answer-restored",
            6505 => "248-dinner-ending", 6518 => "249-dinner-washing-middle", 6535 => "250-washing-after-visitor",
            6565 => "251-before-bedside-call", 6578 => "252-bedside-call-middle", 6595 => "253-bedside-call-cg",
            6625 => "254-bedside-call-restored", 6655 => "255-before-charging", 6668 => "256-charging-middle",
            6685 => "257-charging-phone", 6715 => "258-charging-phone-restored", 6745 => "259-before-hanging-up",
            6758 => "260-hanging-up-middle", 6775 => "261-phone-put-down", 6788 => "262-same-night-viewpoint-middle",
            6805 => "263-tang-room-light-still-on", 6835 => "264-before-folding-apron", 6848 => "265-apron-middle",
            6865 => "266-folding-apron-cg", 6895 => "267-apron-restored", 6908 => "268-turning-light-off-middle",
            6925 => "269-unlit-empty-room", 6955 => "270-unlit-room-restored", 6968 => "271-next-year-viewpoint-middle",
            6985 => "272-next-year-ye-alone", _ => null
        };
        if (_frame >= 490 && _frame < 1010)
        {
            var index = (_frame - 490) / 40;
            var phase = (_frame - 490) % 40;
            if (phase == 0) SeekArtwork(StoryArt[index], StoryArt[index] == "cg10b" ? 1 : StoryArt[index] == "cg10c" ? 2 : 0);
            if (phase == 30) _captureName = $"{15 + index:00}-{StoryArt[index]}";
        }
        _capturePending = _captureName is not null;
    }

    private void Seek(string section, string speaker)
    {
        StartNewGame();
        for (var step = 0; step < 3000 && !Player.IsEnded; step++)
        {
            if (Player.CurrentNodeId.StartsWith(section + "-", StringComparison.Ordinal) && Player.CurrentNode.Speaker == speaker)
                return;
            if (Player.CurrentNode.Kind == NodeKind.Choice) Player.Choose(0);
            else Player.Advance();
        }
        throw new InvalidOperationException($"Could not find {section}/{speaker}.");
    }

    private void SeekChoice(string id)
    {
        StartNewGame();
        for (var step = 0; step < 3000 && !Player.IsEnded; step++)
        {
            if (Player.CurrentNodeId == id) return;
            if (Player.CurrentNode.Kind == NodeKind.Choice) Player.Choose(0);
            else Player.Advance();
        }
        throw new InvalidOperationException($"Could not find {id}.");
    }

    private void Finish()
    {
        for (var step = 0; !Player.IsEnded && step < 3000; step++)
        {
            Require(Player.CurrentNode.Kind != NodeKind.Choice, "A branch contains an extra choice.");
            Player.Advance();
        }
        Require(Player.IsEnded, "Route did not end.");
    }

    private void SeekArtwork(string texture, int route = 0)
    {
        StartNewGame();
        for (var step = 0; step < 3000 && !Player.IsEnded; step++)
        {
            if (Player.Scene.Background == texture) return;
            if (Player.CurrentNode.Kind == NodeKind.Choice) Player.Choose(Player.CurrentNodeId == "q3" ? route : 0);
            else Player.Advance();
        }
        throw new InvalidOperationException($"Could not find story illustration {texture}.");
    }

    private void SeekText(string section, string text, int route = 0)
    {
        StartNewGame();
        for (var step = 0; step < 3000 && !Player.IsEnded; step++)
        {
            if (Player.CurrentNodeId.StartsWith(section + "-", StringComparison.Ordinal) && Player.CurrentNode.Text.StartsWith(text, StringComparison.Ordinal)) return;
            if (Player.CurrentNode.Kind == NodeKind.Choice) Player.Choose(Player.CurrentNodeId == "q3" ? route : 0);
            else Player.Advance();
        }
        throw new InvalidOperationException($"Could not find {section}: {text}.");
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
        if (smokeDirectory is null || !_capturePending) return;
        CaptureScreenshot(Path.Combine(smokeDirectory, _captureName + ".png"));
        _capturePending = false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

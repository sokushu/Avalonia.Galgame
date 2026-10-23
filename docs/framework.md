# 框架接入与扩展

## 宿主与资源

`MonoGalGame.Core` 独立运行剧情，不创建窗口、纹理或音频设备。`MonoGalGame.Framework` 引用 Core，提供默认的 MonoGame 阅读界面。作品自己的可执行项目负责创建 `GalgameGame`、复制资源和分发文件。

启动项目的资源配置示例：

```xml
<ItemGroup>
  <ProjectReference Include="../MonoGalGame.Framework/MonoGalGame.Framework.csproj" />
  <None Update="Game/**/*"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

`Game/game.json`：

```json
{
  "id": "my-first-game",
  "title": "我的第一个故事",
  "subtitle": "从一次相遇开始。",
  "titleCaption": "My First Story",
  "story": "Stories/main.json",
  "font": "Fonts/MyChineseFont.otf",
  "titleBackground": "school",
  "textures": {
    "school": "Images/school.png",
    "heroine": "Images/heroine.png"
  },
  "audio": {
    "theme": "Audio/theme.ogg",
    "voice-001": "Audio/voice-001.wav"
  }
}
```

`id` 由 ASCII 字母、数字、点、短横线和下划线组成，保持稳定以便继续访问原有存档。文件路径相对于 `game.json` 的目录，推荐使用 `/`；禁止绝对路径和 `../` 逃出作品目录。启动不依赖当前工作目录。字体随作品分发，不从玩家电脑上按名称查找。

默认图片服务读取普通图片文件，不要求 XNB。背景等比裁切铺满；立绘使用保留透明通道的图片，按源宽高比缩放并靠右显示。示例的 `title`、`campus`、`library`、`river`、`aoi` 已映射到作品 `Game/Art/` 下的主视觉、场景插画和透明立绘；自己的作品可替换为对应图片。`titleCaption` 是可选的标题页英文副标，旧配置省略时不显示。

默认界面使用直角面板、按钮与细线边框；标题菜单中文逐字竖排，可用左右或上下方向键、Tab 和 Enter 操作。阅读界面保留姓名签、逐字正文和文字工具栏；存档卡按快照中的背景展示场景预览，不另存截图文件。

## 最小剧情

`Stories/main.json`：

```json
{
  "id": "chapter-one-v1",
  "title": "第一章 · 相遇",
  "start": "hello",
  "nodes": {
    "hello": {
      "speaker": "小夏",
      "text": "你好，欢迎来到这个故事。\n接下来由你来选择。",
      "scene": { "background": "school", "portrait": "heroine", "music": "theme" },
      "voice": "voice-001",
      "next": "choice"
    },
    "choice": {
      "kind": "Choice",
      "speaker": "我",
      "text": "要留下来吗？",
      "choices": [
        {
          "text": "留下",
          "next": "stay",
          "effects": [{ "variable": "affection", "value": 1, "operation": "Add" }]
        },
        { "text": "下次再见", "next": "end" }
      ]
    },
    "stay": { "text": "我们又聊了很久。", "next": "end" },
    "end": { "kind": "End", "text": "未完待续。" }
  }
}
```

节点 ID 在本剧情内唯一。未指定 `kind` 时为 `Dialogue`。一个作品当前加载一个剧情图，可以用节点命名划分章节。默认宿主一次最多显示四个选项，更多选项可以用上下方向键浏览。

| 节点类型 | 必需字段 | 行为 |
| --- | --- | --- |
| `Dialogue` | `next` | 等待读者读完并推进；`speaker` 可留空 |
| `Choice` | `choices` | 显示条件满足的选项，等待玩家选择 |
| `Set` | `next` | 执行 `effects` 后自动继续 |
| `Branch` | `condition`、`then`、`else` | 按条件选择后续节点 |
| `Jump` | `next` | 自动跳转 |
| `End` | 无 | 停止推进并显示结局标题 `text` |

所有节点可以附带 `scene` 和 `effects`，进入节点时执行一次。选项的 `effects` 仅在选中时执行。变量是有符号 32 位整数，未设置的变量按 `0` 判断；`Set` 赋值，`Add` 累加，整数溢出会报告错误。条件格式：

```json
{ "variable": "affection", "comparison": "GreaterOrEqual", "value": 3 }
```

比较类型：`Equal`、`NotEqual`、`Greater`、`GreaterOrEqual`、`Less`、`LessOrEqual`。省略比较类型时为 `Equal`。把 `condition` 放到选项上，可以控制该选项是否出现。创作者应保证选择节点至少有一个可用选项；运行时检测到空选择会恢复前一步并报告内容错误。

场景字段省略或设为 `null` 时保留前一状态；空字符串 `""` 清除对应层。进入下一句会停止上一句的语音。背景音乐保持到显式更换或清除，返回标题时停止。

## 字体与排版

`TrueTypeFontFace` 使用 StbTrueTypeSharp 解析可信的本地字体文件并生成字形位图。`DynamicFont` 负责懒加载图集和纹理缓存；同一字形、同一整数像素字号只生成一次。图集为 1024×1024，单实例上限 16 页；达到上限会明确报告错误。渲染与纹理更新在游戏图形线程进行。

设计字号保持不变，光栅化字号随内容区的实际像素密度变化。例如 1920×1080 的内容区中，24 设计像素的对白按 36 物理像素生成。最终字形原点对齐显示像素，图集不跟随低分辨率画布整体放大。调整窗口大小时释放旧分辨率图集，逻辑宽度与分页保持稳定。自定义字体可以实现 `IRenderScaleAwareFont` 接收显示比例。

`PresentationLayout` 统一处理设计坐标、帧缓冲像素和 SDL 鼠标坐标。内容画布始终与实际显示区域一比一呈现；全屏同步当前显示器的窗口尺寸，退出时恢复窗口大小。示例启动程序包含 `app.manifest`，声明 Windows PerMonitorV2；框架也在 SDL 初始化前设置默认 DPI 提示。自己的 Windows 启动项目建议包含同样的清单。MonoGame 3.8.6 创建窗口时的标题编码有问题，宿主在图形窗口创建后通过 UTF-8 标题更新接口重新设置中文标题。

默认初始窗口尺寸会随 Windows DPI 增大，并限制在可用桌面区域内，避免启用 DPI 感知后窗口和字体看起来突然变小。需要精确指定物理窗口尺寸时，设置 `GalgameOptions.ScaleWindowToDpi = false`，自动画面测试使用此选项。

`IGameFont` 可替换，`ITextMetrics` 让 Core 在无图形设备时也能排版。默认采用中文逐字换行、基础避头尾规则、英文单词边界和固定分页。段落在播放前计算完整布局，逐字显示不会引起前面的字重新排版。单个文本元素放不进一行时，仍单独保留这个元素。

需要多字体回退时，可以继承宿主：

```csharp
public sealed class MyGame(GalgameOptions options) : GalgameGame(options)
{
    protected override IGameFont CreateFont() => new DynamicFont(
        GraphicsDevice,
        Options.Project.ResolvePath("Fonts/MyChineseFont.otf"),
        Options.Project.ResolvePath("Fonts/MyFallbackFont.ttf"));
}
```

字体按顺序查找字形，均不覆盖时回退到 `?`。默认实现不做复杂文字 shaping，不保证组合附加符、Emoji 序列或阿拉伯文的视觉排列正确；按文本元素推进仅保证显示进度不会切开这些元素。可以自行接入 HarfBuzz 等排版后端，实现另一个 `IGameFont`。

## 扩展服务与界面

继承 `GalgameGame`，覆盖以下工厂：

- `CreateFont()` → `IGameFont`：文字度量和绘制。
- `CreateTextureSource()` → `ITextureSource`：图片检索、缓存和释放。示例采用文件资源优先、程序美术补充的方式。
- `CreateAudioService()` → `IAudioService`：背景音乐、语音和音量。

默认音频服务支持 OGG 背景音乐流式播放；WAV 背景音乐通过 SoundEffect 一次载入内存并循环，适合短音轨。角色语音通过 WAV SoundEffect 播放。长音轨优先使用 OGG；其他格式可以替换 `IAudioService`。音频能力失败会显示提示，阅读保持可用。示例没有录制对白。

宿主负责释放工厂返回的服务。自定义服务应实现幂等 `Dispose`；纹理和字体只能在图形线程使用。可通过 `PollKeyboard`、`PollMouse` 和 `HasInputFocus` 适配其他输入或自动验证。

如需完全自定义视觉风格和布局，可以只复用 `StoryPlayer`、`TextLayout`、`SaveStore` 及媒体/字体服务，自建 MonoGame `Game` 和界面。第一版默认菜单布局采用固定设计坐标，尚未提供可视化编辑器或主题文件。

## 状态、存档和兼容性

`StoryPlayer.Advance()` 推进对白；`Choose(index)` 接收当前可见选项的索引；`Rollback()` 回到上一步。`Capture()` 和 `Restore(snapshot)` 保存、恢复当前节点、变量、场景和历史。快照复制可变集合，恢复不会重放节点或选项的变量操作。

自动节点连续执行超过 1024 步会报告可能的循环。普通推进中发生命令错误时恢复前一状态。存档使用版本 `1`，同时检查作品 ID、剧情 ID 和节点是否仍有效；修改剧情结构时请主动变更剧情 ID 或自行实现迁移，框架不猜测如何迁移剧情。

`SaveStore` 的槽位 `1..6` 为手动存档，`0` 为独立快速存档。手动覆盖已有位置前，界面提供确认。写入时先完成临时文件并刷新，再替换正式文件；读取损坏存档会给出错误。设置单独存储。第一版没有截图缩略图、自动存档、存档云同步、全局已读记录或跨启动回退栈。

## 验证边界

`--validate` 不创建窗口，检查配置、资源、中文字形，以及示例两条路径能否结束。`--smoke-test <目录>` 实际创建 1024×768 窗口并自动运行示例，检查键盘输入、等比留边后的鼠标命中、快速读写、设置持久化、历史和结局，按内容区实际像素保存八张画面。测试重置独立目录下的设置以保证重复运行一致。

附加 `--display-test` 会继续调整为 1080p、4K 和带留边的窗口，切换全屏、返回窗口、再缩回原尺寸。还会通过左右键和鼠标验证竖排标题菜单，浏览河畔立绘与暂停菜单，并实际确认覆盖存档；共输出十九张画面。检查 SDL 标题和系统实际标题与作品名一致、Windows DPI 感知正确、帧缓冲与可绘制像素一致、字体缓存比例正确、排版宽度不随显示比例改变。每个检查点输出 PNG 和 JSON，成功后写入 `success.json`。截图中的像素是游戏内容，不包含系统标题栏；标题通过系统接口独立读取验证。

## Native AOT

Core 和 Framework 均启用 `IsAotCompatible` 分析；示例启用 `PublishAot`，并将发布时的裁剪及 AOT 警告视为错误。全仓库默认关闭 JSON 反射，使日常构建和测试也使用生成的元数据。剧情、配置、快照、存档、设置及诊断报告均有编译时生成的 JSON 上下文；枚举保持字符串格式，已有作品和存档不需要补字段。

.NET 10 源生成 JSON 对缺失的 `init` 属性有默认值兼容问题，因此 JSON DTO 使用普通可写属性，保留原来的缺省值与显式 `null` 校验。修改反序列化方式时，应同时验证真实示例和省略字段的旧存档。

自己的启动项目可使用以下配置，并在目标操作系统发布：

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
  <TrimmerSingleWarn>false</TrimmerSingleWarn>
  <ILLinkTreatWarningsAsErrors>true</ILLinkTreatWarningsAsErrors>
  <IlcTreatWarningsAsErrors>true</IlcTreatWarningsAsErrors>
</PropertyGroup>
```

`AtomicJson.Write(path, value)` 只接受 Core 上下文注册的类型。扩展自己的保存数据时，生成自己的上下文并传入明确的类型元数据：

```csharp
[JsonSerializable(typeof(MySaveData))]
internal partial class MyJsonContext : JsonSerializerContext;

// 对应类型须能在编译时生成 JSON 元数据。
AtomicJson.Write(path, value, MyJsonContext.Default.MySaveData);
```

`StoryJson.Options` 现在是绑定 Core 元数据的只读配置，不会为外部类型回退到反射。自定义字体、媒体服务或其他依赖也必须能够静态编译；不要用动态加载程序集或运行时生成代码来实现扩展。

发布后执行 `scripts/Verify-NativeAot.ps1`：它检查产物不含托管入口 DLL，换一个工作目录运行 `--validate --require-aot`。加 `-DisplayTest` 会执行完整图形验证，`success.json` 记录动态代码、托管程序集和音频错误信息。加 `-RequireAudio` 可要求音乐加载与播放过程中没有错误；无扬声器的验证环境可以设置 `ALSOFT_DRIVERS=null`，该方式不验证实际可听到的音质。

自动检查不能替代目标设备上的图形驱动、音频输出、输入法、无障碍、签名和安装体验验收。当前框架面向桌面，未提供 Android/iOS 宿主、视频播放、Live2D 或剧情编辑器。

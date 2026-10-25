# MonoGalGame

基于 **MonoGame DesktopGL 3.8.6 / .NET 10** 的 Galgame 框架。剧情、画面与作品资源分离，附带可运行的中文示例《春日来信》。

独立作品 **《玫瑰与旧婚纱》**：按照 `docs/玫瑰与旧婚纱/设计稿/剧情设计/` 的正式剧本制作，包含 1,085 段原文对白与旁白、三次选择、三个独立结局、五十六张透明立绘（九位人物的服装、表情与动作）、主视觉、五十五张背景、三十六张剧情 CG 与五首合成配乐。手工收边、学缝衣、熨裙及探访时坐姿手工贴合原文，会馆配角按实际场次进退，驻留室四季、搬家后的新住处与一月维修状态分别演出；通话区分温和与紧绷表情，挂断后收起电话。2033 年重逢使用成年造型，区分携带和穿上新外套；独自落泪、摸到旧内袋、回消息及叠好外套分别演出。B 线分别呈现穿旧外套收拾、拖行李落泪及车上回消息，C 线补齐箱子旁的坐姿 CG，A 线尾声从橱窗街接回店内与楼上。厨房备菜、姨妈来访时熨衣、独自缝衣和分别前的落泪与冬夜捂手均有对应插画，放下工具及打字按原文切换。柜边段落区分抱旧衬衫、收好后并坐；夜里修图、阳台洗衣与互相拍照也有对应演出。伤腕与秋景整理、婆媳看旧照片到落泪、三人饭桌，以及通话后充电、折旧围裙和关灯均按原文时机切换。全套界面采用直角设计，使用双行书名、六列竖排菜单、章节与视角标识；支持三人同场、人物分别渐变，背景与环境光同步过渡；支持重启后继续有效存档，剧情画廊随阅读解锁。

```powershell
dotnet run --project examples/MonoGalGame.Rose
```

在 IDE 中将 `examples/MonoGalGame.Rose` 设为启动项目。完整运行、验收和发布说明见 [《玫瑰与旧婚纱》](docs/rose-game.md)。

## 运行

安装 .NET 10 SDK，在仓库根目录执行：

```powershell
dotnet restore MonoGalGame.slnx
dotnet run --project examples/MonoGalGame.Demo
```

在 IDE 中，将 `examples/MonoGalGame.Demo` 设置为启动项目。示例自带中文字体、原创日系主视觉、透明角色立绘、三张场景插画及合成背景音乐，不需要安装系统中文字体或 MGCB。

界面采用暖白、灰青与玫瑰色的信笺主题：全幅主视觉、右侧竖排文字菜单、直角细线对话框、场景预览存档卡，以及统一的设置、历史与暂停页面。所有按钮、面板、进度条和提示均为直角。美术资源位于 `examples/MonoGalGame.Demo/Game/Art/`，生成提示词与规格见 [美术说明](docs/art-direction.md)。

## 工程结构

| 工程 | 用途 |
| --- | --- |
| `MonoGalGame.Core` | 无 MonoGame 依赖的剧情图、变量、条件分支、历史、回退、快照、存档和文字布局 |
| `MonoGalGame.Framework` | 可引用的 MonoGame 游戏宿主、动态中文字库、菜单、输入、图片与音频服务 |
| `examples/MonoGalGame.Demo` | 独立启动项目和示例作品；剧情、字体及音频位于 `Game/` |
| `MonoGalGame.Tests` | 剧情路线、状态恢复、异常剧情、存档、Unicode 排版与中文字体测试 |

框架类库不引用示例，示例资源不进入框架 NuGet 包。

## 第一版能力

- JSON 剧情图：对白、条件选项、整数变量赋值与累加、条件分支、跳转和结局。
- 背景与单角色立绘，保持或清除场景层，立绘淡入。
- 自有动态字体图集：TTF/OTF 字形按需生成与缓存、Unicode 码点、可注入字体回退。
- 中文横排、基础标点避头尾、英文单词换行、自动分页，按文本元素播放打字机效果。
- 标题、阅读、暂停、历史、设置、6 个手动存档和独立快速存档。
- 自动阅读、按住 Ctrl 快进、对白回退、隐藏界面、全屏、窗口缩放和等比留黑边。
- OGG 流式背景音乐、WAV 背景音乐和角色语音，独立音乐/语音音量；音频失败时保留文字阅读。
- 存档原子替换、作品与剧情版本检查、无效目标检查、自动命令死循环保护。

字体底层使用 `StbTrueTypeSharp` 生成字形；图集、排版、分页和打字机由本工程实现，没有使用 FontStashSharp。当前为基础中英文横排，不包含完整 OpenType shaping、双向排版、彩色 Emoji、竖排或注音排版。扩展汉字和组合字符能作为完整文本元素推进，但实际字形仍取决于字体覆盖和排版能力。

## 操作

| 操作 | 按键 |
| --- | --- |
| 显示整页 / 下一页 / 下一句 | 左键、Space、Enter |
| 选择选项 | 点击、数字 1–9，或上下方向键后 Enter |
| 快速保存 / 快速读取 | F5 / F9 |
| 自动阅读 | A |
| 快进，遇选择停止 | 按住 Ctrl |
| 历史 / 隐藏界面 | L / H |
| 回退一段 | 滚轮向上 |
| 暂停 / 返回 | Esc；阅读时也可右键 |
| 全屏 | F11 |

默认使用 1280×720 设计坐标，实际按窗口内容区的原生像素绘制，不再把固定 720p 画面整体放大。字体按实际像素密度生成、对齐像素；窗口等比留边，鼠标同步换算。Windows 启用 PerMonitorV2 DPI 感知，避免系统位图放大。示例主视觉与场景原图为 1672×941，透明立绘为 1024×1536；插画等比裁切，立绘独立叠加。菜单支持方向键、Tab 和 Enter。

## 接入自己的作品

准备自己的 `Game/game.json`、剧情、字体和资源，然后执行：

```powershell
dotnet run --project examples/MonoGalGame.Demo -- --game /path/to/MyGame/game.json
```

也可以新建桌面可执行项目，引用 `MonoGalGame.Framework`，创建宿主：

```csharp
using MonoGalGame.Framework.Hosting;

using var game = new GalgameGame(
    GalgameOptions.FromProject(Path.Combine(AppContext.BaseDirectory, "Game", "game.json")));
game.Run();
```

记得在启动项目中把 `Game/**/*` 复制到输出和发布目录。具体配置、剧情格式、扩展服务和字体说明见 [框架接入文档](docs/framework.md)。

玩家数据默认位于 `LocalApplicationData/MonoGalGame/<game-id>/`，与游戏安装目录分离。可通过 `--data-dir` 显式指定。恢复存档会从保存的当前对白开头重新显示；逐字动画进度、自动阅读模式和回退栈不写入存档。回退最多保留 64 步，历史最多保留 500 段。

## 构建与验证

```powershell
dotnet build MonoGalGame.slnx --configuration Release -m:1
dotnet test MonoGalGame.Tests/MonoGalGame.Tests.csproj --configuration Release --no-build

# 无窗口：检查示例剧情、中文字体和资源。
dotnet run --project examples/MonoGalGame.Demo --configuration Release --no-build -- --validate

# 有窗口：自动验证输入、选择、存读档、菜单及结局，并输出 8 张画面。
dotnet run --project examples/MonoGalGame.Demo --configuration Release --no-build -- --smoke-test artifacts/smoke

# 增加 1080p、4K、全屏、竖排菜单点击与方向键、河畔立绘、暂停及覆盖存档验证，共 19 张画面。
dotnet run --project examples/MonoGalGame.Demo --configuration Release --no-build -- --smoke-test artifacts/display-smoke --display-test

dotnet pack MonoGalGame.Core --configuration Release --no-build --output artifacts/packages
dotnet pack MonoGalGame.Framework --configuration Release --no-build --output artifacts/packages
```

自动画面验证使用独立的 `artifacts/smoke/player-data/`，不读写正常游戏存档；它针对本仓库的示例剧情。没有音频设备时，可仅在验证进程中设置 `ALSOFT_DRIVERS=null`。正常游玩不要设置这个环境变量，否则不会输出声音。

## 发布

示例默认启用 **Native AOT**；发布时把托管代码编译为目标平台的原生程序。Windows 需要 Visual Studio 的“使用 C++ 的桌面开发”工具，Linux 需要 `clang` 和 `zlib1g-dev`，macOS 需要 Xcode 命令行工具，详见 [微软发布说明](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)。例如在 Windows 上发布 x64：

```powershell
dotnet publish examples/MonoGalGame.Demo --configuration Release --runtime win-x64 --self-contained true -m:1 --output artifacts/publish/win-x64-aot

# 运行真实发布产物，检查 AOT 运行状态、剧情、字体和资源。
./scripts/Verify-NativeAot.ps1 -PublishDirectory artifacts/publish/win-x64-aot

# 在有桌面和图形驱动的机器上，验证窗口、4K、全屏、存读档和音频初始化。
$env:ALSOFT_DRIVERS = 'null' # 仅验证时使用 OpenAL 静默输出。
./scripts/Verify-NativeAot.ps1 -PublishDirectory artifacts/publish/win-x64-aot -DisplayTest -RequireAudio
Remove-Item Env:ALSOFT_DRIVERS
```

AOT 发布目录仍需完整分发，其中包含 SDL、OpenAL、字体和作品资源。开发时的 `dotnet run` 使用托管构建；`--require-aot` 会拒绝该构建，防止验证时运行错文件。若需普通自包含发布，可显式加 `-p:PublishAot=false` 并使用另一个输出目录。

其他桌面运行时可使用 `linux-x64`、`osx-arm64` 等，在对应操作系统上发布。三种系统的 AOT 构建、单元测试和无窗口验证配置见 `.github/workflows/desktop.yml`；本地实际验收以 Windows x64 为准，其他系统的图形和音频仍需运行验收。本地不保证可直接生成 macOS 应用包或签名产物。

框架代码采用 MIT，字体与依赖许可见 [第三方声明](THIRD-PARTY-NOTICES.md)。

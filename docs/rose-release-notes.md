《玫瑰与旧婚纱》桌面版，全部使用 .NET Native AOT 编译，玩家无需安装 .NET。

| 系统与芯片 | 下载文件结尾 | 启动方式 |
| --- | --- | --- |
| Windows 10/11，Intel / AMD 64 位 | `win-x64.zip` | 完整解压后双击 `RoseAndOldWeddingDress.exe` |
| macOS 14+，Apple 芯片 | `osx-arm64.zip` | 解压，将 `.app` 拖入“应用程序”后打开 |
| macOS 14+，Intel | `osx-x64.zip` | 解压，将 `.app` 拖入“应用程序”后打开 |
| Linux，Intel / AMD 64 位 | `linux-x64.tar.gz` | 完整解压后运行 `./RoseAndOldWeddingDress` |
| Linux，ARM64 / AArch64 | `linux-arm64.tar.gz` | 完整解压后运行 `./RoseAndOldWeddingDress` |

请下载对应系统与芯片的游戏包；GitHub 自动提供的 Source code 是源代码。

所有游戏包均包含完整剧情、插画、字体、配乐、SDL、OpenAL 及许可证。Windows 的 `.exe` 需要与随包文件一起保留。存档存放在个人用户目录。

Linux 面向 Ubuntu 22.04+ 和提供 glibc 2.35+ 的兼容桌面发行版，需要可用的 OpenGL 图形驱动、ICU、X11/Wayland 与音频系统库。Debian/Ubuntu 可执行 `sudo apt-get install libsdl2-2.0-0 libopenal1 libicu-dev` 补齐常用依赖。不提供 ARMv7、32 位 x86、RISC-V、LoongArch 或 Alpine/musl 构建。

macOS 应用使用临时签名，未经过 Apple Developer ID 签名和公证。首次启动可能需要在“系统设置 → 隐私与安全性”中允许打开。

每个包均在相应芯片的构建机器上，从最终压缩包重新解压后验证 Native AOT、全部 12 种选择组合、三个结局、中文字体与资源，并检查 SDL/OpenAL 能否加载。此验证不等同于每种设备上的完整画面与音频试听。

`SHA256SUMS.txt` 提供全部五个压缩包的 SHA-256 校验值。

# 《玫瑰与旧婚纱》自动发布

`.github/workflows/rose-release.yml` 在五台对应系统与芯片的 GitHub 托管机器上构建 Native AOT 游戏：Windows x64、macOS ARM64 / x64、Linux x64 / ARM64。macOS 打包成 `.app` 并使用临时签名；Windows 打包为包含 `.exe` 的完整 ZIP；Linux 使用保留可执行权限的 `.tar.gz`。不构建 ARMv7。

## 发布新版

1. 修改 `examples/MonoGalGame.Rose/MonoGalGame.Rose.csproj` 中的 `Version`，例如 `0.15.0`，并提交、推送全部代码和资源。
2. 创建并推送对应标签，例如 `git tag rose-v0.15.0`、`git push origin rose-v0.15.0`。也接受 `v0.15.0`。
3. 在 GitHub Actions 的 **Rose releases** 查看进度。全部五个平台成功后，工作流上传游戏包与 `SHA256SUMS.txt`，再发布 GitHub Release。

也可以打开 **Actions → Rose releases → Run workflow**，选择要发布的分支。`tag` 留空时使用 `rose-v` 加项目版本；指定标签必须与项目版本完全一致。不存在的标签在发布时创建，已有标签必须指向本次构建的同一个提交。已有 Release 可以在同一提交上重跑；工作流不会移动标签。

`prerelease` 选项或带 `-preview.1` 等后缀的项目版本会标记为预发布。工作流先创建草稿，上传全部文件后才公开；任何构建、测试、打包或验证失败都会阻止发布。构建任务只有读取权限，只有最终发布任务拥有 `contents: write`，使用仓库内置的 `GITHUB_TOKEN`，无需额外发布令牌。

## 打包与验收

可在目标系统上执行，例如 Windows：

```powershell
dotnet publish examples/MonoGalGame.Rose -c Release -r win-x64 --self-contained true -p:PublishAot=true -m:1 -o artifacts/publish/win-x64
python scripts/package_rose.py --publish artifacts/publish/win-x64 --rid win-x64 --version 0.14.0 --output artifacts/release
```

打包脚本检查原生文件的 CPU 架构、拒绝普通托管发布、检查全部作品资源及许可证、移除调试符号，并运行从最终压缩包解压出来的程序，传入 `--validate --require-aot`。验证从另一个工作目录启动，确保资源不依赖当前目录；也会加载 SDL 和 OpenAL 检查其架构及系统依赖。

macOS 的布局是 `Contents/MacOS` 放原生可执行文件、`Contents/Frameworks` 放 SDL/OpenAL、`Contents/Resources` 放游戏与许可证。入口程序会自动寻找 Resources 下的作品。归档和解压使用 `ditto` 保留可执行权限与签名，解压后再次验证签名。没有 Apple 证书时不会声称应用已经公证；玩家须按包内说明允许首次启动。

Linux 使用 Ubuntu 22.04 构建，支持 x64、ARM64 的 glibc 桌面系统；发行版、驱动和系统库的要求详见随 Release 发布的 [说明](rose-release-notes.md)。macOS 最低版本设置为 14.0。发布验证覆盖剧情、字体、资源和原生依赖加载；图形窗口、显卡兼容和真实音频仍需在实际设备上验收。

打包逻辑的检查：`python -m unittest discover -s scripts -p 'test_package_rose.py' -v`。原有 `Desktop framework` 工作流继续负责框架与《春日来信》Demo，不受这个发布流程影响。

剧本清单的 `sourceHashFormat: utf8-lf` 表示对统一为 LF 换行的 UTF-8 文本计算 SHA-256，防止 Windows、Linux 和 macOS 的 Git 换行转换产生不同的清单。剧情正文发生变化仍会被检测到。

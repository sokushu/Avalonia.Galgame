"""Package Rose's Native AOT output and run validation from the extracted archive."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import plistlib
import re
import shutil
import struct
import subprocess
import sys
import tarfile
import tempfile
import zipfile

APP = "RoseAndOldWeddingDress"
TITLE = "玫瑰与旧婚纱"
NATIVE_LIBRARIES = {
    "win": ("SDL2.dll", "openal.dll"),
    "osx": ("libSDL2-2.0.0.dylib", "libopenal.dylib"),
    "linux": ("libSDL2-2.0.so.0", "libopenal.so"),
}
RIDS = ("win-x64", "osx-arm64", "osx-x64", "linux-x64", "linux-arm64")


def binary_architectures(path: Path) -> set[str]:
    """Read PE, ELF or Mach-O headers, including universal macOS libraries."""
    with path.open("rb") as stream:
        header = stream.read(64)
        if header[:2] == b"MZ":
            stream.seek(struct.unpack_from("<I", header, 60)[0])
            pe = stream.read(6)
            if pe[:4] != b"PE\0\0":
                raise ValueError(f"Invalid PE executable: {path}")
            machine = struct.unpack_from("<H", pe, 4)[0]
            return {0x8664: {"x64"}, 0xAA64: {"arm64"}}.get(machine, set())
        if header[:4] == b"\x7fELF":
            endian = "<" if header[5] == 1 else ">"
            machine = struct.unpack_from(endian + "H", header, 18)[0]
            return {62: {"x64"}, 183: {"arm64"}}.get(machine, set())
        magic = header[:4]
        if magic in (b"\xcf\xfa\xed\xfe", b"\xfe\xed\xfa\xcf"):
            endian = "<" if magic[0] == 0xCF else ">"
            machine = struct.unpack_from(endian + "I", header, 4)[0]
            return {0x01000007: {"x64"}, 0x0100000C: {"arm64"}}.get(machine, set())
        if magic in (b"\xca\xfe\xba\xbe", b"\xbe\xba\xfe\xca", b"\xca\xfe\xba\xbf", b"\xbf\xba\xfe\xca"):
            endian = ">" if magic[0] == 0xCA else "<"
            count = struct.unpack_from(endian + "I", header, 4)[0]
            entry_size = 32 if magic in (b"\xca\xfe\xba\xbf", b"\xbf\xba\xfe\xca") else 20
            arches = set()
            for index in range(count):
                stream.seek(8 + index * entry_size)
                machine = struct.unpack(endian + "I", stream.read(4))[0]
                arches.update({0x01000007: {"x64"}, 0x0100000C: {"arm64"}}.get(machine, set()))
            return arches
    raise ValueError(f"Not a native executable or library: {path}")


def check_payload(root: Path, rid: str) -> None:
    platform, architecture = rid.split("-")
    executable = APP + (".exe" if platform == "win" else "")
    for name in (executable, *NATIVE_LIBRARIES[platform]):
        if architecture not in binary_architectures(root / name):
            raise ValueError(f"{name} does not contain {architecture} machine code")
    for name in (f"{APP}.dll", f"{APP}.runtimeconfig.json"):
        if (root / name).exists():
            raise ValueError(f"Managed build found: {name}; publish Native AOT to a clean directory")
    game = (root / "Game").resolve()
    project = json.loads((game / "game.json").read_text(encoding="utf-8"))
    resources = [project["story"], project["font"], "Fonts/OFL.txt"]
    resources.extend(project.get("textures", {}).values())
    resources.extend(project.get("audio", {}).values())
    for resource in resources:
        path = (game / resource).resolve()
        if not path.is_relative_to(game) or not path.is_file():
            raise ValueError(f"Missing or unsafe game resource: {resource}")
    if not (root / "THIRD-PARTY-NOTICES.md").is_file() or not any((root / "Licenses").glob("*")):
        raise ValueError("Third-party notices and licenses must accompany the game")


def stage_package(publish: Path, folder: Path, rid: str, version: str) -> None:
    check_payload(publish, rid)
    folder.mkdir()
    if rid.startswith("osx-"):
        contents = folder / f"{APP}.app" / "Contents"
        for name in ("MacOS", "Frameworks", "Resources"):
            (contents / name).mkdir(parents=True)
        for source in publish.iterdir():
            if source.suffix in (".pdb", ".dbg", ".dSYM"):
                continue
            location = "MacOS" if source.name == APP else "Frameworks" if source.suffix == ".dylib" else "Resources"
            destination = contents / location / source.name
            if source.is_dir():
                shutil.copytree(source, destination)
            else:
                shutil.copy2(source, destination)
        (contents / "MacOS" / APP).chmod(0o755)
        metadata = {
            "CFBundleExecutable": APP,
            "CFBundleIdentifier": "io.github.sokushu.rose-and-old-wedding-dress",
            "CFBundleName": TITLE,
            "CFBundleDisplayName": TITLE,
            "CFBundleInfoDictionaryVersion": "6.0",
            "CFBundlePackageType": "APPL",
            "CFBundleShortVersionString": version.split("-")[0],
            "CFBundleVersion": version.split("-")[0],
            "CFBundleDevelopmentRegion": "zh_CN",
            "LSApplicationCategoryType": "public.app-category.games",
            "LSMinimumSystemVersion": "14.0",
            "NSHighResolutionCapable": True,
            "NSPrincipalClass": "NSApplication",
        }
        with (contents / "Info.plist").open("wb") as output:
            plistlib.dump(metadata, output)
    else:
        shutil.copytree(publish, folder, dirs_exist_ok=True,
                        ignore=shutil.ignore_patterns("*.pdb", "*.dbg", "*.dSYM"))
        if rid.startswith("linux-"):
            (folder / APP).chmod(0o755)
    instructions = f"{TITLE}\n\n"
    if rid.startswith("osx-"):
        instructions += f"将 {APP}.app 拖到“应用程序”后双击运行。需要 macOS 14 或更新版本。\n此包使用临时签名，未经过 Apple 公证；首次打开可能需要在“系统设置 → 隐私与安全性”中允许打开。\n"
    elif rid.startswith("win-"):
        instructions += f"完整解压后双击 {APP}.exe。请保留 Game、SDL2.dll、openal.dll 与许可证。\n"
    else:
        instructions += f"完整解压后运行 ./{APP}。请保留全部随包文件。\n支持 Ubuntu 22.04 或更新版本，以及提供 glibc 2.35+ 的兼容桌面发行版。\n需要 OpenGL 图形驱动、ICU、X11/Wayland 与音频系统库；无需安装 .NET。\nDebian/Ubuntu 可安装：sudo apt-get install libsdl2-2.0-0 libopenal1 libicu-dev\n"
    instructions += "\n不需要安装 .NET。存档保存在个人用户目录，不写入游戏安装目录。\n"
    (folder / "READ-ME.txt").write_text(instructions, encoding="utf-8")


def sign_macos(folder: Path) -> None:
    bundle = folder / f"{APP}.app"
    for library in sorted((bundle / "Contents" / "Frameworks").glob("*.dylib")):
        subprocess.run(["codesign", "--force", "--sign", "-", str(library)], check=True)
    subprocess.run(["codesign", "--force", "--sign", "-", str(bundle / "Contents" / "MacOS" / APP)], check=True)
    subprocess.run(["codesign", "--force", "--sign", "-", str(bundle)], check=True)
    subprocess.run(["codesign", "--verify", "--deep", "--strict", str(bundle)], check=True)


def archive_package(folder: Path, archive: Path, rid: str) -> None:
    if rid.startswith("osx-"):
        # ditto preserves bundle permissions and signing metadata for Finder extraction.
        subprocess.run(["ditto", "-c", "-k", "--keepParent", str(folder), str(archive)], check=True)
    elif rid.startswith("linux-"):
        def executable_mode(info):
            if info.isfile() and Path(info.name).name == APP:
                info.mode |= 0o111
            return info
        with tarfile.open(archive, "w:gz") as output:
            output.add(folder, arcname=folder.name, filter=executable_mode)
    else:
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as output:
            for path in sorted(folder.rglob("*")):
                if path.is_file():
                    output.write(path, path.relative_to(folder.parent))


def verify_archive(archive: Path, folder_name: str, rid: str, destination: Path) -> None:
    if rid.startswith("osx-"):
        subprocess.run(["ditto", "-x", "-k", str(archive), str(destination)], check=True)
        contents = destination / folder_name / f"{APP}.app" / "Contents"
        executable = contents / "MacOS" / APP
        libraries = contents / "Frameworks"
        subprocess.run(["codesign", "--verify", "--deep", "--strict", str(contents.parent)], check=True)
    else:
        shutil.unpack_archive(archive, destination)
        root = destination / folder_name
        check_payload(root, rid)
        executable = root / (APP + (".exe" if rid.startswith("win-") else ""))
        libraries = root
    # Opening both libraries detects wrong architecture and missing system dependencies.
    native_paths = [str(libraries / name) for name in NATIVE_LIBRARIES[rid.split("-")[0]]]
    # A subprocess releases DLL handles before the temporary directory is removed on Windows.
    subprocess.run([sys.executable, "-c", "import ctypes, sys; handles = [ctypes.CDLL(p) for p in sys.argv[1:]]",
                    *native_paths], cwd=destination, check=True, timeout=60)
    # Use a different working directory to catch paths that accidentally depend on cwd.
    subprocess.run([str(executable), "--validate", "--require-aot"], cwd=destination, check=True, timeout=180)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--publish", type=Path, required=True)
    parser.add_argument("--rid", choices=RIDS, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", args.version):
        parser.error("version must have the form 1.2.3 or 1.2.3-preview.1")
    expected_host = "win32" if args.rid.startswith("win-") else "darwin" if args.rid.startswith("osx-") else "linux"
    if sys.platform != expected_host:
        parser.error(f"{args.rid} must be packaged and validated on its target operating system")
    args.output = args.output.resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    name = f"{APP}-v{args.version}-{args.rid}"
    extension = ".tar.gz" if args.rid.startswith("linux-") else ".zip"
    archive = args.output / (name + extension)
    if archive.exists():
        raise FileExistsError(f"Use a fresh output directory: {archive}")
    with tempfile.TemporaryDirectory(prefix="rose-package-", dir=args.output) as temporary:
        work = Path(temporary).resolve()
        assert work.parent == args.output
        folder = work / name
        stage_package(args.publish.resolve(), folder, args.rid, args.version)
        if args.rid.startswith("osx-"):
            sign_macos(folder)
        archive_package(folder, archive, args.rid)
        extracted = work / "extracted"
        extracted.mkdir()
        verify_archive(archive, name, args.rid, extracted)
    print(f"Verified release package: {archive}")


if __name__ == "__main__":
    main()

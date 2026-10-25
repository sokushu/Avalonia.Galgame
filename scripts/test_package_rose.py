"""Regression checks for missing release files, architecture mixups and .app layout."""
import json
from pathlib import Path
import plistlib
import stat
import struct
import tarfile
import tempfile
import unittest

from package_rose import APP, NATIVE_LIBRARIES, archive_package, binary_architectures, check_payload, stage_package


def native_header(platform, architecture):
    header = bytearray(96)
    if platform == "win":
        header[:2] = b"MZ"
        struct.pack_into("<I", header, 60, 64)
        header[64:68] = b"PE\0\0"
        struct.pack_into("<H", header, 68, 0x8664 if architecture == "x64" else 0xAA64)
    elif platform == "linux":
        header[:6] = b"\x7fELF\x02\x01"
        struct.pack_into("<H", header, 18, 62 if architecture == "x64" else 183)
    else:
        header[:4] = b"\xcf\xfa\xed\xfe"
        struct.pack_into("<I", header, 4, 0x01000007 if architecture == "x64" else 0x0100000C)
    return header


class ReleasePackagingTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="rose-packaging-test-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.publish = self.root / "publish"
        self.publish.mkdir()

    def payload(self, rid):
        platform, architecture = rid.split("-")
        executable = APP + (".exe" if platform == "win" else "")
        for name in (executable, *NATIVE_LIBRARIES[platform]):
            (self.publish / name).write_bytes(native_header(platform, architecture))
        game = self.publish / "Game"
        for name in ("Stories/main.json", "Fonts/font.otf", "Fonts/OFL.txt", "Art/portrait.png", "Audio/music.wav"):
            path = game / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"fixture")
        project = {"story": "Stories/main.json", "font": "Fonts/font.otf",
                   "textures": {"portrait": "Art/portrait.png"}, "audio": {"music": "Audio/music.wav"}}
        (game / "game.json").write_text(json.dumps(project), encoding="utf-8")
        (self.publish / "THIRD-PARTY-NOTICES.md").write_text("notices")
        (self.publish / "Licenses").mkdir()
        (self.publish / "Licenses" / "SDL.txt").write_text("license")
        (self.publish / f"{APP}.pdb").write_bytes(b"debug symbols")

    def test_rejects_missing_font_art_and_audio(self):
        self.payload("win-x64")
        for resource in ("Fonts/font.otf", "Art/portrait.png", "Audio/music.wav"):
            with self.subTest(resource=resource):
                path = self.publish / "Game" / resource
                original = path.read_bytes()
                path.unlink()
                with self.assertRaisesRegex(ValueError, "Missing or unsafe"):
                    check_payload(self.publish, "win-x64")
                path.write_bytes(original)

    def test_rejects_library_for_another_chip(self):
        self.payload("linux-arm64")
        (self.publish / "libopenal.so").write_bytes(native_header("linux", "x64"))
        with self.assertRaisesRegex(ValueError, "arm64 machine code"):
            check_payload(self.publish, "linux-arm64")

    def test_rejects_managed_publish(self):
        self.payload("win-x64")
        (self.publish / f"{APP}.runtimeconfig.json").write_text("{}")
        with self.assertRaisesRegex(ValueError, "Managed build"):
            check_payload(self.publish, "win-x64")

    def test_rejects_resource_outside_game_folder(self):
        self.payload("win-x64")
        project = self.publish / "Game" / "game.json"
        data = json.loads(project.read_text())
        data["font"] = "../THIRD-PARTY-NOTICES.md"
        project.write_text(json.dumps(data))
        with self.assertRaisesRegex(ValueError, "unsafe"):
            check_payload(self.publish, "win-x64")

    def test_macos_bundle_separates_native_code_and_resources(self):
        self.payload("osx-arm64")
        folder = self.root / "package"
        stage_package(self.publish, folder, "osx-arm64", "0.14.0-preview.1")
        contents = folder / f"{APP}.app" / "Contents"
        with (contents / "Info.plist").open("rb") as stream:
            metadata = plistlib.load(stream)
        self.assertEqual(metadata["CFBundleExecutable"], APP)
        self.assertEqual(metadata["CFBundleShortVersionString"], "0.14.0")
        self.assertTrue((contents / "MacOS" / APP).is_file())
        self.assertTrue((contents / "Resources" / "Game" / "game.json").is_file())
        self.assertTrue((contents / "Resources" / "Licenses" / "SDL.txt").is_file())
        for name in NATIVE_LIBRARIES["osx"]:
            self.assertTrue((contents / "Frameworks" / name).is_file())
        self.assertFalse(list(folder.rglob("*.pdb")))

    def test_linux_archive_preserves_executable_bit_and_assets(self):
        self.payload("linux-x64")
        folder = self.root / "package"
        stage_package(self.publish, folder, "linux-x64", "0.14.0")
        archive = self.root / "package.tar.gz"
        archive_package(folder, archive, "linux-x64")
        with tarfile.open(archive) as package:
            self.assertTrue(package.getmember(f"package/{APP}").mode & stat.S_IXUSR)
            self.assertIsNotNone(package.getmember("package/Game/Art/portrait.png"))
            self.assertFalse(any(member.name.endswith(".pdb") for member in package.getmembers()))

    def test_accepts_universal_macos_native_libraries(self):
        universal = bytearray(48)
        universal[:4] = b"\xca\xfe\xba\xbe"
        struct.pack_into(">I", universal, 4, 2)
        struct.pack_into(">I", universal, 8, 0x01000007)
        struct.pack_into(">I", universal, 28, 0x0100000C)
        path = self.root / "universal.dylib"
        path.write_bytes(universal)
        self.assertEqual(binary_architectures(path), {"x64", "arm64"})


if __name__ == "__main__":
    unittest.main()

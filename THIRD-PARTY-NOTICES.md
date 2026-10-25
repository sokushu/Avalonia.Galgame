# Third-party notices

- **MonoGame 3.8.6**: Microsoft Public License (MS-PL). https://github.com/MonoGame/MonoGame . Its DesktopGL package brings SDL, OpenAL Soft and NVorbis. Their license texts are included in `licenses/` in this repository and `Licenses/` in the published demo.
- **SDL 2.32.10**: zlib license. https://github.com/libsdl-org/SDL . License copied from MonoGame.Library.SDL 2.32.10.2.
- **OpenAL Soft 1.24.3**: GNU Library General Public License 2.0 or later, with BSD-licensed portions. https://github.com/kcat/openal-soft/tree/1.24.3 . Native binary source/build package: https://github.com/MonoGame/MonoGame.Library.OpenAL/tree/4d08985956a3278adad0bd51486fc1b217b829d2 . Distributed as a separate dynamically loaded library.
- **NVorbis 0.10.4**: MIT license. https://github.com/NVorbis/NVorbis . License copied from the NuGet package.
- **StbTrueTypeSharp 1.26.13**: MIT or Unlicense, used for glyph rasterization. https://github.com/StbSharp/StbTrueTypeSharp . The framework implements its own glyph atlas, layout, pagination and reveal logic.
- **Noto Sans SC Regular**: SIL Open Font License 1.1. Source: https://github.com/notofonts/noto-cjk/blob/main/Sans/SubsetOTF/SC/NotoSansSC-Regular.otf . The unmodified font and complete license are included in the demo's `Game/Fonts/` directory. The demo font is not embedded in framework packages.

The bundled Spring Letters and Roses and the Worn Wedding Dress PNG illustrations were generated with the built-in image_gen tool. Procedural example artwork remains in `DemoTextureSource.cs` as a fallback. Music is synthesized; no downloaded recording or character voice is included. Both games include the unmodified Noto Sans SC font and its complete OFL license in `Game/Fonts/`.

using System.Buffers.Binary;
using System.Text;
using MonoGalGame.Core.Stories;
using MonoGalGame.Framework.Fonts;
using MonoGalGame.Framework.Hosting;

namespace MonoGalGame.Rose;

internal static class RoseValidation
{
    public static void Validate(GalgameOptions options)
    {
        using var font = new TrueTypeFontFace(options.Project.ResolvePath(options.Project.Font));
        foreach (var rune in "玫瑰与旧婚纱叶照唐见月".EnumerateRunes())
            if (!font.Contains(rune) || font.Rasterize(rune, 32).Alpha.All(a => a == 0))
                throw new InvalidDataException($"Missing Chinese glyph: {rune}");
        Span<byte> header = stackalloc byte[26];
        foreach (var (id, path) in options.Project.Textures)
        {
            using var file = File.OpenRead(options.Project.ResolvePath(path));
            file.ReadExactly(header);
            if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
                BinaryPrimitives.ReadInt32BigEndian(header[16..20]) < 512 ||
                BinaryPrimitives.ReadInt32BigEndian(header[20..24]) < 512)
                throw new InvalidDataException($"Invalid or undersized artwork: {id}");
            if (options.Theme.PortraitCharacters.ContainsKey(id) && header[25] != 6)
                throw new InvalidDataException($"Character sprite must have alpha: {id}");
        }
        foreach (var path in options.Project.Audio.Values)
            if (!File.Exists(options.Project.ResolvePath(path))) throw new FileNotFoundException(path);
        foreach (var item in options.Theme.Artwork)
            if (!options.Project.Textures.ContainsKey(item.Texture)) throw new InvalidDataException($"Unknown gallery artwork: {item.Texture}");
        foreach (var texture in options.Theme.PortraitCharacters.Keys)
            if (!options.Project.Textures.ContainsKey(texture)) throw new InvalidDataException($"Unknown character texture: {texture}");
        foreach (var scene in options.Story.Nodes.Values.Select(n => n.Scene).OfType<SceneChange>())
        {
            foreach (var id in new[] { scene.Background, scene.Portrait, scene.SecondaryPortrait, scene.TertiaryPortrait })
                if (!string.IsNullOrEmpty(id) && !options.Project.Textures.ContainsKey(id))
                    throw new InvalidDataException($"Unknown story artwork: {id}");
            var portraits = new[] { scene.Portrait, scene.SecondaryPortrait, scene.TertiaryPortrait }.Where(id => !string.IsNullOrEmpty(id)).ToArray();
            foreach (var id in portraits)
                if (!string.IsNullOrEmpty(id) && !options.Theme.PortraitCharacters.ContainsKey(id))
                    throw new InvalidDataException($"Unassigned portrait identity: {id}");
            if (portraits.Select(id => options.Theme.PortraitCharacters[id!]).Distinct().Count() != portraits.Length)
                throw new InvalidDataException("The same character occupies multiple portrait positions.");
            if (!string.IsNullOrEmpty(scene.Music) && !options.Project.Audio.ContainsKey(scene.Music))
                throw new InvalidDataException($"Unknown story music: {scene.Music}");
        }
        for (var first = 0; first < 2; first++)
            for (var second = 0; second < 2; second++)
                for (var route = 0; route < 3; route++)
                {
                    var player = new StoryPlayer(options.Story);
                    var choices = new[] { first, second, route };
                    var seenChoices = 0;
                    for (var step = 0; !player.IsEnded && step < 3000; step++)
                    {
                        if (player.CurrentNode.Kind == NodeKind.Choice)
                        {
                            if (seenChoices >= 3 || player.CurrentNodeId != $"q{seenChoices + 1}")
                                throw new InvalidDataException("Unexpected choice sequence.");
                            player.Choose(choices[seenChoices++]);
                        }
                        else player.Advance();
                    }
                    if (!player.IsEnded || seenChoices != 3 || player.CurrentNodeId != $"ending-{(char)('a' + route)}")
                        throw new InvalidDataException("A Rose route did not reach its independent ending.");
                }
        Console.WriteLine($"Validated {options.Project.Title}: {options.Story.Nodes.Count} nodes, all 12 choice combinations, 3 endings, Chinese font, {options.Project.Textures.Count} artworks and {options.Project.Audio.Count} music tracks.");
    }
}

using Microsoft.Xna.Framework.Graphics;
using MonoGalGame.Core.Projects;

namespace MonoGalGame.Framework.Assets;

public interface ITextureSource : IDisposable
{
    Texture2D? Get(string id);
}

public sealed class FileTextureSource(GraphicsDevice device, GameProject project) : ITextureSource
{
    private readonly Dictionary<string, Texture2D> _cache = [];

    public Texture2D? Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_cache.TryGetValue(id, out var cached)) return cached;
        if (!project.Textures.TryGetValue(id, out var path)) return null;
        using var stream = File.OpenRead(project.ResolvePath(path));
        var texture = Texture2D.FromStream(device, stream);
        // FromStream returns straight alpha; premultiply once for consistent sprite blending.
        var data = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
        texture.GetData(data);
        for (var i = 0; i < data.Length; i++)
        {
            var c = data[i];
            data[i] = Microsoft.Xna.Framework.Color.FromNonPremultiplied(c.R, c.G, c.B, c.A);
        }
        texture.SetData(data);
        _cache.Add(id, texture);
        return texture;
    }

    public void Dispose()
    {
        foreach (var texture in _cache.Values) texture.Dispose();
        _cache.Clear();
    }
}

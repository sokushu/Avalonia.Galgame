using Microsoft.Xna.Framework;

namespace MonoGalGame.Framework.Rendering;

public readonly record struct SceneLighting(Color PortraitTint, Color Overlay, float Rain)
{
    public static SceneLighting For(string tone, Color paper) => tone switch
    {
        "night-painted" => new(new(220, 223, 232), Color.Transparent, 0),
        "rain-night-painted" => new(new(220, 223, 232), Color.Transparent, 1),
        "cool-painted" => new(new(217, 228, 240), Color.Transparent, 0),
        "night" => new(new(202, 211, 229), new Color(27, 39, 64) * .25f, 0),
        "rain-night" => new(new(202, 211, 229), new Color(27, 39, 64) * .25f, 1),
        "dim" => new(new(139, 150, 172), new Color(27, 39, 64) * .44f, 0),
        "cool" => new(new(217, 228, 240), new Color(27, 39, 64) * .1f, 0),
        "memory" => new(new(205, 198, 184), paper * .22f, 0),
        "rain" => new(Color.White, Color.Transparent, 1),
        _ => new(Color.White, Color.Transparent, 0)
    };

    public static SceneLighting Lerp(SceneLighting from, SceneLighting to, float amount) => new(
        Color.Lerp(from.PortraitTint, to.PortraitTint, amount),
        Color.Lerp(from.Overlay, to.Overlay, amount),
        from.Rain + (to.Rain - from.Rain) * Math.Clamp(amount, 0, 1));
}

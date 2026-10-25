using Microsoft.Xna.Framework;

namespace MonoGalGame.Framework.Hosting;

/// <summary>Artwork and presentation copy supplied by the work, independently of its story graph.</summary>
public sealed record GalgameTheme
{
    public Color Ink { get; init; } = new(49, 65, 72);
    public Color Muted { get; init; } = new(123, 133, 137);
    public Color Accent { get; init; } = new(171, 94, 116);
    public Color Paper { get; init; } = new(255, 252, 247);
    public Color Border { get; init; } = new(222, 210, 205);
    public string[] TitleLines { get; init; } = [];
    public string BrandCaption { get; init; } = "A LETTER FOR YOU";
    public string OpeningCaption { get; init; } = "序章  /  在春天，与你相遇";
    public string Dedication { get; init; } = "致  /  那个还没说出口的春天";
    public string Footer { get; init; } = "每个选择，都会成为故事的一部分。";
    public string FooterCaption { get; init; } = "CLICK TO BEGIN YOUR STORY";
    public string EndingCaption { get; init; } = "E N D I N G  /  春 日 终 章";
    public string EndingThanks { get; init; } = "感谢你，走进这个春天。";
    public string PauseDescription { get; init; } = "不用着急，我们还有整个春天。";
    public string TitleMusic { get; init; } = "";
    public int PortraitHeight { get; init; } = 710;
    public bool AmbientDust { get; init; }
    public float EndingPauseSeconds { get; init; }
    public IReadOnlyDictionary<string, string> PortraitCharacters { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, float> CharacterScales { get; init; } = new Dictionary<string, float>();
    public IReadOnlyList<GalgameArtwork> Artwork { get; init; } = [];
}

public sealed record GalgameArtwork(string Texture, string Title, string Caption,
    bool IsPortrait = false, string Description = "", bool RequiresReading = false);

using System.Globalization;

namespace MonoGalGame.Core.Text;

public interface ITextMetrics
{
    float GetAdvance(string textElement, float fontSize);
}

public sealed record PositionedTextElement(string Text, float X, float Y, int Index);
public sealed record TextPage(IReadOnlyList<PositionedTextElement> Elements, int LineCount)
{
    public int ElementCount => Elements.Count;
}

/// <summary>Horizontal CJK/Latin layout, fixed before typewriter reveal. Not a full shaping engine.</summary>
public static class TextLayout
{
    private const string NoLineStart = "，。！？、；：）》」』】〕〉…,.!?;:%)]}";
    private const string NoLineEnd = "（《「『【〔〈([{“‘";

    public static IReadOnlyList<TextPage> Build(string text, ITextMetrics metrics, float fontSize,
        float maxWidth, float lineHeight, int linesPerPage)
    {
        if (!float.IsFinite(fontSize) || fontSize <= 0 || !float.IsFinite(maxWidth) || maxWidth <= 0 ||
            !float.IsFinite(lineHeight) || lineHeight <= 0 || linesPerPage < 1)
            throw new ArgumentOutOfRangeException(nameof(maxWidth), "Text layout dimensions must be positive and finite.");
        var lines = new List<List<string>>();
        foreach (var paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var elements = Elements(paragraph);
            if (elements.Count == 0) { lines.Add([]); continue; }
            var start = 0;
            while (start < elements.Count)
            {
                var end = start;
                var width = 0f;
                while (end < elements.Count)
                {
                    var next = metrics.GetAdvance(elements[end], fontSize);
                    if (end > start && width + next > maxWidth) break;
                    width += next;
                    end++;
                }
                if (end < elements.Count)
                {
                    var boundary = end;
                    if (IsWord(elements[end]) && IsWord(elements[end - 1]))
                    {
                        var wordStart = end - 1;
                        while (wordStart > start && IsWord(elements[wordStart - 1])) wordStart--;
                        if (wordStart > start) boundary = wordStart;
                    }
                    while (boundary > start + 1 &&
                           (NoLineStart.Contains(elements[boundary], StringComparison.Ordinal) ||
                            NoLineEnd.Contains(elements[boundary - 1], StringComparison.Ordinal))) boundary--;
                    end = boundary;
                }
                lines.Add(elements.GetRange(start, end - start));
                start = end;
                // Discard only wrap-induced leading spaces; preserve explicit indentation.
                while (start < elements.Count && elements[start] == " ") start++;
            }
        }
        var pages = new List<TextPage>();
        for (var offset = 0; offset < lines.Count; offset += linesPerPage)
        {
            var glyphs = new List<PositionedTextElement>();
            var count = Math.Min(linesPerPage, lines.Count - offset);
            for (var line = 0; line < count; line++)
            {
                var x = 0f;
                foreach (var element in lines[offset + line])
                {
                    glyphs.Add(new(element, x, line * lineHeight, glyphs.Count));
                    x += metrics.GetAdvance(element, fontSize);
                }
            }
            pages.Add(new(glyphs.AsReadOnly(), count));
        }
        return pages;
    }

    public static List<string> Elements(string text)
    {
        var result = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext()) result.Add(enumerator.GetTextElement());
        return result;
    }

    private static bool IsWord(string element) => element.Length == 1 &&
        (char.IsAsciiLetterOrDigit(element[0]) || element[0] is '\'' or '_' or '-');
}

public sealed class Typewriter
{
    private double _progress;
    public int ElementCount { get; private set; }
    public int VisibleCount => Math.Min(ElementCount, (int)_progress);
    public bool IsComplete => VisibleCount == ElementCount;

    public void Reset(int elementCount)
    {
        if (elementCount < 0) throw new ArgumentOutOfRangeException(nameof(elementCount));
        ElementCount = elementCount;
        _progress = 0;
    }

    public void Update(TimeSpan elapsed, float elementsPerSecond)
    {
        if (elapsed < TimeSpan.Zero || !float.IsFinite(elementsPerSecond) || elementsPerSecond < 0)
            throw new ArgumentOutOfRangeException(nameof(elementsPerSecond));
        _progress = Math.Min(ElementCount, _progress + elapsed.TotalSeconds * elementsPerSecond);
    }

    public void RevealAll() => _progress = ElementCount;
}

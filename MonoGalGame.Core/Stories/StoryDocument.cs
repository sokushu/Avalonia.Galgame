using System.Text.Json;
using System.Text.Json.Serialization;
using MonoGalGame.Core.Serialization;

namespace MonoGalGame.Core.Stories;

public enum NodeKind { Dialogue, Choice, Set, Branch, Jump, End }
public enum Comparison { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
public enum VariableOperation { Set, Add }

public sealed record VariableEffect(string Variable, int Value, VariableOperation Operation = VariableOperation.Set);

public sealed record StoryCondition(string Variable, int Value, Comparison Comparison = Comparison.Equal)
{
    public bool Evaluate(IReadOnlyDictionary<string, int> variables)
    {
        var current = variables.GetValueOrDefault(Variable);
        return Comparison switch
        {
            Comparison.Equal => current == Value,
            Comparison.NotEqual => current != Value,
            Comparison.Greater => current > Value,
            Comparison.GreaterOrEqual => current >= Value,
            Comparison.Less => current < Value,
            Comparison.LessOrEqual => current <= Value,
            _ => throw new InvalidDataException("Unknown comparison.")
        };
    }
}

public sealed record StoryChoice
{
    public string Text { get; set; } = "";
    public string Next { get; set; } = "";
    public StoryCondition? Condition { get; set; }
    public List<VariableEffect> Effects { get; set; } = [];
}

/// <summary>Null scene fields inherit the previous value; empty strings clear that layer.</summary>
public sealed record SceneChange(string? Background = null, string? Portrait = null, string? Music = null);
public sealed record SceneState(string Background = "", string Portrait = "", string Music = "");

public sealed record StoryNode
{
    // Settable JSON DTO members preserve their initializers when fields are omitted.
    // The .NET 10 JSON source generator overwrites missing init-only members with defaults.
    public NodeKind Kind { get; set; } = NodeKind.Dialogue;
    public string Speaker { get; set; } = "";
    public string Text { get; set; } = "";
    public string? Next { get; set; }
    public string? Then { get; set; }
    public string? Else { get; set; }
    public string? Voice { get; set; }
    public SceneChange? Scene { get; set; }
    public StoryCondition? Condition { get; set; }
    public List<VariableEffect> Effects { get; set; } = [];
    public List<StoryChoice> Choices { get; set; } = [];
}

public sealed class StoryDocument
{
    public string Id { get; set; } = "story";
    public string Title { get; set; } = "";
    public string Start { get; set; } = "";
    public Dictionary<string, StoryNode> Nodes { get; set; } = new(StringComparer.Ordinal);

    public static StoryDocument Load(string path) => Parse(File.ReadAllText(path));

    public static StoryDocument Parse(string json)
    {
        var story = JsonSerializer.Deserialize(json, StoryJson.Context.StoryDocument)
            ?? throw new InvalidDataException("Story document is empty.");
        story.Validate();
        return story;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || Nodes is null || Nodes.Count == 0)
            throw new InvalidDataException("A story needs an id and at least one node.");
        RequireTarget(Start, "start");
        foreach (var (id, node) in Nodes)
        {
            if (string.IsNullOrWhiteSpace(id) || node is null || !Enum.IsDefined(node.Kind) ||
                node.Effects is null || node.Choices is null || node.Text is null || node.Speaker is null)
                throw new InvalidDataException($"Invalid story node '{id}'.");
            ValidateEffects(node.Effects);
            ValidateCondition(node.Condition);
            switch (node.Kind)
            {
                case NodeKind.Dialogue:
                case NodeKind.Set:
                case NodeKind.Jump:
                    RequireTarget(node.Next, id);
                    break;
                case NodeKind.Branch:
                    if (node.Condition is null) throw new InvalidDataException($"Branch '{id}' needs a condition.");
                    RequireTarget(node.Then, id);
                    RequireTarget(node.Else, id);
                    break;
                case NodeKind.Choice:
                    if (node.Choices.Count == 0) throw new InvalidDataException($"Choice '{id}' is empty.");
                    foreach (var choice in node.Choices)
                    {
                        if (choice is null || string.IsNullOrWhiteSpace(choice.Text) || choice.Effects is null)
                            throw new InvalidDataException($"Invalid option in '{id}'.");
                        RequireTarget(choice.Next, id);
                        ValidateCondition(choice.Condition);
                        ValidateEffects(choice.Effects);
                    }
                    break;
            }
        }
    }

    private void RequireTarget(string? target, string from)
    {
        if (string.IsNullOrWhiteSpace(target) || !Nodes.ContainsKey(target))
            throw new InvalidDataException($"Node '{from}' refers to missing target '{target}'.");
    }

    private static void ValidateCondition(StoryCondition? condition)
    {
        if (condition is not null && (string.IsNullOrWhiteSpace(condition.Variable) || !Enum.IsDefined(condition.Comparison)))
            throw new InvalidDataException("Invalid variable condition.");
    }

    private static void ValidateEffects(IEnumerable<VariableEffect> effects)
    {
        if (effects.Any(effect => effect is null || string.IsNullOrWhiteSpace(effect.Variable) || !Enum.IsDefined(effect.Operation)))
            throw new InvalidDataException("Invalid variable effect.");
    }
}

public static class StoryJson
{
    internal static CoreJsonContext Context { get; } = new(CreateOptions());

    /// <summary>Story formatting and generated metadata for the Core document and save types.</summary>
    public static JsonSerializerOptions Options => Context.Options;

    private static JsonSerializerOptions CreateOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter<NodeKind>(allowIntegerValues: false),
            new JsonStringEnumConverter<Comparison>(allowIntegerValues: false),
            new JsonStringEnumConverter<VariableOperation>(allowIntegerValues: false)
        }
    };
}

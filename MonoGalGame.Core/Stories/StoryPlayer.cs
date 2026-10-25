using System.Collections.ObjectModel;

namespace MonoGalGame.Core.Stories;

public sealed record HistoryEntry(string NodeId, string Speaker, string Text);

public sealed record StorySnapshot
{
    public int Version { get; set; } = 1;
    public string StoryId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public Dictionary<string, int> Variables { get; set; } = [];
    public SceneState Scene { get; set; } = new();
    public List<HistoryEntry> History { get; set; } = [];
    public HashSet<string> ReadNodes { get; set; } = [];
}

/// <summary>Deterministic story state, independent of rendering, time, input and audio.</summary>
public sealed class StoryPlayer
{
    private const int MaxAutomaticSteps = 1024;
    private const int MaxHistory = 500;
    private const int MaxRollback = 64;
    private readonly Dictionary<string, int> _variables = new(StringComparer.Ordinal);
    private readonly List<HistoryEntry> _history = [];
    private readonly List<StorySnapshot> _rollback = [];
    private readonly HashSet<string> _readNodes = new(StringComparer.Ordinal);

    public StoryDocument Story { get; }
    public string CurrentNodeId { get; private set; } = "";
    public StoryNode CurrentNode => Story.Nodes[CurrentNodeId];
    public IReadOnlyDictionary<string, int> Variables { get; }
    public IReadOnlyList<HistoryEntry> History { get; }
    public SceneState Scene { get; private set; } = new();
    public long Revision { get; private set; }
    public bool IsEnded => CurrentNode.Kind == NodeKind.End;
    public bool CanRollback => _rollback.Count > 0;

    public IReadOnlyList<StoryChoice> AvailableChoices => CurrentNode.Kind == NodeKind.Choice
        ? CurrentNode.Choices.Where(choice => choice.Condition?.Evaluate(Variables) != false).ToArray()
        : [];

    public StoryPlayer(StoryDocument story)
    {
        story.Validate();
        Story = story;
        Variables = new ReadOnlyDictionary<string, int>(_variables);
        History = _history.AsReadOnly();
        Restart();
    }

    public void Restart()
    {
        _variables.Clear();
        _history.Clear();
        _rollback.Clear();
        _readNodes.Clear();
        Scene = new();
        Enter(Story.Start);
    }

    public bool Advance()
    {
        if (CurrentNode.Kind != NodeKind.Dialogue) return false;
        Transition(() => Enter(CurrentNode.Next!));
        return true;
    }

    public void Choose(int index)
    {
        var choices = AvailableChoices;
        if ((uint)index >= (uint)choices.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var choice = choices[index];
        Transition(() => { Apply(choice.Effects); Enter(choice.Next); });
    }

    private void Transition(Action action)
    {
        var previous = Capture();
        try { action(); }
        catch { RestoreState(previous); throw; }
        _rollback.Add(previous);
        if (_rollback.Count > MaxRollback) _rollback.RemoveAt(0);
    }

    public bool Rollback()
    {
        if (!CanRollback) return false;
        var snapshot = _rollback[^1];
        _rollback.RemoveAt(_rollback.Count - 1);
        RestoreState(snapshot);
        return true;
    }

    public StorySnapshot Capture() => new()
    {
        StoryId = Story.Id,
        NodeId = CurrentNodeId,
        Variables = new(_variables, StringComparer.Ordinal),
        Scene = Scene with { },
        History = [.. _history],
        ReadNodes = new(_readNodes, StringComparer.Ordinal)
    };

    public void Restore(StorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Version != 1 || snapshot.StoryId != Story.Id ||
            !Story.Nodes.TryGetValue(snapshot.NodeId ?? "", out var node) ||
            node.Kind is not (NodeKind.Dialogue or NodeKind.Choice or NodeKind.End) ||
            snapshot.Variables is null || snapshot.Scene is null || snapshot.History is null || snapshot.ReadNodes is null ||
            snapshot.Scene.Background is null || snapshot.Scene.Portrait is null || snapshot.Scene.Music is null ||
            snapshot.Scene.SecondaryPortrait is null || snapshot.Scene.Chapter is null || snapshot.Scene.Location is null ||
            snapshot.Scene.Time is null || snapshot.Scene.Viewpoint is null || snapshot.Scene.Tone is null || snapshot.Scene.TertiaryPortrait is null ||
            snapshot.History.Count > MaxHistory || snapshot.History.Any(entry => entry is null ||
                !Story.Nodes.ContainsKey(entry.NodeId ?? "") || entry.Text is null || entry.Speaker is null) ||
            snapshot.ReadNodes.Any(id => !Story.Nodes.ContainsKey(id)))
            throw new InvalidDataException("Save is incompatible with this story or contains invalid state.");
        // No node effects are replayed: snapshots already include their result.
        if (node.Kind == NodeKind.Choice && !node.Choices.Any(choice => choice.Condition?.Evaluate(snapshot.Variables) != false))
            throw new InvalidDataException("Saved choice has no available options.");
        RestoreState(snapshot);
        _rollback.Clear();
    }

    private void RestoreState(StorySnapshot snapshot)
    {
        CurrentNodeId = snapshot.NodeId;
        _variables.Clear();
        foreach (var pair in snapshot.Variables) _variables.Add(pair.Key, pair.Value);
        Scene = snapshot.Scene with { };
        _history.Clear();
        _history.AddRange(snapshot.History);
        _readNodes.Clear();
        _readNodes.UnionWith(snapshot.ReadNodes);
        Revision++;
    }

    private void Enter(string id)
    {
        for (var step = 0; step < MaxAutomaticSteps; step++)
        {
            CurrentNodeId = id;
            var node = CurrentNode;
            Apply(node.Effects);
            if (node.Scene is { } scene)
                Scene = new(scene.Background ?? Scene.Background, scene.Portrait ?? Scene.Portrait, scene.Music ?? Scene.Music,
                    scene.SecondaryPortrait ?? Scene.SecondaryPortrait, scene.Chapter ?? Scene.Chapter,
                    scene.Location ?? Scene.Location, scene.Time ?? Scene.Time, scene.Viewpoint ?? Scene.Viewpoint,
                    scene.Tone ?? Scene.Tone, scene.TertiaryPortrait ?? Scene.TertiaryPortrait);
            switch (node.Kind)
            {
                case NodeKind.Dialogue:
                    _history.Add(new(id, node.Speaker, node.Text));
                    if (_history.Count > MaxHistory) _history.RemoveAt(0);
                    _readNodes.Add(id);
                    Revision++;
                    return;
                case NodeKind.Choice:
                    if (AvailableChoices.Count == 0) throw new InvalidDataException($"Choice '{id}' has no available options.");
                    Revision++;
                    return;
                case NodeKind.End:
                    Revision++;
                    return;
                case NodeKind.Branch:
                    id = node.Condition!.Evaluate(Variables) ? node.Then! : node.Else!;
                    break;
                default:
                    id = node.Next!;
                    break;
            }
        }
        throw new InvalidDataException("Story exceeded 1024 automatic steps. Check for a command cycle.");
    }

    private void Apply(IEnumerable<VariableEffect> effects)
    {
        foreach (var effect in effects)
            _variables[effect.Variable] = effect.Operation == VariableOperation.Add
                ? checked(_variables.GetValueOrDefault(effect.Variable) + effect.Value) : effect.Value;
    }
}

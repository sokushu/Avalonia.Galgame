namespace MonoGalGame.Framework.Rendering;

public readonly record struct PortraitTarget(string Character, string Texture, float Center, float Scale = 1);
public readonly record struct PortraitFrame(string Character, string Texture, float Center, float Opacity, float OffsetY, float Scale = 1);

/// <summary>Keeps each actor on stage while expressions, positions and other actors change.</summary>
public sealed class PortraitStage
{
    private const float Duration = .4f;
    private readonly List<Track> _tracks = [];

    public IEnumerable<PortraitFrame> Frames => _tracks.SelectMany(track => track.Frames);

    public void Set(IReadOnlyList<PortraitTarget> targets)
    {
        foreach (var track in _tracks)
        {
            var target = targets.FirstOrDefault(target => target.Character == track.Character);
            track.Set(target.Texture ?? "", target.Texture is null ? track.Center : target.Center,
                target.Texture is null ? track.Scale : target.Scale);
        }
        foreach (var target in targets)
            if (!_tracks.Any(track => track.Character == target.Character))
            {
                var track = new Track(target.Character, target.Center, target.Scale);
                track.Set(target.Texture, target.Center, target.Scale);
                _tracks.Add(track);
            }
    }

    public void Update(float seconds)
    {
        foreach (var track in _tracks) track.Update(Math.Max(0, seconds) / Duration);
        _tracks.RemoveAll(track => track.IsEmpty);
    }

    private sealed class Track(string character, float center, float scale)
    {
        private readonly List<Layer> _layers = [];
        private string _texture = "";
        private float _poseProgress = 1, _moveProgress = 1;
        private float _fromCenter = center, _targetCenter = center;
        private float _fromScale = scale, _targetScale = scale;
        public string Character { get; } = character;
        public float Center { get; private set; } = center;
        public float Scale { get; private set; } = scale;
        public bool IsEmpty => _texture.Length == 0 && _layers.Count == 0;
        public IEnumerable<PortraitFrame> Frames => _layers.Where(layer => layer.Opacity > 0)
            .Select(layer => new PortraitFrame(Character, layer.Texture, Center, layer.Opacity, layer.OffsetY, Scale));

        public void Set(string texture, float targetCenter, float targetScale)
        {
            if (_targetCenter != targetCenter || _targetScale != targetScale)
            {
                _fromCenter = Center;
                _targetCenter = targetCenter;
                _fromScale = Scale;
                _targetScale = targetScale;
                _moveProgress = 0;
            }
            if (_texture == texture) return;
            var entering = _layers.Count == 0;
            if (texture.Length > 0 && !_layers.Any(layer => layer.Texture == texture))
                _layers.Add(new Layer(texture) { OffsetY = entering ? 12 : 0 });
            // Capture the visible blend so fast skipping or rollback never snaps back to an old pose.
            foreach (var layer in _layers)
            {
                layer.FromOpacity = layer.Opacity;
                layer.FromOffset = layer.OffsetY;
            }
            _texture = texture;
            _poseProgress = 0;
        }

        public void Update(float elapsed)
        {
            _poseProgress = Math.Min(1, _poseProgress + elapsed);
            _moveProgress = Math.Min(1, _moveProgress + elapsed);
            static float Ease(float value) => value * value * (3 - 2 * value);
            var pose = Ease(_poseProgress);
            Center = _fromCenter + (_targetCenter - _fromCenter) * Ease(_moveProgress);
            Scale = _fromScale + (_targetScale - _fromScale) * Ease(_moveProgress);
            foreach (var layer in _layers)
            {
                var target = layer.Texture == _texture ? 1f : 0f;
                layer.Opacity = layer.FromOpacity + (target - layer.FromOpacity) * pose;
                layer.OffsetY = layer.FromOffset * (1 - pose);
            }
            if (_poseProgress == 1) _layers.RemoveAll(layer => layer.Opacity == 0);
        }

        private sealed class Layer(string texture)
        {
            public string Texture { get; } = texture;
            public float Opacity, FromOpacity, OffsetY, FromOffset;
        }
    }
}

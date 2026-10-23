using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using MonoGalGame.Core.Projects;

namespace MonoGalGame.Framework.Audio;

public interface IAudioService : IDisposable
{
    string? LastError { get; }
    void SetVolumes(float music, float voice);
    void PlayMusic(string id);
    void PlayVoice(string? id);
    void StopVoice();
}

/// <summary>OGG music uses MonoGame's streaming player. WAV music and voice use SoundEffect.</summary>
public sealed class MonoGameAudioService(GameProject project) : IAudioService
{
    private Song? _music;
    private SoundEffect? _musicWav;
    private SoundEffectInstance? _musicInstance;
    private string _musicId = "";
    private SoundEffect? _voice;
    private SoundEffectInstance? _voiceInstance;
    private float _voiceVolume = .8f;
    private float _musicVolume = .55f;
    private bool _disposed;
    public string? LastError { get; private set; }

    public void SetVolumes(float music, float voice)
    {
        _voiceVolume = Math.Clamp(voice, 0, 1);
        _musicVolume = Math.Clamp(music, 0, 1);
        Try(() =>
        {
            if (_music is not null) MediaPlayer.Volume = _musicVolume;
            if (_musicInstance is not null) _musicInstance.Volume = _musicVolume;
            if (_voiceInstance is not null) _voiceInstance.Volume = _voiceVolume;
        });
    }

    public void PlayMusic(string id)
    {
        if (_musicId == id) return;
        Try(() =>
        {
            if (_music is not null) MediaPlayer.Stop();
            _music?.Dispose();
            _music = null;
            _musicInstance?.Dispose();
            _musicWav?.Dispose();
            _musicInstance = null;
            _musicWav = null;
            _musicId = id;
            if (string.IsNullOrEmpty(id)) return;
            if (!project.Audio.TryGetValue(id, out var path)) throw new FileNotFoundException($"Unknown music: {id}");
            if (Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = File.OpenRead(project.ResolvePath(path));
                _musicWav = SoundEffect.FromStream(stream);
                _musicInstance = _musicWav.CreateInstance();
                _musicInstance.IsLooped = true;
                _musicInstance.Volume = _musicVolume;
                _musicInstance.Play();
                return;
            }
            _music = Song.FromUri(id, new Uri(project.ResolvePath(path)));
            MediaPlayer.Volume = _musicVolume;
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_music);
        });
    }

    public void PlayVoice(string? id)
    {
        StopVoice();
        if (string.IsNullOrEmpty(id)) return;
        Try(() =>
        {
            if (!project.Audio.TryGetValue(id, out var path)) throw new FileNotFoundException($"Unknown voice: {id}");
            using var stream = File.OpenRead(project.ResolvePath(path));
            _voice = SoundEffect.FromStream(stream);
            _voiceInstance = _voice.CreateInstance();
            _voiceInstance.Volume = _voiceVolume;
            _voiceInstance.Play();
        });
    }

    public void StopVoice()
    {
        _voiceInstance?.Dispose();
        _voice?.Dispose();
        _voiceInstance = null;
        _voice = null;
    }

    private void Try(Action action)
    {
        if (_disposed) return;
        try { action(); }
        catch (Exception ex) when (ex is NoAudioHardwareException or InvalidOperationException or IOException or
                                   DllNotFoundException or TypeInitializationException)
        { LastError = ex.Message; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Try(() => { StopVoice(); if (_music is not null) MediaPlayer.Stop(); _music?.Dispose(); _musicInstance?.Dispose(); _musicWav?.Dispose(); });
        _disposed = true;
    }
}

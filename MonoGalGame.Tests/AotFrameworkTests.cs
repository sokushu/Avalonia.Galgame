using MonoGalGame.Framework.Hosting;
using Xunit;

namespace MonoGalGame.Tests;

public sealed class AotFrameworkTests
{
    [Fact]
    public void SettingsRoundTripAllValuesWithGeneratedMetadata()
    {
        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "nested", "settings.json");
        var original = new UserSettings
        {
            TextSpeed = 49, AutoDelay = 3.5f, MusicVolume = .24f, VoiceVolume = .63f, Fullscreen = true
        };

        original.Save(path);
        var actual = UserSettings.Load(path);

        Assert.Contains("\"textSpeed\"", File.ReadAllText(path));
        Assert.Equal(original.TextSpeed, actual.TextSpeed);
        Assert.Equal(original.AutoDelay, actual.AutoDelay);
        Assert.Equal(original.MusicVolume, actual.MusicVolume);
        Assert.Equal(original.VoiceVolume, actual.VoiceVolume);
        Assert.Equal(original.Fullscreen, actual.Fullscreen);
    }

    [Fact]
    public void GeneratedSettingsMetadataAcceptsLegacyPropertyCasingAndDefaults()
    {
        using var folder = new TemporaryDirectory();
        var path = Path.Combine(folder.Path, "settings.json");
        File.WriteAllText(path, """{"TextSpeed":52,"Fullscreen":true}""");

        var actual = UserSettings.Load(path);

        Assert.Equal(52, actual.TextSpeed);
        Assert.True(actual.Fullscreen);
        Assert.Equal(.55f, actual.MusicVolume);
        Assert.Equal(.8f, actual.VoiceVolume);
        Assert.Equal(2, actual.AutoDelay);
    }
}

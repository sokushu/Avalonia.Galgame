"""Render five quiet original synthesized waltz cues. No downloaded recordings."""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "examples/MonoGalGame.Rose/Game/Audio"
RATE = 22050


def render(name, chords, melody, bpm=72):
    beat = 60 / bpm
    seconds = 48 * beat
    mix = np.zeros(round(seconds * RATE), dtype=np.float64)

    def note(midi, at, duration, volume, soft=False):
        start = round(at * RATE)
        length = min(round(duration * RATE), len(mix) - start)
        if length <= 0:
            return
        t = np.arange(length) / RATE
        frequency = 440 * 2 ** ((midi - 69) / 12)
        envelope = np.minimum(1, t / (.12 if soft else .012)) * np.exp(-t / (duration * (.8 if soft else .33)))
        envelope *= np.minimum(1, (duration - t) / .1).clip(0, 1)
        sound = np.sin(2 * np.pi * frequency * t) + .24 * np.sin(2 * np.pi * frequency * 2 * t)
        sound += .07 * np.sin(2 * np.pi * frequency * 3 * t)
        mix[start:start + length] += sound * envelope * volume

    for bar in range(16):
        chord = chords[(bar // 2) % len(chords)]
        start = bar * 3 * beat
        note(chord[0] - 12, start, 3.6 * beat, .052, True)
        for index, key in enumerate(chord):
            note(key, start + index * .55 * beat, 3.4 * beat, .028, True)
        for part in range(3):
            key = melody[(bar * 3 + part) % len(melody)]
            if key:
                note(key, start + part * beat, 2.6 * beat, .065)
    # A little sparse echo gives the notes room without masking reading.
    for delay, volume in ((.19, .16), (.37, .09), (.61, .05)):
        shift = round(delay * RATE)
        mix[shift:] += mix[:-shift].copy() * volume
    fade = round(.65 * RATE)
    mix[:fade] *= np.linspace(0, 1, fade)
    mix[-fade:] *= np.linspace(1, 0, fade)
    pcm = (np.tanh(mix * 1.3) * 24000).astype("<i2")
    DEST.mkdir(parents=True, exist_ok=True)
    with wave.open(str(DEST / (name + ".wav")), "wb") as file:
        file.setparams((1, 2, RATE, len(pcm), "NONE", "not compressed"))
        file.writeframes(pcm.tobytes())


if __name__ == "__main__":
    render("atelier", [[48, 55, 60, 64], [45, 52, 57, 60], [41, 48, 53, 57], [43, 50, 55, 59]],
           [72, 76, 79, 76, 74, 72, 69, 72, 76, 74, 72, 67])
    render("winter", [[50, 57, 62, 65], [46, 53, 58, 62], [43, 50, 55, 58], [45, 52, 57, 61]],
           [74, 77, 81, 77, 76, 74, 72, 74, 77, 76, 0, 69], 68)
    render("tender", [[53, 60, 65, 69], [50, 57, 62, 65], [46, 53, 58, 62], [48, 55, 60, 64]],
           [77, 81, 84, 81, 79, 77, 74, 77, 81, 79, 77, 72], 76)
    render("distance", [[45, 52, 57, 60], [41, 48, 53, 57], [38, 45, 50, 53], [40, 47, 52, 56]],
           [76, 0, 72, 71, 0, 69, 72, 0, 65, 64, 0, 68], 64)
    render("letter", [[48, 55, 60, 64], [47, 54, 59, 62], [45, 52, 57, 60], [41, 48, 53, 57]],
           [79, 76, 0, 74, 71, 0, 76, 72, 0, 69, 72, 0], 70)
    print("Rendered 5 original synthesized music tracks.")

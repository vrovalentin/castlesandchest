# Two original background loops, synthesized: a calm one for the building screen and a livelier one for the mine.
# Usage: python3 make_music.py <output folder>   (writes music_build.ogg and music_mine.ogg; needs ffmpeg)
# Each loop is rendered with its tail wrapped round to the start, so it repeats without a click or a gap.
import sys, os, subprocess, wave, tempfile
import numpy as np
from scipy.signal import lfilter

SR = 44100
rng = np.random.default_rng(11)


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def adsr(n, a=0.005, d=4.0):
    x = np.arange(n) / SR
    return np.clip(x / a, 0, 1) * np.exp(-x * d)


def lowpass(x, cut):
    k = 1 - np.exp(-2 * np.pi * cut / SR)
    return lfilter([k], [1, k - 1], x)


def mallet(m, sec):
    """Soft marimba-like note: the fundamental with a short bright overtone."""
    x = np.arange(int(SR * sec)) / SR
    f = hz(m)
    y = np.sin(2 * np.pi * f * x) * adsr(len(x), 0.004, 3.2)
    y += 0.25 * np.sin(2 * np.pi * f * 3.99 * x) * adsr(len(x), 0.002, 14)
    y += 0.08 * np.sin(2 * np.pi * f * 9.9 * x) * adsr(len(x), 0.001, 30)
    return y


def bell(m, sec):
    """Glassy celesta-like note."""
    x = np.arange(int(SR * sec)) / SR
    f = hz(m)
    y = np.sin(2 * np.pi * f * x + 0.6 * np.sin(2 * np.pi * f * 2 * x) * np.exp(-x * 6)) * adsr(len(x), 0.003, 2.6)
    return y


def pluck(m, sec, bright=2500):
    """Short plucked note for arpeggios: a soft saw through a closing filter."""
    x = np.arange(int(SR * sec)) / SR
    f = hz(m)
    saw = 2 * ((f * x) % 1) - 1
    y = lowpass(saw, bright) * adsr(len(x), 0.002, 9)
    return lowpass(y, bright * 0.6)


def pad(ms, sec):
    """Warm chord: detuned triangles, slow attack and release."""
    x = np.arange(int(SR * sec)) / SR
    y = np.zeros_like(x)
    for m in ms:
        for det in (-0.12, 0.12):
            f = hz(m + det)
            y += 2 / np.pi * np.arcsin(np.sin(2 * np.pi * f * x))
    att = np.clip(x / 0.35, 0, 1)
    rel = np.clip((sec - x) / 0.4, 0, 1)
    return lowpass(y / len(ms), 1400) * att * rel


def bass(m, sec, decay=3.0):
    x = np.arange(int(SR * sec)) / SR
    f = hz(m)
    y = np.sin(2 * np.pi * f * x) + 0.3 * np.sin(4 * np.pi * f * x)
    return y * adsr(len(x), 0.006, decay)


def kick(sec=0.3):
    x = np.arange(int(SR * sec)) / SR
    f = 50 + 70 * np.exp(-x * 30)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * adsr(len(x), 0.001, 12)


def hat(sec=0.06, decay=60):
    n = rng.standard_normal(int(SR * sec))
    n = n - lowpass(n, 6000)
    return n * adsr(len(n), 0.001, decay)


def shaker(sec=0.12):
    n = rng.standard_normal(int(SR * sec))
    n = n - lowpass(n, 3500)
    x = np.arange(len(n)) / SR
    return n * np.clip(x / 0.03, 0, 1) * np.exp(-x * 30)


CH = {'C': [60, 64, 67], 'Am': [57, 60, 64], 'F': [53, 57, 60], 'G': [55, 59, 62], 'Em': [52, 55, 59],
      'Dm': [50, 53, 57], 'E': [52, 56, 59]}
ROOT = {'C': 36, 'Am': 33, 'F': 29, 'G': 31, 'Em': 28, 'Dm': 26, 'E': 28}


class Track:
    def __init__(self, bpm, bars):
        self.beat = 60 / bpm
        self.n = int(SR * self.beat * 4 * bars)
        self.buf = np.zeros(self.n + SR * 4)

    def add(self, at_beats, y, gain):
        s = int(at_beats * self.beat * SR)
        self.buf[s:s + len(y)] += y * gain

    def melody(self, notes, inst, gain, start_bar=0, unit=0.5):
        t = start_bar * 4
        for m, ln in notes:
            if m is not None:
                self.add(t, inst(m, ln * unit * self.beat + 0.8), gain)
            t += ln * unit

    def loop(self):
        y = self.buf[:self.n].copy()
        tail = self.buf[self.n:]
        y[:len(tail)] += tail
        return y


def save_ogg(path, y, gain=0.7):
    y = y / (np.abs(y).max() + 1e-9) * gain
    with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as f:
        tmp = f.name
    with wave.open(tmp, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((y * 32767).astype(np.int16).tobytes())
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', tmp, '-c:a', 'libvorbis', '-q:a', '3', path], check=True)
    os.remove(tmp)


def build_theme():
    """Calm and sunny, C major, 92 BPM, 16 bars."""
    chords = 'C Am F G C Em F G Am F C G F G C C'.split()
    tr = Track(92, 16)
    for i, c in enumerate(chords):
        tr.add(i * 4, pad(CH[c], tr.beat * 4 + 0.3), 0.16)
        tr.add(i * 4, bass(ROOT[c], tr.beat * 1.8), 0.32)
        tr.add(i * 4 + 2.5, bass(ROOT[c] + 7, tr.beat * 1.2), 0.18)
        for b in range(4):
            tr.add(i * 4 + b + 0.5, shaker(), 0.035)
        # a soft mallet arpeggio under the tune
        for k, m in enumerate([CH[c][0] + 12, CH[c][1] + 12, CH[c][2] + 12, CH[c][1] + 12]):
            tr.add(i * 4 + k, mallet(m, tr.beat * 1.5), 0.1)
    tune = [(76, 2), (79, 2), (84, 2), (79, 1), (76, 1), (81, 3), (79, 1), (76, 2), (72, 2),
            (77, 2), (81, 2), (84, 2), (81, 1), (77, 1), (79, 4), (74, 2), (71, 2),
            (76, 2), (79, 2), (84, 2), (86, 1), (84, 1), (83, 3), (79, 1), (76, 4),
            (77, 2), (76, 1), (74, 1), (72, 2), (74, 2), (79, 6), (None, 2),
            (72, 2), (76, 2), (81, 2), (79, 2), (77, 3), (76, 1), (74, 2), (72, 2),
            (76, 2), (79, 2), (84, 3), (83, 1), (81, 2), (79, 2), (74, 4),
            (77, 2), (81, 2), (84, 2), (81, 2), (83, 2), (79, 2), (86, 4),
            (84, 2), (79, 2), (76, 2), (74, 2), (72, 6), (None, 2)]
    tr.melody(tune, mallet, 0.42)
    return tr.loop()


def mine_theme():
    """Lively and a little mysterious, A minor, 116 BPM, 16 bars: four bars of groove, then the tune."""
    chords = 'Am F C G Am F C G Dm Am F G Dm Am E E'.split()
    tr = Track(116, 16)
    for i, c in enumerate(chords):
        tr.add(i * 4, pad(CH[c], tr.beat * 4 + 0.3), 0.08)
        for b in range(8):
            tr.add(i * 4 + b * 0.5, bass(ROOT[c] + (12 if b % 4 == 3 else 0), tr.beat * 0.45, 7), 0.3)
        for b in (0, 2):
            tr.add(i * 4 + b, kick(), 0.32)
        for b in range(4):
            tr.add(i * 4 + b + 0.5, hat(), 0.05)
        arp = [CH[c][0], CH[c][1], CH[c][2], CH[c][1] + 12, CH[c][2], CH[c][1], CH[c][0] + 12, CH[c][2]]
        for k, m in enumerate(arp):
            tr.add(i * 4 + k * 0.5, pluck(m + 12, tr.beat * 0.5), 0.13)
    tune = [(76, 2), (72, 2), (69, 2), (72, 2), (77, 3), (76, 1), (72, 4),
            (79, 2), (76, 2), (72, 2), (76, 2), (74, 6), (None, 2),
            (77, 2), (74, 2), (69, 2), (74, 2), (76, 4), (72, 2), (69, 2),
            (72, 2), (77, 2), (81, 2), (79, 2), (79, 4), (83, 2), (79, 2),
            (81, 2), (77, 2), (74, 2), (77, 2), (76, 3), (74, 1), (72, 4),
            (71, 2), (68, 2), (71, 2), (76, 2), (80, 6), (None, 2)]
    tr.melody(tune, bell, 0.3, start_bar=4)
    return tr.loop()


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    save_ogg(os.path.join(out, 'music_build.ogg'), build_theme())
    save_ogg(os.path.join(out, 'music_mine.ogg'), mine_theme())

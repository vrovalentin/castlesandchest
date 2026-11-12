# Calm background loops: soft electric piano, a warm pad, a round bass and a sparse tune, all in a room reverb.
# No drums and no bright square/saw tones, so it does not sound like an 8-bit console.
# Standard library only (runs with any Python 3, also the one inside Unity's Emscripten); Unity compresses the WAVs.
# Usage: python make_music_calm.py <output folder> [build|mine ...]   -> music_build.wav, music_mine.wav
# Each loop is rendered with its tail (and the reverb's) wrapped round to the start, so it repeats without a seam.
import sys, os, wave, math, random, struct, array

SR = 32000
TAU = 2 * math.pi
rng = random.Random(5)


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


class Track:
    def __init__(self, bpm, bars):
        self.beat = 60 / bpm
        self.n = int(SR * self.beat * 4 * bars)
        self.buf = array.array('d', bytes(8 * (self.n + SR * 6)))

    def add(self, at_beats, y, gain):
        s = int(at_beats * self.beat * SR)
        b = self.buf
        for i, v in enumerate(y):
            b[s + i] += v * gain

    def wrapped(self, y):
        out = array.array('d', y[:self.n])
        for i in range(len(y) - self.n):
            out[i % self.n] += y[self.n + i]
        return out


def epiano(m, sec, vel=1.0):
    """A soft Rhodes-like note: two-operator FM whose brightness fades fast, the body slowly."""
    f = hz(m); n = int(SR * sec)
    out = [0.0] * n
    wc, wm = TAU * f / SR, TAU * f / SR
    for i in range(n):
        t = i / SR
        idx = 0.25 + 1.1 * vel * math.exp(-t * 5)
        a = min(1.0, t / 0.006) * (0.65 * math.exp(-t * 1.6) + 0.35 * math.exp(-t * 0.5))
        rel = min(1.0, (sec - t) / 0.25)
        out[i] = a * rel * math.sin(wc * i + idx * math.sin(wm * i)) + 0.12 * a * rel * math.sin(2 * wc * i) * math.exp(-t * 3)
    return out


def bell(m, sec):
    """A gentle glassy bell for the tune: inharmonic FM, soft attack."""
    f = hz(m); n = int(SR * sec)
    out = [0.0] * n
    wc, wm = TAU * f / SR, TAU * f * 3.5 / SR
    for i in range(n):
        t = i / SR
        a = min(1.0, t / 0.01) * math.exp(-t * 1.3) * min(1.0, (sec - t) / 0.3)
        out[i] = a * math.sin(wc * i + 0.8 * math.exp(-t * 4) * math.sin(wm * i))
    return out


def pad(ms, sec):
    """Warm chord: three slightly detuned sines per note, slow swell in and out, a slow shimmer."""
    n = int(SR * sec)
    out = [0.0] * n
    voices = []
    for m in ms:
        for det in (-0.08, 0.0, 0.07):
            voices.append((TAU * hz(m + det) / SR, rng.random() * TAU))
    k = 1.0 / len(voices)
    for i in range(n):
        t = i / SR
        a = min(1.0, t / 1.4) * min(1.0, (sec - t) / 1.2)
        s = 0.0
        for w, ph in voices:
            s += math.sin(w * i + ph)
        out[i] = s * k * a * (0.9 + 0.1 * math.sin(TAU * 0.25 * t))
    return out


def bass(m, sec):
    f = hz(m); n = int(SR * sec)
    w = TAU * f / SR
    return [min(1.0, (i / SR) / 0.02) * math.exp(-(i / SR) * 0.9) * min(1.0, (sec - i / SR) / 0.3)
            * (math.sin(w * i) + 0.15 * math.sin(2 * w * i)) for i in range(n)]


def brush(sec=0.25):
    """A very soft brushed noise on the off-beat (only in the mine loop)."""
    n = int(SR * sec); acc = 0.0; out = []
    for i in range(n):
        v = rng.uniform(-1, 1)
        hp = v - acc; acc += 0.3 * (v - acc)
        t = i / SR
        out.append(hp * min(1.0, t / 0.02) * math.exp(-t * 18))
    return out


def lowpass(x, cut):
    k = 1 - math.exp(-TAU * cut / SR)
    y = array.array('d', x); acc = 0.0
    for i, v in enumerate(x):
        acc += k * (v - acc); y[i] = acc
    return y


def reverb(x, wet=0.3, room=0.84, damp=0.35):
    """A small Freeverb-style room: eight damped combs in parallel, then four all-passes."""
    scale = SR / 44100
    combs = [int(d * scale) for d in (1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617)]
    alls = [int(d * scale) for d in (556, 441, 341, 225)]
    n = len(x)
    out = array.array('d', bytes(8 * n))
    for d in combs:
        buf = [0.0] * d; j = 0; store = 0.0
        for i in range(n):
            o = buf[j]
            store = o * (1 - damp) + store * damp
            buf[j] = x[i] * 0.015 + store * room
            out[i] += o
            j += 1
            if j == d: j = 0
    for d in alls:
        buf = [0.0] * d; j = 0
        for i in range(n):
            b = buf[j]
            v = out[i]
            out[i] = -v + b
            buf[j] = v + b * 0.5
            j += 1
            if j == d: j = 0
    return array.array('d', (a * (1 - wet) + b * wet * 3 for a, b in zip(x, out)))


def save_wav(path, y, gain=0.6):
    peak = max(abs(v) for v in y) or 1
    k = gain / peak
    with wave.open(path, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(array.array('h', (int(max(-1, min(1, v * k)) * 32767) for v in y)).tobytes())


# Chords as MIDI notes (pad voicing) and their bass roots.
CH = {
    'Cmaj7': [60, 64, 67, 71], 'Am7': [57, 60, 64, 67], 'Dm9': [57, 60, 62, 65], 'G6': [55, 59, 62, 64],
    'Em7': [52, 55, 59, 62], 'Fmaj7': [57, 60, 64, 65], 'Fm6': [56, 60, 62, 65], 'Gsus': [55, 60, 62, 67],
    'Am9': [55, 59, 60, 64], 'Dm7': [57, 60, 62, 65], 'Bbmaj7': [58, 62, 65, 69], 'E7': [56, 59, 62, 64],
}
ROOT = {'Cmaj7': 36, 'Am7': 33, 'Dm9': 38, 'G6': 31, 'Em7': 28, 'Fmaj7': 29, 'Fm6': 29, 'Gsus': 31,
        'Am9': 33, 'Dm7': 38, 'Bbmaj7': 34, 'E7': 28}


def build_theme():
    """Sunny and unhurried, C major with sevenths, 68 BPM, 16 bars (~56 s)."""
    chords = 'Cmaj7 Am7 Dm9 G6 Cmaj7 Em7 Fmaj7 Fm6 Cmaj7 Am7 Dm9 Gsus Em7 Am7 Fmaj7 Gsus'.split()
    tr = Track(68, 16)
    for i, c in enumerate(chords):
        notes = CH[c]
        tr.add(i * 4, pad(notes, tr.beat * 4 + 1.3), 0.10)
        tr.add(i * 4, bass(ROOT[c], tr.beat * 3.5), 0.30)
        # the piano rolls the chord gently on the first beat, and touches it again later in the bar
        for k, m in enumerate(notes):
            tr.add(i * 4 + k * 0.12, epiano(m, tr.beat * 3.4, 0.8), 0.13)
        for k, m in enumerate(notes[1:]):
            tr.add(i * 4 + 2.5 + k * 0.1, epiano(m + 12, tr.beat * 1.6, 0.5), 0.06)
    # A sparse tune on the pentatonic, two bars per phrase: (note or None, length in beats).
    tune = [(None, 1), (76, 1), (79, 2), (81, 3), (79, 1),
            (76, 4), (None, 4),
            (None, 1), (72, 1), (74, 2), (76, 2), (79, 2),
            (77, 4), (76, 2), (74, 2),
            (None, 1), (76, 1), (79, 2), (84, 3), (83, 1),
            (81, 4), (79, 4),
            (None, 1), (77, 1), (76, 2), (74, 2), (72, 2),
            (74, 6), (None, 2)]
    # The first half on the bell with the piano under it; the second half on the piano alone, an octave lower,
    # so the loop breathes.
    t = 0.0
    for m, ln in tune:
        if m is not None:
            if t < 32:
                tr.add(t, bell(m, ln * tr.beat + 1.2), 0.17)
                tr.add(t, epiano(m, ln * tr.beat + 0.6, 0.6), 0.10)
            else:
                tr.add(t, epiano(m - 12, ln * tr.beat + 0.6, 0.5), 0.14)
        t += ln
    return tr


def mine_theme():
    """For the mine: still calm, a little curious, A minor, 80 BPM, 16 bars, a soft brush on the off-beats."""
    chords = 'Am9 Fmaj7 Cmaj7 G6 Am9 Fmaj7 Dm7 E7 Am9 Fmaj7 Cmaj7 Gsus Dm7 Am7 Bbmaj7 E7'.split()
    tr = Track(80, 16)
    for i, c in enumerate(chords):
        notes = CH[c]
        tr.add(i * 4, pad(notes, tr.beat * 4 + 1.2), 0.08)
        tr.add(i * 4, bass(ROOT[c], tr.beat * 1.8), 0.30)
        tr.add(i * 4 + 2, bass(ROOT[c] + 7 if ROOT[c] + 7 < 43 else ROOT[c] - 5, tr.beat * 1.8), 0.22)
        # a slow broken chord in eighths, the piano quietly picking its way
        arp = [notes[0], notes[2], notes[1] + 12, notes[3], notes[2] + 12, notes[3], notes[1] + 12, notes[2]]
        for k, m in enumerate(arp):
            tr.add(i * 4 + k * 0.5, epiano(m, tr.beat * 1.2, 0.45), 0.07)
        for b in range(4):
            tr.add(i * 4 + b + 0.5, brush(), 0.025)
    tune = [(None, 2), (76, 2), (72, 2), (69, 2),
            (72, 3), (74, 1), (76, 4),
            (None, 2), (79, 2), (77, 2), (76, 2),
            (74, 6), (None, 2),
            (None, 2), (76, 2), (81, 2), (79, 2),
            (77, 3), (76, 1), (74, 4),
            (None, 2), (72, 2), (74, 2), (71, 2),
            (76, 6), (None, 2)]
    t = 0.0
    for m, ln in tune:
        if m is not None:
            if t < 32: tr.add(t, bell(m, ln * tr.beat + 1.0), 0.15)
            else: tr.add(t, epiano(m, ln * tr.beat + 0.5, 0.6), 0.12)
        t += ln
    return tr


def render(tr, path):
    y = lowpass(tr.buf, 5200)
    y = reverb(y)
    save_wav(path, tr.wrapped(y))


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else '.'
    which = sys.argv[2:] or ['build', 'mine']
    os.makedirs(out, exist_ok=True)
    if 'build' in which:
        render(build_theme(), os.path.join(out, 'music_build.wav'))
    if 'mine' in which:
        render(mine_theme(), os.path.join(out, 'music_mine.wav'))
    print('ok', which)

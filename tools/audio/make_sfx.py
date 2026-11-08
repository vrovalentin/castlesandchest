# Placeholder sound effects, synthesized so the game has sound before real ones are made.
# Usage: python3 make_sfx.py <output folder>   (writes 16-bit mono WAVs at 44.1 kHz)
import sys, os, wave
import numpy as np

SR = 44100
out = sys.argv[1]
os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(7)

def t(sec):
    return np.arange(int(SR * sec)) / SR

def env(n, attack=0.004, decay=8.0):
    x = np.arange(n) / SR
    a = np.clip(x / attack, 0, 1)
    return a * np.exp(-x * decay)

def tone(freq, sec, decay=8.0, shape='sine', attack=0.004):
    x = t(sec)
    f = np.broadcast_to(freq if np.ndim(freq) else np.full(len(x), freq), x.shape)
    ph = 2 * np.pi * np.cumsum(f) / SR
    if shape == 'sine':
        w = np.sin(ph)
    elif shape == 'tri':
        w = 2 / np.pi * np.arcsin(np.sin(ph))
    else:
        w = np.sign(np.sin(ph)) * 0.6
    return w * env(len(x), attack, decay)

def noise(sec, decay=20.0, lp=0.3):
    n = rng.standard_normal(int(SR * sec))
    # one-pole low-pass: lp near 0 = dark, near 1 = bright
    y = np.zeros_like(n); acc = 0.0
    for i, v in enumerate(n):
        acc += lp * (v - acc); y[i] = acc
    return y / (np.abs(y).max() + 1e-9) * env(len(n), 0.002, decay)

from scipy.signal import lfilter
def reson(x, f, q=30):
    w = 2 * np.pi * f / SR; r = np.exp(-w / (2 * q))
    return lfilter([1 - r], [1, -2 * r * np.cos(w), r * r], x)

def mix(*parts):
    n = max(int(start * SR) + len(p) for start, p in parts)
    y = np.zeros(n)
    for start, p in parts:
        s = int(start * SR)
        y[s:s + len(p)] += p
    return y

def save(name, y, gain=0.8):
    y = y / (np.abs(y).max() + 1e-9) * gain
    tail = int(0.004 * SR)
    y[-tail:] *= np.linspace(1, 0, tail)
    with wave.open(os.path.join(out, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((y * 32767).astype(np.int16).tobytes())

# blocks popping: a clear crystal "тынь" (struck glass: a few inharmonic partials, quick attack, ringing tail)
def glass(f, sec=0.45, decay=9.0):
    return mix((0, tone(f, sec, decay)), (0, 0.45 * tone(f * 2.76, sec * 0.7, decay * 1.8)),
               (0, 0.22 * tone(f * 5.40, sec * 0.4, decay * 3.0)), (0, 0.12 * noise(0.012, 300, 0.95)))
save('pop', glass(2350), 0.6)
# a tap that does nothing: soft wooden bonk
save('bonk', mix((0, tone(170, 0.14, 25)), (0, 0.4 * tone(340, 0.08, 40))), 0.55)
# chest reaches the bottom: coins
save('chest', mix((0, tone(1318, 0.35, 9)), (0.06, tone(1760, 0.4, 8)), (0.06, 0.3 * tone(3520, 0.25, 14)),
                  (0, 0.3 * noise(0.05, 60, 0.9))), 0.7)
# chest rattling before it opens: four wooden knocks
save('rattle', mix(*[(i * 0.1, 0.8 * tone(220 + 30 * (i % 2), 0.08, 45)) for i in range(4)],
                   *[(i * 0.1, 0.4 * noise(0.04, 80, 0.4)) for i in range(4)]), 0.6)
# chest opens: an old lid creaking open (stick-slip clicks through wood resonances), then a soft sparkle
def creak(sec=0.55, f0=38, f1=70):
    n = int(SR * sec); y = np.zeros(n); pos = 0.0; i = 0
    while True:
        k = pos / sec
        rate = f0 + (f1 - f0) * k + 10 * np.sin(k * 9)
        pos += (1 / rate) * (1 + 0.25 * (rng.random() - 0.5))
        j = int(pos * SR)
        if j >= n: break
        y[j] = 1 - 0.5 * rng.random(); i += 1
    y = reson(y, 820, 9) + 0.7 * reson(y, 1650, 12) + 0.35 * reson(y, 2900, 14)
    shape = np.sin(np.linspace(0, np.pi, n)) ** 0.6
    return y * shape
notes = [1047, 1319, 1568, 2093]
save('open', mix((0, creak()), *[(0.48 + i * 0.05, 0.22 * tone(f, 0.5, 7)) for i, f in enumerate(notes)],
                 (0.66, 0.08 * tone(4186, 0.4, 9))), 0.75)
# a part lands on its tile
save('tick', mix((0, tone(1500, 0.06, 60)), (0, 0.5 * tone(750, 0.06, 60))), 0.5)
# parts fly to storage: soft whoosh
x = t(0.3)
wh = noise(0.3, 6, 0.15) * np.sin(np.linspace(0, np.pi, len(x)))
save('whoosh', wh, 0.5)
# a part goes onto the castle: a mason's hammer on stone, "клак-дзынь" (bright knock + short metallic ring)
knock = noise(0.05, 90, 0.7)
ring = sum(a * tone(f, 0.35, d) for a, f, d in ((1.0, 1480, 14), (0.6, 3170, 20), (0.35, 4630, 28)))
save('place', mix((0, 0.9 * reson(knock, 520, 6)), (0, 0.5 * knock), (0.004, 0.55 * ring),
                  (0, 0.5 * tone(np.linspace(260, 200, int(SR * 0.09)), 0.09, 35))), 0.8)
# "Поставить" pressed: a short dull knock, like a wooden mallet on a beam ("тык")
save('tock', mix((0, tone(np.linspace(210, 150, int(SR * 0.09)), 0.09, 45)),
                 (0, 0.6 * reson(noise(0.04, 120, 0.25), 330, 6)),
                 (0, 0.35 * tone(95, 0.07, 60))), 0.6)
# a part lands on the castle: a dull construction "бум" (low thump of a stone block set down, a little dust)
thump = tone(np.linspace(95, 48, int(SR * 0.32)), 0.32, 13)
body = reson(noise(0.12, 38, 0.12), 160, 3)
save('thud', mix((0, thump), (0, 0.55 * body), (0, 0.35 * tone(np.linspace(190, 140, int(SR * 0.07)), 0.07, 50)),
                 (0.01, 0.3 * reson(noise(0.25, 12, 0.1), 260, 2))), 0.85)
# two parts merge: chime up
save('merge', mix((0, tone(784, 0.35, 8, 'tri')), (0.09, tone(1175, 0.45, 7, 'tri'))), 0.65)
# button click
save('click', mix((0, tone(1900, 0.03, 120)), (0, 0.4 * noise(0.015, 200, 0.9))), 0.4)
# broom: swish; hammer: two knocks
x = t(0.28)
save('broom', noise(0.28, 5, 0.6) * np.sin(np.linspace(0, np.pi, len(x))) ** 2, 0.55)
save('hammer', mix((0, tone(140, 0.12, 30)), (0, 0.7 * noise(0.05, 60, 0.35)), (0.12, 0.8 * tone(120, 0.15, 25)),
                   (0.12, 0.6 * noise(0.05, 60, 0.35))), 0.8)
# castle finished: little fanfare
fan = [(0, 523), (0.12, 659), (0.24, 784), (0.38, 1047)]
save('fanfare', mix(*[(s, tone(f, 0.9 if i == 3 else 0.35, 3 if i == 3 else 6, 'tri')) for i, (s, f) in enumerate(fan)],
                    (0.38, 0.5 * tone(1319, 0.9, 3, 'tri')), (0.38, 0.4 * tone(1568, 0.9, 3, 'tri'))), 0.75)
# «Забрать»: a quiet little two-note tinkle
save('collect', mix((0, tone(2093, 0.28, 14)), (0, 0.3 * tone(4186, 0.18, 22)), (0.07, 0.8 * tone(2637, 0.35, 11)),
                    (0.07, 0.25 * tone(5274, 0.2, 20))), 0.45)
print('ok', sorted(os.listdir(out)))

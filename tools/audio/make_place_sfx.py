# Candidate sounds for placing a part on the building: a hammer on wood, an axe into a log, a short saw stroke.
# Standard library only (runs with any Python 3, also the one inside Unity's Emscripten).
# Usage: python make_place_sfx.py <output folder>   (16-bit mono WAVs at 44.1 kHz: place_hammer, place_axe, place_saw)
import sys, os, wave, math, random, struct

SR = 44100
rng = random.Random(11)


def n(sec):
    return int(SR * sec)


def noise(sec):
    return [rng.uniform(-1, 1) for _ in range(n(sec))]


def env(length, attack, decay):
    """Quick rise over `attack` seconds, then exponential fall at `decay` per second."""
    return [min(1.0, (i / SR) / attack if attack > 0 else 1.0) * math.exp(-(i / SR) * decay) for i in range(length)]


def mul(a, b):
    return [x * y for x, y in zip(a, b)]


def bandpass(x, f, q):
    """RBJ band-pass biquad (0 dB peak)."""
    w = 2 * math.pi * f / SR
    al = math.sin(w) / (2 * q)
    b0, b2 = al, -al
    a0, a1, a2 = 1 + al, -2 * math.cos(w), 1 - al
    y = []; x1 = x2 = y1 = y2 = 0.0
    for v in x:
        o = (b0 * v + b2 * x2 - a1 * y1 - a2 * y2) / a0
        x2, x1, y2, y1 = x1, v, y1, o
        y.append(o)
    return y


def lowpass(x, k):
    y = []; acc = 0.0
    for v in x:
        acc += k * (v - acc); y.append(acc)
    return y


def tone(f0, f1, sec, decay, attack=0.001):
    length = n(sec); ph = 0.0; out = []
    for i in range(length):
        f = f0 + (f1 - f0) * i / max(1, length - 1)
        ph += 2 * math.pi * f / SR
        out.append(math.sin(ph))
    return mul(out, env(length, attack, decay))


def norm(x):
    m = max(abs(v) for v in x) or 1
    return [v / m for v in x]


def mix(*parts):
    length = max(n(s) + len(p) for s, p, _ in parts)
    y = [0.0] * length
    for s, p, g in parts:
        o = n(s)
        for i, v in enumerate(p):
            y[o + i] += g * v
    return y


def save(folder, name, y, gain):
    y = norm(y)
    tail = n(0.006)
    for i in range(tail):
        y[-1 - i] *= i / tail
    with wave.open(os.path.join(folder, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, v * gain)) * 32767)) for v in y))


def hammer():
    """A carpenter's hammer on a wooden beam: a dry, short «тук» (~0.12 s)."""
    click = mul(bandpass(noise(0.012), 3200, 1.2), env(n(0.012), 0.0003, 420))
    body = noise(0.12)
    wood = [a + 0.7 * b + 0.35 * c for a, b, c in zip(bandpass(body, 620, 9), bandpass(body, 1150, 11), bandpass(body, 1900, 13))]
    wood = mul(norm(wood), env(n(0.12), 0.0006, 42))
    thump = tone(190, 140, 0.08, 55)
    return mix((0, click, 0.55), (0, wood, 1.0), (0, thump, 0.5))


def axe():
    """An axe biting into a log: a heavy «тюк» with a crack of splitting wood after it (~0.2 s)."""
    bite = noise(0.2)
    body = [a + 0.8 * b for a, b in zip(bandpass(bite, 260, 4), bandpass(bite, 470, 6))]
    body = mul(norm(body), env(n(0.2), 0.0008, 24))
    blade = mul(bandpass(noise(0.02), 2600, 2.5), env(n(0.02), 0.0003, 260))
    crack = []
    for i in range(n(0.09)):
        crack.append(rng.uniform(-1, 1) if rng.random() < 0.06 else 0.0)
    crack = mul(bandpass(crack, 1700, 3), env(len(crack), 0.002, 30))
    thump = tone(120, 70, 0.14, 30)
    return mix((0, blade, 0.5), (0, body, 1.0), (0, thump, 0.7), (0.025, norm(crack), 0.35))


def saw():
    """One short stroke of a hand saw: teeth rasping through wood, rising a little (~0.26 s)."""
    sec = 0.26; length = n(sec)
    raw = noise(sec)
    rasp = [a + 0.6 * b for a, b in zip(bandpass(raw, 2600, 2.2), bandpass(raw, 4700, 3))]
    out = []; ph = 0.0
    for i in range(length):
        k = i / length
        rate = 55 + 40 * k                      # teeth per second speed up during the stroke
        ph += 2 * math.pi * rate / SR
        teeth = 0.35 + 0.65 * (0.5 + 0.5 * math.sin(ph)) ** 3
        shape = math.sin(math.pi * min(1.0, k * 1.15)) ** 0.7 if k < 0.87 else 0.0
        out.append(rasp[i] * teeth * shape)
    body = mul(bandpass(lowpass(raw, 0.2), 420, 2), [math.sin(math.pi * i / length) for i in range(length)])
    return mix((0, norm(out), 1.0), (0, norm(body), 0.25))


if __name__ == '__main__':
    folder = sys.argv[1] if len(sys.argv) > 1 else '.'
    os.makedirs(folder, exist_ok=True)
    save(folder, 'place_hammer', hammer(), 0.6)
    save(folder, 'place_axe', axe(), 0.6)
    save(folder, 'place_saw', saw(), 0.45)
    print('ok', sorted(f for f in os.listdir(folder) if f.startswith('place_')))

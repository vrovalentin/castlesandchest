# UI icons for the round buttons and tool buttons, drawn here so they match the game's chunky look.
# White glyphs with a dark brown outline (back, restart, sound on/off) and two coloured tools (hammer, broom).
# Usage: python3 make_icons.py <Art/Resources folder>   → UI/icon_<name>.png, 256x256
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out = os.path.join(sys.argv[1], 'UI')
os.makedirs(out, exist_ok=True)
N, SS = 256, 4
S = N * SS
INK = (74, 44, 20, 255)

def canvas(): return Image.new('RGBA', (S, S), (0, 0, 0, 0))
def P(*xy): return [v * S for v in xy]
def finish(img, name):
    img = img.convert('RGBa').resize((N, N), Image.LANCZOS).convert('RGBA')
    img.save(os.path.join(out, f'icon_{name}.png'), optimize=True)

def glyph(draw_fn, name):
    """draw_fn(d, width_scale) draws the shape in one colour; drawn fat in ink, then thinner in white, then a soft top light."""
    ink = canvas(); draw_fn(ImageDraw.Draw(ink), 1.0, INK)
    white = canvas(); draw_fn(ImageDraw.Draw(white), 0.55, (255, 252, 240, 255))
    # light from above: darken the lower part of the white a little
    a = np.array(white).astype(float)
    yy = np.linspace(0, 1, S)[:, None]
    shade = 1 - 0.13 * np.clip((yy - 0.45) * 2, 0, 1)
    a[..., :3] *= shade[..., None]
    white = Image.fromarray(a.astype(np.uint8))
    ink.alpha_composite(white)
    finish(ink, name)

def stroke_line(d, pts, w, col):
    d.line([(x * S, y * S) for x, y in pts], fill=col, width=int(w * S), joint='curve')
    r = w * S / 2
    for x, y in (pts[0], pts[-1]):
        d.ellipse((x * S - r, y * S - r, x * S + r, y * S + r), fill=col)

def arc_pts(cx, cy, r, a0, a1, n=60):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]

# back: a fat arrow pointing left
def back(d, k, col):
    w = 0.13 + 0.12 * (1 - k) * 0 + (0.11 if k < 1 else 0.2) - 0.11
    w = 0.20 if k == 1 else 0.10
    stroke_line(d, [(0.30, 0.5), (0.76, 0.5)], w, col)
    stroke_line(d, [(0.30, 0.5), (0.52, 0.27)], w, col)
    stroke_line(d, [(0.30, 0.5), (0.52, 0.73)], w, col)
glyph(back, 'back')

# restart: an open ring with an arrow head
def restart(d, k, col):
    w = 0.19 if k == 1 else 0.095
    pts = arc_pts(0.5, 0.52, 0.25, -60, 220)
    stroke_line(d, pts, w, col)
    ex, ey = pts[0]
    t = math.radians(-60)
    # arrow head at the start of the ring, pointing along the ring backwards (counter-clockwise)
    tip = (ex + 0.13 * math.sin(t) * -1 + 0.0, ey - 0.13 * math.cos(t) * -1)
    hx, hy = math.cos(t), math.sin(t)
    a = (ex + hx * 0.13, ey + hy * 0.13)
    b = (ex - hx * 0.13, ey - hy * 0.13)
    c = (ex + math.sin(t) * 0.15, ey - math.cos(t) * 0.15)
    grow = 0.05 if k == 1 else 0
    d.polygon([(a[0] * S + hx * grow * S, a[1] * S + hy * grow * S), (b[0] * S - hx * grow * S, b[1] * S - hy * grow * S),
               (c[0] * S + math.sin(t) * grow * S, c[1] * S - math.cos(t) * grow * S)], fill=col)
glyph(restart, 'restart')

def speaker(d, k, col):
    g = 0.045 if k == 1 else 0
    d.polygon([((0.16 - g) * S, (0.39 - g) * S), ((0.30 + g / 2) * S, (0.39 - g) * S), ((0.48 + g) * S, (0.22 - g * 1.5) * S),
               ((0.48 + g) * S, (0.78 + g * 1.5) * S), ((0.30 + g / 2) * S, (0.61 + g) * S), ((0.16 - g) * S, (0.61 + g) * S)], fill=col)

def sound_on(d, k, col):
    speaker(d, k, col)
    w = 0.12 if k == 1 else 0.055
    stroke_line(d, arc_pts(0.50, 0.5, 0.14, -45, 45, 30), w, col)
    stroke_line(d, arc_pts(0.50, 0.5, 0.27, -48, 48, 40), w, col)
glyph(sound_on, 'sound_on')

def sound_off(d, k, col):
    speaker(d, k, col)
    w = 0.13 if k == 1 else 0.06
    stroke_line(d, [(0.62, 0.37), (0.86, 0.63)], w, col)
    stroke_line(d, [(0.86, 0.37), (0.62, 0.63)], w, col)
glyph(sound_off, 'sound_off')

# ---- coloured tools ----
def rot(pts, cx, cy, ang):
    c, s = math.cos(ang), math.sin(ang)
    return [((x - cx) * c - (y - cy) * s + cx, (x - cx) * s + (y - cy) * c + cy) for x, y in pts]

def poly(d, pts, fill, outline=INK, w=0.035):
    d.polygon([(x * S, y * S) for x, y in pts], fill=fill)
    d.line([(x * S, y * S) for x, y in pts + [pts[0]]], fill=outline, width=int(w * S), joint='curve')

def rrect(cx, cy, w, h, ang, r=0.03, n=8):
    pts = []
    for qx, qy, a0 in ((1, -1, -90), (1, 1, 0), (-1, 1, 90), (-1, -1, 180)):
        ox, oy = cx + qx * (w / 2 - r), cy + qy * (h / 2 - r)
        pts += [(ox + r * math.cos(math.radians(a0 + 90 * i / n)), oy + r * math.sin(math.radians(a0 + 90 * i / n))) for i in range(n + 1)]
    return rot(pts, cx, cy, ang)

def hammer():
    img = canvas(); d = ImageDraw.Draw(img)
    ang = math.radians(-40)
    handle = rrect(0.5, 0.58, 0.12, 0.62, ang, 0.05)
    poly(d, handle, (196, 128, 66, 255))
    hl = rrect(0.5, 0.58, 0.04, 0.56, ang, 0.02); d.polygon([(x * S - 0.02 * S, y * S) for x, y in hl], fill=(226, 166, 96, 255))
    # head, across the handle's top
    hx, hy = 0.5 + 0.30 * math.sin(-ang) * -1, 0.58 - 0.30 * math.cos(ang)
    head = rrect(hx, hy, 0.50, 0.21, ang, 0.05)
    poly(d, head, (150, 162, 176, 255))
    top = rrect(hx - 0.02 * math.sin(ang), hy - 0.045, 0.42, 0.06, ang, 0.03)
    d.polygon([(x * S, y * S) for x, y in top], fill=(205, 214, 224, 255))
    finish(img, 'hammer')

def broom():
    img = canvas(); d = ImageDraw.Draw(img)
    ang = math.radians(35)
    # straw: a fan at the bottom left
    cx, cy = 0.36, 0.68
    fan = [(cx - 0.20, cy + 0.20), (cx + 0.20, cy + 0.20), (cx + 0.09, cy - 0.08), (cx - 0.09, cy - 0.08)]
    fan = rot(fan, cx, cy, ang)
    poly(d, fan, (242, 196, 90, 255))
    for i in range(-3, 4):
        x = cx + i * 0.045
        line = rot([(x * 0.6 + cx * 0.4, cy - 0.04), (x, cy + 0.18)], cx, cy, ang)
        d.line([(px * S, py * S) for px, py in line], fill=(196, 140, 40, 255), width=int(0.014 * S))
    band = rrect(cx, cy - 0.09, 0.22, 0.06, ang, 0.02)
    poly(d, band, (190, 60, 48, 255), w=0.03)
    # stick up to the top right
    stick = rrect(0.58, 0.36, 0.085, 0.58, ang, 0.04)
    poly(d, stick, (196, 128, 66, 255))
    finish(img, 'broom')

hammer(); broom()
print('ok', sorted(os.listdir(out)))

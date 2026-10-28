# Art for the chest window: night-sky panel in a wooden frame, golden rays, glints, part stands,
# a red title ribbon and a gold button. Everything is drawn here so it can be tuned and redrawn.
# Usage: python3 make_reward_ui.py <Art/Resources folder>
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out = sys.argv[1]
SS = 4  # supersampling for smooth edges

def save(img, path):
    os.makedirs(os.path.dirname(os.path.join(out, path)), exist_ok=True)
    img.save(os.path.join(out, path), optimize=True)

def big(w, h):
    return Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))

def small(img, w, h):
    return img.convert('RGBa').resize((w, h), Image.LANCZOS).convert('RGBA')

def rr(d, box, rad, **kw):
    d.rounded_rectangle([v * SS for v in box], rad * SS, **{k: (v * SS if k == 'width' else v) for k, v in kw.items()})

# ---- panel: 256x256, 9-slice with border 56 ----
def panel():
    n = 256
    im = big(n, n); d = ImageDraw.Draw(im)
    rr(d, (0, 0, n - 1, n - 1), 40, fill=(122, 78, 42, 255), outline=(74, 44, 20, 255), width=5)
    rr(d, (9, 8, n - 10, 20), 7, fill=(160, 108, 62, 255))           # light top plank edge
    inner = (18, 18, n - 19, n - 19)
    # vertical gradient inside
    g = np.zeros((n * SS, n * SS, 4), np.uint8)
    t = np.linspace(0, 1, n * SS)[:, None]
    top, bot = np.array([62, 92, 158]), np.array([34, 46, 96])
    g[..., :3] = (top * (1 - t[..., None]) + bot * t[..., None]).astype(np.uint8)
    g[..., 3] = 255
    mask = Image.new('L', im.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([v * SS for v in inner], 26 * SS, fill=255)
    im.paste(Image.fromarray(g), (0, 0), mask)
    d = ImageDraw.Draw(im)
    rr(d, inner, 26, outline=(60, 34, 14, 255), width=3)
    save(small(im, n, n), 'UI/ui_panel_night.png')

# ---- rays and halo behind the chest: 512, transparent ----
def rays():
    n = 512
    y, x = np.mgrid[0:n, 0:n] + 0.5
    c = n / 2
    ang = np.arctan2(y - c, x - c); dist = np.hypot(y - c, x - c) / c
    ray = np.clip((np.cos(ang * 12) - 0.3) * 4, 0, 1) * np.clip(1 - dist, 0, 1) ** 1.3 * 0.42
    halo = np.clip(1 - dist / 0.62, 0, 1) ** 1.6 * 0.9
    a = np.clip(ray + halo, 0, 1)
    img = np.zeros((n, n, 4), np.uint8)
    img[..., 0], img[..., 1], img[..., 2] = 255, 214, 110
    img[..., 3] = (a * 255).astype(np.uint8)
    save(Image.fromarray(img), 'Fx/fx_rays.png')

# ---- glint: bright gold four-point star with a white core, no outline ----
def glint():
    n = 128
    y, x = (np.mgrid[0:n, 0:n] + 0.5) / n * 2 - 1
    ax, ay = np.abs(x), np.abs(y)
    star = np.clip((1 - (np.sqrt(ax) + np.sqrt(ay)) / 0.92) * 3.5, 0, 1)
    d = np.hypot(x, y)
    glow = np.clip(1 - d / 0.8, 0, 1) ** 2.2 * 0.6
    core = np.clip(1 - d / 0.25, 0, 1)
    a = np.clip(np.maximum(star, glow), 0, 1)
    gold, white = np.array([255, 200, 50]), np.array([255, 255, 240])
    mix = np.clip(core * 1.5 + star * 0.4, 0, 1)[..., None]
    img = np.zeros((n, n, 4), np.uint8)
    img[..., :3] = (gold * (1 - mix) + white * mix).astype(np.uint8)
    img[..., 3] = (a * 255).astype(np.uint8)
    save(Image.fromarray(img), 'Fx/fx_glint.png')

# ---- stand under a part: glowing oval with a gold rim, 256x64 ----
def plate():
    w, h = 256, 64
    im = big(w, h)
    gl = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(gl).ellipse([v * SS for v in (6, 6, w - 7, h - 7)], fill=(255, 220, 130, 170))
    im.alpha_composite(gl.convert('RGBa').filter(ImageFilter.GaussianBlur(6 * SS)).convert('RGBA'))
    d = ImageDraw.Draw(im)
    d.ellipse([v * SS for v in (22, 14, w - 23, h - 15)], fill=(96, 128, 200, 255), outline=(255, 214, 110, 255), width=4 * SS)
    d.ellipse([v * SS for v in (40, 20, w - 41, 32)], fill=(140, 170, 230, 255))
    save(small(im, w, h), 'Fx/fx_plate.png')

# ---- title ribbon: 512x128, drawn at a fixed size ----
def ribbon():
    w, h = 512, 128
    im = big(w, h); d = ImageDraw.Draw(im)
    S = lambda pts: [(px * SS, py * SS) for px, py in pts]
    dark, mid, light, edge = (150, 40, 32, 255), (170, 50, 40, 255), (214, 72, 56, 255), (120, 30, 22, 255)
    bx0, bx1, by0, by1 = 44, w - 45, 14, 96
    d.polygon(S([(4, 34), (bx0 + 20, 34), (bx0 + 20, 118), (4, 118), (24, 76)]), fill=mid, outline=edge)
    d.polygon(S([(w - 5, 34), (bx1 - 20, 34), (bx1 - 20, 118), (w - 5, 118), (w - 25, 76)]), fill=mid, outline=edge)
    d.polygon(S([(bx0, by1), (bx0 + 20, 118), (bx0 + 20, by1)]), fill=dark)
    d.polygon(S([(bx1, by1), (bx1 - 20, 118), (bx1 - 20, by1)]), fill=dark)
    rr(d, (bx0, by0, bx1, by1), 14, fill=light, outline=edge, width=4)
    rr(d, (bx0 + 10, by0 + 8, bx1 - 10, by0 + 22), 7, fill=(236, 112, 92, 255))
    save(small(im, w, h), 'UI/ui_ribbon.png')

# ---- gold button: 128x128, 9-slice border 40; a darker lip under the face ----
def button(path, pressed):
    n = 128
    im = big(n, n); d = ImageDraw.Draw(im)
    lip = 10
    if not pressed:
        rr(d, (2, lip + 2, n - 3, n - 3), 30, fill=(176, 98, 10, 255))
        face = (2, 2, n - 3, n - 3 - lip)
        col = (255, 178, 40, 255)
    else:
        face = (2, lip + 2, n - 3, n - 3)
        col = (240, 156, 24, 255)
    rr(d, face, 30, fill=col, outline=(140, 74, 6, 255), width=4)
    gl = Image.new('RGBA', im.size, (0, 0, 0, 0))
    rr(ImageDraw.Draw(gl), (face[0] + 12, face[1] + 8, face[2] - 12, face[1] + 34), 13, fill=(255, 255, 255, 95 if not pressed else 60))
    im.alpha_composite(gl)
    save(small(im, n, n), path)

panel(); rays(); glint(); plate(); ribbon()
button('UI/ui_btn_gold.png', False); button('UI/ui_btn_gold_down.png', True)
print('ok')

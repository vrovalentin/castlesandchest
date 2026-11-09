# Wooden scaffolding pieces for an unfinished building, light from the top left:
#   scaff_pole.png  : 32x256 round pole (vertical), a rope lashing near the top
#   scaff_plank.png : 256x40 board with grain and a nail at each end
# Usage: python3 make_scaffold.py <output folder>
import sys, os
import numpy as np
from PIL import Image

out = sys.argv[1]
os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(5)
WOOD = np.array([176, 122, 70], np.float32)


def save(name, rgb, a):
    img = np.dstack([np.clip(rgb, 0, 255), np.clip(a * 255, 0, 255)]).astype(np.uint8)
    Image.fromarray(img, 'RGBA').save(os.path.join(out, name), optimize=True)


def grain(h, w, along_x):
    """Streaks running along the wood."""
    n = w if not along_x else h
    lines = rng.random(n // 2 + 1)
    s = np.repeat(lines, 2)[:n]
    s = np.convolve(s, np.ones(3) / 3, 'same')
    return np.tile(s, (h, 1)) if not along_x else np.tile(s[:, None], (1, w))


# ---------- pole ----------
W, H = 32, 256
x = (np.arange(W) + 0.5) / W * 2 - 1
xx = np.tile(x, (H, 1))
a = np.clip((1 - np.abs(xx)) * W / 2 - 1.5, 0, 1)           # soft sides, 1px edges
z = np.sqrt(np.clip(1 - xx ** 2, 0, 1))
light = np.clip(0.55 + 0.6 * (-0.6 * xx + 0.5 * z), 0.25, 1.2)
g = grain(H, W, False)
rgb = WOOD * light[..., None] * (0.86 + 0.22 * g[..., None])
rgb *= 1 - 0.55 * np.clip(np.abs(xx) - 0.72, 0, 1)[..., None] / 0.28   # dark outline at the sides
# knots
for ky in rng.uniform(40, 230, 3):
    yy, xg = np.mgrid[0:H, 0:W]
    d = np.hypot((yy - ky) / 4.5, (xg - rng.uniform(10, 22)) / 3)
    rgb *= (1 - 0.35 * np.clip(1 - d, 0, 1))[..., None]
# rope lashing at the top
yy = np.arange(H)[:, None] * np.ones((1, W))
rope = ((yy > 14) & (yy < 30)).astype(np.float32)
stripes = 0.75 + 0.25 * np.sin((yy + xx * 6) * 1.3)
rope_rgb = np.array([214, 190, 130], np.float32) * (light[..., None] * 0.9) * stripes[..., None]
rgb = rgb * (1 - rope[..., None]) + rope_rgb * rope[..., None]
a = np.maximum(a, rope * np.clip((1.06 - np.abs(xx)) * W / 2, 0, 1))
save('scaff_pole.png', rgb, a)

# ---------- plank ----------
W, H = 256, 40
yy, xg = np.mgrid[0:H, 0:W].astype(np.float32)
v = (yy + 0.5) / H
a = np.clip(np.minimum(np.minimum(xg, W - 1 - xg), np.minimum(yy - 2, H - 3 - yy)) + 0.5, 0, 1)
light = np.where(v < 0.25, 1.15, np.where(v > 0.8, 0.62, 0.95 - 0.15 * v))  # lit top edge, dark side below
g = grain(H, W, True)
wav = np.sin(xg / W * 9 + g * 3) * 0.04
rgb = WOOD * light[..., None] * (0.86 + 0.2 * g[..., None] + wav[..., None])
edge = (np.minimum(np.minimum(xg, W - 1 - xg), np.minimum(yy - 2, H - 3 - yy)) < 2).astype(np.float32)
rgb *= (1 - 0.5 * edge)[..., None]
for nx in (12, W - 13):
    d = np.hypot(xg - nx, yy - H * 0.42)
    nail = np.clip(3.2 - d, 0, 1)
    rgb = rgb * (1 - nail[..., None]) + nail[..., None] * np.array([70, 66, 64]) + np.clip(1.6 - np.hypot(xg - nx + 1, yy - H * 0.42 + 1), 0, 1)[..., None] * 90
save('scaff_plank.png', rgb, a)
print('scaff_pole.png, scaff_plank.png in', out)

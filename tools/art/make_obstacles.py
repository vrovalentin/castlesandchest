# Board obstacles, drawn to match the glossy blocks (256x256, light from the top left):
#   ice.png   : a sheet of ice laid over a block (see-through, bright rim, glints, a few cracks)
#   stone.png : a dark boulder that does not explode, only falls
# Usage: python3 make_obstacles.py <output folder>
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

N = 256
out = sys.argv[1]
os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(11)
yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
u, v = (xx + 0.5) / N - 0.5, (yy + 0.5) / N - 0.5


def rounded_box(half, radius, soft=1.2):
    dx = np.abs(u) - (half - radius)
    dy = np.abs(v) - (half - radius)
    d = np.hypot(np.maximum(dx, 0), np.maximum(dy, 0)) + np.minimum(np.maximum(dx, dy), 0) - radius
    return np.clip(0.5 - d * N / soft, 0, 1), d


def save(name, rgb, a):
    img = np.dstack([np.clip(rgb, 0, 255), np.clip(a * 255, 0, 255)]).astype(np.uint8)
    Image.fromarray(img, 'RGBA').save(os.path.join(out, name), optimize=True)


# ---------- ice ----------
inside, d = rounded_box(0.47, 0.12)
rim = np.clip(1 - np.abs(d * N + 7) / 6, 0, 1) * inside          # a bright band just inside the edge
glint = np.clip(1 - np.abs((u + v) * 3.2 + 0.9) * 6, 0, 1) * np.clip((0.12 - u) * 8, 0, 1) * np.clip((0.12 - v) * 8, 0, 1) * inside     # diagonal streak top left
glint2 = np.clip(1 - np.abs((u + v) * 3.2 + 0.45) * 12, 0, 1) * np.clip((0.06 - u) * 8, 0, 1) * np.clip((0.06 - v) * 8, 0, 1) * inside
frost = np.clip(0.35 + 0.65 * (-(u + v)), 0, 1)                  # whiter to the top left
a = inside * (0.38 + 0.12 * frost) + rim * 0.45 + glint * 0.45 + glint2 * 0.35
rgb = np.dstack([200 + 40 * frost, 232 + 20 * frost, 255 + 0 * frost])
rgb = rgb * (1 - rim[..., None] * 0.0) + rim[..., None] * 25
# cracks: a few thin jagged lines from one point
crack = Image.new('L', (N, N), 0)
dr = ImageDraw.Draw(crack)
cx, cy = 168, 150
for ang in (0.3, 1.9, 3.4, 4.6):
    x, y = cx, cy
    for k in range(5):
        ang += rng.uniform(-0.5, 0.5)
        nx, ny = x + np.cos(ang) * rng.uniform(12, 22), y + np.sin(ang) * rng.uniform(12, 22)
        dr.line((x, y, nx, ny), fill=255, width=2)
        x, y = nx, ny
crack = np.asarray(crack.filter(ImageFilter.GaussianBlur(0.6)), np.float32) / 255 * inside
a = np.clip(a + crack * 0.55, 0, 1)
rgb = rgb * (1 - crack[..., None]) + crack[..., None] * np.array([250, 254, 255])
save('ice.png', rgb, a)

# ---------- stone ----------
ang = np.arctan2(v, u)
r = np.hypot(u, v)
wob = sum(rng.uniform(0.008, 0.02) * np.cos(k * ang + rng.uniform(0, 6.28)) for k in range(2, 7))
edge = 0.43 + wob - 0.03 * np.clip(v, 0, 1)                         # a little flatter at the bottom
dist = (r - edge) * N
a = np.clip(0.5 - dist / 1.5, 0, 1)
# shading: sphere-ish normal, light from the top left
z = np.sqrt(np.clip(1 - (r / np.maximum(edge, 1e-3)) ** 2, 0, 1))
nx_, ny_ = u / np.maximum(edge, 1e-3), v / np.maximum(edge, 1e-3)
light = np.clip(-0.7 * nx_ - 0.8 * ny_ + 0.45 * z, 0, 1)
base = np.array([92, 84, 80], np.float32)
noise = np.asarray(Image.fromarray((rng.random((32, 32)) * 255).astype(np.uint8)).resize((N, N), Image.BICUBIC), np.float32) / 255
rgb = base * (0.45 + 0.85 * light[..., None]) * (0.88 + 0.24 * noise[..., None])
# darker outline and a soft rim of reflected light at the bottom right
outline = np.clip(1 - np.abs(dist + 4) / 4, 0, 1)
rgb = rgb * (1 - 0.6 * outline[..., None])
spec = np.clip(1 - np.hypot(u + 0.16, v + 0.2) / 0.11, 0, 1) ** 2
rgb = rgb + spec[..., None] * 70
# cracks
crack = Image.new('L', (N, N), 0)
dr = ImageDraw.Draw(crack)
for (x, y, a0) in ((150, 95, 2.2), (100, 165, 0.2)):
    for k in range(4):
        a0 += rng.uniform(-0.6, 0.6)
        nx2, ny2 = x + np.cos(a0) * rng.uniform(14, 24), y + np.sin(a0) * rng.uniform(14, 24)
        dr.line((x, y, nx2, ny2), fill=255, width=4)
        x, y = nx2, ny2
crack = np.asarray(crack.filter(ImageFilter.GaussianBlur(0.8)), np.float32) / 255 * (dist < -10)
rgb = rgb * (1 - 0.55 * crack[..., None])
save('stone.png', rgb, a)
print('ice.png, stone.png in', out)

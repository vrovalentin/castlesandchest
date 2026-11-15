# One-off: the innkeeper's mug without beer foam (kvass, for the 0+ rating). Usage: kvas_mug.py <8_innkeeper.png> <out.png>
import sys, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as ndi
src, out = sys.argv[1:3]
im = np.asarray(Image.open(src).convert('RGB')).astype(np.int32)
H, W, _ = im.shape
L, R = np.array([247., 174.]), np.array([362., 155.])
C = (L + R) / 2; a = np.linalg.norm(R - L) / 2; ang = math.atan2(*(R - L)[::-1])
b = 12.0
ux, uy = math.cos(ang), math.sin(ang)          # along rim
vx, vy = -uy, ux                                # down (perpendicular)
yy, xx = np.mgrid[0:H, 0:W].astype(float)
U = (xx - C[0]) * ux + (yy - C[1]) * uy
V = (xx - C[0]) * vx + (yy - C[1]) * vy
mn, mx = im.min(2), im.max(2)
bg = (mn > 240) & (mx - mn < 14)
box = (xx > 180) & (xx < 420) & (yy > 80) & (yy < 225)
foam = box & (mn > 95) & (mx > 150) & (im[..., 0] - im[..., 1] < 46) & ~bg
lab, n = ndi.label(foam); sz = ndi.sum(foam, lab, range(1, n + 1)); foam = lab == 1 + int(np.argmax(sz))
foam = ndi.binary_fill_holes(foam)
foam = ndi.binary_closing(foam, iterations=4)
foam = ndi.binary_dilation(foam, iterations=4) & box & ~bg
inell = (U / a) ** 2 + (V / b) ** 2 <= 1.0
hull = ndi.binary_fill_holes(ndi.binary_closing(foam, iterations=10)) & box
inside = (V >= 0) & (np.abs(U) < a)
for k in range(1, 7):
    foam |= np.roll(foam, k, axis=0) & inside & ~bg
res = im.copy()
mug = (np.abs(U) < a + 2) | ((U < 0) & (U > -a - 22) & (V >= 0))
above = ((foam | hull | (mn > 200)) & box & (V < 0) & ~inell & ~((im[..., 0] - im[..., 1] > 50) & ~foam & ~hull))
res[above] = 255
band = foam & (V >= 0) & (V < 28) & mug
stave = foam & (V >= 28) & mug
# band: nearest clean pixel along U at the same V
clean = ~foam & (V >= 0) & ~bg & (np.abs(U) < a + 40)
ys, xs = np.nonzero(band)
for y, x in zip(ys, xs):
    for d in range(1, 120):
        hit = None
        for s in ((-1,) if U[y, x] < -a + 2 else (1, -1)):
            px, py = int(round(x + s * d * ux)), int(round(y + s * d * uy))
            if 0 <= px < W and 0 <= py < H and clean[py, px]: hit = (py, px); break
        if hit: res[y, x] = im[hit]; break
ys, xs = np.nonzero(stave)
for y, x in zip(ys, xs):
    for d in range(1, 150):
        px, py = int(round(x + d * vx)), int(round(y + d * vy))
        if 0 <= px < W and 0 <= py < H and clean[py, px]: res[y, x] = im[py, px]; break
# what is left of the foam outside the mug sides -> background
rest = foam & ~above & ~band & ~stave & ~inell
res[rest] = 255
# soften the seams of the fill a little
fill = band | stave
blur = np.stack([ndi.gaussian_filter(res[..., c].astype(float), 0.7) for c in range(3)], -1)
edge = ndi.binary_dilation(fill, iterations=1) & ~ndi.binary_erosion(fill, iterations=1)
res[edge] = blur[edge]
# the opening: wooden lip and kvass inside, drawn 4x and scaled down
S = 4
lay = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
def ell(aa, bb, dv, fill):
    pts = [((C[0] + aa * math.cos(t) * ux + (bb * math.sin(t) + dv) * vx) * S, (C[1] + aa * math.cos(t) * uy + (bb * math.sin(t) + dv) * vy) * S) for t in np.linspace(0, 2 * math.pi, 240)]
    d.polygon(pts, fill=fill)
ell(a, b, 0, (92, 50, 24, 255))              # rim, dark wood
ell(a - 2, b - 1.5, -0.6, (176, 104, 52, 255))  # lit top face of the staves
ell(a - 7, b - 4.5, 0.4, (70, 34, 14, 255))   # kvass, dark
ell(a - 11, b - 6.5, 1.2, (104, 54, 22, 255)) # kvass, warmer middle
ell(a * 0.38, 1.6, -2.6, (196, 140, 92, 200))  # soft glint
lay = lay.filter(ImageFilter.GaussianBlur(S * 0.5)).resize((W, H), Image.LANCZOS)
out_im = Image.fromarray(res.clip(0, 255).astype(np.uint8), 'RGB').convert('RGBA')
out_im.alpha_composite(lay)
out_im.convert('RGB').save(out)

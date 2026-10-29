# Cut castle 2 into its building steps, bottom-up.
#   castle_2_clean.png : the castle without porches, lanterns, princess and knight (walls are taken from it)
#   castle_2_full.png  : the same castle with them (porches, lanterns, knight, princess, flower pots are taken from it)
# Both pictures are 1024x1024 on white and line up pixel for pixel.
# Usage: python3 cut_castle_2.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
C = np.array(Image.open(clean_p).convert('RGB')).astype(np.int32)
F = np.array(Image.open(full_p).convert('RGB')).astype(np.int32)
H, W = C.shape[:2]
Y, X = np.mgrid[0:H, 0:W]

def cutout(img):
    """Alpha from the white background: flood from the border over near-white, then a soft edge."""
    mn, mx = img.min(2), img.max(2)
    white = (mn > 236) & (mx - mn < 22)
    lab, _ = ndi.label(white)
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    bg = np.isin(lab, border[border > 0])
    bg = ndi.binary_opening(bg, iterations=1)
    fg = ~bg
    fg = ndi.binary_fill_holes(fg) & fg | ndi.binary_fill_holes(fg) & ~ndi.binary_dilation(bg, iterations=1)
    # soft 1-px edge
    a = ndi.gaussian_filter(fg.astype(float), 0.7)
    a[ndi.binary_erosion(fg, iterations=1)] = 1
    return np.clip(a, 0, 1)

aC, aF = cutout(C), cutout(F)
on = aC > 0.02
r, g, b = C[..., 0], C[..., 1], C[..., 2]

SLOTS = []
def slot(part, mask, src='clean'):
    SLOTS.append((part, mask, src))
    return len(SLOTS) - 1

# ---------- regions ----------
def tower_of(x, y):
    """0..4 = outer left, inner left, keep, inner right, outer right, by where a pixel stands."""
    t = np.full(x.shape, 2)
    batt = (y >= 528) & (y <= 655)
    l1 = np.where(batt, 262, 238); l2 = np.where(y < 460, 402, 386)
    r2 = np.where(y < 460, 622, 638); r1 = np.where(batt, 762, 786)
    t[x < l2] = 1; t[x < l1] = 0; t[x > r2] = 3; t[x > r1] = 4
    return t
T = tower_of(X, Y)

red = (r > 150) & (g < 115) & (r - b > 90) & (r - g > 70)
gold = (r > 170) & (g > 90) & (b < 110) & (r - b > 100) & ~red
# roofs: red tiles and the dark wooden brackets under them, inside each cone's box
cones = [  # x0, x1, y0, y1 of each roof (tower order)
    (30, 272, 340, 532), (212, 428, 220, 405), (372, 652, 100, 315), (596, 812, 220, 405), (752, 994, 340, 532)]
spires = [(120, 190, 255, 365), (295, 355, 140, 250), (478, 548, 5, 125), (668, 728, 140, 250), (834, 904, 255, 365)]
win_boxes = {  # x0, x1, y0, y1 incl. stone frame and sill
    0: (102, 198, 668, 812), 4: (826, 922, 668, 812), 1: (272, 378, 378, 520), 3: (646, 752, 378, 520), 2: (440, 585, 278, 465)}
door_box = (418, 606, 655, 918)

# Roofs are cones under golden spires. A pixel belongs to the front-most cone whose triangle holds it
# (outer towers in front, then the keep, then the inner towers); a spire is everything above its cone.
CONES = {  # tower: centre x, spire bottom y, eave y, half width at the eave
    0: (150, 362, 532, 122), 1: (322, 246, 404, 108), 2: (512, 122, 314, 142), 3: (702, 246, 404, 108), 4: (874, 362, 532, 122)}
roofish = red | ((r < 150) & (g < 90) & (b < 60)) | ((r > 120) & (g < 110) & (b < 70))   # tiles, dark gaps, brackets
roof = np.full((H, W), -1)
spire = np.full((H, W), -1)
for t in (1, 3, 2, 0, 4):            # later wins: front-most last
    cx, sy, ey, hw = CONES[t]
    k = np.clip((Y - sy) / (ey - sy), 0, 1)
    tri = (Y >= sy) & (Y <= ey + 6) & (np.abs(X - cx) <= hw * (0.15 + 0.85 * k) + 12)
    near_eave = Y > ey - 40
    roof[tri & on & (~near_eave | roofish)] = t
    spire[(Y < sy) & (np.abs(X - cx) <= 44) & on] = t
roof_m = ndi.binary_opening(roof >= 0, iterations=1)
roof = np.where(roof_m, roof, -1)
spire[roof >= 0] = -1
# red tile crumbs left on the walls near an eave go to the nearest roof
dist, idx = ndi.distance_transform_edt(roof < 0, return_indices=True)
crumbs = on & (roof < 0) & (spire < 0) & roofish & (dist < 30)
roof[crumbs] = roof[idx[0][crumbs], idx[1][crumbs]]

def arch(x0, x1, y0, y1):
    cx, half = (x0 + x1) / 2, (x1 - x0) / 2
    ac = y0 + half
    return (((Y >= ac) & (np.abs(X - cx) <= half) & (Y <= y1)) | ((X - cx) ** 2 + (Y - ac) ** 2 <= half ** 2))

win = {t: arch(*bx) & on & (roof < 0) & (spire < 0) for t, bx in win_boxes.items()}
door = arch(*door_box) & on

rest = on & (roof < 0) & (spire < 0) & ~door
for m in win.values():
    rest &= ~m

# foundation: the stone band at the bottom; under the gate it starts at the steps
fy = np.where((X > 380) & (X < 646), 918, 872)
# drop stray specks (a few pixels in the picture's corners) so no part reaches into them
_lab, _n = ndi.label(rest)
_sz = ndi.sum(rest, _lab, range(1, _n + 1))
rest &= np.isin(_lab, 1 + np.where(_sz >= 10)[0])
found = rest & (Y >= fy)
# battlements of the outer towers and the keep's balcony
batt = {0: rest & (T == 0) & (Y >= 530) & (Y < 652), 4: rest & (T == 4) & (Y >= 530) & (Y < 652),
        2: rest & (T == 2) & (Y >= 458) & (Y < 590) & (X > 380) & (X < 645)}
walls = rest & ~found
for m in batt.values():
    walls &= ~m
split = np.where((T == 1) | (T == 3), 528, 458)       # inner towers and keep: upper and lower parts
lower = walls & (Y >= split)
upper = walls & (Y < split)
# above the outer towers' battlements there is only the roof's eave: give it to that tower's roof
eave = lower & ((T == 0) | (T == 4)) & (Y < 530)
roof = np.where(eave, T, roof)
lower &= ~eave

# ---------- slots, in building order ----------
# 15 foundation pieces left to right, cut at equal widths over the band
fx = np.where(found.any(0))[0]
edges = np.linspace(fx.min(), fx.max() + 1, 16).round().astype(int)
for k in range(15):
    slot('clay', found & (X >= edges[k]) & (X < edges[k + 1]))
for t in (0, 1, 2, 3, 4):
    slot('wall', lower & (T == t))
slot('door', door)
slot('window', win[0]); slot('window', win[4])
slot('floor', batt[0]); slot('floor', batt[4]); slot('floor', batt[2])
for t in (1, 2, 3):
    slot('wall', upper & (T == t))
slot('window', win[1]); slot('window', win[2]); slot('window', win[3])
for t in range(5):
    slot('roof', roof == t)
for t in range(5):
    slot('spire', spire == t)

# ---------- things only in the full picture ----------
aFm = aF > 0.5
diff = (np.abs(F - C).sum(2) > 70) & aFm
def blob(region, close=4):
    m = ndi.binary_opening(diff & region, iterations=1)
    m = ndi.binary_fill_holes(ndi.binary_closing(m, iterations=close)) & region & aFm
    lab, n = ndi.label(m)
    if n == 0: return m
    sizes = ndi.sum(np.ones_like(lab), lab, range(1, n + 1))
    return np.isin(lab, [i + 1 for i, sz in enumerate(sizes) if sz > 150])
def box(x0, x1, y0, y1): return (X >= x0) & (X <= x1) & (Y >= y0) & (Y <= y1)
def mirror(m): return m[:, ::-1] if W % 2 == 0 else m[:, ::-1]
CX = 512  # the picture's axis
# a porch: the canopy (a triangle under its little spire), its columns and door, and the steps
def porch_region(side):
    x0, x1, xs0, top = (140, 405, 148, 520)
    k = np.clip((Y - top) / (700 - top), 0, 1)
    pcx = (x0 + x1) / 2 + 3
    roofp = (Y >= top) & (Y < 700) & (np.abs(X - pcx) <= 26 + ((x1 - x0) / 2 - 26) * k)
    body = box(178, 395, 700, 870) | box(xs0, 352, 870, 978)
    m = (roofp | body) & aFm
    return m if side == 0 else np.roll(m[:, ::-1], 2 * CX - W + 1, axis=1)
porchL, porchR = porch_region(0), porch_region(1)
lantL = box(244, 300, 628, 732) & aFm & ~((F.min(2) > 200))
lantR = np.roll(lantL[:, ::-1], 2 * CX - W + 1, axis=1)
pots = np.zeros((H, W), bool)
for x0 in (141, 305, 649, 813):
    pots |= blob(box(x0, x0 + 66, 778, 874), 3)
knight = ndi.binary_dilation(blob(box(582, 708, 712, 930), 8), iterations=3) & aFm
princess = blob(box(446, 580, 318, 548), 4)
pots &= ~knight
porchL &= ~lantL & ~pots
porchR &= ~lantR & ~pots & ~knight
slot('porch', porchL, 'full'); slot('porch', porchR, 'full')
slot('lantern', lantL, 'full'); slot('lantern', lantR, 'full')
slot('knight', knight, 'full')
slot('princess', princess, 'full')

# ---------- write ----------
Cu, Fu = C.astype(np.uint8), F.astype(np.uint8)
def rgba(img, mask, alpha):
    o = np.zeros((H, W, 4), np.uint8); o[..., :3] = img; o[..., 3] = (np.where(mask, alpha, 0) * 255).astype(np.uint8)
    o[o[..., 3] == 0, :3] = 0   # clear colour under full transparency: much smaller files
    return o
GRAY = 0
for f in os.listdir(out):
    if f.startswith('c2_') and f.endswith('.png'): os.remove(os.path.join(out, f))
comp = Image.new('RGBA', (W, H))
taken = np.zeros((H, W), bool)
for i, (part, m, src) in enumerate(SLOTS):
    img, al = (Cu, aC) if src == 'clean' else (Fu, aF)
    if part == 'clay':
        # grey rough stone: the same shading, without the warm colour
        lum = (0.3 * img[..., 0] + 0.59 * img[..., 1] + 0.11 * img[..., 2])
        gimg = np.clip(np.stack([lum * 0.78, lum * 0.8, lum * 0.84], -1), 0, 255).astype(np.uint8)
        img = gimg
    if src == 'clean':
        m = m & ~taken
    taken |= m
    draw = m
    if src == 'clean':
        # overlap neighbours by 2 px so no seam of background shows between pieces;
        # walls don't spill onto the grey foundation (it is recoloured)
        grow = ndi.binary_dilation(m, iterations=2) & on
        if part != 'clay':
            grow &= ~found
        draw = m | grow
    layer = Image.fromarray(rgba(img, draw, al))
    layer.save(f'{out}/c2_{i:02d}_{part}.png', optimize=True)
    rim = m & ~ndi.binary_erosion(m, iterations=3)
    o = np.zeros((H, W, 4), np.uint8); o[rim] = [70, 110, 170, 220]
    Image.fromarray(o).save(f'{out}/c2_line_{i:02d}.png', optimize=True)
    if part == 'porch' and i == next(j for j, s in enumerate(SLOTS) if s[0] == 'porch'):
        comp.alpha_composite(Image.fromarray(rgba(Fu, pots, aF)))
    comp.alpha_composite(layer)
Image.fromarray(rgba(Fu, pots, aF)).save(f'{out}/c2_decor.png', optimize=True)
# blueprint: the clean castle faded, like c1_base
base = rgba(Cu, on, aC); base[..., 3] = (base[..., 3] * 0.25).astype(np.uint8)
Image.fromarray(base).save(f'{out}/c2_base.png', optimize=True)
comp.save(f'{out}/castle_2_full.png', optimize=True)
left = on & ~taken
print('slots', len(SLOTS), 'unassigned px', int(left.sum()))
print(' '.join(s[0] for s in SLOTS))
if len(sys.argv) > 4:
    rng = np.random.default_rng(3)
    pal = rng.integers(60, 255, (len(SLOTS), 3))
    vis = np.full((H, W, 3), 255, np.uint8)
    for i, (part, m, src) in enumerate(SLOTS):
        a = np.array(Image.open(f'{out}/c2_{i:02d}_{part}.png'))[..., 3] > 60
        vis[a] = pal[i]
    vis[left & (aC > 0.5)] = [255, 0, 255]
    Image.fromarray(vis).save(sys.argv[4])

# Cut castle 1 into 17 building steps, bottom-up, from two pictures of the same tower:
#   castle_1_full.png      the finished tower (porch and lantern are taken from it)
#   castle_1_noporch.png   the same tower redrawn without the porch (everything else is taken from it)
# Old layer files give the slot shapes; the windows are re-placed to where they are in the new picture.
# Usage: python3 recut_castle_1_v3.py <castle_1 art folder> <noporch.png (transparent)> <output folder>
import sys, glob, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

src, noporch, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
names = sorted(glob.glob(src + '/c1_[0-9]*.png'))
full = np.array(Image.open(src + '/castle_1_full.png').convert('RGBA')).astype(np.float32)
new = np.array(Image.open(noporch).convert('RGBA')).astype(np.float32)
H, W = full.shape[:2]
Y, X = np.mgrid[0:H, 0:W]
old = np.full((H, W), -1)
for i, f in enumerate(names):
    old[np.array(Image.open(f))[:, :, 3] > 0] = i

CLAY, WALLS, FLOORS = [0, 1, 2, 3], [4, 5, 6, 7], [8, 9]
ROOF, TILES, WIN1, WIN2, PORCH, SPIRE, LANTERN = 10, 11, 12, 13, 14, 15, 16

def lum(img):
    return (0.3 * img[:, :, 0] + 0.59 * img[:, :, 1] + 0.11 * img[:, :, 2]) / 255.0

# ---------- porch and lantern: from the full picture ----------
r, g, b = full[:, :, 0], full[:, :, 1], full[:, :, 2]
sat = (np.max(full[:, :, :3], 2) - np.min(full[:, :, :3], 2)) / 255.0
porch = np.isin(old, [11, 14])
porch |= np.isin(old, WALLS) & (r > 170) & (g < 120) & (Y > 530) & (Y < 670) & (X > 495)
porch &= ~((old == 14) & (X < 536) & (lum(full) > 0.55) & (sat < 0.25))
porch &= full[:, :, 3] > 0
lantern = (old == LANTERN) & (full[:, :, 3] > 0)

# ---------- bushes: green in the new picture, always drawn ----------
nr, ng, nb, na = new[:, :, 0], new[:, :, 1], new[:, :, 2], new[:, :, 3]
green = (ng > nr + 20) & (ng > nb + 10) & (na > 0)
green = ndi.binary_opening(green, iterations=1)
cc, n = ndi.label(green)
sizes = ndi.sum(np.ones_like(cc), cc, range(1, n + 1))
green = np.isin(cc, [i + 1 for i, s in enumerate(sizes) if s > 300])
bush = ndi.binary_dilation(green, iterations=3) & (na > 0)   # take in the dark outline

# ---------- labels on the new picture ----------
lab = np.where((na > 0) & (old >= 0) & ~np.isin(old, [11, 14, WIN1, WIN2, LANTERN]), old, -1)
# windows: move the old window shapes onto the glass of the new picture
def glass(img):
    return (img[:, :, 0] > 190) & (img[:, :, 1] > 120) & (img[:, :, 2] < 110) & (img[:, :, 0] - img[:, :, 2] > 110) & (img[:, :, 3] > 0)
gn = glass(new)
for w in (WIN1, WIN2):
    shape = old == w
    # the glass in the new picture near where this window was
    cand, k = ndi.label(ndi.binary_closing(gn, iterations=6) & ndi.binary_dilation(shape, iterations=40))
    big = np.argmax(ndi.sum(np.ones_like(cand), cand, range(1, k + 1))) + 1
    pane = ndi.binary_fill_holes(ndi.binary_closing(cand == big, iterations=10))
    ys, xs = np.where(pane)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    # stone arch and jambs around the glass, and the sill under it
    # a clean arched opening: glass plus a stone frame of about 32 px, then the wider sill
    cx, half = (x0 + x1) / 2, (x1 - x0) / 2 + 32
    ac = y0 - 32 + half
    win = ((Y >= ac) & (np.abs(X - cx) <= half) & (Y <= y1 + 14)) | ((X - cx) ** 2 + (Y - ac) ** 2 <= half ** 2)
    win |= (Y >= y1) & (Y <= y1 + 38) & (np.abs(X - cx) <= half + 10)
    win &= na > 0
    print('window', w, 'glass', x0, x1, y0, y1)
    lab[win] = w
# everything else on the tower takes the nearest label
# (orange belongs to the foundation, the rest to the wall rows)
unl = (na > 0) & (lab < 0) & ~bush
orange = (nr > ng + 60) & (nr > 180) & (Y > 820)
for group, sel in ((CLAY, unl & orange), (WALLS, unl & ~orange)):
    idx = ndi.distance_transform_edt(~np.isin(lab, group), return_distances=False, return_indices=True)
    lab[sel] = lab[idx[0][sel], idx[1][sel]]
lab[bush] = -1

# main roof: lower rows «крыша», upper rows «черепица», along the darkest seam between tile rows
L = lum(new)
roof = lab == ROOF
seam = np.full(W, 262.0)
for x in np.where(roof.any(0))[0]:
    lo, hi = 244, 280
    seg = L[lo:hi, x] + 0.004 * np.abs(np.arange(lo, hi) - 262)
    seam[x] = lo + int(seg.argmin())
seam = ndi.median_filter(seam, size=15)
lab[roof & (Y < seam[None, :])] = TILES

# clean specks
for it in range(2):
    for i in range(17):
        cc, n = ndi.label(lab == i)
        if n <= 1:
            continue
        sizes = ndi.sum(np.ones_like(cc), cc, range(1, n + 1))
        keepc = int(np.argmax(sizes)) + 1
        for j in range(1, n + 1):
            if j == keepc or sizes[j - 1] > 1500:
                continue
            m = cc == j
            ring = ndi.binary_dilation(m, iterations=2) & ~m
            nbh = lab[ring]
            nbh = nbh[nbh >= 0]
            if len(nbh):
                lab[m] = np.bincount(nbh).argmax()

# ---------- write ----------
newu, fullu = new.astype(np.uint8), full.astype(np.uint8)
comp = Image.new('RGBA', (W, H), (0, 0, 0, 0))
shapes = {}
for i, f in enumerate(names):
    name = os.path.basename(f)
    if i == PORCH:
        m, base = porch, fullu
    elif i == LANTERN:
        m, base = lantern, fullu
    else:
        m, base = lab == i, newu
    im = base.copy(); im[:, :, 3] = np.where(m, base[:, :, 3], 0)
    shapes[i] = m
    Image.fromarray(im).save(out + '/' + name, optimize=True)
decor = newu.copy(); decor[:, :, 3] = np.where(bush, newu[:, :, 3], 0)
Image.fromarray(decor).save(out + '/c1_decor.png', optimize=True)
# bushes stand in front of the walls but behind the porch, like CastleArt draws them
for i in range(17):
    if i == PORCH:
        comp.alpha_composite(Image.fromarray(decor))
    comp.alpha_composite(Image.open(out + '/' + os.path.basename(names[i])))
comp.save(out + '/castle_1_full.png', optimize=True)
for i in range(17):
    m = shapes[i]
    rim = m & ~ndi.binary_erosion(m, iterations=3)
    o = np.zeros((H, W, 4), np.uint8); o[rim] = [70, 110, 170, 220]
    Image.fromarray(o).save(out + '/c1_line_%02d.png' % i, optimize=True)
print('done')

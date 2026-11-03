# Shared helpers for cutting a building picture into its layers (see cut_house_3.py).
import os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi


def cutout(img, snow=False, open_holes=0):
    """Alpha from the white background: flood from the border over near-white, then a soft edge.
    snow=True is for white snowy buildings: only near-pure white is background, and no shadow band.
    open_holes=N: closed patches of white bigger than N pixels (sky seen through a tree crown) are background too."""
    mn, mx = img.min(2), img.max(2)
    white = (mn > (247 if snow else 236)) & (mx - mn < (8 if snow else 22))
    # the soft grey-white shadow under the base counts as background too, so no light fringe stays there
    H = img.shape[0]
    low = np.zeros_like(white); low[int(H * 0.8):] = True
    if not snow:
        white |= low & (mn > 214) & (mx - mn < 16)
    lab, _ = ndi.label(white)
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    bg = np.isin(lab, border[border > 0])
    if open_holes:
        sizes = ndi.sum(np.ones_like(lab), lab, range(1, lab.max() + 1))
        big = 1 + np.flatnonzero(sizes > open_holes)
        bg |= np.isin(lab, big)
    bg = ndi.binary_opening(bg, iterations=1)
    fg = ~bg
    fg = ndi.binary_fill_holes(fg) & fg | ndi.binary_fill_holes(fg) & ~ndi.binary_dilation(bg, iterations=1)
    a = ndi.gaussian_filter(fg.astype(float), 0.7)
    a[ndi.binary_erosion(fg, iterations=1)] = 1
    return np.clip(a, 0, 1)


def arch(X, Y, x0, x1, y0, y1):
    """A door or window shape: a rectangle with a round top."""
    cx, half = (x0 + x1) / 2, (x1 - x0) / 2
    ac = y0 + half
    return ((Y >= ac) & (np.abs(X - cx) <= half) & (Y <= y1)) | ((X - cx) ** 2 + (Y - ac) ** 2 <= half ** 2)


def _rgba(img, mask, alpha):
    H, W = mask.shape
    o = np.zeros((H, W, 4), np.uint8)
    o[..., :3] = img
    o[..., 3] = (np.where(mask, alpha, 0) * 255).astype(np.uint8)
    o[o[..., 3] == 0, :3] = 0
    return o


def split_even(found, X, pieces):
    """Masks of `pieces` foundation pieces, left to right, each with about the same number of pixels."""
    cols = np.cumsum(found.sum(0))
    edges = [0] + [int(np.searchsorted(cols, cols[-1] * k / pieces)) for k in range(1, pieces)] + [found.shape[1]]
    return [found & (X >= edges[k]) & (X < edges[k + 1]) for k in range(pieces)]


def settle(slots, found):
    """Bits of a step that would hang in the air (not joined to the foundation through what is built so far)
    move on to the next step of the house, until they have something to hang on. A step that would float
    whole stays as it is."""
    slots = list(slots)
    acc = found.copy()
    eight = np.ones((3, 3), bool)
    for i, (part, m, src) in enumerate(slots):
        if part == 'clay' or src != 'clean':
            acc |= m
            continue
        lab, _ = ndi.label(acc | m, structure=eight)
        grounded = np.isin(lab, np.unique(lab[found & (lab > 0)]))
        loose = m & ~grounded
        j = next((k for k in range(i + 1, len(slots)) if slots[k][2] == 'clean' and slots[k][0] != 'clay'), None)
        if loose.any() and (m & ~loose).any() and j is not None:
            slots[i] = (part, m & ~loose, src)
            slots[j] = (slots[j][0], slots[j][1] | loose, slots[j][2])
        acc |= slots[i][1]
    return slots


def write_castle(out, prefix, castle_id, slots, C, F, aC, aF, on, found, preview=None, decor=None, grey_clay=True):
    """Writes <prefix>_NN_<part>.png layers, outlines, base, decor (from the clean picture) and the full picture.
    slots: (part, mask, 'clean'|'full') in building order. 'clay' pieces are recoloured to grey stone unless grey_clay is False."""
    os.makedirs(out, exist_ok=True)
    H, W = on.shape
    for f in os.listdir(out):
        if f.startswith(prefix + '_') and f.endswith('.png'):
            os.remove(os.path.join(out, f))
    Cu, Fu = C.astype(np.uint8), F.astype(np.uint8)
    comp = Image.new('RGBA', (W, H))
    taken = np.zeros((H, W), bool)
    first_porch = next((i for i, s in enumerate(slots) if s[0] == 'porch'), None)
    decor = decor if decor is not None else np.zeros((H, W), bool)
    for i, (part, m, src) in enumerate(slots):
        img, al = (Cu, aC) if src == 'clean' else (Fu, aF)
        if part == 'clay' and grey_clay:
            lum = 0.3 * img[..., 0] + 0.59 * img[..., 1] + 0.11 * img[..., 2]
            img = np.clip(np.stack([lum * 0.78, lum * 0.8, lum * 0.84], -1), 0, 255).astype(np.uint8)
        if src == 'clean':
            m = m & ~taken
        taken |= m
        draw = m
        if src == 'clean':
            grow = ndi.binary_dilation(m, iterations=3) & on
            if part != 'clay':
                grow &= ~found
            draw = m | grow
        layer = Image.fromarray(_rgba(img, draw, al))
        layer.save(f'{out}/{prefix}_{i:02d}_{part}.png', optimize=True)
        rim = m & ~ndi.binary_erosion(m, iterations=3)
        o = np.zeros((H, W, 4), np.uint8)
        o[rim] = [70, 110, 170, 220]
        Image.fromarray(o).save(f'{out}/{prefix}_line_{i:02d}.png', optimize=True)
        if i == first_porch:
            comp.alpha_composite(Image.fromarray(_rgba(Cu, decor, aC)))
        comp.alpha_composite(layer)
    Image.fromarray(_rgba(Cu, decor, aC)).save(f'{out}/{prefix}_decor.png', optimize=True)
    base = _rgba(Cu, on, aC)
    base[..., 3] = (base[..., 3] * 0.25).astype(np.uint8)
    Image.fromarray(base).save(f'{out}/{prefix}_base.png', optimize=True)
    comp.save(f'{out}/{castle_id}_full.png', optimize=True)
    left = on & ~taken
    print('slots', len(slots), 'unassigned px', int(left.sum()))
    print(' '.join(s[0] for s in slots))
    if preview:
        rng = np.random.default_rng(3)
        pal = rng.integers(60, 255, (len(slots), 3))
        vis = np.full((H, W, 3), 255, np.uint8)
        for i, (part, m, src) in enumerate(slots):
            a = np.array(Image.open(f'{out}/{prefix}_{i:02d}_{part}.png'))[..., 3] > 60
            vis[a] = pal[i]
        vis[left & (aC > 0.5)] = [255, 0, 255]
        Image.fromarray(vis).save(preview)


class Shapes:
    """Shape masks on an H×W picture, in picture pixels."""

    def __init__(self, H, W):
        self.Y, self.X = np.mgrid[0:H, 0:W]

    def box(self, x0, x1, y0, y1):
        X, Y = self.X, self.Y
        return (X >= x0) & (X <= x1) & (Y >= y0) & (Y <= y1)

    def ell(self, cx, cy, rx, ry):
        return ((self.X - cx) / rx) ** 2 + ((self.Y - cy) / ry) ** 2 <= 1

    def arch(self, x0, x1, y0, y1):
        return arch(self.X, self.Y, x0, x1, y0, y1)

    def poly(self, *pts):
        """Inside a convex polygon given as (x, y) points in order."""
        X, Y = self.X, self.Y
        m = np.ones(X.shape, bool)
        n = len(pts)
        sign = None
        for i in range(n):
            (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
            c = (x1 - x0) * (Y - y0) - (y1 - y0) * (X - x0)
            m &= c >= 0
        if not m.any():   # other winding
            m = np.ones(X.shape, bool)
            for i in range(n):
                (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
                m &= (x1 - x0) * (Y - y0) - (y1 - y0) * (X - x0) <= 0
        return m


def claim(on, regions):
    """regions: (name, mask) in priority order; each pixel of `on` goes to the first region holding it.
    Returns {name: mask} (names may repeat: their masks are OR-ed) and what is left."""
    out, left = {}, on.copy()
    for name, m in regions:
        got = m & left
        out[name] = out.get(name, np.zeros_like(on)) | got
        left &= ~got
    return out, left


def full_only(F, C, aF, close=4):
    """Pixels that the full picture adds over the clean one (people, lanterns, steps)."""
    aFm = aF > 0.5
    d = (np.abs(F - C).sum(2) > 60) & aFm
    return ndi.binary_fill_holes(ndi.binary_closing(ndi.binary_opening(d, iterations=2), iterations=close)) & aFm

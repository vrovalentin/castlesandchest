# Helpers for cutting a building by its drawing (see cut_house_10.py, the first one cut this way):
# every part is a shape drawn around the real thing in the picture (pointed or round windows with their
# frames, roofs with their eaves, ledges), parts claim pixels in priority order, leftovers go to the nearest
# part, and a part overlaps only the parts built before it, so the hole it leaves keeps its shape.
import os
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from cutlib import load_pair, write_castle, split_even, Shapes, full_only


class Cut:
    def __init__(self, clean_p, full_p, **cutout_args):
        self.C, self.F, self.aC, self.aF = load_pair(clean_p, full_p, **cutout_args)
        self.H, self.W = self.C.shape[:2]
        self.S = Shapes(self.H, self.W)
        self.X, self.Y = self.S.X, self.S.Y
        self.on = self.aC > 0.02

    # ---- shapes ----
    def box(self, x0, x1, y0, y1):
        return self.S.box(x0, x1, y0, y1)

    def poly(self, *pts):
        return self.S.poly(*pts)

    def ell(self, cx, cy, rx, ry):
        return self.S.ell(cx, cy, rx, ry)

    def gothic(self, x0, x1, ya, yb, k=0.85):
        """A pointed window or door, frame included: sides x0..x1, apex at ya, bottom at yb."""
        X, Y = self.X, self.Y
        w = x1 - x0
        ys = ya + k * w
        h = ys - ya
        r = (w * w / 4 + h * h) / w
        inL = (X - (x0 + r)) ** 2 + (Y - ys) ** 2 <= r * r
        inR = (X - (x1 - r)) ** 2 + (Y - ys) ** 2 <= r * r
        return ((Y >= ys) & (Y <= yb) & (X >= x0) & (X <= x1)) | ((Y < ys) & (Y >= ya) & inL & inR)

    def round(self, x0, x1, ya, yb, k=1.0):
        """A round-topped window or door, frame included: sides x0..x1, top at ya, bottom at yb.
        k < 1 flattens the arch (an ellipse k times as tall as a half circle)."""
        X, Y = self.X, self.Y
        cx, rx = (x0 + x1) / 2, (x1 - x0) / 2
        ry = rx * k
        yc = ya + ry
        return ((Y >= yc) & (Y <= yb) & (X >= x0) & (X <= x1)) | ((((X - cx) / rx) ** 2 + ((Y - yc) / ry) ** 2 <= 1) & (Y < yc))

    def below(self, pts):
        """Everything under a polyline through (x, y) points."""
        xs, ys = zip(*pts)
        return self.Y >= np.interp(np.arange(self.W), xs, ys)[None, :]

    def color(self, kind):
        C = self.C
        r, g, b = C[..., 0], C[..., 1], C[..., 2]
        mn, mx = C.min(2), C.max(2)
        if kind == 'light':
            return mn > 205
        if kind == 'green':
            return (g > r + 10) & (g > b + 10)
        if kind == 'red':
            return (r > 150) & (r > g + 70) & (r > b + 70)
        if kind == 'glass':      # warm lit window glass
            return (r > 200) & (g > 130) & (b < 120) & (r - b > 100)
        raise ValueError(kind)

    def hanging(self, x0, x1, y0, y1, thr=205):
        """Light pixels in a band under an edge that hang from it (icicles, drips)."""
        band = self.box(x0, x1, y0, y1) & (self.C.min(2) > thr)
        lab, _ = ndi.label(band | self.box(x0, x1, y0 - 3, y0 - 1))
        top = np.unique(lab[y0 - 2, x0:x1 + 1])
        return band & np.isin(lab, top[top > 0])

    # ---- claiming ----
    def claim(self, order, on=None):
        """order: (name, mask) in priority order. Each picture pixel goes to the first shape holding it,
        the rest to the nearest named part. Returns {name: mask}."""
        on = self.on if on is None else on
        R, left = {}, on.copy()
        for name, m in order:
            assert name not in R, f'two shapes named {name}'
            R[name] = m & left
            left &= ~R[name]
        names = [n for n, _ in order]
        idx = np.zeros((self.H, self.W), np.int32)
        for i, n in enumerate(names):
            idx[R[n]] = i + 1
        if left.any():
            _, (iy, ix) = ndi.distance_transform_edt(idx == 0, return_indices=True)
            near = idx[iy, ix]
            for i, n in enumerate(names):
                R[n] |= left & (near == i + 1)
        self.R, self.names = R, names
        return R

    def merge(self, into, *names):
        for n in names:
            self.R[into] |= self.R.pop(n)
            self.names.remove(n)

    def full_only(self, close=4):
        return full_only(self.F, self.C, self.aF, close)

    # ---- output ----
    def write(self, out, prefix, castle_id, found_key, clay, build, extra=(), decor=None, grey_clay=False, decor_full=False):
        """build: (part, region name) in building order after `clay` foundation pieces; extra: (part, mask)
        taken from the full picture (porch, lanterns, figure)."""
        R = self.R
        used = [k for _, k in build] + [found_key]
        missing = [n for n in self.names if n not in used]
        assert not missing, f'regions not built: {missing}'
        X = self.X
        slots = [('clay', m, 'clean') for m in split_even(R[found_key], X, clay)]
        slots += [(p, R[k], 'clean') for p, k in build]
        slots += [(p, m, 'full') for p, m in extra]
        write_castle(out, prefix, castle_id, slots, self.C, self.F, self.aC, self.aF, self.on, R[found_key],
                     decor=decor, grey_clay=grey_clay, grow_back=True, decor_full=decor_full)
        return [s[0] for s in slots]


def overlay(cut, path, scale=1):
    """The claimed regions outlined in red on the clean picture, with their names."""
    lab = np.zeros((cut.H, cut.W), int)
    for i, n in enumerate(cut.names):
        lab[cut.R[n]] = i + 1
    v = cut.C.astype(np.uint8).copy()
    e = (ndi.grey_dilation(lab, 3) != ndi.grey_erosion(lab, 3)) & (lab > 0)
    v[e] = [255, 0, 0]
    im = Image.fromarray(v)
    d = ImageDraw.Draw(im)
    for i, n in enumerate(cut.names):
        if cut.R[n].any():
            y, x = ndi.center_of_mass(cut.R[n])
            d.text((x - 8, y - 5), n, fill=(150, 0, 150))
    if scale != 1:
        im = im.resize((cut.W * scale, cut.H * scale), Image.NEAREST)
    im.save(path)


def stages(folder, prefix, path, steps=None, bg=(150, 200, 240, 255), T=400):
    import glob
    fs = sorted(glob.glob(f'{folder}/{prefix}_[0-9][0-9]_*.png'))
    n = len(fs)
    steps = steps or [round(n * k / 8) for k in range(1, 9)]
    base = Image.open(f'{folder}/{prefix}_base.png')
    decor = Image.open(f'{folder}/{prefix}_decor.png')
    sheet = Image.new('RGB', (T * 4, T * ((len(steps) + 3) // 4)))
    for k, s in enumerate(steps):
        im = Image.new('RGBA', base.size, bg)
        im.alpha_composite(base)
        im.alpha_composite(decor)
        for f in fs[:s]:
            im.alpha_composite(Image.open(f))
        sheet.paste(im.convert('RGB').resize((T, T), Image.LANCZOS), ((k % 4) * T, (k // 4) * T))
    sheet.save(path)

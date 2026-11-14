# Finds where a figure drawn on its own (on white) stood in the full building picture: the scale and offset
# at which its colours match the full picture best, near the figure that the two pictures' difference shows.
# Then puts it onto the full picture with the old figure painted out (from the clean picture), and writes
#   <full>_v2.png            the full picture with the new figure
#   <full>_v2_<name>.png     the new figure alone, on the building's canvas (its alpha is the cut mask)
# Usage: python3 align_figures.py <clean.png> <full.png> <figure.png> <name> x0,y0,x1,y1 [old=layer.png@x,y] [behind=mask.png] [fixed=s,ox,oy] [fill=all]
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from place_figures import cut_white


def old_figure(C, F, box):
    x0, y0, x1, y1 = box
    d = np.abs(C - F).max(2) > 40
    m = np.zeros(d.shape, bool); m[y0:y1, x0:x1] = d[y0:y1, x0:x1]
    m = ndi.binary_opening(m, iterations=1)
    lab, n = ndi.label(m)
    if n: m = np.isin(lab, 1 + np.flatnonzero(ndi.sum(m, lab, range(1, n + 1)) > 150))
    return ndi.binary_fill_holes(ndi.binary_closing(m, iterations=3))


def place(fig, s, ox, oy, W, H):
    im = fig.convert('RGBa').transform((W, H), Image.AFFINE, (1 / s, 0, -ox / s, 0, 1 / s, -oy / s), resample=Image.BICUBIC)
    return np.array(im.convert('RGBA'))


def search(fig, F, old, k=3):
    """Coarse to fine: at 1/k size over scales and offsets, then refine at full size."""
    fa = np.array(fig); fb = np.array(fig.getchannel('A')) > 128
    ys, xs = np.nonzero(fb); fy0, fy1, fx0, fx1 = ys.min(), ys.max(), xs.min(), xs.max()
    oys, oxs = np.nonzero(old); oy0, oy1, ox0, ox1 = oys.min(), oys.max(), oxs.min(), oxs.max()
    s0 = np.sqrt(((oy1 - oy0) * (ox1 - ox0)) / ((fy1 - fy0) * (fx1 - fx0)))
    H, W = old.shape
    best = None
    for s in s0 * np.linspace(0.75, 1.3, 23):
        w, h = int(fig.width * s / k), int(fig.height * s / k)
        g = np.array(fig.resize((w, h), Image.BILINEAR)).astype(np.int32)
        gm = g[..., 3] > 200
        gy0, gy1 = int(fy0 * s / k), int(fy1 * s / k) + 1
        gx0, gx1 = int(fx0 * s / k), int(fx1 * s / k) + 1
        g = g[gy0:gy1, gx0:gx1, :3]; gm = gm[gy0:gy1, gx0:gx1]
        Fs = F[::k, ::k]; Os = old[::k, ::k]
        cy, cx = (oy0 + oy1) / 2 / k, (ox0 + ox1) / 2 / k
        hh, ww = gm.shape
        for ty in range(int(cy - hh / 2 - 25), int(cy - hh / 2 + 26), 1):
            for tx in range(int(cx - ww / 2 - 25), int(cx - ww / 2 + 26), 1):
                if ty < 0 or tx < 0 or ty + hh > Fs.shape[0] or tx + ww > Fs.shape[1]: continue
                win = Fs[ty:ty + hh, tx:tx + ww]
                e = np.abs(win - g).sum(2)[gm].mean()
                ow = Os[ty:ty + hh, tx:tx + ww]
                unc = (Os.sum() - (ow & gm).sum()) / max(Os.sum(), 1)   # old figure left uncovered
                sc = e + 120 * unc
                if best is None or sc < best[0]:
                    best = (sc, s, (tx - gx0) * k, (ty - gy0) * k)
    return best


if __name__ == '__main__':
    clean_p, full_p, fig_p, name, box = sys.argv[1:6]
    opts = dict(a.split('=', 1) for a in sys.argv[6:])
    C = np.array(Image.open(clean_p).convert('RGB')).astype(np.int32)
    F = np.array(Image.open(full_p).convert('RGB')).astype(np.int32)
    H, W = C.shape[:2]
    if 'old' in opts:   # the figure layer cut before: path@x,y on the building's canvas
        p, xy = opts['old'].split('@'); x, y = map(int, xy.split(','))
        im = Image.open(p)
        la = np.array(im.getchannel('A') if 'A' in im.getbands() else im.convert('L')) > 110
        old = np.zeros((H, W), bool); old[y:y + la.shape[0], x:x + la.shape[1]] = la
    else:
        old = old_figure(C, F, [int(v) for v in box.split(',')])
    fig = cut_white(fig_p, holes='holes' in opts)
    if 'fixed' in opts:
        s, ox, oy = map(float, opts['fixed'].split(','))
        print(name, 'fixed', s, ox, oy)
    else:
        sc, s, ox, oy = search(fig, F, old)
        print(name, 'score %.1f' % sc, 's=%.4f ox=%.1f oy=%.1f' % (s, ox, oy))
    a = place(fig, s, ox, oy, W, H)
    if 'behind' in opts:   # something of the building stands in front of the figure (a balustrade)
        a[..., 3] = np.minimum(a[..., 3], 255 - np.array(Image.open(opts['behind']).convert('L')))
    # The old figure goes: what the new one doesn't cover is filled in from around it (porch steps it sat on),
    # and where that fill comes out like the clean picture anyway, the clean picture is taken.
    import cv2
    rem = ndi.binary_dilation(old, iterations=4) & ~(a[..., 3] > 200)
    out = cv2.inpaint(F.astype(np.uint8)[..., ::-1].copy(), rem.astype(np.uint8) * 255, 6, cv2.INPAINT_TELEA)[..., ::-1].astype(np.int32)
    others = (np.abs(F - C).max(2) > 40) & ~ndi.binary_dilation(old, iterations=4)   # porch, lanterns: the other full parts
    others = ndi.binary_opening(others, iterations=2)
    like = rem & ~ndi.binary_dilation(others, iterations=10)
    if opts.get('fill') == 'all':   # the figure sat on the porch: all of what it leaves is porch
        like[:] = False
    out[like] = C[like]
    al = a[..., 3:4] / 255.0
    out = (out * (1 - al) + a[..., :3] * al).round().astype(np.uint8)
    base = full_p.replace('.png', '_v2.png')
    Image.fromarray(out).save(base)
    Image.fromarray(a).save(base.replace('.png', f'_{name}.png'))
    Image.fromarray((old * 255).astype(np.uint8)).save(base.replace('.png', f'_{name}_old.png'))

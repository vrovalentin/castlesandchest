# Puts objects drawn on their own (on white) onto a clean building picture: each is cut from its white
# background, scaled and moved to where it stood in the first picture (scale and offset found by matching).
# Later objects go behind earlier ones where a "behind" index is given (a figure standing behind a rail).
# Writes <out>.png (the full picture) and <out>_<name>.png (each object alone, on the building's canvas).
# Usage: python3 place_figures.py <clean.png> <out.png> name=src.png:scale:ox:oy[:ymax][:holes][:behind-name] ...
import sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi


def cut_white(p, holes=False):
    """holes=True: white patches enclosed by the object (sky between a rail's posts) are background too."""
    a = np.array(Image.open(p).convert('RGB')).astype(np.int32)
    mn, mx = a.min(2), a.max(2)
    lab, _ = ndi.label((mn > 240) & (mx - mn < 14))
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    bg = np.isin(lab, border[border > 0])
    if holes:
        sizes = ndi.sum(np.ones_like(lab), lab, range(1, lab.max() + 1))
        bg |= np.isin(lab, 1 + np.flatnonzero(sizes > 60))
    fg = ~bg
    lab, n = ndi.label(fg)
    fg = np.isin(lab, 1 + np.flatnonzero(ndi.sum(fg, lab, range(1, n + 1)) > 200))
    al = ndi.gaussian_filter(fg.astype(float), 0.8)
    al[ndi.binary_erosion(fg, iterations=1)] = 1
    al[~ndi.binary_dilation(fg, iterations=1)] = 0
    return Image.fromarray(np.dstack([a.astype(np.uint8), (al * 255).astype(np.uint8)]), 'RGBA')


def main():
    clean_p, out = sys.argv[1], sys.argv[2]
    base = Image.open(clean_p).convert('RGBA')
    W, H = base.size
    layers = {}
    for spec in sys.argv[3:]:
        name, rest = spec.split('=', 1)
        parts = rest.split(':')
        src, s, ox, oy = parts[0], float(parts[1]), float(parts[2]), float(parts[3])
        ymax = behind = None
        holes = False
        for p in parts[4:]:
            if p == 'holes':
                holes = True
            elif p.startswith('behind'):
                behind = p.split('-', 1)[1]
            elif p:
                ymax = int(p)
        im = cut_white(src, holes).convert('RGBa')
        im = im.transform((W, H), Image.AFFINE, (1 / s, 0, -ox / s, 0, 1 / s, -oy / s), resample=Image.BICUBIC).convert('RGBA')
        a = np.array(im)
        if ymax is not None:
            a[ymax:, :, 3] = 0
        if behind:
            a[..., 3] = np.minimum(a[..., 3], 255 - np.array(layers[behind])[..., 3])
        layers[name] = Image.fromarray(a)
        layers[name].save(out.replace('.png', f'_{name}.png'))
    # paint back to front: the later objects (behind) first
    for name in reversed(list(layers)):
        base.alpha_composite(layers[name])
    base.save(out)


if __name__ == '__main__':
    main()

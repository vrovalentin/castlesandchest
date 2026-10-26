# Remove the white background from images (blocks, chests, part icons), crop square to the object and save PNGs.
# Usage: python3 cut_blocks.py <input folder> <output folder> [file prefix, default block_] [size, default 256]
import sys, glob, os, numpy as np
from PIL import Image
from scipy import ndimage as ndi
src, out = sys.argv[1], sys.argv[2]
prefix = sys.argv[3] if len(sys.argv) > 3 else 'block_'
size = int(sys.argv[4]) if len(sys.argv) > 4 else 256
for f in sorted(glob.glob(os.path.join(src, prefix + '*.png'))):
    a = np.array(Image.open(f).convert('RGB')).astype(np.float32)
    white = a.min(2)                       # how white a pixel is
    grey = a.max(2) - white                # 0 for white and grey, high for colours
    # Background candidates: white, or the light grey of a fake transparency checkerboard
    # that some generators bake into the picture.
    near = (white > 232) | ((white > 200) & (grey < 12))
    if os.environ.get('LOOSE'):            # also take soft grey shadows under the object
        near |= (white > 150) & (grey < 16)
    lab, n = ndi.label(near)
    border = set(np.unique(np.r_[lab[0], lab[-1], lab[:, 0], lab[:, -1]])) - {0}
    bg = np.isin(lab, list(border))
    # Enclosed holes (e.g. under an arch) that show the checkerboard: both its light and grey cells.
    sizes = ndi.sum(np.ones_like(white), lab, index=np.arange(1, n + 1))
    for k in np.nonzero(sizes > white.size * 0.005)[0] + 1:
        if k in border: continue
        v = white[lab == k]
        if (v > 245).mean() > 0.2 and ((v > 205) & (v < 238)).mean() > 0.2: bg |= lab == k
    # soft edge: alpha ramps over 2 px from the background
    dist = ndi.distance_transform_edt(~bg)
    alpha = np.clip(dist / 2.0, 0, 1)
    # remove the white halo on edge pixels (unmix from white)
    al = alpha[..., None]
    rgb = np.where(al > 0, (a - 255 * (1 - al)) / np.maximum(al, 1e-3), 0)
    rgba = np.dstack([np.clip(rgb, 0, 255), alpha * 255]).astype(np.uint8)
    ys, xs = np.where(alpha > 0)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    side = max(y1 - y0, x1 - x0)
    cy, cx = (y0 + y1) // 2, (x0 + x1) // 2
    im = Image.fromarray(rgba).crop((cx - side // 2, cy - side // 2, cx - side // 2 + side, cy - side // 2 + side))
    im.resize((size, size), Image.LANCZOS).save(os.path.join(out, os.path.basename(f)))
    print(os.path.basename(f), 'block', x1 - x0, 'x', y1 - y0)

# Remove the white background from block images, crop to the block and save 256x256 PNGs.
# Usage: python3 cut_blocks.py <input folder> <output folder>
import sys, glob, os, numpy as np
from PIL import Image
from scipy import ndimage as ndi
src, out = sys.argv[1], sys.argv[2]
for f in sorted(glob.glob(os.path.join(src, 'block_*.png'))):
    a = np.array(Image.open(f).convert('RGB')).astype(np.float32)
    white = a.min(2)                       # how white a pixel is
    near = white > 232                     # background candidates
    lab, _ = ndi.label(near)
    border = set(np.unique(np.r_[lab[0], lab[-1], lab[:, 0], lab[:, -1]])) - {0}
    bg = np.isin(lab, list(border))
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
    im.resize((256, 256), Image.LANCZOS).save(os.path.join(out, os.path.basename(f)))
    print(os.path.basename(f), 'block', x1 - x0, 'x', y1 - y0)

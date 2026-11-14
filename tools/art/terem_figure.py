# The terem's full picture with Emelya cut cleanly: the figure picture (Emelya on his stove, the lanterns and the
# sled on white, drawn on its own) is cut from its white background, scaled and moved onto the clean terem where
# the figure stood in the first picture (found by matching the stove), so the full picture differs from the
# clean one only by the figure.
# Usage: python3 terem_figure.py <terem_clean.png> <emelya_src.png> <out terem_full.png>
import sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

clean_p, fig_p, out = sys.argv[1:4]
S, OX, OY = 0.59, 269.6, 371.7      # scale and offset of the figure picture, matched on the stove

fig = np.array(Image.open(fig_p).convert('RGB')).astype(np.int32)
mn, mx = fig.min(2), fig.max(2)
lab, _ = ndi.label((mn > 240) & (mx - mn < 14))
border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
fg = ~np.isin(lab, border[border > 0])
lab, n = ndi.label(fg)
sizes = ndi.sum(fg, lab, range(1, n + 1))
fg = np.isin(lab, 1 + np.flatnonzero(sizes > 500))
a = ndi.gaussian_filter(fg.astype(float), 0.8)
a[ndi.binary_erosion(fg, iterations=1)] = 1
a[~ndi.binary_dilation(fg, iterations=1)] = 0
rgba = np.dstack([fig.astype(np.uint8), (a * 255).astype(np.uint8)])
im = Image.fromarray(rgba, 'RGBA').convert('RGBa')
W, H = im.size
im = im.transform((1024, 1024), Image.AFFINE, (1 / S, 0, -OX / S, 0, 1 / S, -OY / S), resample=Image.BICUBIC).convert('RGBA')
base = Image.open(clean_p).convert('RGBA')
base.alpha_composite(im)
base.save(out)
im.save(out.replace('.png', '_figure.png'))

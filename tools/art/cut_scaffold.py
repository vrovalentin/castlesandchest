# Cuts the scaffolding kit (generated on a white background) for the build screen:
#   section.png (one storey: two posts, deck on top, a brace), top.png (deck with a rail), ladder.png,
#   debris1..3.png (piles at the foot of the building).
# White that touches the border, and large white areas inside (between ladder rungs, under the deck) go
# transparent; small white highlights stay. Edges are softened and the white fringe is taken out.
# Each piece is trimmed to its picture and scaled so its longer side is at most MAX px.
# Usage: python3 cut_scaffold.py <folder with the sources> <output folder>
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage

MAX = 512
src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)


def cut(path):
    im = np.asarray(Image.open(path).convert('RGB'), np.float32)
    lo, hi = im.min(2), im.max(2)
    white = (lo > 236) & (hi - lo < 14)
    lab, n = ndimage.label(white)
    sizes = ndimage.sum(white, lab, range(1, n + 1))
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    keep = np.zeros(n + 1, bool)
    for i, s in enumerate(sizes, 1):
        keep[i] = i in border or s > 1500
    bg = keep[lab]
    # soft edge: how far into the object, in px
    dist = ndimage.distance_transform_edt(~bg)
    a = np.clip((dist - 0.5) / 1.5, 0, 1)
    # remove the white the edge pixels were blended with
    k = np.maximum(a, 1e-3)[..., None]
    rgb = np.clip((im - (1 - k) * 255) / k, 0, 255)
    rgb = np.where(a[..., None] > 0, rgb, 0)
    img = Image.fromarray(np.dstack([rgb, a * 255]).astype(np.uint8), 'RGBA')
    img = img.crop(img.getchannel('A').getbbox())
    s = min(1, MAX / max(img.size))
    if s < 1:
        img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    return img


for name in ('section', 'top', 'ladder', 'debris1', 'debris2', 'debris3'):
    p = os.path.join(src, name + '.png')
    if os.path.exists(p):
        img = cut(p)
        img.save(os.path.join(out, f'scaff_{name}.png'), optimize=True)
        print(name, img.size)

# Cuts the scaffolding kit (generated on a white background) for the build screen:
#   section.png (one storey: two posts, deck on top, a brace), top.png (deck with a rail), ladder.png,
#   debris1..3.png (piles at the foot of the building), jib.png (crane arm with a pulley; flipped so the pulley is on the left),
#   crate.png / crate_empty.png (a crate on short ropes, with stones and empty), rope.png.
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
    # the background: the typical colour along the border (white, or a light paper tint)
    edge = np.concatenate([im[0], im[-1], im[:, 0], im[:, -1]])
    bgc = np.median(edge, 0)
    white = np.abs(im - bgc).max(2) < 16
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
    rgb = np.clip((im - (1 - k) * bgc) / k, 0, 255)
    rgb = np.where(a[..., None] > 0, rgb, 0)
    img = Image.fromarray(np.dstack([rgb, a * 255]).astype(np.uint8), 'RGBA')
    img = img.crop(img.getchannel('A').getbbox())
    s = min(1, MAX / max(img.size))
    if s < 1:
        img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    return img


def warm(img, sat, val):
    """Brings realistic grey-brown wood closer to the glossy orange wood of the kit: browns get more colour and
    light; greys (stones, iron) stay as they are."""
    a = img.getchannel('A')
    h, s_, v = [np.asarray(c, np.float32) for c in img.convert('RGB').convert('HSV').split()]
    brown = np.clip((s_ - 25) / 30, 0, 1) * np.clip(1 - np.abs(h - 20) / 20, 0, 1)
    s_ = np.clip(s_ * (1 + (sat - 1) * brown), 0, 255)
    v = np.clip(v * (1 + (val - 1) * brown), 0, 255)
    h = h + (17 - h) * brown * 0.5
    out = Image.merge('HSV', [Image.fromarray(c.astype(np.uint8)) for c in (h, s_, v)]).convert('RGB')
    out.putalpha(a)
    return out


WARM = {'jib': (2.1, 1.2), 'crate': (2.0, 1.2), 'crate_empty': (2.0, 1.2), 'rope': (1.3, 1.05)}

for name in ('section', 'top', 'ladder', 'debris1', 'debris2', 'debris3', 'jib', 'crate', 'crate_empty', 'rope'):
    p = os.path.join(src, name + '.png')
    if os.path.exists(p):
        img = cut(p)
        if name == 'jib':      # the pulley goes on the left: the jib reaches left from the right scaffolding tower
            img = img.transpose(Image.FLIP_LEFT_RIGHT)
        if name in WARM:
            img = warm(img, *WARM[name])
        img.save(os.path.join(out, f'scaff_{name}.png'), optimize=True)
        print(name, img.size)

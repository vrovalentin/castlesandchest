# Cut building 16, the pirate fortress, into its building steps, bottom-up.
#   pirate1_cut.png : without the parrot
#   pirate1_full.png: with it
# Usage: python3 cut_house_16.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C, F, aC, aF = load_pair(clean_p, full_p)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
on = aC > 0.02

r, g, b = C[..., 0], C[..., 1], C[..., 2]
sand = (r > 170) & (g > 135) & (b > 95) & (r - b < 110)      # the light sandstone of the walls


def rwin(x0, x1, ya, yb, sill):
    """A round-topped window with its stone frame, and the sill under it a little wider."""
    rx = (x1 - x0) / 2
    return S.ell((x0 + x1) / 2, ya + rx, rx, rx) & (Y <= ya + rx) | S.box(x0, x1, ya + rx, yb) | S.box(x0 - 5, x1 + 5, yb, sill)


def lantern(cx):
    """A hanging lantern: the ring and cap, the glass body, the finial under it."""
    return (S.poly((cx - 22, 606), (cx + 22, 606), (cx + 28, 640), (cx + 20, 674), (cx + 6, 688), (cx - 6, 688),
                   (cx - 20, 674), (cx - 28, 640)) | S.box(cx - 8, cx + 8, 578, 608))


def cannon(x0, x1):
    """The cannon and its wooden carriage, without the stone arch around them."""
    m = ndi.binary_opening(S.box(x0, x1, 640, 724) & ~sand & on, iterations=2)
    lab, n = ndi.label(m)
    sizes = ndi.sum(m, lab, range(1, n + 1))
    return ndi.binary_fill_holes(lab == 1 + np.argmax(sizes)) & S.arch(x0 + 4, x1, 630, 724)   # inside its arched hole


def barrel(x0, x1, y0, y1):
    """A barrel: round rims at top and bottom."""
    cx, rx, ry = (x0 + x1) / 2, (x1 - x0) / 2, (x1 - x0) / 7
    return S.ell(cx, y0 + ry, rx, ry) | S.box(x0, x1, y0 + ry, y1 - ry) | S.ell(cx, y1 - ry, rx, ry)


R, rest = claim(on, [
    ('lanL', lantern(404)), ('lanR', lantern(620)),
    ('found', (Y >= 880) | ((Y >= 830) & ((X < 180) | (X > 840)))),
    ('porch', S.box(403, 642, 858, 978)),
    ('deck', S.box(298, 752, 733, 900)),
    ('barrels', barrel(174, 278, 760, 862) | barrel(250, 318, 794, 864)),
    ('cannon', cannon(158, 272) | cannon(758, 872)),
    ('door', S.arch(443, 582, 636, 842)),
    ('sign', S.ell(535, 562, 56, 38)),
    ('sideL', (X < 300) & (Y >= 515)), ('sideR', (X > 742) & (Y >= 515)),
    ('frontL', (Y >= 560) & (X < 512)), ('frontR', Y >= 560),
    ('chimney', S.box(653, 752, 298, 398)),
    ('gwin', S.ell(540, 486, 42, 42)), ('swin', rwin(690, 750, 452, 512, 528)),
    ('roofL', (Y >= 355) & (X >= 380) & (X < 540) | (Y >= 470) & (X < 540)), ('roofR', (Y >= 355) & (X >= 480)),
    ('ladder', S.box(413, 472, 252, 422)),
    ('twin', rwin(306, 374, 356, 426, 444)),
    ('tower', Y >= 330),
    ('ring', Y >= 212),
    ('scope', S.box(398, 502, 112, 168)),
    ('nest', Y >= 118),
    ('flag', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('floor:deck wall:sideL wall:sideR cannon:cannon brick:barrels wall:frontL wall:frontR door:door '
                 'window:gwin sign:sign roof:roofL tiles:roofR window:swin chimney:chimney wall:tower window:twin '
                 'floor:ring ladder:ladder floor:nest telescope:scope spire:flag porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('parrot', D & S.box(615, 735, 615, 775), 'full')]
write_castle(out, 'c16', 'castle_16', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

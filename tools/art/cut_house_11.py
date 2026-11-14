# Cut building 11, the mushroom house, into its building steps, bottom-up.
#   grib1_cut.png : without the hedgehog
#   grib1_full.png: with it
# Usage: python3 cut_house_11.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import figure_of, load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C, F, aC, aF = load_pair(clean_p, full_p)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
on = aC > 0.02
r, g, b = C[..., 0], C[..., 1], C[..., 2]
green = (g > r + 10) & (g > b + 30)
redcap = (r > g + 80) & (r > b + 60)
light = (C.min(2) > 165) & (r - b < 70)

shrooms = (S.box(25, 130, 710, 830) | S.box(98, 155, 765, 815) | S.box(200, 262, 860, 930)
           | S.box(660, 720, 722, 805) | S.box(700, 790, 862, 958) | S.box(752, 792, 915, 962) | S.box(330, 360, 765, 800))
glow = (r > 200) & (g > 140) & (b < 120) & (r - b > 110)
dark = C.max(2) < 110
def lamp(x0, x1, y0, y1):
    """A lantern in a box: its glass, metal and red cap, without the wall or post behind it."""
    m = S.box(x0, x1, y0, y1) & ((C.max(2) < 140) | redcap)
    m = ndi.binary_closing(m, iterations=4)
    lab, n = ndi.label(m)
    if n:
        m = lab == 1 + int(np.argmax(ndi.sum(m, lab, range(1, n + 1))))
    return ndi.binary_dilation(ndi.binary_fill_holes(m), iterations=2)
stepc = (C.min(2) > 110) & (r - b < 60) & ~green
R, rest = claim(on, [
    ('found', Y >= 850),
    ('shrooms', shrooms & ndi.binary_fill_holes(ndi.binary_closing(redcap | (C.min(2) > 175), iterations=2))),
    ('porch', S.box(330, 690, 750, 878) & ndi.binary_fill_holes(ndi.binary_closing(stepc, iterations=3))),
    # lanterns drawn around their shape: hanger, cap and glass body, not the wall or beam behind them
    ('lanL', S.ell(160, 500, 38, 22) | S.poly((126, 505), (194, 505), (192, 585), (128, 585)) | S.box(153, 167, 420, 490)),
    ('lanR', S.ell(972, 690, 36, 24) | S.poly((938, 695), (1006, 695), (1004, 808), (940, 808)) | S.box(964, 980, 655, 680)),
    ('lanC', S.poly((518, 378), (578, 378), (594, 412), (588, 460), (508, 460), (500, 412)) | S.box(536, 560, 312, 382)),
    ('lanR', S.box(860, 1010, 610, 860) & ~green & ~light),
    ('barrel', S.box(700, 862, 640, 850)),
    ('door', S.arch(432, 668, 488, 760)),
    ('ivy', S.box(380, 800, 330, 660) & green),
    ('winL', S.ell(320, 520, 66, 74)), ('winR', S.ell(876, 590, 64, 60) | S.box(815, 940, 615, 668)),
    # the dormer: its arched wooden frame and the flower box under it
    ('dormer', S.arch(390, 588, 58, 205) | S.box(368, 608, 168, 252)),
    ('chimney', S.box(750, 885, 38, 228)),
    ('bench', S.box(100, 368, 600, 770) & ~(light & (X > 240))),
    ('gills', S.box(60, 940, 268, 410) & ~redcap),
    ('gills', S.box(40, 1024, 268, 430) & ((X < 130) | (X > 870))),
    ('awning', S.box(5, 372, 350, 480) & ~S.box(255, 372, 420, 480)),
    ('posts', S.box(5, 250, 430, 780)),
    ('sideCap', S.box(745, 1024, 392, 562) & (Y < 540)),
    ('sideWall', S.box(745, 1000, 500, 780)),
    ('wallLow', (Y >= 560) & (X < 520)), ('wallLow2', Y >= 560),
    ('wallUp', (Y >= 330) & (X < 520) & ~redcap), ('wallUp2', (Y >= 330) & ~redcap),
    ('capL', X < 512), ('capR', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('wall:wallLow wall:wallLow2 door:door wall:wallUp wall:wallUp2 window:winL brick:barrel '
                 'floor:bench floor:posts roof:awning wall:sideWall window:winR roof:sideCap '
                 'floor:gills roof:capL tiles:capR window:dormer chimney:chimney vines:ivy '
                 'porch:porch lantern:lanC lantern:lanL lantern:lanR shrooms:shrooms', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('hedgehog', figure_of(full_p, 'hedgehog', D & S.box(230, 470, 600, 970)), 'full')]
write_castle(out, 'c11', 'castle_11', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

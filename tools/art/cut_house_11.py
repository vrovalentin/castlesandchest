# Cut building 11, the mushroom house, into its building steps, bottom-up.
#   grib1_cut.png : without the hedgehog
#   grib1_full.png: with it
# Usage: python3 cut_house_11.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

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
R, rest = claim(on, [
    ('shrooms', shrooms),
    ('porch', S.box(330, 690, 750, 878)),
    ('found', Y >= 850),
    ('lanL', S.box(122, 198, 420, 592)), ('lanR', S.box(928, 1012, 655, 812)), ('lanC', S.box(505, 590, 310, 458) & ~light),
    ('lanR', S.box(860, 1010, 610, 860)),
    ('barrel', S.box(700, 862, 640, 850)),
    ('door', S.arch(432, 668, 488, 760)),
    ('ivy', S.box(380, 800, 330, 660) & green),
    ('winL', S.ell(320, 520, 66, 74)), ('winR', S.ell(876, 590, 64, 60) | S.box(815, 940, 615, 668)),
    ('dormer', S.box(388, 602, 48, 252) & ~redcap & ~(light & (Y < 70))),
    ('chimney', S.box(750, 885, 38, 228)),
    ('bench', S.box(100, 368, 600, 770) & ~(light & (X > 240))),
    ('awning', S.box(5, 372, 350, 480) & ~S.box(255, 372, 420, 480)),
    ('posts', S.box(5, 250, 430, 780)),
    ('sideCap', S.box(745, 1024, 392, 562) & (Y < 540)),
    ('sideWall', S.box(745, 1000, 500, 780)),
    ('gills', S.box(60, 940, 268, 410) & ~redcap),
    ('wallLow', (Y >= 560) & (X < 520)), ('wallLow2', Y >= 560),
    ('wallUp', (Y >= 330) & (X < 520)), ('wallUp2', Y >= 330),
    ('capL', X < 512), ('capR', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('wall:wallLow wall:wallLow2 door:door wall:wallUp wall:wallUp2 window:winL brick:barrel '
                 'floor:bench roof:awning floor:posts wall:sideWall window:winR roof:sideCap '
                 'floor:gills roof:capL tiles:capR window:dormer chimney:chimney vines:ivy '
                 'porch:porch lantern:lanC lantern:lanL lantern:lanR shrooms:shrooms', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('hedgehog', D & S.box(230, 470, 600, 970), 'full')]
write_castle(out, 'c11', 'castle_11', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

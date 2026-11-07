# Cut building 16, the pirate fortress, into its building steps, bottom-up.
#   pirate1_cut.png : without the parrot
#   pirate1_full.png: with it
# Usage: python3 cut_house_16.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C, F, aC, aF = load_pair(clean_p, full_p)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
on = aC > 0.02

R, rest = claim(on, [
    ('lanL', S.box(373, 432, 578, 682)), ('lanR', S.box(593, 652, 578, 682)),
    ('porch', S.box(403, 642, 858, 978) | S.box(298, 752, 733, 900)),
    ('found', (Y >= 880) | ((Y >= 830) & ((X < 180) | (X > 840)))),
    ('barrels', S.box(168, 322, 758, 868)),
    ('cannon', S.box(158, 272, 636, 724) | S.box(762, 872, 636, 724)),
    ('door', S.arch(443, 582, 636, 842)),
    ('sideL', X < 300), ('sideR', (X > 742) & (Y >= 515)),
    ('frontL', (Y >= 560) & (X < 512)), ('frontR', Y >= 560),
    ('chimney', S.box(653, 752, 298, 398)),
    ('gwin', S.ell(540, 486, 42, 42)), ('swin', S.box(688, 752, 452, 527)), ('sign', S.box(488, 590, 524, 600)),
    ('roofL', (Y >= 355) & (X >= 380) & (X < 540) | (Y >= 470) & (X < 540)), ('roofR', (Y >= 355) & (X >= 480)),
    ('ladder', S.box(413, 472, 252, 422)),
    ('twin', S.box(308, 372, 358, 442)),
    ('tower', Y >= 330),
    ('ring', Y >= 212),
    ('scope', S.box(398, 502, 112, 168)),
    ('nest', Y >= 118),
    ('flag', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('wall:sideL wall:sideR cannon:cannon brick:barrels wall:frontL wall:frontR door:door '
                 'window:gwin sign:sign roof:roofL tiles:roofR window:swin chimney:chimney wall:tower window:twin '
                 'floor:ring ladder:ladder floor:nest telescope:scope spire:flag porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('parrot', D & S.box(615, 735, 615, 775), 'full')]
write_castle(out, 'c16', 'castle_16', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

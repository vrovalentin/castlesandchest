# Cut building 15, the dragon castle, into its building steps, bottom-up.
#   zamokdrakon1_cut.png : without the baby dragon and the nest with eggs
#   zamokdrakon1_full.png: with them
# Usage: python3 cut_house_15.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C, F, aC, aF = load_pair(clean_p, full_p)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
on = aC > 0.02

gable = S.poly((250, 652), (512, 478), (774, 652))
R, rest = claim(on, [
    ('lanL', S.box(348, 412, 612, 728)), ('lanR', S.box(618, 682, 612, 728)),
    ('porch', S.box(338, 702, 838, 928) | S.box(342, 412, 782, 852) | S.box(616, 692, 782, 852)),
    ('found', (Y >= 862) | ((Y >= 758) & ((X < 300) | (X > 735)))),
    ('door', S.arch(393, 634, 612, 852)),
    ('sign', S.box(283, 338, 512, 608) | S.box(686, 742, 512, 608) | S.box(383, 443, 312, 408)
             | S.box(587, 647, 312, 408) | S.box(468, 562, 298, 398)),
    ('gableL', gable & (X < 512)), ('gableR', gable),
    ('frontL', (Y >= 455) & (X >= 290) & (X < 512) & (X <= 735)), ('frontR', (Y >= 455) & (X >= 290) & (X <= 735)),
    ('twinL', S.box(160, 250, 452, 545) | S.box(155, 250, 580, 670)),
    ('twinR', S.box(775, 865, 452, 545) | S.box(775, 870, 580, 670)),
    ('towL', (Y >= 398) & (X < 300)), ('towR', (Y >= 398) & (X > 720)),
    ('lampL', (Y >= 318) & (X < 300)), ('lampR', (Y >= 318) & (X > 720)),
    ('capL', (Y >= 250) & (X < 300)), ('capR', (Y >= 250) & (X > 720)),
    ('spL', X < 300), ('spR', X > 720),
    ('kwin', S.box(402, 455, 192, 262) | S.box(476, 554, 182, 262) | S.box(572, 625, 192, 262)),
    ('crenel', Y >= 252),
    ('keep', Y >= 178),
    ('kroof', Y >= 88),
    ('kspire', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('wall:frontL wall:frontR door:door roof:gableL tiles:gableR wall:towL wall:towR window:twinL window:twinR '
                 'wall:keep sign:sign window:kwin floor:crenel window:lampL window:lampR roof:capL tiles:capR '
                 'spire:spL spire:spR roof:kroof spire:kspire porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('nest', D & S.box(400, 620, 370, 510), 'full'), ('dragon', D & S.box(555, 705, 690, 870), 'full')]
write_castle(out, 'c15', 'castle_15', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

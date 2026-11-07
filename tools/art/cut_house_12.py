# Cut building 12, the clock tower, into its building steps, bottom-up.
#   chasi1_cut.png : without the owl
#   chasi1_full.png: with it
# Usage: python3 cut_house_12.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C, F, aC, aF = load_pair(clean_p, full_p)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
on = aC > 0.02

roof = S.poly((436, 614), (615, 446), (786, 614))
gable = S.poly((506, 614), (615, 508), (726, 614))
R, rest = claim(on, [
    ('lanL', S.box(492, 558, 598, 708)), ('lanR', S.box(688, 752, 598, 708)),
    ('door', S.arch(558, 664, 626, 818)),
    ('porch', S.box(486, 734, 728, 908)),
    ('found', Y >= 790),
    ('chimney', S.box(643, 737, 410, 522)),
    ('winR', S.ell(610, 570, 32, 32)),
    ('roofH', roof & ~gable & (X < 615)), ('tilesH', roof & ~gable),
    ('houseWall', (X > 520) & (Y >= 500)),
    ('clock', S.ell(415, 437, 96, 98) | S.box(328, 502, 505, 568)),
    ('winT', S.box(326, 446, 574, 712)),
    ('lowL', (Y >= 568) & (X < 418)), ('lowR', Y >= 568),
    ('midL', (Y >= 330) & (X < 418)), ('midR', Y >= 330),
    ('rail', Y >= 255),
    ('belfry', Y >= 186),
    ('vane', Y < 92),
    ('domeL', X < 416), ('domeR', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('wall:lowL wall:lowR window:winT wall:houseWall door:door window:winR roof:roofH tiles:tilesH '
                 'wall:midL wall:midR clock:clock floor:rail wall:belfry roof:domeL tiles:domeR spire:vane '
                 'chimney:chimney porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('owl', D & S.box(500, 668, 640, 850), 'full')]
write_castle(out, 'c12', 'castle_12', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

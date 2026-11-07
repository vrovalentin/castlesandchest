# Cut building 13, the pagoda, into its building steps, bottom-up.
#   pagoda1_cut.png : without the panda
#   pagoda1_full.png: with it
# Usage: python3 cut_house_13.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C, F, aC, aF = load_pair(clean_p, full_p)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
on = aC > 0.02

bells = (S.box(316, 358, 178, 270) | S.box(666, 708, 178, 270) | S.box(256, 304, 338, 440) | S.box(720, 766, 338, 440)
         | S.box(186, 234, 533, 642) | S.box(793, 842, 533, 642))
R, rest = claim(on, [
    ('bells', bells),
    ('lanL', S.box(333, 420, 563, 712)), ('lanR', S.box(606, 692, 563, 712)),
    ('porch', S.box(382, 664, 782, 952)),
    ('found', (Y >= 822) | S.box(150, 252, 680, 822) | S.box(770, 870, 680, 822)),
    ('rail', S.box(208, 816, 712, 842)),
    ('door', S.box(428, 600, 578, 792)),
    ('winL', S.box(283, 347, 608, 702)), ('winR', S.box(681, 744, 608, 702)),
    ('w2', S.box(363, 422, 373, 437) | S.box(468, 562, 358, 437) | S.box(603, 662, 373, 437)),
    ('w3', S.box(408, 452, 213, 264) | S.box(473, 552, 203, 264) | S.box(568, 612, 213, 264)),
    ('lowL', (Y >= 568) & (X < 512)), ('lowR', Y >= 568),
    ('roof1L', (Y >= 452) & (X < 512)), ('roof1R', Y >= 452),
    ('wall2', Y >= 345),
    ('roof2', Y >= 255),
    ('wall3', Y >= 192),
    ('roof3', Y >= 118),
    ('spire', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('brick:rail wall:lowL wall:lowR door:door window:winL window:winR roof:roof1L tiles:roof1R '
                 'wall:wall2 window:w2 roof:roof2 wall:wall3 window:w3 tiles:roof3 spire:spire bells:bells '
                 'porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('panda', D & S.box(525, 675, 650, 830), 'full')]
write_castle(out, 'c13', 'castle_13', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

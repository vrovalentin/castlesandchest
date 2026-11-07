# Cut building 17, the desert palace, into its building steps, bottom-up.
#   dvorecverblud1_cut.png : without the baby camel
#   dvorecverblud1_full.png: with it
# Usage: python3 cut_house_17.py <clean.png> <full.png> <output folder> [preview.png]
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
    ('lanL', S.box(353, 427, 568, 667)), ('lanR', S.box(623, 697, 568, 667)),
    ('porch', S.box(368, 642, 850, 968)),
    ('palm', S.box(72, 228, 638, 842) | S.box(788, 948, 642, 842)),
    ('found', (Y >= 880) | ((Y >= 850) & ((X < 160) | (X > 860)))),
    ('terrace', S.box(148, 878, 758, 880)),
    ('jars', S.box(603, 732, 692, 822)),
    ('door', S.box(428, 622, 573, 807)),
    ('winL', S.box(203, 342, 518, 707)), ('winR', S.box(710, 850, 518, 707)),
    ('awning', S.box(333, 722, 448, 587)),
    ('mBody', (X < 300) & (Y >= 262)), ('mBal', (X < 300) & (Y >= 222)), ('mDome', (X < 300) & (Y >= 98)), ('mSpire', X < 300),
    ('lowL', (Y >= 515) & (X < 512)), ('lowR', Y >= 515),
    ('ledge', Y >= 462),
    ('domeSL', S.box(238, 402, 305, 462)), ('domeSR', S.box(652, 828, 300, 462)),
    ('upWin', S.box(448, 607, 328, 442)),
    ('upper', Y >= 262),
    ('dome', Y >= 100),
    ('spire', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('floor:terrace wall:lowL wall:lowR door:door window:winL window:winR brick:jars floor:ledge '
                 'wall:mBody floor:mBal roof:mDome spire:mSpire roof:domeSL roof:domeSR wall:upper window:upWin '
                 'tiles:awning roof:dome spire:spire palm:palm porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('camel', D & S.box(270, 500, 630, 870), 'full')]
write_castle(out, 'c17', 'castle_17', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

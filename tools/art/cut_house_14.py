# Cut building 14, the seashell palace, into its building steps, bottom-up.
#   saeadvorec1_cut.png : without the mermaid
#   saeadvorec1_full.png: with her
# Usage: python3 cut_house_14.py <clean.png> <full.png> <output folder> [preview.png]
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
    ('lanL', S.box(36, 162, 412, 642)), ('lanR', S.box(858, 990, 412, 642)),
    ('pearl', S.box(352, 670, 22, 242)),
    ('porch', S.box(340, 642, 820, 962) | S.box(332, 412, 712, 842) | S.box(598, 682, 712, 842)),
    ('found', (Y >= 870) | ((Y >= 760) & ((X < 175) | (X > 850)))),
    ('door', S.arch(413, 617, 588, 832)),
    ('arch', S.box(343, 682, 498, 600)),
    ('winL', S.ell(265, 625, 60, 60)), ('winR', S.ell(765, 625, 60, 60)),
    ('shellL', S.box(150, 352, 676, 812)), ('shellR', S.box(672, 875, 676, 812)),
    ('upL', S.box(252, 418, 442, 600)), ('upR', S.box(602, 765, 442, 600)),
    ('topWin', S.box(383, 642, 238, 322)),
    ('spiral', S.box(288, 742, 288, 520)),
    ('lowL', (Y >= 470) & (X < 512)), ('lowR', Y >= 470),
    ('rails', (Y >= 395) & ((X < 300) | (X > 724))),
    ('spTL', (Y < 222) & (X < 512) & (X > 230)), ('spTR', (Y < 222) & (X > 512) & (X < 795)),
    ('spSL', (Y < 378) & (X < 245)), ('spSR', (Y < 378) & (X > 780)),
    ('towL', X < 512), ('towR', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('wall:lowL wall:lowR tiles:shellL tiles:shellR window:winL window:winR brick:arch door:door '
                 'floor:rails roof:upL roof:upR wall:towL wall:towR wall:spiral window:topWin '
                 'spire:spSL spire:spSR spire:spTL spire:spTR pearl:pearl porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('mermaid', D & S.box(630, 840, 600, 1010), 'full')]
write_castle(out, 'c14', 'castle_14', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

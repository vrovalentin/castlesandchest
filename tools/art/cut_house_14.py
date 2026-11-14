# Cut building 14, the seashell palace, into its building steps, bottom-up.
#   saeadvorec1_cut.png : without the mermaid
#   saeadvorec1_full.png: with her
# Usage: python3 cut_house_14.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import figure_of, load_pair, write_castle, Shapes, claim, full_only, slots_of, settle

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C, F, aC, aF = load_pair(clean_p, full_p)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
on = aC > 0.02

r, g, b = C[..., 0], C[..., 1], C[..., 2]
gold = (r > 170) & (g > 120) & (b < 110) & (r - b > 90)
green = (g > r + 10) & (g > b + 10)


def jelly(cx, hook_box):
    """A jellyfish lantern: its dome, the tentacles under it, the cap and the golden hook it hangs on."""
    m = S.ell(cx, 505, 58, 38) | S.poly((cx - 46, 520), (cx + 46, 520), (cx + 40, 645), (cx - 42, 645))
    m = ((m & ~green) | S.box(cx - 20, cx + 20, 460, 482) | ndi.binary_opening(S.box(*hook_box) & gold, iterations=2)) & on
    lab, _ = ndi.label(m)
    return lab == lab[505, cx]       # one piece: no coral crumbs or weed tips


R, rest = claim(on, [
    ('lanL', jelly(96, (70, 205, 405, 482))), ('lanR', jelly(928, (819, 954, 405, 482))),
    ('pearl', S.box(352, 670, 22, 242)),
    ('found', (Y >= 870) | ((Y >= 760) & ((X < 175) | (X > 850)))),
    ('porch', S.poly((395, 826), (632, 826), (702, 966), (322, 966)) |      # the steps
              S.ell(368, 745, 32, 32) | S.box(338, 400, 760, 850) |         # the pearl pillars
              S.ell(640, 745, 32, 32) | S.box(610, 674, 760, 850)),
    ('door', S.arch(413, 617, 588, 832)),
    ('arch', S.arch(343, 682, 494, 720)),
    ('winL', S.ell(265, 625, 60, 60)), ('winR', S.ell(765, 625, 60, 60)),
    # the big fan shells: half ellipses on their base
    ('shellL', S.ell(250, 812, 100, 130) & (Y <= 818)), ('shellR', S.ell(774, 812, 100, 130) & (Y <= 818)),
    ('upL', S.ell(338, 560, 84, 122) & (Y <= 604)), ('upR', S.ell(684, 560, 84, 122) & (Y <= 604)),
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
SLOTS += [('mermaid', figure_of(full_p, 'mermaid', D & S.box(630, 840, 600, 1010)), 'full')]
write_castle(out, 'c14', 'castle_14', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

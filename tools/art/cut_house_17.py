# Cut building 17, the desert palace, into its building steps, bottom-up.
#   dvorecverblud1_cut.png : without the baby camel
#   dvorecverblud1_full.png: with it
# Usage: python3 cut_house_17.py <clean.png> <full.png> <output folder> [preview.png]
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
green = (g > r + 10) & (g > b + 10)
bright = C.max(2) - C.min(2) > 110             # the striped cloth, wood and tassels, not the pale plaster


def pointed(x0, x1, ya, yb, k=0.85):
    """A pointed arch from x0 to x1, apex at ya, down to yb."""
    w = x1 - x0
    ys = ya + k * w
    h = ys - ya
    rr = (w * w / 4 + h * h) / w
    inL = (X - (x0 + rr)) ** 2 + (Y - ys) ** 2 <= rr * rr
    inR = (X - (x1 - rr)) ** 2 + (Y - ys) ** 2 <= rr * rr
    return S.box(x0, x1, ys, yb) | ((Y < ys) & (Y >= ya) & inL & inR)


def window(sx0, sx1, gx0, gx1, hx0, hx1):
    """A pointed window: the carved hood over it, the glass, the blue shutters at its sides, the sill under it."""
    return (pointed(hx0, hx1, 522, 640) | pointed(gx0, gx1, 568, 672) | S.box(sx0, sx1, 592, 672)
            | S.box(hx0 + 5, hx1 - 5, 668, 706))


def oil_lamp(cx):
    """A hanging oil lamp: the chain, the dome over the flame, the bowl and its foot."""
    return S.box(cx - 6, cx + 6, 564, 600) | S.ell(cx, 616, 24, 22) | S.ell(cx, 646, 40, 20) | S.box(cx - 12, cx + 12, 650, 670)


def palm(x0, x1, pcx):
    """A palm in its pot: the green leaves and the round pot under them."""
    leaves = ndi.binary_closing(S.box(x0, x1, 636, 790) & green, iterations=3)
    return leaves | S.ell(pcx, 800, 44, 42)


R, rest = claim(on, [
    ('lanL', oil_lamp(390)), ('lanR', oil_lamp(658)),
    ('found', (Y >= 880) | ((Y >= 850) & ((X < 160) | (X > 860)))),
    ('porch', S.box(368, 642, 850, 968)),
    ('palm', palm(72, 232, 165) | palm(786, 948, 858)),
    ('jars', ndi.binary_fill_holes((S.ell(684, 758, 46, 56) | S.box(648, 720, 692, 716) | S.ell(640, 788, 32, 32)) & (C.max(2) - C.min(2) > 70))),
    ('terrace', S.box(148, 878, 758, 880)),
    ('door', pointed(436, 616, 574, 808, k=0.75)),
    ('winL', window(205, 337, 243, 307, 220, 332)), ('winR', window(713, 847, 746, 810, 720, 832)),
    ('awning', ndi.binary_fill_holes(ndi.binary_closing(S.poly((372, 452), (682, 452), (726, 510), (720, 580), (334, 580), (328, 510)) & bright, iterations=2))),
    ('mBody', (X < 300) & (Y >= 262)), ('mBal', (X < 300) & (Y >= 222)), ('mDome', (X < 300) & (Y >= 98)), ('mSpire', X < 300),
    ('lowL', (Y >= 515) & (X < 512)), ('lowR', Y >= 515),
    ('ledge', Y >= 462),
    ('domeSL', S.box(238, 402, 305, 462)), ('domeSR', S.box(652, 828, 300, 462)),
    ('upWin', S.box(452, 603, 352, 438) | pointed(488, 568, 334, 438) | S.box(474, 586, 432, 450)),
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
write_castle(out, 'c17', 'castle_17', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

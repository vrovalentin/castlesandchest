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

def rwin(x0, x1, ya, yb, sill):
    """A round-topped window with its stone frame, and the sill under it a little wider."""
    rx = (x1 - x0) / 2
    return S.ell((x0 + x1) / 2, ya + rx, rx, rx) & (Y <= ya + rx) | S.box(x0, x1, ya + rx, yb) | S.box(x0 - 5, x1 + 5, yb, sill)


def lantern(cx):
    """A hanging lantern: its cap and hook, the glass body narrowing to the bottom finial."""
    return (S.poly((cx - 25, 640), (cx + 25, 640), (cx + 27, 698), (cx + 10, 726), (cx - 10, 726), (cx - 27, 698))
            | S.box(cx - 12, cx + 12, 600, 642))


gable = S.poly((250, 652), (512, 478), (774, 652))
R, rest = claim(on, [
    ('lanL', lantern(380)), ('lanR', lantern(650)),
    ('found', (Y >= 862) | ((Y >= 758) & ((X < 300) | (X > 735)))),
    ('porch', S.box(338, 702, 838, 928) | S.box(342, 412, 782, 852) | S.box(616, 692, 782, 852)),
    ('door', S.arch(393, 634, 612, 852)),
    ('sign', S.box(283, 338, 512, 608) | S.box(686, 742, 512, 608) | S.box(383, 443, 312, 408)    # banners
             | S.box(587, 647, 312, 408) | S.poly((468, 298), (562, 298), (564, 352), (515, 400), (466, 352))),  # shield
    ('gableL', gable & (X < 512)), ('gableR', gable),
    ('frontL', (Y >= 455) & (X >= 290) & (X < 512) & (X <= 735)), ('frontR', (Y >= 455) & (X >= 290) & (X <= 735)),
    ('twinL', rwin(163, 247, 456, 532, 550) | rwin(160, 244, 584, 654, 674)),
    ('twinR', rwin(777, 861, 456, 532, 550) | rwin(780, 864, 584, 654, 674)),
    ('towL', (Y >= 398) & (X < 300)), ('towR', (Y >= 398) & (X > 720)),
    ('lampL', (Y >= 318) & (X < 300)), ('lampR', (Y >= 318) & (X > 720)),
    ('capL', (Y >= 250) & (X < 300)), ('capR', (Y >= 250) & (X > 720)),
    ('spL', X < 300), ('spR', X > 720),
    ('kwin', rwin(403, 454, 195, 252, 266) | rwin(477, 553, 183, 252, 266) | rwin(573, 624, 195, 252, 266)),
    ('crenel', Y >= 252),
    ('keep', Y >= 178),
    ('kroof', Y >= 88),
    ('kspire', np.ones_like(on)),
])
found = R['found']
SLOTS = slots_of('wall:frontL wall:frontR door:door roof:gableL tiles:gableR wall:towL wall:towR window:twinL window:twinR '
                 'wall:crenel sign:sign window:kwin floor:keep window:lampL window:lampR roof:capL tiles:capR '
                 'spire:spL spire:spR roof:kroof spire:kspire porch:porch lantern:lanL lantern:lanR', R, found, 6)
SLOTS = settle(SLOTS, found)
D = full_only(F, C, aF)
SLOTS += [('nest', D & S.box(400, 620, 370, 510), 'full'), ('dragon', D & S.box(555, 705, 690, 870), 'full')]
write_castle(out, 'c15', 'castle_15', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

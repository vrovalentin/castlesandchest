# Cut building 5, the treehouse, into its building steps, bottom-up.
#   domicnaderevve5_cut.png : the tree without the squirrel, the balcony rail and the lanterns
#   domicnaderevve5.png     : with them
# Usage: python3 cut_house_5.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import settle, cutout, write_castle, split_even, Shapes, claim, full_only

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C = np.array(Image.open(clean_p).convert('RGB')).astype(np.int32)
F = np.array(Image.open(full_p).convert('RGB')).astype(np.int32)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
aC, aF = cutout(C, open_holes=300), cutout(F, open_holes=300)
on = aC > 0.02
r, g, b = C[..., 0], C[..., 1], C[..., 2]
leafy = (g > r + 8) & (g > b + 30)

R, rest = claim(on, [
    ('found', (Y >= 880)),
    ('ladder', S.box(726, 802, 488, 868)),
    # big house
    ('chimney1', S.box(232, 298, 92, 172)),
    ('win_round', S.ell(383, 218, 36, 38)),
    ('door1', S.arch(322, 424, 268, 412)),
    ('win_left', S.box(196, 282, 280, 384)),
    ('roof1', S.poly((150, 268), (392, 86), (552, 280), (520, 300), (392, 150), (180, 300))),
    ('wall1', S.box(170, 525, 150, 412)),
    ('deck1', S.box(160, 560, 398, 478)),
    # small house
    ('spire2', S.box(902, 946, 288, 362)),
    ('door2', S.arch(770, 818, 392, 474)),
    ('win2', S.ell(840, 422, 24, 26)),
    ('roof2', S.poly((688, 418), (810, 306), (972, 418), (950, 425), (810, 330), (705, 425))),
    ('wall2', S.box(696, 950, 360, 492)),
    ('deck2', S.box(676, 945, 488, 532)),
    # the bridge and the branch stub above its left end, which would hang in the air until the bridge is built
    ('bridge', S.poly((540, 370), (580, 370), (745, 430), (745, 510), (580, 505), (540, 470)) | S.box(520, 612, 320, 402)),
    # the crown: left, top and right, and the trunk below it
    ('crownL', (Y < 565) & (X < 330)),
    ('crownR', (Y < 600) & (X >= 650)),
    ('crownT', (Y < 330) | leafy & (Y < 470)),
    ('trunkL_lo', (Y >= 700) & (X < 512)), ('trunkR_lo', (Y >= 700)),
    ('trunkL_hi', (X < 512)), ('trunkR_hi', np.ones_like(on)),
])
found = R['found']
# leaves inside the houses' boxes that grow on from the crown outside them go with the crown,
# so the houses don't show up as rectangles cut out of the foliage
house = ['wall1', 'roof1', 'win_left', 'chimney1', 'deck1', 'wall2', 'roof2', 'deck2', 'bridge']
inside = np.zeros_like(on)
for k in house: inside |= R[k]
lab, _ = ndi.label(leafy & on, structure=np.ones((3, 3), bool))
outer = np.isin(lab, np.unique(lab[leafy & on & ~inside & (lab > 0)])) & (lab > 0)
for k in house:
    moved = R[k] & outer
    moved &= ~ndi.binary_fill_holes(ndi.binary_closing(R[k] & ~moved, iterations=6))   # ivy on the walls stays
    R[k] &= ~moved
    R['crownL'] |= moved & (X < 330); R['crownR'] |= moved & (X >= 650); R['crownT'] |= moved & (X >= 330) & (X < 650)
SLOTS = [('clay', m, 'clean') for m in split_even(found, X, 6)]
SLOTS += [('wall', R['trunkL_lo'], 'clean'), ('wall', R['trunkR_lo'], 'clean'),
          ('wall', R['trunkL_hi'], 'clean'), ('wall', R['trunkR_hi'], 'clean'),
          ('floor', R['deck1'], 'clean'), ('floor', R['bridge'], 'clean'),
          ('brick', R['wall1'], 'clean'), ('door', R['door1'], 'clean'),
          ('window', R['win_left'], 'clean'), ('window', R['win_round'], 'clean'),
          ('roof', R['roof1'], 'clean'), ('chimney', R['chimney1'], 'clean'),
          ('brick', R['wall2'], 'clean'), ('floor', R['deck2'], 'clean'), ('door', R['door2'], 'clean'), ('window', R['win2'], 'clean'),
          ('tiles', R['roof2'], 'clean'), ('spire', R['spire2'], 'clean'),
          # the crown last, so the houses don't stand in holes of sky for half the build
          ('roof', R['crownL'], 'clean'), ('tiles', R['crownT'], 'clean'), ('roof', R['crownR'], 'clean'),
          ('ladder', R['ladder'], 'clean')]

SLOTS = settle(SLOTS, found)

D = full_only(F, C, aF)
squirrel = D & S.box(415, 545, 296, 425) & ~S.box(400, 560, 345, 425) | D & S.box(455, 512, 345, 418)
lantL = D & (S.box(60, 140, 385, 540) | S.box(285, 325, 245, 305))
lantR = D & (S.box(612, 690, 195, 345) | S.box(860, 935, 535, 670) | S.box(735, 775, 395, 440))
rail = D & S.box(160, 580, 340, 470) & ~squirrel & ~lantL & ~lantR
SLOTS += [('porch', rail, 'full'), ('lantern', lantL, 'full'), ('lantern', lantR, 'full'), ('squirrel', squirrel, 'full')]

write_castle(out, 'c5', 'castle_5', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

# Cut building 10, the ice palace, into its building steps, bottom-up.
#   ledyanoi_zamok10_cut.png : without the snow queen, the stairs with their rails and the lanterns
#   ledyanoi_zamok10.png     : with them
# Usage: python3 cut_house_10.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import cutout, write_castle, split_even, Shapes, claim, full_only

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C = np.array(Image.open(clean_p).convert('RGB')).astype(np.int32)
F = np.array(Image.open(full_p).convert('RGB')).astype(np.int32)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
aC, aF = cutout(C, snow=True), cutout(F, snow=True)
on = aC > 0.02

# The five towers, left to right: x range of the body and the top of its cone.
T = [(128, 284), (294, 414), (414, 608), (598, 760), (760, 900)]
regions = [
    ('gate', S.box(430, 600, 468, 852)),
    ('found', Y >= 800),
    ('sp1', S.box(194, 228, 200, 290)), ('sp2', S.box(342, 374, 115, 200)), ('sp3', S.box(494, 532, 8, 86)),
    ('sp4', S.box(646, 682, 110, 192)), ('sp5', S.box(806, 844, 218, 312)),
    ('wl1', S.box(196, 242, 662, 758)), ('wl2', S.box(322, 384, 596, 750)), ('wl4', S.box(640, 700, 596, 750)),
    ('wl5', S.box(810, 858, 652, 758)),
    ('wu1', S.box(180, 224, 452, 562)), ('wu2', S.box(332, 372, 348, 422)), ('wu3', S.box(448, 578, 262, 378)),
    ('wu4', S.box(652, 694, 366, 432)), ('wu5', S.box(806, 852, 488, 592)),
    ('cone1', S.box(126, 286, 280, 462) | S.box(126, 160, 455, 500)), ('cone2', S.box(292, 416, 190, 338)), ('cone3', S.box(412, 610, 70, 282)),
    ('cone4', S.box(596, 750, 184, 402)), ('cone5', S.box(760, 902, 300, 498)),
    ('bat2', S.box(296, 410, 414, 472)), ('bat3', S.box(406, 616, 366, 446)), ('bat4', S.box(598, 758, 426, 508)),
    ('gable', S.box(380, 642, 446, 540)),
]
for i, (x0, x1) in enumerate(T, 1):
    regions.append((f'up{i}', (X >= x0) & (X < x1) & (Y < 470)))
regions += [('lowA', X < 290), ('lowB', X < 512), ('lowC', X < 760), ('lowD', np.ones_like(on))]
R, rest = claim(on, regions)
found = R['found']
SLOTS = [('clay', m, 'clean') for m in split_even(found, X, 6)]
SLOTS += [('wall', R['lowA'], 'clean'), ('wall', R['lowD'], 'clean'), ('wall', R['lowB'], 'clean'),
          ('wall', R['lowC'], 'clean'), ('door', R['gate'], 'clean')]
SLOTS += [('window', R[k], 'clean') for k in ('wl1', 'wl2', 'wl4', 'wl5')]
# the gable and the battlements first, so the upper tower walls have something to stand on
SLOTS += [('wall', R['gable'], 'clean'),
          ('floor', R['bat2'], 'clean'), ('floor', R['bat4'], 'clean'), ('floor', R['bat3'], 'clean')]
SLOTS += [('wall', R[f'up{i}'], 'clean') for i in (1, 5, 2, 4, 3)]
SLOTS += [('window', R[k], 'clean') for k in ('wu1', 'wu5', 'wu2', 'wu4', 'wu3')]
SLOTS += [('roof', R['cone1'], 'clean'), ('roof', R['cone5'], 'clean'), ('tiles', R['cone2'], 'clean'),
          ('tiles', R['cone4'], 'clean'), ('roof', R['cone3'], 'clean')]
SLOTS += [('spire', R[f'sp{i}'], 'clean') for i in (1, 5, 2, 4, 3)]

D = full_only(F, C, aF)
queen = D & S.box(438, 608, 515, 856)
lantL = D & S.box(300, 372, 758, 892)
lantR = D & S.box(656, 728, 758, 892)
stairs = D & S.box(205, 815, 690, 1000) & ~queen & ~lantL & ~lantR
SLOTS += [('porch', stairs, 'full'), ('lantern', lantL, 'full'), ('lantern', lantR, 'full'), ('princess', queen, 'full')]

write_castle(out, 'c10', 'castle_10', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

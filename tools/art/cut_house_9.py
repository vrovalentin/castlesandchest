# Cut building 9, the wizard tower, into its building steps, bottom-up.
#   volshebnaya_bashnya9_cut.png : without the wizard and the lanterns (its steps go with the foundation)
#   volshebnaya_bashnya9.png     : with them
# Usage: python3 cut_house_9.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import settle, cutout, write_castle, split_even, Shapes, claim, full_only

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C = np.array(Image.open(clean_p).convert('RGB')).astype(np.int32)
F = np.array(Image.open(full_p).convert('RGB')).astype(np.int32)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
aC, aF = cutout(C), cutout(F)
on = aC > 0.02

R, rest = claim(on, [
    ('door', S.box(526, 628, 720, 910)),
    ('found', Y >= 900),
    ('scope', S.box(578, 770, 20, 205) & ((X >= 600) | (Y < 120))),
    ('finial', S.box(518, 558, 10, 62)),
    ('dome', Y < 200),
    ('spL', S.box(282, 340, 38, 140)), ('spR', S.box(752, 802, 190, 290)),
    ('wTL', S.box(330, 386, 312, 378)), ('wTR', S.box(704, 760, 472, 545)),
    ('coneL', (X < 450) & (Y < 335) & ~S.box(412, 700, 190, 340)),
    ('coneR', (X > 668) & (Y < 478) & (Y > 280) & ~S.box(600, 700, 290, 420)),
    ('turL', (X < 425) & (Y < 525) & (Y >= 290) & ~S.box(400, 700, 290, 420)),
    ('turR', (X > 662) & (Y < 660) & (Y >= 280) & ~S.box(400, 690, 500, 650)),
    ('wTop', S.box(482, 562, 225, 322)), ('wMid', S.box(562, 626, 460, 562)),
    ('balc', S.box(404, 692, 302, 402)),
    ('stair', S.poly((350, 610), (680, 500), (690, 560), (690, 610), (355, 720), (348, 660))),
    ('top', Y < 302),
    ('mid', Y < 640),
    ('lowL', X < 528), ('lowR', np.ones_like(on)),
])
found = R['found']
SLOTS = [('clay', m, 'clean') for m in split_even(found, X, 6)]
SLOTS += [('wall', R['lowL'], 'clean'), ('wall', R['lowR'], 'clean'), ('door', R['door'], 'clean'),
          ('wall', R['mid'], 'clean'), ('floor', R['stair'], 'clean'), ('window', R['wMid'], 'clean'),
          ('wall', R['turL'], 'clean'), ('window', R['wTL'], 'clean'),
          ('wall', R['turR'], 'clean'), ('window', R['wTR'], 'clean'),
          ('floor', R['balc'], 'clean'), ('wall', R['top'], 'clean'), ('window', R['wTop'], 'clean'),
          ('roof', R['coneL'], 'clean'), ('spire', R['spL'], 'clean'),
          ('tiles', R['coneR'], 'clean'), ('spire', R['spR'], 'clean'),
          ('roof', R['dome'], 'clean'), ('spire', R['finial'], 'clean'), ('telescope', R['scope'], 'clean')]

SLOTS = settle(SLOTS, found)

D = full_only(F, C, aF)
lantL = D & (S.box(170, 335, 430, 655) | S.box(460, 522, 676, 795))
lantR = D & (S.box(755, 855, 580, 725) | S.box(664, 724, 686, 802))
wizard = D & S.box(238, 548, 612, 1000) & ~lantL
SLOTS += [('lantern', lantL, 'full'), ('lantern', lantR, 'full'), ('wizard', wizard, 'full')]

write_castle(out, 'c9', 'castle_9', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

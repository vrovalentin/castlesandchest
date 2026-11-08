# Cut building 7, the pumpkin house, into its building steps, bottom-up.
#   tikva7_cut.png : without the fairy, the steps with their posts and the lanterns
#   tikva7.png     : with them
# Usage: python3 cut_house_7.py <clean.png> <full.png> <output folder> [preview.png]
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
aC, aF = cutout(C, open_holes=300), cutout(F, open_holes=300)
on = aC > 0.02
r, g, b = C[..., 0], C[..., 1], C[..., 2]
green = (g > r + 10) & (g > b + 25)

R, rest = claim(on, [
    ('found', (Y >= 880) | (Y >= 775) & ((X < 300) | (X > 740))),
    ('chimney', S.box(265, 405, 140, 300) | S.box(300, 492, 0, 158)),
    ('finial', S.box(705, 765, 28, 105)),
    ('cap', green & S.box(605, 870, 88, 210)),
    ('w5', S.arch(722, 792, 178, 270) | S.box(692, 732, 200, 275) | S.box(792, 832, 200, 275) | S.box(715, 805, 250, 300)),
    ('turret', S.box(622, 858, 112, 338)),
    ('stem', S.box(436, 628, 155, 262)),
    ('vinesL', (X < 192) & (Y >= 420)),
    ('vinesR', (X > 862) & (Y >= 420)),
    # windows: the arched frame, the two shutters and the flower box, not the pumpkin around them
    ('w1', S.arch(226, 312, 385, 500) | S.box(181, 229, 424, 507) | S.box(299, 352, 408, 502) | S.box(205, 312, 486, 542)),
    ('w2', S.arch(232, 342, 592, 720) | S.box(192, 241, 644, 738) | S.box(329, 382, 638, 742) | S.box(218, 344, 703, 772)),
    ('w3', S.arch(462, 560, 330, 450) | S.box(416, 468, 360, 457) | S.box(566, 620, 360, 457) | S.box(450, 582, 443, 488)),
    ('w4', S.arch(750, 812, 405, 505) | S.box(703, 752, 426, 507) | S.box(808, 852, 438, 522) | S.box(736, 830, 493, 550)),
    ('canopy', green & S.box(330, 722, 488, 612)),
    ('door', S.arch(440, 612, 580, 870) | S.box(410, 455, 600, 860) | S.box(600, 652, 600, 860)),
    ('roofL', green & (Y < 440) & (X < 512)), ('roofR', green & (Y < 440)),
    ('lowL', (Y >= 560) & (X < 512)), ('lowR', Y >= 560),
    ('midL', X < 390), ('midR', X > 636), ('top', np.ones_like(on)),
])
found = R['found']
SLOTS = [('clay', m, 'clean') for m in split_even(found, X, 8)]
# the leafy canopy comes with the door, so there is no hole of sky over the door for half the build
SLOTS += [('wall', R['lowL'], 'clean'), ('wall', R['lowR'], 'clean'), ('door', R['door'], 'clean'),
          ('vines', R['canopy'], 'clean'), ('window', R['w2'], 'clean'), ('wall', R['midL'], 'clean'), ('wall', R['midR'], 'clean'),
          ('window', R['w1'], 'clean'), ('window', R['w4'], 'clean'), ('wall', R['top'], 'clean'),
          ('window', R['w3'], 'clean'),
          ('roof', R['roofL'], 'clean'), ('tiles', R['roofR'], 'clean'), ('spire', R['stem'], 'clean'),
          ('chimney', R['chimney'], 'clean'), ('brick', R['turret'], 'clean'), ('window', R['w5'], 'clean'),
          ('roof', R['cap'], 'clean'), ('spire', R['finial'], 'clean'),
          ('vines', R['vinesL'], 'clean'), ('vines', R['vinesR'], 'clean')]

SLOTS = settle(SLOTS, found)

D = full_only(F, C, aF)
fairy = D & S.box(700, 865, 562, 795)
lantL = D & (S.box(40, 140, 485, 595) | S.box(640, 705, 580, 700))
lantR = D & S.box(885, 990, 490, 605)
steps = D & S.box(398, 712, 712, 925) & ~fairy & ~lantL
SLOTS += [('porch', steps, 'full'), ('lantern', lantL, 'full'), ('lantern', lantR, 'full'), ('fairy', fairy, 'full')]

write_castle(out, 'c7', 'castle_7', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

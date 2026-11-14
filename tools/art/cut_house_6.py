# Cut building 6, the lighthouse, into its building steps, bottom-up.
#   mayak6_cut.png : without the cat sailor, the steps with their posts and the lanterns
#   mayak6.png     : with them
# Usage: python3 cut_house_6.py <clean.png> <full.png> <output folder> [preview.png]
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
aC, aF = cutout(C), cutout(F)
on = aC > 0.02
r, g, b = C[..., 0], C[..., 1], C[..., 2]
green = (g > r + 15) & (g > b + 15)

R, rest = claim(on, [
    ('found', (Y >= 800) | green & (Y >= 700)),
    # the cottage in front of the tower
    ('chimney', S.box(640, 738, 412, 548)),
    ('cwin', S.ell(612, 572, 30, 30)),
    ('door', S.arch(556, 662, 628, 815)),
    ('croof', S.poly((455, 595), (520, 437), (650, 450), (800, 600), (790, 642), (455, 620)) & ~((X < 527) & (Y < 560))),
    ('cwall', S.box(470, 770, 500, 820) & ~((X < 530) & (Y < 560))),   # not the tower's band above it
    # the tower, bottom-up
    ('spire', Y < 96),
    ('roof', Y < 198),
    ('lamp', S.box(335, 525, 150, 280) & (Y >= 198)),
    ('rail', Y < 300),
    ('cornice', Y < 352),
    ('win1', S.arch(368, 470, 364, 470) | S.box(362, 474, 462, 492)),
    ('win2', S.arch(356, 462, 556, 672) | S.box(350, 466, 664, 694)),
    # bands of the tower along the edges of its red and white stripes
    ('bandD', Y < 470), ('bandC', Y < 600), ('bandB', Y < 718), ('bandA', np.ones_like(on)),
])
found = R['found']
SLOTS = [('clay', m, 'clean') for m in split_even(found, X, 8)]
SLOTS += [('wall', R['bandA'], 'clean'), ('wall', R['bandB'], 'clean'), ('window', R['win2'], 'clean'),
          ('brick', R['cwall'], 'clean'), ('door', R['door'], 'clean'),
          ('wall', R['bandC'], 'clean'), ('wall', R['bandD'], 'clean'), ('window', R['win1'], 'clean'),
          # the round window sits in the gable, which comes with the roof
          ('tiles', R['croof'], 'clean'), ('window', R['cwin'], 'clean'), ('chimney', R['chimney'], 'clean'),
          ('floor', R['cornice'], 'clean'), ('floor', R['rail'], 'clean'), ('beacon', R['lamp'], 'clean'),
          ('roof', R['roof'], 'clean'), ('spire', R['spire'], 'clean')]

D = full_only(F, C, aF)
cat = D & S.box(528, 645, 692, 852)
lantL = D & S.box(495, 555, 598, 705)
lantR = D & S.box(686, 750, 598, 708)
steps = D & S.box(490, 735, 720, 905) & ~cat & ~lantL & ~lantR
SLOTS += [('porch', steps, 'full'), ('lantern', lantL, 'full'), ('lantern', lantR, 'full'), ('catsailor', cat, 'full')]

write_castle(out, 'c6', 'castle_6', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

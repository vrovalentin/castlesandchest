# Cut building 8, the tavern, into its building steps, bottom-up.
#   traktir8_cut.png : without the innkeeper, the steps with their posts and the door lantern
#   traktir8.png     : with them
# Usage: python3 cut_house_8.py <clean.png> <full.png> <output folder> [preview.png]
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
aC, aF = cutout(C, open_holes=300), cutout(F, open_holes=300)
on = aC > 0.02
r, g, b = C[..., 0], C[..., 1], C[..., 2]
red = (r > g + 70) & (r > b + 60)

R, rest = claim(on, [
    ('door', S.box(462, 634, 646, 958)),
    ('found', (Y >= 850) | (Y >= 765) & (X > 640)),
    ('sign', S.box(700, 868, 552, 756)),
    ('chimney', S.box(678, 806, 0, 100) | S.box(678, 806, 0, 290) & ~red & ~S.box(632, 762, 128, 300)),
    ('canopy', S.box(405, 708, 538, 664) & (red | (Y >= 590))),
    ('wLow', S.box(298, 414, 660, 822)),
    ('beam', S.box(200, 802, 536, 614)),
    ('dL', S.box(266, 347, 188, 292)), ('dR', S.box(666, 747, 188, 292)),
    ('dC', S.box(434, 582, 168, 330)),
    ('wUL', S.box(256, 372, 384, 542)), ('wUC', S.box(438, 580, 374, 532)),
    ('wUR', S.box(641, 754, 384, 542)),
    ('gable', S.poly((262, 300), (262, 214), (305, 166), (350, 214), (350, 300))
              | S.poly((660, 300), (660, 214), (705, 166), (752, 214), (752, 300))
              | S.poly((420, 338), (420, 196), (505, 96), (590, 196), (590, 338))),
    ('roofL', (Y < 400) & (X < 505)), ('roofR', Y < 400),
    ('upL', (Y < 536) & (X < 505)), ('upR', Y < 536),
    ('lowL', X < 505), ('lowR', np.ones_like(on)),
])
found = R['found']
SLOTS = [('clay', m, 'clean') for m in split_even(found, X, 6)]
SLOTS += [('wall', R['lowL'], 'clean'), ('wall', R['lowR'], 'clean'), ('door', R['door'], 'clean'),
          ('window', R['wLow'], 'clean'), ('tiles', R['canopy'], 'clean'), ('sign', R['sign'], 'clean'),
          ('floor', R['beam'], 'clean'),
          ('wall', R['upL'], 'clean'), ('wall', R['upR'], 'clean'),
          ('window', R['wUL'], 'clean'), ('window', R['wUC'], 'clean'), ('window', R['wUR'], 'clean'),
          ('roof', R['roofL'], 'clean'), ('tiles', R['roofR'], 'clean'), ('wall', R['gable'], 'clean'),
          ('window', R['dL'], 'clean'), ('window', R['dC'], 'clean'), ('window', R['dR'], 'clean'),
          ('chimney', R['chimney'], 'clean')]

D = full_only(F, C, aF)
keeper = D & S.box(248, 482, 700, 992)
lant = D & S.box(645, 718, 636, 752)
steps = D & S.box(462, 716, 780, 990) & ~keeper
SLOTS += [('porch', steps, 'full'), ('lantern', lant, 'full'), ('innkeeper', keeper, 'full')]

write_castle(out, 'c8', 'castle_8', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

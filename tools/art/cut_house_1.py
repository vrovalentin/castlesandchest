# Cut building 1, the two-storey tower, by its drawing, bottom-up.
#   castle_1_noporch.png : the tower without the porch and the lantern (transparent)
#   castle_1_full.png    : with them
# Walls follow the stone rows, windows are round arches with their stone frames and sills, the brown eaves
# under the roof are the floors, the red roof is split along a row of tiles.
# Usage: python3 cut_house_1.py <noporch.png> <full.png> <output folder> [map.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from recut import Cut, overlay

c = Cut(sys.argv[1], sys.argv[2])
X, Y = c.X, c.Y
r, g, b = c.C[..., 0], c.C[..., 1], c.C[..., 2]
# bushes stand on the site from the start
green = c.color('green') & c.on
lab, n = ndi.label(ndi.binary_opening(green, iterations=1))
sizes = ndi.sum(green, lab, range(1, n + 1))
bush = ndi.binary_dilation(np.isin(lab, 1 + np.flatnonzero(sizes > 300)), iterations=3) & c.on
on = c.on & ~bush

orange = (r > g + 50) & (r > 170) & (Y > 838)
found = (Y >= 848) | orange
spire = c.box(380, 560, 0, 146)
red = (r > 170) & (r > g + 80) & (Y < 450) & ~spire
red = ndi.binary_fill_holes(ndi.binary_closing(red, iterations=3)) & on & ~spire
brown = (r > b + 45) & (r < 215) & (g < 170) & ~red & (Y < 450) & (Y > 300)
brown = ndi.binary_opening(ndi.binary_closing(brown, iterations=2), iterations=1)
brown &= ~c.below([(290, 440), (372, 418), (430, 392), (512, 362), (595, 392), (655, 418), (740, 440)])
lab, n = ndi.label(brown)
brown = np.isin(lab, 1 + np.flatnonzero(ndi.sum(brown, lab, range(1, n + 1)) > 150))
seam = c.below([(300, 330), (400, 272), (512, 258), (620, 268), (735, 318)])
c.claim([
    ('found', found),
    ('spire', spire),
    ('winUp', c.round(440, 582, 352, 500) | c.box(436, 588, 497, 532)),
    ('winLo', c.round(385, 519, 585, 730) | c.box(381, 522, 724, 758)),
    ('tiles', red & ~seam),
    ('roof', red & seam),
    ('eaveL', brown & (X < 512)),
    ('eaveR', brown & (X >= 512)),
    ('wall1', Y >= 747), ('wall2', Y >= 602), ('wall3', Y >= 452), ('wall4', c.box(366, 662, 300, 460)),
    ('roofRim', Y >= 0),
], on=on)
c.merge('roof', 'roofRim')
if len(sys.argv) > 4:
    overlay(c, sys.argv[4])
D = c.full_only()
lantern = D & c.box(695, 760, 632, 742)
porch = D & c.box(510, 745, 540, 910) & ~lantern
slots = c.write(sys.argv[3], 'c1', 'castle_1', 'found', 4, [
    ('wall', 'wall1'), ('wall', 'wall2'), ('window', 'winLo'), ('wall', 'wall3'), ('wall', 'wall4'), ('window', 'winUp'),
    ('floor', 'eaveL'), ('floor', 'eaveR'), ('roof', 'roof'), ('tiles', 'tiles'), ('spire', 'spire')],
    extra=[('porch', porch), ('lantern', lantern)], decor=bush)
print(' '.join(slots))

# Cut building 4, the windmill, by its drawing, bottom-up.
#   melnica4_cut.png : the mill without the miller, the porch with its door and the lanterns
#   melnica4.png     : with them
# The sails are four blades along their diagonals; the roof splits along a row of tiles (lower rows with the
# rafters, upper rows); the balcony is its wood and flower box only, the stone behind the rails is the wall;
# walls follow the stone rows; sacks, barrels and bushes stand on the ground with the foundation.
# Usage: python3 cut_house_4.py <clean.png> <full.png> <output folder> [map.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import figure_of
from recut import Cut, overlay

c = Cut(sys.argv[1], sys.argv[2])
X, Y, C, on = c.X, c.Y, c.C, c.on
r, g, b = C[..., 0], C[..., 1], C[..., 2]
green = (g > r + 15) & (g > b + 15)
red = (r > 150) & (r - g > 70) & (r - b > 70)
wood = (r < 190) & (r > 60) & (r - b > 45) & (g < 135) & ~red
stone = (C.min(2) > 150) & (r - b < 70)

HX, HY = 522, 290
def blade(dx, dy):
    n = np.hypot(dx, dy); ux, uy = dx / n, dy / n
    along = (X - HX) * ux + (Y - HY) * uy
    across = np.abs(-(X - HX) * uy + (Y - HY) * ux)
    return (along > 0) & (((along >= 110) & (across <= 74)) | ((along < 110) & (across <= 20)))
hub = (X - HX) ** 2 + (Y - HY) ** 2 <= 44 ** 2

seam = c.below([(340, 300), (430, 215), (516, 200), (600, 215), (690, 300)])
roofzone = ((Y < 255) | ndi.binary_dilation(ndi.binary_closing(red | wood, iterations=2), iterations=2)) & (Y < 345)
balcony_wood = ndi.binary_opening(ndi.binary_closing((wood | green) & c.box(300, 700, 484, 640), iterations=2), iterations=1)
c.claim([
    ('found', (Y >= 905) | (c.box(220, 810, 770, 1000) & ~c.box(330, 660, 560, 900) & (Y >= 770)) |
              c.box(275, 480, 780, 930) & ~c.box(475, 660, 560, 900)),
    ('spire', c.box(476, 556, 20, 112)),
    ('hub', hub | blade(-267, -235)),
    ('sail2', blade(278, -245)), ('sail3', blade(-317, 225)), ('sail4', blade(293, 235)),
    ('winUp', c.round(452, 584, 356, 500)),
    ('winLo', c.round(334, 446, 640, 745) | c.box(330, 452, 740, 772)),
    ('tiles', roofzone & ~seam), ('roof', roofzone & seam),
    ('balcony', balcony_wood),
    ('up', Y < 560), ('mid', Y < 745), ('low', Y >= 0),
])
if len(sys.argv) > 4:
    overlay(c, sys.argv[4])

F = c.F
aFm = c.aF > 0.5
diff = (np.abs(F - C).sum(2) > 60) & aFm
diff = ndi.binary_fill_holes(ndi.binary_closing(ndi.binary_opening(diff, iterations=2), iterations=4)) & aFm
lantR = diff & c.box(662, 740, 478, 620)
lantL = diff & c.box(436, 500, 664, 772)
miller = diff & (X >= 618) & (Y >= 646) & ~lantR
Fr, Fg, Fb = F[..., 0], F[..., 1], F[..., 2]
miller &= ~((X < 650) & ((Y < 762) | (Y > 832))) & ~((Fr > 150) & (Fr - Fg > 70) & (Y < 700) & (X < 690))
miller = figure_of(sys.argv[2], 'miller', miller)
lantL &= ~miller; lantR &= ~miller
porch_all = diff & c.box(424, 700, 570, 935) & ~miller & ~lantL & ~lantR
door = porch_all & c.round(504, 602, 688, 846)
porch = porch_all & ~door

slots = c.write(sys.argv[3], 'c4', 'castle_4', 'found', 6, [
    ('wall', 'low'), ('wall', 'mid'), ('window', 'winLo'), ('floor', 'balcony'), ('wall', 'up'), ('window', 'winUp'),
    ('sails', 'hub'), ('sails', 'sail2'), ('sails', 'sail3'), ('sails', 'sail4'),
    ('roof', 'roof'), ('tiles', 'tiles'), ('spire', 'spire')],
    extra=[('door', door), ('porch', porch), ('lantern', lantL), ('lantern', lantR), ('miller', miller)])
print(' '.join(slots))

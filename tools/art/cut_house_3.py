# Cut building 3, the gingerbread house, by its drawing, bottom-up.
#   pryanik3_cut.png : the house without the baker, lanterns and jelly steps
#   pryanik3.png     : with them
# Windows are their icing frames with the lollipop shutters, the red candy on top and the holly under them;
# the roof halves are the pink and white icing with the drips; gingerbread between the drips is the gable.
# Usage: python3 cut_house_3.py <clean.png> <full.png> <output folder> [map.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from recut import Cut, overlay

c = Cut(sys.argv[1], sys.argv[2])
X, Y, C = c.X, c.Y, c.C
r, g, b = C[..., 0], C[..., 1], C[..., 2]
green = c.color('green') & c.on
ginger = (r > 120) & (r - b > 80) & (g > 75) & (g < 170) & (r - g > 35) & (b < 95)
inner = c.below([(110, 468), (250, 446), (515, 182), (790, 446), (915, 468)])
icing = (C.min(2) > 195) | ((r > 190) & (b > 110) & (r - g > 50))     # white and pink icing
drips = inner & icing & ~c.below([(110, 520), (250, 500), (515, 240), (790, 500), (915, 520)])
lab, _ = ndi.label(drips | (~inner & c.on))
drips &= np.isin(lab, np.unique(lab[~inner & c.on]))


def window(cx, cy, rx, ry, sh_dx, sh_ry, top):
    """Icing frame, lollipop shutters on both sides, the candy on top, the holly under it."""
    m = c.ell(cx, cy, rx, ry) | c.ell(cx - sh_dx, cy + 8, 27, sh_ry) | c.ell(cx + sh_dx, cy + 8, 27, sh_ry)
    m |= c.ell(cx, top, 15, 15)
    return m


c.claim([
    ('found', (Y >= 786) | (green & (Y >= 690))),
    ('spire', c.box(462, 572, 15, 112)),
    ('chimney', c.box(612, 822, 0, 280) & ~c.below([(600, 250), (760, 262)]) | c.box(612, 760, 100, 280) & ~icing | c.box(612, 822, 0, 150)),
    ('pillarL', c.box(200, 264, 420, 792) & ~green),
    ('pillarR', c.box(760, 826, 420, 792) & ~green),
    ('door', c.round(414, 618, 462, 768)),
    ('winL', window(342, 600, 60, 76, 66, 48, 532) | c.ell(340, 680, 50, 24)),
    ('winR', window(695, 600, 60, 76, 66, 48, 532) | c.ell(695, 680, 50, 24)),
    ('winUp', window(515, 345, 76, 94, 92, 60, 266) | c.ell(515, 430, 16, 16)),
    ('roofL', ((~inner & ~ginger) | drips) & (X < 516)),
    ('roofR', ((~inner & ~ginger) | drips) & (X >= 516)),
    ('wallL', (Y >= 456) & (X < 516)),
    ('wallR', (Y >= 456) & (X >= 516)),
    ('gable', Y >= 0),
])
if len(sys.argv) > 4:
    overlay(c, sys.argv[4])

F = c.F
aFm = c.aF > 0.5
diff = (np.abs(F - C).sum(2) > 60) & aFm
diff = ndi.binary_fill_holes(ndi.binary_closing(ndi.binary_opening(diff, iterations=2), iterations=4)) & aFm
lantL = diff & c.box(100, 215, 430, 620)
lantR = diff & c.box(810, 925, 430, 620)
baker = (diff & c.box(225, 446, 460, 845) | diff & c.box(446, 492, 600, 760)) & ~lantL
Fr, Fg, Fb = F[..., 0], F[..., 1], F[..., 2]
baker &= ~((Fr > 180) & (Fr - Fg > 60) & (Fb > 120)) & ~((Fg > Fr + 15) & (Fg > Fb + 15)) & (Y < 838)
baker = ndi.binary_fill_holes(ndi.binary_opening(baker, iterations=2))
porch = diff & c.box(420, 665, 660, 900) & ~baker

slots = c.write(sys.argv[3], 'c3', 'castle_3', 'found', 6, [
    ('brick', 'pillarL'), ('brick', 'pillarR'), ('wall', 'wallL'), ('wall', 'wallR'),
    ('door', 'door'), ('window', 'winL'), ('window', 'winR'), ('wall', 'gable'), ('window', 'winUp'),
    ('roof', 'roofL'), ('tiles', 'roofR'), ('chimney', 'chimney'), ('spire', 'spire')],
    extra=[('porch', porch), ('lantern', lantL), ('lantern', lantR), ('baker', baker)])
print(' '.join(slots))

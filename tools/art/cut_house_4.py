# Cut building 4, the windmill, into its building steps, bottom-up.
#   melnica4_cut.png : the mill without the miller, the porch with its door and the lanterns
#   melnica4.png     : the same mill with them (they are taken from the difference)
# Usage: python3 cut_house_4.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import cutout, arch, write_castle, split_even

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C = np.array(Image.open(clean_p).convert('RGB')).astype(np.int32)
F = np.array(Image.open(full_p).convert('RGB')).astype(np.int32)
H, W = C.shape[:2]
Y, X = np.mgrid[0:H, 0:W]
aC, aF = cutout(C), cutout(F)
on = aC > 0.02
r, g, b = C[..., 0], C[..., 1], C[..., 2]
def box(x0, x1, y0, y1): return (X >= x0) & (X <= x1) & (Y >= y0) & (Y <= y1)

green = (g > r + 15) & (g > b + 15)
red = (r > 150) & (r - g > 70) & (r - b > 70)
brown = (r < 175) & (r > 60) & (r - b > 45) & (g < 120)

# the sails: four blades on two diagonals through the hub; near the hub only the spar, further out the whole frame
HX, HY = 522, 290
def blade(dx, dy):
    n = np.hypot(dx, dy); ux, uy = dx / n, dy / n
    along = (X - HX) * ux + (Y - HY) * uy
    across = np.abs(-(X - HX) * uy + (Y - HY) * ux)
    return on & (along > 0) & (((along >= 110) & (across <= 74)) | ((along < 110) & (across <= 20)))
hub = on & ((X - HX) ** 2 + (Y - HY) ** 2 <= 44 ** 2)
blades = [blade(-267, -235), blade(278, -245), blade(-317, 225), blade(293, 235)]
blades[0] = blades[0] | hub
sails = np.zeros((H, W), bool)
for k in range(4):
    blades[k] &= ~sails
    sails |= blades[k]

spire = on & (Y < 112) & (np.abs(X - 516) < 40) & ~sails
roof = on & (Y < 340) & ~sails & ~spire & ((Y < 252) | red | brown)
roof = ndi.binary_opening(roof, iterations=1)
roofL, roofR = roof & (X < 516), roof & (X >= 516)
upper_win = arch(X, Y, 452, 584, 356, 500) & on & ~sails
low_win = arch(X, Y, 334, 446, 640, 770) & on
balcony = on & box(300, 695, 484, 636) & ~sails & ~upper_win
decor = on & (((green & (Y > 740)) | box(280, 476, 784, 928)) & (Y < 940))
found = on & (Y >= 905) & ~decor
tower = on & ~sails & ~spire & ~roof & ~upper_win & ~low_win & ~balcony & ~decor & ~found
upper = tower & (Y < 484)
# loose bits of the upper tower (brackets under the eaves, between the sails) go with the roof
_lab, _n = ndi.label(upper)
if _n > 1:
    _main = _lab == 1 + int(np.argmax(ndi.sum(upper, _lab, range(1, _n + 1))))
    roofL |= upper & ~_main & (X < 516); roofR |= upper & ~_main & (X >= 516)
    upper = _main
lower = tower & (Y >= 484)
# the sacks, barrels and bushes go up with the lower walls on their side
lower |= decor
wallL, wallR = lower & (X < 500), lower & (X >= 500)

SLOTS = []
for m in split_even(found, X, 6):
    SLOTS.append(('clay', m, 'clean'))
SLOTS += [('wall', wallL, 'clean'), ('wall', wallR, 'clean'), ('window', low_win, 'clean'),
          ('floor', balcony, 'clean'), ('wall', upper, 'clean'), ('window', upper_win, 'clean'),
          ('roof', roofL, 'clean'), ('tiles', roofR, 'clean'), ('spire', spire, 'clean')]
SLOTS += [('sails', blades[k], 'clean') for k in range(4)]

aFm = aF > 0.5
diff = (np.abs(F - C).sum(2) > 60) & aFm
diff = ndi.binary_fill_holes(ndi.binary_closing(ndi.binary_opening(diff, iterations=2), iterations=4)) & aFm
lantR = diff & box(662, 740, 478, 620)
lantL = diff & box(436, 500, 664, 772)
miller = diff & (X >= 618) & (Y >= 646) & ~lantR
Fr, Fg, Fb = F[..., 0], F[..., 1], F[..., 2]
# the porch's post and roof edge next to him are not the miller
miller &= ~((X < 650) & ((Y < 762) | (Y > 832))) & ~((Fr > 150) & (Fr - Fg > 70) & (Y < 700) & (X < 690))
porch_all = diff & box(424, 700, 570, 935) & ~miller & ~lantL & ~lantR
door = porch_all & arch(X, Y, 504, 602, 688, 846)
porch = porch_all & ~door
SLOTS += [('door', door, 'full'), ('porch', porch, 'full'), ('lantern', lantL, 'full'), ('lantern', lantR, 'full'),
          ('miller', miller, 'full')]

write_castle(out, 'c4', 'castle_4', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None,
             grey_clay=False)

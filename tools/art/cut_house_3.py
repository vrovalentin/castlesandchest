# Cut building 3, the gingerbread house, into its building steps, bottom-up.
#   pryanik3_cut.png : the house without the baker, lanterns and jelly steps (the house is taken from it)
#   pryanik3.png     : the same house with them (they are taken from the difference)
# Both are 1024x1024 on white and line up pixel for pixel.
# Usage: python3 cut_house_3.py <clean.png> <full.png> <output folder> [preview.png]
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

pink = (r > 180) & (r - g > 60) & (b > 80) & (b - g > -10)
cream = (r > 205) & (g > 185) & (b > 165)
green = (g > r + 15) & (g > b + 15)

# the lollipop on the ridge and the chimney with its smoke
lolly = on & (Y < 112) & (np.abs(X - 518) < 58)
chimney = on & (X >= 616) & (X <= 830) & ((Y < 205) | ((Y < 282) & ~cream)) & ~pink & ~lolly
# the gable: a triangle under the ridge; everything above the eaves outside it (and the icing at its edge) is roof
gable = (Y > 190) & (np.abs(X - 512) < (Y - 190) * 1.16 - 6)
upper_win = ((((X - 516) / 128.0) ** 2 + ((Y - 352) / 100.0) ** 2) <= 1) & on
upper_win |= box(495, 535, 245, 285) & on | box(495, 535, 410, 448) & on      # the red candies above and below
roof = on & (Y < 462) & ~lolly & ~chimney & ~upper_win & (~gable | ((pink | cream) & (Y <= 300)))
roof |= on & (Y < 470) & ((X < 205) | (X > 822)) & ~chimney
roof &= ~((Y > 300) & gable & ~pink)            # keep the swirls and gingerbread out of the roof
roof = ndi.binary_opening(roof, iterations=1)
roofL, roofR = roof & (X < 516), roof & (X >= 516)

# candy-cane pillars, door, windows
pillarL = on & box(200, 262, 448, 792) & ~green
pillarR = on & box(762, 824, 448, 792) & ~green
door = arch(X, Y, 414, 618, 462, 770) & on
winL = on & box(248, 432, 518, 702) & ~green
winR = on & box(596, 792, 518, 702) & ~green & ~pillarR
holly = on & ((box(290, 390, 650, 700)) | box(640, 745, 650, 700))   # window boxes with their leaves
winL |= holly & (X < 512); winR |= holly & (X >= 512)

# cookie-stone foundation, six pieces left to right
found = on & (Y >= 792) & ~green
# body of the house: lower walls left and right of the door (with the bushes), and the gable above
body = on & ~roof & ~lolly & ~chimney & ~upper_win & ~found & ~pillarL & ~pillarR & ~door & ~winL & ~winR
lower = body & (Y >= 470)
gable_wall = body & (Y < 470)
# crumbs of smoke and icing outside the gable go with the chimney or the roof, not hang in the air with the gable
stray = gable_wall & ~gable & (Y < 300)
chimney |= stray & (X >= 600) & (X <= 830)
roofL |= stray & ~chimney & (X < 516); roofR |= stray & ~chimney & (X >= 516)
gable_wall &= ~stray
wallL, wallR = lower & (X < 516), lower & (X >= 516)

SLOTS = []
for m in split_even(found, X, 6):
    SLOTS.append(('clay', m, 'clean'))
# the candy-cane pillars first: the eave beam over the walls rests on them
SLOTS += [('brick', pillarL, 'clean'), ('brick', pillarR, 'clean'),
          ('wall', wallL, 'clean'), ('wall', wallR, 'clean'),
          ('door', door, 'clean'), ('window', winL, 'clean'), ('window', winR, 'clean'),
          ('wall', gable_wall, 'clean'), ('window', upper_win, 'clean'),
          ('roof', roofL, 'clean'), ('tiles', roofR, 'clean'), ('chimney', chimney, 'clean'),
          ('spire', lolly, 'clean')]

# things only in the full picture
aFm = aF > 0.5
diff = (np.abs(F - C).sum(2) > 60) & aFm
diff = ndi.binary_fill_holes(ndi.binary_closing(ndi.binary_opening(diff, iterations=2), iterations=4)) & aFm
lantL = diff & box(100, 215, 430, 620)
lantR = diff & box(810, 925, 430, 620)
baker = (diff & box(225, 446, 460, 845) | diff & box(446, 492, 600, 760)) & ~lantL
Fr, Fg, Fb = F[..., 0], F[..., 1], F[..., 2]
# the window shutter, the bush and the ground beside him changed a little too: not the baker
baker &= ~((Fr > 180) & (Fr - Fg > 60) & (Fb > 120)) & ~((Fg > Fr + 15) & (Fg > Fb + 15)) & (Y < 838)
baker = ndi.binary_fill_holes(ndi.binary_opening(baker, iterations=2))
porch = diff & box(420, 665, 660, 900) & ~baker
SLOTS += [('porch', porch, 'full'), ('lantern', lantL, 'full'), ('lantern', lantR, 'full'), ('baker', baker, 'full')]

write_castle(out, 'c3', 'castle_3', SLOTS, C, F, aC, aF, on, found, sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False)

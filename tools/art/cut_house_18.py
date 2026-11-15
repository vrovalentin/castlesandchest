# Cut building 18, the boyar terem, by its drawing, bottom-up.
#   terem_clean.png : without Emelya on his stove, the sled and the lanterns
#   terem_full2.png : with them, made by terem_figure.py from emelya_src.png (Emelya drawn on his own)
# The firs stand on the site from the start. Then the log basement, the gallery with its balustrade, carved
# columns and windows in their nalichniki, the porch with its stairs and roof, the upper storeys with snowy
# ledges, the bell tower, the tent roofs and onion domes, the golden roosters last.
# Usage: python3 cut_house_18.py <clean.png> <full.png> <output folder> [map.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from recut import Cut, overlay

clean_p, full_p = sys.argv[1], sys.argv[2]
c = Cut(clean_p, full_p)
X, Y = c.X, c.Y
C = c.C
r, g, b = C[..., 0], C[..., 1], C[..., 2]

# Firs: dark green needles and the snow lying on them, in the two corners.
green = (g > r + 8) & (g > b - 10) & (C.max(2) < 170)
corner = c.box(30, 305, 640, 900) | c.box(790, 1000, 640, 905)
fir = ndi.binary_closing(green & corner, iterations=6)
fir = ndi.binary_dilation(fir, iterations=6) & corner & c.on
fir = ndi.binary_fill_holes(fir)
# keep the logs of the basement and the gallery corner out of the firs
logs = (r > g + 25) & (g > b + 15) & (C.min(2) < 200)
fir &= ~(ndi.binary_dilation(logs & ~ndi.binary_dilation(green, iterations=4), iterations=1) & (Y < 800))
lab, n = ndi.label(fir)
fir = np.isin(lab, 1 + np.flatnonzero(ndi.sum(fir, lab, range(1, n + 1)) > 2000)) & c.on
on = c.on & ~fir

found = c.below([(0, 795), (368, 800), (372, 824), (642, 824), (646, 800), (1024, 795)])

def onion(cx, cy, rx, ry):
    """An onion dome: the round body and its pointed top."""
    return c.ell(cx, cy, rx, ry) | c.poly((cx, cy - ry * 1.45), (cx - rx * 0.45, cy - ry * 0.7), (cx + rx * 0.45, cy - ry * 0.7))

c.claim([
    ('found', found),
    # golden roosters with their balls and stems
    ('rkL', c.box(200, 244, 140, 214)), ('rkDL', c.box(362, 404, 100, 158)), ('rkC', c.box(480, 545, 4, 96)),
    ('rkDR', c.box(617, 664, 100, 158)), ('rkB', c.box(795, 848, 36, 112)),
    # windows in their carved frames
    ('wL1', c.box(172, 238, 414, 528)), ('wLC', c.box(350, 410, 400, 502)),
    ('wC1', c.box(446, 506, 364, 474)), ('wC2', c.box(507, 569, 364, 474)),
    ('wRC', c.box(608, 670, 400, 504)), ('wB', c.box(797, 856, 436, 524)),
    ('emblem', c.box(458, 560, 266, 350)),
    ('g1', c.box(204, 249, 604, 682)), ('g2', c.box(313, 384, 584, 686)), ('g3', c.box(618, 694, 584, 686)),
    ('g4', c.box(764, 805, 604, 682)), ('g5', c.box(816, 860, 608, 682)),
    ('door', c.box(438, 576, 586, 716)),
    ('stairs', c.box(370, 642, 680, 824)),
    ('porchRoof', c.poly((507, 455), (370, 556), (372, 604), (644, 604), (642, 556))),
    ('cols', c.box(92, 120, 588, 690) | c.box(178, 202, 590, 690) | c.box(252, 280, 590, 690) | c.box(404, 434, 585, 690)
             | c.box(578, 606, 585, 690) | c.box(722, 750, 588, 690) | c.box(884, 910, 595, 695)),
    ('galRoofL', c.poly((76, 560), (150, 528), (390, 522), (390, 592), (76, 592)) | c.hanging(76, 390, 592, 625)),
    ('galRoofR', c.poly((625, 522), (880, 528), (928, 556), (928, 600), (625, 596)) | c.hanging(625, 928, 600, 630)),
    ('balL', c.box(90, 412, 668, 734)), ('balR', c.box(600, 915, 668, 734)),
    ('baseL', c.box(230, 440, 726, 800)), ('baseR', c.box(575, 810, 726, 800)),
    ('groundL', (Y >= 585) & (X < 440)), ('groundR', (Y >= 585) & (X > 575)),
    # bell tower
    ('bellTent', c.poly((818, 104), (774, 234), (864, 234))),
    ('bellBrim', c.box(736, 900, 222, 266) | c.hanging(736, 900, 266, 290)),
    ('belfry', c.box(758, 875, 266, 348)),
    ('bellLedge', c.box(750, 892, 340, 394)),
    ('bellBody', c.box(750, 890, 394, 585)),
    ('farRoof', c.box(866, 940, 430, 528)), ('farCap', c.box(866, 945, 522, 585)),
    # left tower
    ('tentL', c.poly((226, 200), (156, 370), (294, 370))),
    ('brimL', c.box(106, 304, 352, 404) | c.hanging(106, 304, 404, 445)),
    ('towerL', c.box(140, 300, 404, 560)),
    ('backL', c.box(276, 324, 410, 506)), ('backR', c.box(686, 782, 426, 512)),
    # domes
    ('onionL', onion(381, 208, 48, 48)), ('onionR', onion(636, 208, 48, 48)), ('onionC', onion(507, 150, 74, 58)),
    ('drumL', c.box(343, 418, 250, 322)), ('drumR', c.box(598, 674, 250, 322)),
    ('gable', c.poly((510, 172), (392, 350), (628, 350))),
    ('roofLC', c.box(300, 445, 290, 398)), ('roofRC', c.box(588, 726, 290, 398)),
    ('ledgeL', c.box(236, 445, 498, 585)), ('ledgeR', c.box(588, 800, 496, 585)),
    ('wallLC', c.box(296, 445, 380, 500)), ('wallC', c.box(440, 590, 340, 500)), ('wallRC', c.box(585, 720, 380, 500)),
    ('rest', Y >= 0),
], on=on)
c.merge('onionC', 'rest')
# fewer, bigger steps where two pieces go together
c.merge('g4', 'g5'); c.merge('wC1', 'wC2'); c.merge('farRoof', 'farCap'); c.merge('wallLC', 'backL'); c.merge('wallRC', 'backR')
c.merge('onionL', 'drumL'); c.merge('onionR', 'drumR'); c.merge('gable', 'emblem')
if len(sys.argv) > 4:
    overlay(c, sys.argv[4])

# Emelya, his stove, its lanterns and the sled were drawn on their own and put onto the clean terem
# (terem_figure.py): the figure's own outline is the mask, not the difference of two pictures.
fig_p = full_p.replace('.png', '_figure.png')
D = np.array(Image.open(fig_p))[..., 3] > 110 if os.path.exists(fig_p) else c.full_only()
# the lanterns hang on the stove's sides: only their dark iron and lit glass, not the white stove behind them
Fu = c.F
stove = (Fu.min(2) > 175) & (Fu.max(2) - Fu.min(2) < 40)
def lantern(x0, x1):
    m = D & c.box(x0, x1, 708, 784) & ~stove
    lab, n = ndi.label(m)
    m = np.isin(lab, 1 + np.argmax(ndi.sum(m, lab, range(1, n + 1)))) if n else m
    # its light parts (the top ring, glints) inside its outline go with it: fill each row and column between its ends
    rows, cols = np.zeros_like(m), np.zeros_like(m)
    for y in np.flatnonzero(m.any(1)):
        xs = np.flatnonzero(m[y]); rows[y, xs[0]:xs[-1] + 1] = True
    for x in np.flatnonzero(m.any(0)):
        ys = np.flatnonzero(m[:, x]); cols[ys[0]:ys[-1] + 1, x] = True
    m = D & c.box(x0, x1, 700, 784) & (rows & cols | m)
    lab, n = ndi.label(m)
    return np.isin(lab, 1 + np.argmax(ndi.sum(m, lab, range(1, n + 1)))) if n else m
lanL = lantern(594, 648)
lanR = lantern(758, 814)
sled = D & (X < 578) & ~lanL & ~lanR
emelya = D & ~sled & ~ndi.binary_dilation(lanL | lanR, iterations=3)
lab, n = ndi.label(emelya)
emelya = np.isin(lab, 1 + np.flatnonzero(ndi.sum(emelya, lab, range(1, n + 1)) > 400))   # no slivers round the lanterns
build = [
    ('wall', 'baseL'), ('wall', 'baseR'), ('floor', 'balL'), ('floor', 'balR'),
    ('wall', 'groundL'), ('wall', 'groundR'), ('door', 'door'), ('porch', 'stairs'), ('brick', 'cols'),
    ('window', 'g1'), ('window', 'g2'), ('window', 'g3'), ('window', 'g4'),
    ('roof', 'galRoofL'), ('roof', 'galRoofR'), ('roof', 'porchRoof'),
    ('floor', 'ledgeL'), ('floor', 'ledgeR'),
    ('wall', 'towerL'), ('wall', 'wallLC'), ('wall', 'wallC'), ('wall', 'wallRC'), ('wall', 'bellBody'),
    ('tiles', 'farRoof'),
    ('window', 'wL1'), ('window', 'wLC'), ('window', 'wC1'), ('window', 'wRC'), ('window', 'wB'),
    ('floor', 'brimL'), ('floor', 'bellLedge'), ('bells', 'belfry'), ('floor', 'bellBrim'),
    ('tiles', 'roofLC'), ('tiles', 'roofRC'), ('roof', 'gable'),
    ('tiles', 'tentL'), ('tiles', 'bellTent'),
    ('roof', 'onionL'), ('roof', 'onionR'), ('roof', 'onionC'),
    ('spire', 'rkL'), ('spire', 'rkB'), ('spire', 'rkDL'), ('spire', 'rkDR'), ('spire', 'rkC'),
]
slots = c.write(sys.argv[3], 'c18', 'castle_18', 'found', 6, build,
                extra=[('sled', sled), ('emelya', emelya), ('lantern', lanL), ('lantern', lanR)], decor=fir)
print(' '.join(slots))

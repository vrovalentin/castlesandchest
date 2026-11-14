# Cut building 2, the castle with five towers, by its drawing, bottom-up.
#   castle_2_clean_src.png : without porches, lanterns, flower pots, the knight and the princess
#   castle_2_src.png       : with them
# Roofs are the red tiles with their brown rafters, windows round arches with frames and sills, the gate
# with its stone arch, battlements and the balcony are floors; walls are what is left of each tower.
# Usage: python3 cut_house_2.py <clean.png> <full.png> <output folder> [map.png]
import sys, os
import numpy as np
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from recut import Cut, overlay

c = Cut(sys.argv[1], sys.argv[2])
X, Y = c.X, c.Y
r, g, b = c.C[..., 0], c.C[..., 1], c.C[..., 2]
on = c.on

red = (r > 160) & (r > g + 115) & (r > b + 100)
gold = (r > 180) & (g > 90) & (b < 90) & (r > g + 30) & ~red
brown = (r > b + 50) & (r < 200) & (g < 140) & ~red & ~gold
roofish = ndi.binary_dilation(ndi.binary_fill_holes(ndi.binary_closing(red | brown, iterations=3)), iterations=2) & on
roofish |= ndi.binary_dilation(roofish, iterations=6) & (r > g + 45) & (r > b + 40) & (r > 80) & on

towers = [  # spire box, roof cone (apex, left, right), eave box
    ((125, 190, 255, 362), ((157, 350), (28, 512), (272, 512)), (55, 255, 495, 532)),
    ((290, 360, 140, 248), ((325, 238), (212, 385), (428, 385)), (240, 405, 370, 400)),
    ((475, 550, 5, 122), ((512, 108), (372, 285), (652, 285)), (400, 625, 268, 300)),
    ((663, 730, 140, 248), ((698, 238), (595, 385), (812, 385)), (618, 785, 370, 400)),
    ((833, 900, 255, 362), ((866, 350), (752, 512), (996, 512)), (768, 968, 495, 532)),
]
order = [('found', (Y >= 876) | c.box(340, 685, 908, 1000))]
for i, (sp, _, _) in enumerate(towers, 1):
    order.append((f'spire{i}', c.box(*sp) & ~roofish))
order += [
    ('gate', c.round(384, 640, 604, 912)),
    ('win1', c.round(104, 194, 670, 778) | c.box(102, 197, 772, 804)),
    ('win5', c.round(830, 920, 670, 778) | c.box(827, 922, 772, 804)),
    ('win2', c.round(278, 373, 390, 492) | c.box(270, 380, 488, 520)),
    ('win4', c.round(651, 746, 390, 492) | c.box(644, 752, 488, 520)),
    ('win3', c.round(444, 582, 288, 448)),
]
for i, (_, (apex, bl, br), eave) in enumerate(towers, 1):
    order.append((f'roof{i}', ndi.binary_dilation(c.poly(apex, br, bl) | c.box(*eave), iterations=22) & roofish))
order += [
    ('bat1', c.box(30, 264, 530, 652)), ('bat5', c.box(758, 994, 530, 652)),
    ('balcony', c.box(382, 642, 460, 592)),
    ('lo1', c.box(40, 245, 640, 900)), ('lo5', c.box(780, 990, 640, 900)),
    ('up2', c.box(232, 410, 300, 640)), ('lo2', c.box(232, 410, 640, 900)),
    ('up4', c.box(612, 792, 300, 640)), ('lo4', c.box(612, 792, 640, 900)),
    ('up3', c.box(380, 645, 120, 470)), ('lo3', c.box(380, 645, 560, 912)),
]
c.claim(order)
if len(sys.argv) > 4:
    overlay(c, sys.argv[4])

# ---------- things only in the full picture ----------
F, C, aF = c.F, c.C, c.aF
aFm = aF > 0.5
diff = (np.abs(F - C).sum(2) > 70) & aFm
W = c.W
def blob(region, close=4):
    m = ndi.binary_opening(diff & region, iterations=1)
    m = ndi.binary_fill_holes(ndi.binary_closing(m, iterations=close)) & region & aFm
    lab, n = ndi.label(m)
    if n == 0:
        return m
    sizes = ndi.sum(np.ones_like(lab), lab, range(1, n + 1))
    return np.isin(lab, [i + 1 for i, sz in enumerate(sizes) if sz > 150])
CX = 512
def porch_region(side):
    x0, x1, xs0, top = (140, 405, 148, 520)
    k = np.clip((Y - top) / (700 - top), 0, 1)
    pcx = (x0 + x1) / 2 + 3
    roofp = (Y >= top) & (Y < 700) & (np.abs(X - pcx) <= 26 + ((x1 - x0) / 2 - 26) * k)
    body = c.box(178, 395, 700, 870) | c.box(xs0, 352, 870, 978)
    m = (roofp | body) & aFm
    return m if side == 0 else np.roll(m[:, ::-1], 2 * CX - W + 1, axis=1)
porchL, porchR = porch_region(0), porch_region(1)
lantL = c.box(244, 300, 628, 732) & aFm & ~(F.min(2) > 200)
lantR = np.roll(lantL[:, ::-1], 2 * CX - W + 1, axis=1)
pots = np.zeros(on.shape, bool)
for x0 in (141, 305, 649, 813):
    pots |= blob(c.box(x0, x0 + 66, 778, 874), 3)
knight = ndi.binary_dilation(blob(c.box(582, 708, 712, 930), 8), iterations=3) & aFm
princess = blob(c.box(446, 580, 318, 548), 4)
pots &= ~knight
porchL &= ~lantL & ~pots
porchR &= ~lantR & ~pots & ~knight

slots = c.write(sys.argv[3], 'c2', 'castle_2', 'found', 15, [
    ('wall', 'lo1'), ('wall', 'lo2'), ('wall', 'lo3'), ('wall', 'lo4'), ('wall', 'lo5'),
    ('door', 'gate'), ('window', 'win1'), ('window', 'win5'),
    ('floor', 'bat1'), ('floor', 'balcony'), ('floor', 'bat5'),
    ('wall', 'up2'), ('wall', 'up3'), ('wall', 'up4'),
    ('window', 'win2'), ('window', 'win3'), ('window', 'win4'),
    ('roof', 'roof1'), ('roof', 'roof2'), ('roof', 'roof3'), ('roof', 'roof4'), ('roof', 'roof5'),
    ('spire', 'spire1'), ('spire', 'spire2'), ('spire', 'spire3'), ('spire', 'spire4'), ('spire', 'spire5')],
    extra=[('porch', porchL), ('porch', porchR), ('lantern', lantL), ('lantern', lantR), ('knight', knight), ('princess', princess)],
    decor=pots, decor_full=True, grey_clay=True)
print(' '.join(slots))

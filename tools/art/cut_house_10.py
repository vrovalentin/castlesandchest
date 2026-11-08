# Cut building 10, the ice palace, into its building steps, bottom-up, following the drawing:
# pointed windows with their frames, cones with their snow brims and icicles, snowy ledges and battlements
# as their own parts, the walls around them.
#   ledyanoi_zamok10_cut.png : without the snow queen, the stairs with their rails and the lanterns
#   ledyanoi_zamok10.png     : with them
# Usage: python3 cut_house_10.py <clean.png> <full.png> <output folder> [preview.png]
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(__file__))
from cutlib import cutout, write_castle, split_even, Shapes, full_only

clean_p, full_p, out = sys.argv[1], sys.argv[2], sys.argv[3]
C = np.array(Image.open(clean_p).convert('RGB')).astype(np.int32)
F = np.array(Image.open(full_p).convert('RGB')).astype(np.int32)
H, W = C.shape[:2]
S = Shapes(H, W); X, Y = S.X, S.Y
aC, aF = cutout(C, snow=True), cutout(F, snow=True)
# snow by the steps is as white as the background, so the cutout bites bays into it: close them in the snowy base
for a_ in (aC, aF):
    bay = ndi.binary_closing(a_ > 0.5, structure=np.ones((3, 3), bool), iterations=24) & (Y >= 760)
    a_[bay] = 1.0
on = aC > 0.02
light = C.min(2) > 200          # snow and icicles


def gothic(x0, x1, ya, ys, yb):
    """A pointed window: sides x0..x1, apex at ya, the arch springs at ys, bottom at yb."""
    w, h = x1 - x0, ys - ya
    r = (w * w / 4 + h * h) / w
    inL = (X - (x0 + r)) ** 2 + (Y - ys) ** 2 <= r * r
    inR = (X - (x1 - r)) ** 2 + (Y - ys) ** 2 <= r * r
    return ((Y >= ys) & (Y <= yb) & (X >= x0) & (X <= x1)) | ((Y < ys) & (Y >= ya) & inL & inR)


def cone(apex, bl, br):
    return S.poly(apex, br, bl)


def hanging(x0, x1, y0, y1):
    """Icicles: light pixels in a band under a brim or a ledge that hang from its edge (bright spots on the
    wall below, not joined to the edge, stay with the wall)."""
    band = S.box(x0, x1, y0, y1) & (C.min(2) > 205)
    lab, _ = ndi.label(band | S.box(x0, x1, y0 - 3, y0 - 1))
    top = np.unique(lab[y0 - 2, x0:x1 + 1])
    return band & np.isin(lab, top[top > 0])


def line_y(pts):
    """Per column, the height of a polyline through (x, y) points."""
    xs, ys = zip(*pts)
    return np.interp(np.arange(W), xs, ys)[None, :]


# The top of the snow mounds, rocks and bushes in front of the palace.
mound = Y >= line_y([(0, 760), (110, 762), (200, 775), (260, 782), (300, 800), (350, 815), (400, 842), (430, 852),
                     (595, 852), (630, 828), (680, 800), (730, 786), (780, 778), (830, 765), (1023, 760)])
earth = (C[..., 0] > C[..., 2] + 20) | ((C[..., 1] > C[..., 2] + 5) & (C[..., 1] > C[..., 0]))   # rocks, bushes
found = on & (mound | (earth & (Y >= 740)))
# snow bumps of the mounds that rise above the line belong to the mounds
lab, _ = ndi.label((light & (Y >= 735)) | found)
found |= on & np.isin(lab, np.unique(lab[found])) & (Y >= 735) & light

# Windows (frame included), the gate.
win = {
    'w1lo': gothic(196, 244, 664, 705, 758), 'w1up': gothic(179, 229, 452, 494, 558),
    'w2lo': gothic(320, 390, 597, 657, 748), 'w2up': gothic(332, 373, 350, 385, 418),
    'w3c': gothic(486, 542, 262, 310, 378), 'w3l': gothic(454, 481, 298, 321, 380), 'w3r': gothic(544, 571, 295, 318, 380),
    'w4lo': gothic(641, 699, 596, 645, 748), 'w4up': gothic(653, 692, 372, 405, 430),
    'w5lo': gothic(806, 855, 646, 688, 752), 'w5up': gothic(806, 854, 484, 525, 578),
}
gate = gothic(427, 593, 486, 627, 853)
# Sills under the windows, with their icicles, go with their window.
sills = {
    'w1lo': S.box(188, 252, 756, 772), 'w1up': S.box(172, 234, 556, 570),
    'w2lo': S.box(310, 396, 746, 764) | hanging(310, 396, 765, 800),
    'w4lo': S.box(628, 712, 746, 764) | hanging(628, 712, 765, 795),
    'w5lo': S.box(802, 864, 750, 768), 'w5up': S.box(800, 860, 576, 592),
}
for k, m in sills.items():
    win[k] = win[k] | m

# Roofs: cone + snow brim + icicles; spires on top.
spire = {1: S.box(194, 230, 200, 292), 2: S.box(342, 374, 115, 202), 3: S.box(494, 532, 5, 92),
         4: S.box(646, 684, 108, 192), 5: S.box(806, 844, 218, 305)}
roof = {
    1: cone((212, 280), (146, 412), (278, 412)) | S.box(130, 282, 392, 442) | hanging(128, 284, 442, 500),
    2: cone((358, 192), (303, 302), (412, 302)) | S.box(294, 414, 280, 326) | hanging(294, 414, 326, 350),
    3: cone((513, 82), (424, 230), (604, 230)) | S.box(416, 610, 205, 268) | hanging(416, 610, 268, 305),
    4: cone((665, 182), (607, 315), (740, 315)) | S.box(598, 746, 290, 345) | hanging(598, 746, 345, 430),
    5: cone((825, 298), (771, 440), (885, 440)) | S.box(760, 898, 422, 480) | hanging(760, 898, 480, 508),
}
# Snowy ledges, caps and battlements (floors between the storeys).
floor = {
    'l1': S.box(124, 302, 586, 642) | hanging(124, 302, 642, 695),          # left tower, between its storeys
    'b2': S.box(294, 410, 413, 476),                                          # battlements of tower 2
    'cap2': S.box(280, 392, 526, 575) | hanging(280, 392, 575, 625),         # snow on the lower block of tower 2
    'b3': S.box(408, 616, 372, 446),                                          # battlements of the middle tower
    'b4': S.box(598, 758, 424, 512) | hanging(598, 758, 512, 528),           # balcony of tower 4
    'cap4': S.box(632, 748, 526, 576) | hanging(632, 748, 576, 612),         # snow on the lower block of tower 4
    'l5': S.box(738, 892, 584, 636) | hanging(738, 892, 636, 662),           # right tower, between its storeys
}
# The gable over the gate: its snowy slopes are a roof, the ice frame with the snowflake a wall.
outerG = S.poly((510, 358), (362, 500), (364, 514), (656, 520), (658, 506))
innerG = S.poly((510, 400), (394, 508), (626, 513))
gable_roof = outerG & ~innerG
gable = innerG | S.box(382, 640, 505, 855)
# Roofs seen behind, between the towers.
back = {'bL': S.box(255, 395, 470, 560), 'bR': S.box(730, 790, 500, 600)}
# Walls, storey by storey; a tower's wall is a vertical band.
wall = {
    'lo1': S.box(138, 262, 640, 800), 'up1': S.box(148, 262, 440, 600), 'link1': S.box(255, 300, 555, 800),
    'lo2': S.box(286, 388, 560, 805), 'mid2': S.box(300, 404, 470, 560), 'up2': S.box(310, 404, 320, 418),
    'up3': S.box(436, 594, 250, 380),
    'lo4': S.box(636, 744, 560, 805), 'mid4': S.box(612, 748, 500, 560), 'up4': S.box(616, 740, 335, 430),
    'link5': S.box(738, 790, 600, 800), 'lo5': S.box(775, 892, 625, 790), 'up5': S.box(780, 880, 462, 590),
}

# Claim: the first shape holding a pixel gets it.
order = ([('found', found)] + [(f's{k}', m) for k, m in spire.items()] + [(k, m) for k, m in win.items()] +
         [('gate', gate)] + [(f'r{k}', m) for k, m in roof.items()] + [('gableRoof', gable_roof), ('gable', gable)] +
         [(k, m) for k, m in floor.items()] + [(k, m) for k, m in wall.items()] + [(k, m) for k, m in back.items()])
R, left = {}, on.copy()
for name, m in order:
    R[name] = m & left
    left &= ~R[name]
# what is left goes to the nearest part
names = [n for n, _ in order]
idx = np.zeros((H, W), np.int32)
for i, n in enumerate(names):
    idx[R[n]] = i + 1
_, (iy, ix) = ndi.distance_transform_edt(idx == 0, return_indices=True)
near = idx[iy, ix]
for i, n in enumerate(names):
    R[n] |= left & (near == i + 1)

SLOTS = [('clay', m, 'clean') for m in split_even(R['found'], X, 6)]
build = [
    ('wall', 'lo1'), ('wall', 'link1'), ('wall', 'lo2'), ('wall', 'gable'), ('wall', 'lo4'), ('wall', 'link5'), ('wall', 'lo5'),
    ('door', 'gate'), ('window', 'w1lo'), ('window', 'w2lo'), ('window', 'w4lo'), ('window', 'w5lo'),
    ('floor', 'l1'), ('floor', 'cap2'), ('floor', 'cap4'), ('floor', 'l5'),
    ('roof', 'bL'), ('roof', 'bR'), ('roof', 'gableRoof'),
    ('wall', 'up1'), ('wall', 'mid2'), ('wall', 'mid4'), ('wall', 'up5'),
    ('window', 'w1up'), ('window', 'w5up'),
    ('floor', 'b2'), ('floor', 'b3'), ('floor', 'b4'),
    ('wall', 'up2'), ('wall', 'up3'), ('wall', 'up4'),
    ('window', 'w2up'), ('window', 'w3c'), ('window', 'w3l'), ('window', 'w3r'), ('window', 'w4up'),
    ('roof', 'r1'), ('roof', 'r5'), ('tiles', 'r2'), ('tiles', 'r4'), ('roof', 'r3'),
    ('spire', 's1'), ('spire', 's5'), ('spire', 's2'), ('spire', 's4'), ('spire', 's3'),
]
SLOTS += [(part, R[k], 'clean') for part, k in build]

D = full_only(F, C, aF)
queen = D & S.box(438, 608, 515, 856)
lantL = D & S.box(300, 372, 758, 892)
lantR = D & S.box(656, 728, 758, 892)
stairs = D & S.box(205, 815, 690, 1000) & ~queen & ~lantL & ~lantR
SLOTS += [('porch', stairs, 'full'), ('lantern', lantL, 'full'), ('lantern', lantR, 'full'), ('princess', queen, 'full')]

if os.environ.get('CUT_MAP'):
    np.save(os.environ['CUT_MAP'], np.stack([R[n] for n in names]))
    open(os.environ['CUT_MAP'] + '.names', 'w').write(' '.join(names))
write_castle(out, 'c10', 'castle_10', SLOTS, C, F, aC, aF, on, R['found'], sys.argv[4] if len(sys.argv) > 4 else None, grey_clay=False, grow_back=True)

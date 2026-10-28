# Tap map of a castle: which slots cover each point of the picture, so a tap on the drawing finds its part.
# Writes <prefix>_hit.bytes: N×N pixels, 3 bytes each, a little-endian bit mask of the slots there
# (bit i = slot i), slightly grown so a fingertip near an edge still counts.
# Usage: python3 make_hitmap.py <castle art folder> <prefix, e.g. c1> [N=256]
import sys, glob, os, re
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

src, prefix = sys.argv[1], sys.argv[2]
n = int(sys.argv[3]) if len(sys.argv) > 3 else 256
files = sorted(f for f in glob.glob(f'{src}/{prefix}_[0-9][0-9]_*.png'))
assert files, 'no layers'
mask = np.zeros((n, n), np.uint32)
for f in files:
    i = int(re.search(rf'{prefix}_(\d\d)_', os.path.basename(f)).group(1))
    a = np.array(Image.open(f).getchannel('A').resize((n, n), Image.BILINEAR)) > 40
    a = ndi.binary_dilation(a, iterations=2)
    mask[a] |= np.uint32(1 << i)
out = np.zeros((n, n, 3), np.uint8)
out[..., 0] = mask & 0xff
out[..., 1] = (mask >> 8) & 0xff
out[..., 2] = (mask >> 16) & 0xff
open(f'{src}/{prefix}_hit.bytes', 'wb').write(out.tobytes())   # row 0 = top of the picture
print('slots', len(files), 'covered', int((mask > 0).sum()), 'of', n * n)

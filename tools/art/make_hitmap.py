# Tap map of a castle: which slots cover each point of the picture, so a tap on the drawing finds its part.
# Writes <prefix>_hit.bytes: N×N pixels, 8 bytes each, a little-endian 64-bit mask of the slots there
# (bit i = slot i, so up to 64 slots), slightly grown so a fingertip near an edge still counts.
# Usage: python3 make_hitmap.py <castle art folder> <prefix, e.g. c1> [N=256]
import sys, glob, os, re
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

src, prefix = sys.argv[1], sys.argv[2]
n = int(sys.argv[3]) if len(sys.argv) > 3 else 256
files = sorted(f for f in glob.glob(f'{src}/{prefix}_[0-9][0-9]_*.png'))
assert files, 'no layers'
mask = np.zeros((n, n), np.uint64)
for f in files:
    i = int(re.search(rf'{prefix}_(\d\d)_', os.path.basename(f)).group(1))
    a = np.array(Image.open(f).getchannel('A').resize((n, n), Image.BILINEAR)) > 40
    a = ndi.binary_dilation(a, iterations=2)
    mask[a] |= np.uint64(1 << i)
open(f'{src}/{prefix}_hit.bytes', 'wb').write(mask.astype('<u8').tobytes())   # row 0 = top of the picture
print('slots', len(files), 'covered', int((mask > 0).sum()), 'of', n * n)

# Preview of a cut building: the slot map and the building after a few steps, side by side.
# Usage: python3 preview_stages.py <cut folder> <prefix> <map.png> <out.png> [steps...]
import sys, glob
from PIL import Image
d, p, mp, outp = sys.argv[1:5]
fs = sorted(glob.glob(f'{d}/{p}_[0-9][0-9]_*.png'))
steps = [int(s) for s in sys.argv[5:]] or [len(fs) // 3, 2 * len(fs) // 3, len(fs)]
tiles = [Image.open(mp).convert('RGB').resize((400, 400))]
for n in steps:
    base = Image.new('RGBA', (1024, 1024), (150, 200, 240, 255))
    for f in fs[:n]:
        base.alpha_composite(Image.open(f))
    if n < len(fs):
        base.alpha_composite(Image.open(f'{d}/{p}_line_{n:02d}.png'))
    tiles.append(base.convert('RGB').resize((400, 400)))
out = Image.new('RGB', (400 * len(tiles), 400))
for i, t in enumerate(tiles):
    out.paste(t, (400 * i, 0))
out.save(outp)

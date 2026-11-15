# Puts a new figure into a built castle from an edited picture of the whole building (same framing).
# The figure is what differs between the edited picture and the castle without that layer; it becomes the new
# layer (trimmed, stored at the castle's 0.75 scale), the finished picture is composed again, the tap map redone.
# Usage: python3 replace_figure.py <castle dir> <prefix c10> <old layer name> <new layer name> <edited full.png> [grade json key]
import sys, os, re, glob, json, subprocess
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage as ndi
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from trim_layers import open_piece, read_rects

d, prefix, old, new, edited = sys.argv[1:6]
key = sys.argv[6] if len(sys.argv) > 6 else None
size, rects = read_rects(d)
layers = sorted(glob.glob(f'{d}/{prefix}_[0-9][0-9]_*.png'))
idx = lambda f: int(re.search(rf'{prefix}_(\d\d)_', os.path.basename(f)).group(1))

bg = Image.new('RGBA', (size, size))
target = None
for f in layers:
    if os.path.splitext(os.path.basename(f))[0] == old: target = f; continue
    bg.alpha_composite(open_piece(f))
assert target, 'no layer ' + old
B = np.asarray(bg).astype(float)
N = np.asarray(Image.open(edited).convert('RGB').resize((size, size), Image.LANCZOS)).astype(float) / 255
if key:   # the castle's colour pass (grade_castles.py)
    p = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'grade_done.json')))[key]
    rgb = N ** p['gamma']; L = (rgb @ [0.299, 0.587, 0.114])[..., None]
    rgb = L + (rgb - L) * p['sat']; N = np.clip(p['mean'] + (rgb - p['mean']) * p['contrast'], 0, 1)
N = N * 255

x, y, w, h = rects[old]
pad = 70
box = np.zeros((size, size), bool); box[max(0, y - pad):y + h + pad, max(0, x - pad):x + w + pad] = True
white = (N.min(-1) > 235)
diff = np.where(B[..., 3] > 128, np.abs(N - B[..., :3]).mean(-1), np.where(white, 0, 255))
m = (diff > 38) & box
m = ndi.binary_opening(m, iterations=2)
m = ndi.binary_closing(m, iterations=3)
lab, n = ndi.label(m)
if n > 1:
    sizes = ndi.sum(m, lab, range(1, n + 1)); m = lab == (1 + int(np.argmax(sizes)))
m = ndi.binary_fill_holes(m)
alpha = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))).astype(float)
ys, xs = np.where(alpha > 8)
x0, y0 = xs.min() // 16 * 16, ys.min() // 16 * 16
x1, y1 = min(size, (xs.max() + 16) // 16 * 16), min(size, (ys.max() + 16) // 16 * 16)
piece = np.dstack([N, alpha])[y0:y1, x0:x1].astype(np.uint8)
pw, ph = x1 - x0, y1 - y0
img = Image.fromarray(piece, 'RGBA').resize((pw * 3 // 4, ph * 3 // 4), Image.LANCZOS)
os.remove(target)
if os.path.exists(target + '.meta'):
    os.rename(target + '.meta', f'{d}/{new}.png.meta')
img.save(f'{d}/{new}.png', optimize=True)
txt = open(f'{d}/{prefix}_rects.txt').read().splitlines()
txt = [f'{new} {x0} {y0} {pw} {ph}' if l.split()[:1] == [old] else l for l in txt]
open(f'{d}/{prefix}_rects.txt', 'w').write('\n'.join(txt) + '\n')
# finished picture: all layers in order, at the stored 0.75 size
comp = Image.new('RGBA', (size, size))
decor = f'{d}/{prefix}_decor.png'
put = not os.path.exists(decor)
for f in sorted(glob.glob(f'{d}/{prefix}_[0-9][0-9]_*.png')):
    if not put and f.endswith('_porch.png'):   # bushes stand behind the porch (as cutlib composes it)
        comp.alpha_composite(open_piece(decor)); put = True
    comp.alpha_composite(open_piece(f))
if not put: comp.alpha_composite(open_piece(decor))
full = glob.glob(f'{d}/castle_*_full.png')[0]
# only the figure's box changes in the finished picture (a full re-compose differs a little at edges)
k = Image.open(full).size[0] / size
ux0, uy0, ux1, uy1 = min(x, x0), min(y, y0), max(x + w, x1), max(y + h, y1)
bx = (int(ux0 * k), int(uy0 * k), int(ux1 * k) + 1, int(uy1 * k) + 1)
fim = Image.open(full).convert('RGBA'); fim.paste(comp.resize(fim.size, Image.LANCZOS).crop(bx), bx[:2]); fim.save(full, optimize=True)
subprocess.run([sys.executable, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'make_hitmap.py'), d, prefix], check=True)
print('new rect', x0, y0, pw, ph, 'px', int(m.sum()))

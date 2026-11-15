# One colour pass over all buildings so they read as one set: each building's brightness, saturation and
# contrast are moved halfway towards the shared middle. The same curve goes on every picture of a building
# (layers, blueprint, decor, finished picture), so the pieces still match each other. Outline files are left alone.
# Pale-by-design buildings (ice palace, sand palace) keep their brightness and saturation.
# Usage: python3 grade_castles.py <Art/Resources/Castles> [--preview out.jpg]   (writes castles once: tools/art/grade_done.json)
import sys, os, math, json
import numpy as np
from PIL import Image

root = sys.argv[1]
preview = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None
LUM, SAT, CON, AMOUNT = 0.48, 0.70, 0.22, 0.5
PALE = {10, 14}
W = np.array([0.299, 0.587, 0.114])

def stats(img):
    a = img[..., 3] > 0.5
    rgb = img[..., :3][a]
    mx, mn = rgb.max(1), rgb.min(1)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0).mean()
    lum = rgb @ W
    return lum.mean(), sat, lum.std()

def params(i, img):
    m, s, c = stats(img)
    g = 1.0
    k = 1.0
    if i not in PALE:
        mt = m + (LUM - m) * AMOUNT
        g = math.log(mt) / math.log(m)
        k = float(np.clip(1 + (SAT / s - 1) * AMOUNT, 0.85, 1.3))
    ct = 1.0 if i in PALE else float(np.clip(1 + (CON / c - 1) * AMOUNT, 0.9, 1.1))
    return dict(gamma=round(g, 4), sat=round(k, 4), contrast=round(ct, 4), mean=round(float(m), 4))

def apply(img, p):
    rgb = np.clip(img[..., :3], 0, 1) ** p['gamma']
    L = (rgb @ W)[..., None]
    rgb = L + (rgb - L) * p['sat']
    rgb = p['mean'] + (rgb - p['mean']) * p['contrast']
    out = img.copy()
    out[..., :3] = np.clip(rgb, 0, 1)
    return out

def load(path): return np.asarray(Image.open(path).convert('RGBA')).astype(np.float32) / 255
def save(arr, path): Image.fromarray((arr * 255 + 0.5).astype(np.uint8), 'RGBA').save(path, optimize=True)

done_mark = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'grade_done.json')
allp, before, after = {}, [], []
for i in range(1, 100):
    d = os.path.join(root, f'castle_{i}')
    if not os.path.isdir(d): break
    full = load(os.path.join(d, f'castle_{i}_full.png'))
    p = params(i, full)
    p['mean'] = round(float(stats(full)[0]) ** p['gamma'], 4)
    allp[i] = p
    if preview:
        before.append(full); after.append(apply(full, p))
        continue
    if os.path.exists(done_mark): sys.exit('already graded: ' + done_mark)
    for f in sorted(os.listdir(d)):
        if not f.endswith('.png') or '_line_' in f: continue
        path = os.path.join(d, f)
        save(apply(load(path), p), path)
    print(i, p)

if preview:
    t = 300
    sheet = Image.new('RGB', (t * 6, t * 6 + 0), (236, 232, 244))
    for n, (b, a) in enumerate(zip(before, after)):
        col, row = n % 6, n // 6
        for k, im in enumerate((b, a)):
            pic = Image.fromarray((im * 255).astype(np.uint8), 'RGBA').resize((t, t), Image.LANCZOS)
            sheet.paste(pic, (col * t, (row * 2 + k) * t), pic)
    sheet.save(preview, quality=88)
    print(json.dumps(allp, indent=0))
else:
    open(done_mark, 'w').write(json.dumps(allp, indent=1))

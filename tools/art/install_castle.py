# Copies a cut building into Unity: Art/Resources/Castles/<castle_id>/, adds the tap map and .meta files
# (cloned from castle 2's, with fresh GUIDs) and, optionally, storage icons cut from its layers.
# Usage: python3 install_castle.py <cut folder> <castle_id> <prefix> [part=layer_index ...]
#   e.g. python3 install_castle.py /tmp/c3 castle_3 c3 chimney=15 baker=22
import sys, os, shutil, glob, uuid, subprocess
from PIL import Image
import numpy as np
from scipy import ndimage as ndi

src, cid, prefix = sys.argv[1], sys.argv[2], sys.argv[3]
# part=layer or part=layer@x0,y0,x1,y1 (only that box of the picture goes into the icon)
icons = dict(a.split('=') for a in sys.argv[4:])
root = os.path.join(os.path.dirname(__file__), '..', '..', 'unity', 'Assets', '_Game', 'Art', 'Resources')
c2 = os.path.join(root, 'Castles', 'castle_2')
dst = os.path.join(root, 'Castles', cid)
os.makedirs(dst, exist_ok=True)

def meta_from(template, target):
    if os.path.exists(target + '.meta'):
        return
    lines = open(template).read().splitlines()
    lines[1] = f'guid: {uuid.uuid4().hex}'
    open(target + '.meta', 'w').write('\n'.join(lines) + '\n')

if not os.path.exists(dst + '.meta'):
    meta_from(c2 + '.meta', dst)
for f in glob.glob(f'{src}/{prefix}_*.png') + glob.glob(f'{src}/{cid}_full.png'):
    t = os.path.join(dst, os.path.basename(f))
    shutil.copy(f, t)
    tmpl = 'c2_line_00.png' if '_line_' in f else 'c2_00_clay.png'
    meta_from(os.path.join(c2, tmpl + '.meta'), t)
# drop layers from an earlier cut that no longer exist
for f in glob.glob(f'{dst}/{prefix}_*.png'):
    if not os.path.exists(os.path.join(src, os.path.basename(f))):
        os.remove(f); os.remove(f + '.meta') if os.path.exists(f + '.meta') else None
subprocess.run([sys.executable, os.path.join(os.path.dirname(__file__), 'make_hitmap.py'), dst, prefix], check=True)
meta_from(os.path.join(c2, 'c2_hit.bytes.meta'), os.path.join(dst, f'{prefix}_hit.bytes'))
# keep only the box around each piece (the game reads where it goes from <prefix>_rects.txt)
subprocess.run([sys.executable, os.path.join(os.path.dirname(__file__), 'trim_layers.py'), dst], check=True)

# storage icons: the layer cropped to its picture and fitted into 256x256
for part, spec in icons.items():
    i, _, bx = spec.partition('@')
    f = glob.glob(f'{src}/{prefix}_{int(i):02d}_*.png')[0]
    im = Image.open(f)
    if bx:
        x0, y0, x1, y1 = map(int, bx.split(','))
        keep = Image.new('L', im.size, 0); keep.paste(255, (x0, y0, x1, y1))
        im.putalpha(Image.fromarray(np.minimum(np.array(im.getchannel('A')), np.array(keep))))
    # keep the main body: thin bits of neighbours that came along with the layer are cut off
    a = np.array(im)
    m = a[..., 3] > 40
    core = ndi.binary_opening(m, iterations=7)
    lab, n = ndi.label(core)
    if n > 0:
        big = 1 + int(np.argmax(ndi.sum(core, lab, range(1, n + 1))))
        keep = ndi.binary_dilation(lab == big, iterations=8) & m
        a[~keep] = 0
        im = Image.fromarray(a)
    im = im.crop(im.getchannel('A').point(lambda a: 255 if a > 40 else 0).getbbox())
    k = 244 / max(im.size)  # small pictures are scaled up too, so every icon fills its square
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new('RGBA', (256, 256))
    out.paste(im, ((256 - im.width) // 2, 256 - im.height - 4))
    t = os.path.join(root, 'Parts', f'part_{part}.png')
    out.save(t, optimize=True)
    meta_from(os.path.join(root, 'Parts', 'part_spire.png.meta'), t)
print('installed', cid, 'icons', list(icons))

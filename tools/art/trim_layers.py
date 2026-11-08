# Trims the layers, outlines and decor of installed buildings to the box around their picture, so the game
# does not download and hold the transparent rest of every 1024x1024 layer. Where each piece goes on the
# canvas is written to <prefix>_rects.txt (first line: canvas size; then "name x y w h", y from the top).
# Boxes are grown by a couple of pixels and to a multiple of 4 (compressed textures need it).
# A piece that is already trimmed keeps its rect, so running it again changes nothing.
# Usage: python3 trim_layers.py [--no-lines] [castle folder ...]   (no folders: every building in Resources/Castles)
#   --no-lines: delete the outline files, the game draws the outline of an empty slot from its layer
#        open_piece(path) gives back a trimmed piece on its full canvas (for the other tools).
import sys, os, glob, re
from PIL import Image

PAD = 2
CASTLES = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'unity', 'Assets', '_Game', 'Art',
                       'Resources', 'Castles')
HIT_META = os.path.join(CASTLES, 'castle_2', 'c2_hit.bytes.meta')


def prefix_of(folder):
    return 'c' + os.path.basename(os.path.normpath(folder)).split('_')[-1]


def read_rects(folder):
    p = os.path.join(folder, prefix_of(folder) + '_rects.txt')
    if not os.path.exists(p):
        return None, {}
    lines = open(p).read().split('\n')
    size = int(lines[0])
    rects = {}
    for ln in lines[1:]:
        if ln.strip():
            name, *v = ln.split()
            rects[name] = tuple(map(int, v))
    return size, rects


def open_piece(path):
    """The piece at path on its full canvas, whether trimmed or not."""
    im = Image.open(path).convert('RGBA')
    size, rects = read_rects(os.path.dirname(path))
    name = os.path.splitext(os.path.basename(path))[0]
    if size is None or name not in rects or im.size == (size, size):
        return im
    x, y, w, h = rects[name]
    full = Image.new('RGBA', (size, size))
    full.paste(im, (x, y))
    return full


def up4(v):
    return (v + 3) // 4 * 4


def set_meta_npot_none(png):
    """Trimmed pieces are not powers of two: keep their size (no rescale), so they line up."""
    meta = png + '.meta'
    if os.path.exists(meta):
        s = open(meta).read()
        s2 = re.sub(r'nPOTScale: \d', 'nPOTScale: 0', s)
        if s2 != s:
            open(meta, 'w').write(s2)


def trim(folder, drop_lines=False):
    p = prefix_of(folder)
    if drop_lines:
        for f in glob.glob(f'{folder}/{p}_line_*.png'):
            os.remove(f)
            os.remove(f + '.meta') if os.path.exists(f + '.meta') else None
    size, rects = read_rects(folder)
    files = sorted(glob.glob(f'{folder}/{p}_[0-9][0-9]_*.png') + glob.glob(f'{folder}/{p}_line_*.png')
                   + glob.glob(f'{folder}/{p}_decor.png'))
    canvas = None
    out = {}
    for f in files:
        name = os.path.splitext(os.path.basename(f))[0]
        im = Image.open(f)
        if im.width == im.height and (size is None or im.width == size) and im.width >= 512:
            canvas = im.width
            im = im.convert('RGBA')
            box = im.getchannel('A').getbbox() or (0, 0, 1, 1)
            x0, y0 = max(0, box[0] - PAD), max(0, box[1] - PAD)
            w, h = up4(min(canvas, box[2] + PAD) - x0), up4(min(canvas, box[3] + PAD) - y0)
            x0, y0 = min(x0, canvas - w), min(y0, canvas - h)
            im.crop((x0, y0, x0 + w, y0 + h)).save(f, optimize=True)
            out[name] = (x0, y0, w, h)
        else:
            assert name in rects, f'{f}: trimmed but no rect'
            out[name] = rects[name]
        set_meta_npot_none(f)
    canvas = canvas or size
    rp = os.path.join(folder, f'{p}_rects.txt')
    with open(rp, 'w') as fh:
        fh.write(f'{canvas}\n')
        for name in sorted(out):
            fh.write(name + ' ' + ' '.join(map(str, out[name])) + '\n')
    if not os.path.exists(rp + '.meta') and os.path.exists(HIT_META):
        import uuid
        lines = open(HIT_META).read().splitlines()
        lines[1] = f'guid: {uuid.uuid4().hex}'
        open(rp + '.meta', 'w').write('\n'.join(lines) + '\n')
    area = sum(w * h for _, _, w, h in out.values())
    print(f'{os.path.basename(folder)}: {len(out)} pieces, {area / (len(out) * canvas * canvas):.1%} of full size')


if __name__ == '__main__':
    args = sys.argv[1:]
    drop = '--no-lines' in args
    folders = [a for a in args if a != '--no-lines'] or sorted(glob.glob(os.path.join(CASTLES, 'castle_*/')))
    for d in folders:
        if os.path.isdir(d):
            trim(d, drop)

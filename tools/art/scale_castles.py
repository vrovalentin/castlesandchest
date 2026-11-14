# Stores the building pictures at 3/4 of their canvas (1024 -> 768): on a phone a building is drawn ~600-700 px
# tall, so the extra pixels only made the download heavier. Where each piece goes stays in canvas units
# (<prefix>_rects.txt), so the game draws them in the same places. Sizes stay multiples of 4 (compressed textures).
# Sizes come from the rects, not from the files: running it again changes nothing.
# Usage: python3 scale_castles.py [castle folder ...]   (no folders: every building in Resources/Castles)
import sys, os, glob, re
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from trim_layers import CASTLES, prefix_of, read_rects

SCALE = 0.75


def up16(v):
    return max(16, (v + 15) // 16 * 16)


def scale(folder):
    """A piece w x h of the canvas becomes (w' x h') * 3/4 with w', h' grown to multiples of 16 (transparent on the
    right and bottom): then the stored size is a whole multiple of 4 and maps back onto the canvas exactly."""
    p = prefix_of(folder)
    size, rects = read_rects(folder)
    n = 0
    for f in glob.glob(f'{folder}/*.png'):
        name = os.path.splitext(os.path.basename(f))[0]
        im = Image.open(f)
        if name in rects:
            x, y, w, h = rects[name]
            if im.size == (w, h):   # not scaled yet: grow the piece to multiples of 16
                w2, h2 = min(up16(w), size - x), min(up16(h), size - y)
                if w2 % 16 or h2 % 16:   # at the canvas edge: grow to the left/top instead
                    w2, h2 = up16(w), up16(h)
                    x, y = min(x, size - w2), min(y, size - h2)
                    big = Image.new('RGBA', (w2, h2)); big.paste(im.convert('RGBA'), (rects[name][0] - x, rects[name][1] - y))
                else:
                    big = Image.new('RGBA', (w2, h2)); big.paste(im.convert('RGBA'), (0, 0))
                rects[name] = (x, y, w2, h2)
                im = big
            w, h = rects[name][2:]
        else:   # the blueprint and the finished picture: the whole canvas
            w = h = size or 1024
        target = (round(w * SCALE), round(h * SCALE))
        if im.size != target:
            im.convert('RGBA').resize(target, Image.LANCZOS).save(f, optimize=True)
            n += 1
        meta = f + '.meta'
        if os.path.exists(meta):   # not a power of two: keep the size as it is
            s = open(meta).read()
            s2 = re.sub(r'nPOTScale: \d', 'nPOTScale: 0', s)
            if s2 != s:
                open(meta, 'w').write(s2)
    with open(os.path.join(folder, f'{p}_rects.txt'), 'w') as fh:
        fh.write(f'{size}\n')
        for name in sorted(rects):
            fh.write(name + ' ' + ' '.join(map(str, rects[name])) + '\n')
    print(f'{os.path.basename(os.path.normpath(folder))}: {n} pictures scaled')


if __name__ == '__main__':
    for d in sys.argv[1:] or sorted(glob.glob(os.path.join(CASTLES, 'castle_*/'))):
        if os.path.isdir(d):
            scale(d)

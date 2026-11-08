# Reads a building cut by hand in Photoshop and puts it into the game.
# The PSD (see castle_to_psd.py for a starting one):
#   canvas: the building's picture size (1024x1024), transparent background;
#   one layer per slot named NN_part (00_clay, 07_wall, 29_camel...), NN = building order from 00, no gaps;
#   optional "decor" layer (bushes on the site from the start); every other layer (ref...) is ignored.
# Writes the layers, outlines, faded blueprint, full picture and tap map into Unity (install_castle.py)
# and updates the slot list of the building in DemoContent.cs. Prints warnings about parts that would
# hang in the air while building and about soft edges inside the building.
# Usage: python3 psd_to_castle.py <castle_N.psd> [building number, default from the file name] [--dry] [--preview out.png]
import sys, os, re, tempfile, subprocess
import numpy as np
from PIL import Image
from scipy import ndimage as ndi
from psd_tools import PSDImage

HERE = os.path.dirname(os.path.abspath(__file__))
DEMO = os.path.join(HERE, '..', '..', 'unity', 'Assets', '_Game', 'Scripts', 'Core', 'DemoContent.cs')
SLOT = re.compile(r'^(\d\d)_([a-z]+)$')


def layer_pixels(layer, W, H):
    """The layer as an RGBA array on the whole canvas, with its mask applied; empty pixels are cleared."""
    im = layer.composite(viewport=(0, 0, W, H), force=True)
    a = np.zeros((H, W, 4), np.uint8)
    if im is not None:
        a = np.array(im.convert('RGBA').resize((W, H)) if im.size != (W, H) else im.convert('RGBA'))
    a[a[..., 3] < 8] = 0
    return a


def compress(slots):
    """['clay','clay','wall'] -> 'clay*2 wall'."""
    out, i = [], 0
    while i < len(slots):
        j = i
        while j < len(slots) and slots[j] == slots[i]:
            j += 1
        out.append(slots[i] if j - i == 1 else f'{slots[i]}*{j - i}')
        i = j
    return ' '.join(out)


def update_demo(cid, slots):
    """Rewrites the Slots(...) of the building in DemoContent.cs and adds its own parts to AllTiers(...)."""
    src = open(DEMO, encoding='utf-8').read()
    parts = re.findall(r'new PartInfo\("(\w+)"', src)
    shared = re.search(r'Shared\s*=\s*\{([^}]*)\}', src).group(1)
    shared = re.findall(r'"(\w+)"', shared)
    unknown = sorted(set(slots) - set(parts))
    if unknown:
        sys.exit(f'new part ids {unknown}: add them to DemoContent.Parts (and an icon) first')
    m = re.search(rf'new CastleInfo\("{cid}",[^;]*?Slots\((.*?)\),\s*AllTiers\((.*?)\)\);', src, re.S)
    if not m:
        print(f'! {cid}: slot list is not in the Slots("...") form, update DemoContent.cs by hand:')
        print('   ', compress(slots))
        return
    text = compress(slots)
    # keep lines short: break after about 110 characters
    words, lines, cur = text.split(), [], ''
    for w in words:
        if cur and len(cur) + len(w) > 110:
            lines.append(cur + ' '); cur = ''
        cur += (' ' if cur else '') + w
    lines.append(cur)
    indent = ' ' * 16
    new_slots = '\n' + ' +\n'.join(f'{indent}"{l}"' for l in lines)
    own = re.findall(r'"(\w+)"', m.group(2))
    for s in slots:
        if s not in shared and s not in own:
            own.append(s)
    new_tiers = ', '.join(f'"{o}"' for o in own)
    new = src[:m.start(1)] + new_slots + src[m.end(1):m.start(2)] + new_tiers + src[m.end(2):]
    if new != src:
        open(DEMO, 'w', encoding='utf-8').write(new)
        print(f'DemoContent.cs: {cid} slots updated')


def main():
    args = [a for a in sys.argv[1:]]
    dry = '--dry' in args
    preview = None
    if '--preview' in args:
        k = args.index('--preview'); preview = args[k + 1]; del args[k:k + 2]
    args = [a for a in args if a != '--dry']
    path = args[0]
    n = int(args[1]) if len(args) > 1 else int(re.search(r'(\d+)', os.path.basename(path)).group(1))
    cid, p = f'castle_{n}', f'c{n}'
    psd = PSDImage.open(path)
    W, H = psd.size
    slots, decor = {}, None
    for layer in psd.descendants():
        if layer.is_group():
            continue
        name = layer.name.strip()
        m = SLOT.match(name)
        if m:
            i = int(m.group(1))
            assert i not in slots, f'two layers numbered {i:02d}'
            if not layer.visible:
                print(f'note: layer {name} is hidden in the PSD, taken anyway')
            slots[i] = (m.group(2), layer_pixels(layer, W, H))
        elif name == 'decor':
            decor = layer_pixels(layer, W, H)
    assert slots, 'no NN_part layers'
    idx = sorted(slots)
    assert idx == list(range(len(idx))), f'slot numbers must go 00..{len(idx) - 1:02d} without gaps, got {idx}'
    assert len(idx) <= 64, 'at most 64 slots'
    names = [slots[i][0] for i in idx]
    if decor is None:
        decor = np.zeros((H, W, 4), np.uint8)

    out = tempfile.mkdtemp(prefix=f'{cid}_')
    comp = Image.new('RGBA', (W, H))
    porch = names.index('porch') if 'porch' in names else len(names)
    built = np.zeros((H, W), bool)
    eight = np.ones((3, 3), bool)
    for i in idx:
        part, a = slots[i]
        solid = a[..., 3] > 127
        if not solid.any():
            print(f'! {i:02d}_{part}: empty layer')
        # hanging in the air: a piece of the part not touching what is built so far (the first part stands on the ground)
        if built.any() and solid.any():
            lab, k = ndi.label(solid | built, structure=eight)
            ground = np.unique(lab[built])
            loose = solid & ~np.isin(lab, ground)
            if loose.sum() > 30:
                print(f'! {i:02d}_{part}: {int(loose.sum())} px do not touch the parts built before it (would hang in the air)')
        built |= solid
        Image.fromarray(a).save(f'{out}/{p}_{i:02d}_{part}.png', optimize=True)
        rim = solid & ~ndi.binary_erosion(solid, iterations=3)
        o = np.zeros((H, W, 4), np.uint8)
        o[rim] = [70, 110, 170, 220]
        Image.fromarray(o).save(f'{out}/{p}_line_{i:02d}.png', optimize=True)
        if i == porch:
            comp.alpha_composite(Image.fromarray(decor))
        comp.alpha_composite(Image.fromarray(a))
    if porch == len(names):
        comp.alpha_composite(Image.fromarray(decor))
    # soft edges inside the building: half-transparent pixels of the finished picture away from its outline
    ca = np.array(comp)[..., 3]
    inner = ndi.binary_erosion(ca > 0, iterations=4)
    soft = inner & (ca < 230)
    if soft.sum() > 50:
        ys, xs = np.nonzero(soft)
        print(f'! {int(soft.sum())} half-transparent px inside the building (seams), around x {xs.min()}-{xs.max()}, y {ys.min()}-{ys.max()}')
    Image.fromarray(decor).save(f'{out}/{p}_decor.png', optimize=True)
    base = np.array(comp)
    base[..., 3] = (base[..., 3] * 0.25).astype(np.uint8)
    Image.fromarray(base).save(f'{out}/{p}_base.png', optimize=True)
    comp.save(f'{out}/{cid}_full.png', optimize=True)
    print(f'{cid}: {len(names)} slots:', compress(names))

    if preview:
        steps = [len(names) // 4, len(names) // 2, 3 * len(names) // 4, len(names)]
        T = 400
        sheet = Image.new('RGB', (T * len(steps), T), (243, 234, 216))
        for k, s in enumerate(steps):
            im = Image.new('RGBA', (W, H), (243, 234, 216, 255))
            im.alpha_composite(Image.fromarray(base))
            for i in idx[:s]:
                if i == porch:
                    im.alpha_composite(Image.fromarray(decor))
                im.alpha_composite(Image.fromarray(slots[i][1]))
            sheet.paste(im.convert('RGB').resize((T, T), Image.LANCZOS), (k * T, 0))
        sheet.save(preview)
        print('preview', preview)
    if dry:
        print('dry run, files in', out)
        return
    subprocess.run([sys.executable, os.path.join(HERE, 'install_castle.py'), out, cid, p], check=True)
    update_demo(cid, names)


if __name__ == '__main__':
    main()

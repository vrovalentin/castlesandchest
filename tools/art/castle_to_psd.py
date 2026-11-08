# Makes a layered Photoshop file of a building from its layers in Unity, as a starting point for cutting by hand.
# Layers, bottom to top: "ref" (the source picture, hidden, for reference only), then one layer per slot named
# NN_part in building order ("decor" sits under the porch, as in the game). psd_to_castle.py reads it back.
# Usage: python3 castle_to_psd.py <building number> <out.psd>
#   e.g. python3 castle_to_psd.py 10 /tmp/castle_10.psd
import sys, os, glob, re
from PIL import Image
from psd_tools import PSDImage
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from trim_layers import open_piece
from psd_tools.api.layers import PixelLayer

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', '..')
CASTLES = os.path.join(ROOT, 'unity', 'Assets', '_Game', 'Art', 'Resources', 'Castles')
INC = os.path.join(ROOT, 'art_source', 'incoming')
# The picture each building was cut from (with its figure), shown in the hidden "ref" layer.
SOURCE = {
    1: 'castle_1/castle_1_noporch_src.png', 2: 'castle_2/castle_2_src.png',
    3: 'game1part4/pryanik3.png', 4: 'game1part4/melnica4.png', 5: 'game1part4/domicnaderevve5.png',
    6: 'game1part4/mayak6.png', 7: 'game1part4/tikva7.png', 8: 'game1part4/traktir8.png',
    9: 'game1part4/volshebnaya_bashnya9.png', 10: 'game1part4/ledyanoi_zamok10.png',
    11: 'game1part5/grib1_full.png', 12: 'game1part5/chasi1_full.png', 13: 'game1part5/pagoda1_full.png',
    14: 'game1part5/saeadvorec1_full.png', 15: 'game1part5/zamokdrakon1_full.png',
    16: 'game1part5/pirate1_full.png', 17: 'game1part5/dvorecverblud1_full.png',
}


def add(psd, im, name, visible=True):
    """Adds a layer cropped to its picture (the file stays small); an empty picture still gets a 1-pixel layer."""
    box = im.getchannel('A').getbbox() or (0, 0, 1, 1)
    layer = PixelLayer.frompil(im.crop(box), psd, name, top=box[1], left=box[0])
    layer.visible = visible
    return layer


def main():
    n, out = int(sys.argv[1]), sys.argv[2]
    d, p = os.path.join(CASTLES, f'castle_{n}'), f'c{n}'
    files = sorted(glob.glob(f'{d}/{p}_[0-9][0-9]_*.png'))
    assert files, f'no layers in {d}'
    W, H = open_piece(files[0]).size
    psd = PSDImage.new('RGBA', (W, H))
    src = os.path.join(INC, SOURCE[n]) if n in SOURCE else os.path.join(d, f'castle_{n}_full.png')
    ref = Image.open(src).convert('RGBA').resize((W, H))
    add(psd, ref, 'ref', visible=False)
    decor = os.path.join(d, f'{p}_decor.png')
    decor = open_piece(decor) if os.path.exists(decor) else None
    if decor is not None and decor.getchannel('A').getbbox() is None:
        decor = None
    for f in files:
        name = re.match(rf'{p}_(\d\d_[a-z]+)\.png', os.path.basename(f)).group(1)
        if decor is not None and name.endswith('_porch'):
            add(psd, decor, 'decor'); decor = None
        add(psd, open_piece(f), name)
    if decor is not None:
        add(psd, decor, 'decor')
    psd.save(out)
    print('wrote', out, len(files), 'slots')


if __name__ == '__main__':
    main()

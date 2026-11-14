# Storage icon of a figure from its own drawing on white: cut from the white, fit into 244 px, standing at the
# bottom of the 256 px icon.  Usage: python3 figure_icon.py <figure.png> <part_name.png>
import sys, os
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from place_figures import cut_white

im = cut_white(sys.argv[1])
im = im.crop(im.getchannel('A').point(lambda v: 255 if v > 40 else 0).getbbox())
k = 244 / max(im.size)
im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
out = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
out.alpha_composite(im, ((256 - im.width) // 2, 250 - im.height))
out.save(sys.argv[2], optimize=True)

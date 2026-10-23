# Stage preview sheet (0, 4, 8, 12, 17 slots) on the build-screen colour.
# Usage: python3 castle_stages_preview.py <castle_1 art folder> <out.png>
import sys,glob
from PIL import Image
d=sys.argv[1]; out=sys.argv[2]
L=sorted(glob.glob(d+'/c1_[0-9]*.png')); lines=sorted(glob.glob(d+'/c1_line_*.png'))
base=Image.open(d+'/c1_base.png').convert('RGBA')
order=list(range(17)); stages=[0,4,8,12,17]; W=480
bg=(0xf3,0xea,0xd8,255)
sheet=Image.new('RGBA',(W*len(stages),W),bg)
for k,n in enumerate(stages):
    im=Image.new('RGBA',base.size,bg); im.alpha_composite(base)
    for i in range(17):
        if i>=n: im.alpha_composite(Image.open(lines[i]).convert('RGBA'))
    for i in range(n): im.alpha_composite(Image.open(L[i]).convert('RGBA'))
    sheet.paste(im.crop((230,0,810,1024)).resize((int(580*W/1024),W),Image.LANCZOS),(k*W+70,0))
sheet.convert('RGB').save(out)

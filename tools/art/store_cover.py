import sys, math
sys.path.insert(0,'/tmp/claude-0/-home-user/dc296321-e3cc-5c60-8e17-e2dafb292243/scratchpad/kvas')
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageEnhance
R='/home/user/castlesandchest/unity/Assets/_Game/Art/Resources/'
OUT='/mnt/project-files/store/yandex_art/'
W,H=800,470
def crop(im): return im.crop(im.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox())
def castle(n,h):
    im=crop(Image.open(R+f'Castles/castle_{n}/castle_{n}_full.png').convert('RGBA')); return im.resize((round(im.width*h/im.height),h),Image.LANCZOS)
def obj(p,h,rot=0):
    im=crop(Image.open(R+p).convert('RGBA')); im=im.resize((round(im.width*h/im.height),h),Image.LANCZOS)
    return im.rotate(rot,Image.BICUBIC,expand=True) if rot else im
def put(cv,im,cx,base,sh=0.45,haze=0.0):
    if haze:
        a=im.getchannel('A'); im=Image.blend(im,Image.new('RGBA',im.size,(214,232,240,255)),haze); im.putalpha(a)
    x=int(cx-im.width/2); y=int(base-im.height)
    if sh:
        e=Image.new('RGBA',(int(im.width*0.9),max(8,im.height//9)),(0,0,0,0)); ImageDraw.Draw(e).ellipse([0,0,e.width-1,e.height-1],fill=(30,50,20,int(255*sh)))
        e=e.filter(ImageFilter.GaussianBlur(6)); cv.alpha_composite(e,(int(cx-e.width/2),int(base-e.height*0.6)))
    cv.alpha_composite(im,(x,y))
b=Image.open(R+'Backgrounds/bg_meadow.jpg').convert('RGBA'); k=W/b.width; b=b.resize((W,round(b.height*k)),Image.LANCZOS)
cv=b.crop((0,int((b.height-H)*0.5),W,int((b.height-H)*0.5)+H))
g=Image.new('RGBA',cv.size,(0,0,0,0)); ImageDraw.Draw(g).ellipse([300,60,780,380],fill=(255,244,200,120)); cv.alpha_composite(g.filter(ImageFilter.GaussianBlur(70)))
title=sys.argv[1]; fn=sys.argv[2]
# arc of five castles: far at the back left-centre, coming forward to the right
arc=[(15,350,236,128,0.32),(10,450,250,150,0.24),(7,560,290,180,0.14),(6,700,345,215,0.06),(2,560,465,265,0.0)]
arc=[(6,330,232,118,0.32),(10,440,248,145,0.24),(15,575,282,180,0.14),(7,688,355,205,0.06),(2,505,468,265,0.0)]
for n,cx,base,h,hz in arc: put(cv,castle(n,h),cx,base,0.35,hz)
# open chest on the left with parts spilling out
ch=obj('Board/chest_purple_open.png',175); put(cv,ch,150,462,0.5)
gl=Image.new('RGBA',cv.size,(0,0,0,0)); ImageDraw.Draw(gl).ellipse([70,210,230,340],fill=(255,236,150,170)); cv.alpha_composite(gl.filter(ImageFilter.GaussianBlur(28)))
parts=[('part_window',56,-15,95,228),('part_roof',62,12,205,205),('part_lantern',52,-8,45,280),('part_door',58,10,150,160),('part_brick',44,-25,262,262),('part_spire',54,18,62,170),('part_clock',46,-10,272,350),('part_tiles',42,22,240,438),('part_bells',44,8,35,430)]
for nm,h,rot,cx,cy in parts:
    o=obj(f'Parts/{nm}.png',h,rot); cv.alpha_composite(o,(int(cx-o.width/2),int(cy-o.height/2)))
# sparkles
sp=Image.new('RGBA',cv.size,(0,0,0,0)); d=ImageDraw.Draw(sp)
for x,y,r in [(120,210,4),(230,180,3),(90,300,3),(285,285,4),(175,170,3),(30,260,2)]:
    d.ellipse([x-r,y-r,x+r,y+r],fill=(255,250,210,255))
cv.alpha_composite(sp.filter(ImageFilter.GaussianBlur(1.2)))
d=ImageDraw.Draw(cv); f=ImageFont.truetype(R+'Fonts/Nunito-Black.ttf',58)
w=d.textlength(title,font=f); d.text(((W-w)/2,18),title,font=f,fill=(255,236,170),stroke_width=7,stroke_fill=(92,48,24))
cv.convert('RGB').save(OUT+fn)

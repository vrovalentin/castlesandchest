# Recut castle 1 layers along stone seams, make a 30% blueprint and slot outlines.
# Usage: python3 recut_castle_1.py <castle_1 art folder> <output folder>
import sys, glob, numpy as np
from PIL import Image
src=sys.argv[1]; out=sys.argv[2]
names=sorted(n for n in glob.glob(src+'/c1_[0-9]*.png'))
full=np.array(Image.open(src+'/castle_1_full.png').convert('RGBA')).astype(np.float32)
H,W=full.shape[:2]
lab=np.full((H,W),-1)
for i,f in enumerate(names):
    a=np.array(Image.open(f))[:,:,3]; lab[a>0]=i
alpha=full[:,:,3]
lum=(0.3*full[:,:,0]+0.59*full[:,:,1]+0.11*full[:,:,2])/255.0

def seam_v(x0,ya,yb,d,mask):
    # min-cost top->bottom path, x within x0±d, cost=lum on mask else 0.6
    xs=np.arange(x0-d,x0+d+1)
    C=np.where(mask[ya:yb][:,xs],lum[ya:yb][:,xs],0.6)+0.002*np.abs(xs-x0)[None,:]
    n,m=C.shape; D=C.copy(); B=np.zeros((n,m),int)
    for r in range(1,n):
        prev=np.stack([np.r_[np.inf,D[r-1,:-1]],D[r-1],np.r_[D[r-1,1:],np.inf]])
        k=prev.argmin(0); B[r]=k-1; D[r]+=prev.min(0)
    j=int(D[-1].argmin()); path=np.zeros(n,int)
    for r in range(n-1,-1,-1): path[r]=xs[j]; j+=B[r,j] if r>0 else 0
    full_path=np.full(H,x0); full_path[ya:yb]=path
    full_path[:ya]=path[0]; full_path[yb:]=path[-1]
    return full_path  # x per row
def seam_h(y0,xa,xb,d,mask):
    p=seam_v_T(y0,xa,xb,d,mask); return p
def seam_v_T(y0,xa,xb,d,mask):
    global lum
    l0=lum; lum=l0.T
    p=seam_v(y0,xa,xb,d,mask.T)
    lum=l0
    return p  # y per column

L={n.split('/')[-1][:-4]:i for i,n in enumerate(names)}
clay=[L['c1_0%d_clay'%i] for i in range(4)]
walls=[L['c1_0%d_wall'%i] for i in range(4,8)]
floors=[L['c1_08_floor'],L['c1_09_floor']]
new=lab.copy()
# foundation: 3 vertical seams
F=np.isin(lab,clay)
ys=np.where(F.any(1))[0]; ya,yb=ys.min(),ys.max()+1
cuts=[seam_v(x,ya,yb,40,F) for x in (403,518,633)]
X=np.arange(W)[None,:]
k=np.zeros((H,W),int)
for c in cuts: k+= (X>c[:,None])
new[F]=np.array(clay)[k[F]]
# walls+floors: horizontal seams
R=np.isin(lab,walls+floors)
xs=np.where(R.any(0))[0]; xa,xb=xs.min(),xs.max()+1
hs=[seam_h(y,xa,xb,25,R) for y in (432,516,574,768)]
Y=np.arange(H)[:,None]
band=np.zeros((H,W),int)
for c in hs: band+=(Y>c[None,:])
# band0 = floor, 1..4 = walls top->bottom; slot 04 lowest
fl=R&(band==0)
vs=seam_v(508,0,int(hs[0].max())+1,40,fl)
new[fl]=np.where(X>vs[:,None],floors[1],floors[0])[fl]
for b in range(1,5):
    m=R&(band==b); new[m]=walls[4-b]
# clean specks: small pieces of a label join their biggest neighbour
from scipy import ndimage as ndi
for it in range(2):
    for i in range(17):
        cc,n=ndi.label(new==i)
        if n<=1: continue
        sizes=ndi.sum(np.ones_like(cc),cc,range(1,n+1))
        keep=int(np.argmax(sizes))+1
        for j in range(1,n+1):
            if j==keep or sizes[j-1]>1500: continue
            m=cc==j; ring=ndi.binary_dilation(m,iterations=2)&~m
            nb=new[ring]; nb=nb[nb>=0]
            if len(nb): new[m]=np.bincount(nb).argmax()
# write layers
base=Image.open(src+'/c1_base.png')
for i,n in enumerate(names):
    a=np.where(new==i,alpha,0).astype(np.uint8)
    im=full.copy().astype(np.uint8); im[:,:,3]=a
    Image.fromarray(im).save(out+'/'+n.split('/')[-1])
np.save(out+'/labels.npy',new)
# blueprint: faded, desaturated, 30% for slot pixels; decor (bushes) in full colour
g=(0.3*full[:,:,0]+0.59*full[:,:,1]+0.11*full[:,:,2])
tint=np.array([200,225,250],np.float32)
b=np.zeros_like(full)
b[:,:,:3]=(0.55*g[:,:,None]/255.0*tint+0.45*tint)
b[:,:,3]=alpha*0.30
deco=(new<0)&(alpha>0)
b[deco]=full[deco]
Image.fromarray(b.astype(np.uint8)).save(out+'/c1_base.png')
# outline per slot: 3 px white line on the inside of the slot edge
for i in range(17):
    m=new==i
    edge=m&~ndi.binary_erosion(m,iterations=3)
    o=np.zeros((H,W,4),np.uint8); o[edge]=[70,110,170,220]
    Image.fromarray(o).save(out+'/c1_line_%02d.png'%i)

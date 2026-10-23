# Turn block emblems into faint light outlines (originals in art_source/blocks).
# Usage: python3 ghost_emblems.py art_source/blocks <out folder> [line alpha 0.6] [emblem opacity 0.5], then cut_blocks.py <out folder> unity/Assets/_Game/Art/Resources/Board
import sys, os, numpy as np
from PIL import Image
from scipy import ndimage as ndi
def emblem_mask(a):
    lum=a.mean(2); H,W=lum.shape
    body=np.median(lum[300:720,180:250])
    dark=lum<0.6*body
    reg=np.zeros_like(dark); reg[240:800,240:800]=True
    # colour far from the body colour of the same row (the gloss runs top to bottom)
    rowcol=np.median(np.concatenate([a[:,150:200],a[:,820:870]],1),1)
    far=np.linalg.norm(a-rowcol[:,None,:],axis=2)>70
    dark=(dark|far)&reg
    dark=ndi.binary_closing(dark,iterations=3)
    lab,n=ndi.label(dark); sz=ndi.sum(dark,lab,range(1,n+1))
    keep=np.isin(lab,1+np.where(sz>2000)[0])
    m=ndi.binary_fill_holes(keep)
    return ndi.binary_dilation(m,iterations=3)
src, out = sys.argv[1], sys.argv[2]
line_alpha = float(sys.argv[3]) if len(sys.argv) > 3 else 0.6
# how much of the original emblem stays visible under the lines (0 = none)
emblem_opacity = float(sys.argv[4]) if len(sys.argv) > 4 else 0.5
for c in ['blue','brown','gray','green','purple']:
    a=np.array(Image.open(f'{src}/block_{c}.png').convert('RGB')).astype(np.float32)
    m=emblem_mask(a)
    lum=a.mean(2); body=np.median(lum[300:720,180:250])
    # paint over: diffuse the surrounding body colour into the emblem area
    f=a.copy(); rowcol=np.median(np.concatenate([a[:,150:200],a[:,820:870]],1),1)
    f[m]=np.broadcast_to(rowcol[:,None,:],a.shape)[m]
    small=m[::4,::4]; fs=f[::4,::4].copy()
    for _ in range(400):
        blur=np.stack([ndi.uniform_filter(fs[...,k],3) for k in range(3)],-1)
        fs[small]=blur[small]
    up=np.array(Image.fromarray(fs.clip(0,255).astype(np.uint8)).resize((1024,1024),Image.BILINEAR)).astype(np.float32)
    soft=ndi.gaussian_filter(m.astype(np.float32),2)[...,None]
    f=a*(1-soft)+up*soft
    f=f*(1-emblem_opacity*soft)+a*emblem_opacity*soft
    # line art: the outer contour of the emblem plus its strongest inner edges
    core=ndi.binary_erosion(m,iterations=3)          # the emblem before the safety margin
    outer=core&~ndi.binary_erosion(core,iterations=4)
    g=np.hypot(ndi.sobel(ndi.gaussian_filter(lum,1.5),0),ndi.sobel(ndi.gaussian_filter(lum,1.5),1))
    inner_zone=ndi.binary_erosion(core,iterations=12)
    inner=(g>np.percentile(g[inner_zone],88))&inner_zone if inner_zone.any() else np.zeros_like(m)
    la=(ndi.gaussian_filter(outer.astype(np.float32),1.0)*line_alpha
        +ndi.gaussian_filter(inner.astype(np.float32),1.0)*line_alpha*0.6)
    la=np.clip(la,0,line_alpha)[...,None]
    light=np.array([255,255,250],np.float32)
    f=f*(1-la)+light*la
    Image.fromarray(f.clip(0,255).astype(np.uint8)).save(f'{out}/block_{c}.png')

"""A compact, solid pineapple with a strict 1,000 occupied-voxel budget."""
import math
import struct
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets' / 'Voxel Models'
PAL = [(246,185,38),(255,209,64),(219,143,23),(163,103,26),
       (56,127,43),(89,164,49),(132,185,61),(35,88,37),(127,105,38)]
cells = {}
def put(x,y,z,c):
    cells[(round(x),round(y),round(z))] = c

# Small rounded barrel; all interior cells count toward the budget.
radii = [2,2.9,3.5,3.8,4,4,4,4,3.8,3.5,2.9,2]
for z,r in enumerate(radii,2):
    for x in range(-4,5):
        for y in range(-4,5):
            if x*x+y*y > r*r: continue
            angle = math.atan2(y,x)
            u = angle/(2*math.pi)*8
            a,b = (u+z/3)%1, (u-z/3)%1
            seam = min(a,1-a,b,1-b)<.14
            c = 4 if seam else (2 if (x-y+z)%3==0 else 1)
            if x*x+y*y < (r-1)**2: c=1
            put(x,y,z,c)
for z in (0,1): put(0,0,z,9)
for z in (13,14,15):
    put(0,0,z,9 if z<15 else 5)
    if z==14:
        for x,y in [(1,0),(-1,0),(0,1),(0,-1)]: put(x,y,z,5)

def leaf(angle,length,height,base,shade):
    for i in range(65):
        t=i/64
        r=length*t
        z=base+height*(1.3*t-.3*t*t)
        width=.85*(1-t)
        for side in (-width,0,width):
            x=math.cos(angle)*r-math.sin(angle)*side
            y=math.sin(angle)*r+math.cos(angle)*side
            put(x,y,z,shade if side else 7)
            if t<.45: put(x,y,z-1,8)

for n in range(7): leaf(n*math.tau/7+.1,5.4,4.5+(n%2),15,5+n%2)
for n in range(4): leaf(n*math.tau/4+.5,2.8,8-(n%2),15,5+n%2)
put(0,0,22,6)
put(0,0,23,7)
assert len(cells)<=1000, len(cells)

def chunk(tag,data=b'',children=b''):
    return tag+struct.pack('<II',len(data),len(children))+data+children
mn=[min(p[a] for p in cells) for a in range(3)]
mx=[max(p[a] for p in cells) for a in range(3)]
size=chunk(b'SIZE',struct.pack('<III',*(mx[a]-mn[a]+1 for a in range(3))))
xyzi=chunk(b'XYZI',struct.pack('<I',len(cells))+bytes(v for p,c in sorted(cells.items()) for v in (*(p[a]-mn[a] for a in range(3)),c)))
rgba=b''.join(bytes((*c,255)) for c in PAL)+bytes((0,0,0,255))*(256-len(PAL))
target=OUT/'Pineapple_Small.vox'
target.write_bytes(b'VOX '+struct.pack('<I',150)+chunk(b'MAIN',children=size+xyzi+chunk(b'RGBA',rgba)))

# Render the actual exported cells, not an artist's approximation.
im=Image.new('RGB',(720,850),(32,40,49)); draw=ImageDraw.Draw(im)
scale=23
def project(p):
    x,y,z=p
    return (360+(x-y)*scale,745+(x+y)*scale*.5-z*scale)
faces=[((1,0,0),[(1,0,0),(1,1,0),(1,1,1),(1,0,1)],.72),
       ((0,1,0),[(0,1,0),(1,1,0),(1,1,1),(0,1,1)],.88),
       ((0,0,1),[(0,0,1),(1,0,1),(1,1,1),(0,1,1)],1.08)]
for p,c in sorted(cells.items(),key=lambda item:sum(item[0])):
    for normal,verts,light in faces:
        if tuple(p[a]+normal[a] for a in range(3)) in cells: continue
        color=tuple(min(255,round(v*light)) for v in PAL[c-1])
        poly=[project(tuple(p[a]+v[a] for a in range(3))) for v in verts]
        draw.polygon(poly,fill=color)
im.save(Path(__file__).parent/'Pineapple_Small_Preview.png')
print(f'{target}: {len(cells)} occupied voxels; dimensions {tuple(mx[a]-mn[a]+1 for a in range(3))}')

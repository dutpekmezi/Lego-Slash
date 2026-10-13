"""Deterministic connected VOX models with exact occupied-cell budgets."""
from pathlib import Path
import heapq
import math
import struct
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets' / 'Voxel Models'
PREVIEW = ROOT / 'ArtSource' / 'VoxelBatch'
DIRS = ((1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1))
SPECS = [('Apple',300),('Banana',300),('Pumpkin',1000),('Watermelon',1000),
         ('Nar',700),('Melon',700),('Coconut',700),('Ice_Cube',1000)]
PALETTES = {
 'Apple': [(194,35,35),(225,54,42),(155,24,30),(102,65,30),(65,139,40)],
 'Banana': [(247,205,38),(255,226,70),(211,167,24),(99,67,29)],
 'Pumpkin': [(233,108,17),(252,141,27),(185,75,13),(70,103,39),(109,82,36),(220,102,16)],
 'Watermelon': [(40,111,43),(88,165,57),(24,81,38)],
 'Nar': [(169,27,47),(202,47,57),(134,21,38),(127,73,34)],
 'Melon': [(224,188,98),(248,219,146),(178,147,78),(105,120,45)],
 'Coconut': [(114,76,43),(148,103,59),(82,53,32)],
 'Ice_Cube': [(138,208,235),(188,234,249),(101,173,209),(224,248,255)]}

def score(name,p):
    x,y,z = p
    if name == 'Banana':
        # A crescent in the X/Z plane, solid across its thickness.
        angle = math.atan2(z-8,x)
        clamped = min(-0.28,max(-math.pi+0.28,angle))
        cx,cz = 8*math.cos(clamped),8+8*math.sin(clamped)
        taper = 0.5 + 0.9*math.sin((clamped+math.pi-0.28)/(math.pi-0.56)*math.pi)
        return ((x-cx)**2+(z-cz)**2)/(taper*taper) + (y/1.65)**2
    if name == 'Ice_Cube':
        return max(abs(x-0.5),abs(y-0.5),abs(z-0.5))
    rx,ry,rz = {'Apple':(4.1,3.8,3.8),'Pumpkin':(6.6,6.6,4.8),
                  'Watermelon':(8,5.6,5.6),'Nar':(5.3,5.3,5.1),
                  'Melon':(6.7,5.0,5.1),'Coconut':(5.4,5.1,5.7)}[name]
    r = math.hypot(x,y)
    if name == 'Pumpkin':
        rib = 1+0.10*math.cos(10*math.atan2(y,x))
        return (x/(rx*rib))**2+(y/(ry*rib))**2+(z/rz)**2
    if name == 'Apple':
        # Broad shoulders and a narrower base, plus a shallow stem dimple.
        width = 1+0.09*z-0.035*z*z
        return (x/(rx*max(.6,width)))**2+(y/(ry*max(.6,width)))**2+(z/rz)**2 + (0.2 if z>2 and r<1.4 else 0)
    return (x/rx)**2+(y/ry)**2+(z/rz)**2

def pumpkin_cavity(p):
    x,y,z = p
    return (x/5.8)**2+(y/5.8)**2+(z/4.5)**2 < 1

def pumpkin_face(p):
    x,y,z = p
    if y <= 0: return False
    # Triangular eyes, nose, and a broad grin with alternating attached teeth.
    eyes = (z==3 and abs(x)==3) or (z==2 and 2<=abs(x)<=4) or (z==1 and 1<=abs(x)<=4)
    nose = (x==0 and z==0)
    mouth = (-4<=z<=-2 and abs(x)<=4) or (z==-1 and abs(x)==4)
    teeth = (z==-2 and x in (-2,2)) or (z==-4 and x in (-1,1))
    return eyes or nose or (mouth and not teeth)

def pumpkin_shell(n):
    def shell_score(p):
        x,y,z=p
        rib=1+.055*math.cos(10*math.atan2(y,x))
        radial=math.sqrt((x/(8*rib))**2+(y/(8*rib))**2+(z/6.3)**2)
        return abs(radial-1)
    start=(0,0,6)
    frontier=[(shell_score(start),start)]; queued={start}; cells={}
    while len(cells)<n:
        _,p=heapq.heappop(frontier)
        cells[p]=1
        for d in DIRS:
            q=tuple(p[a]+d[a] for a in range(3))
            if q not in queued:
                queued.add(q)
                if not pumpkin_cavity(q) and not pumpkin_face(q):
                    heapq.heappush(frontier,(shell_score(q),q))
    assert not any(pumpkin_cavity(p) or pumpkin_face(p) for p in cells)
    return cells

def volume(name,n):
    if name=='Pumpkin': return pumpkin_shell(n)
    start = (0,0,0) if name != 'Banana' else (0,0,0)
    frontier = [(score(name,start),start)]
    queued = {start}
    cells = {}
    while len(cells)<n:
        _,p = heapq.heappop(frontier)
        cells[p] = 1
        for d in DIRS:
            q = tuple(p[a]+d[a] for a in range(3))
            if q not in queued:
                queued.add(q)
                heapq.heappush(frontier,(score(name,q),q))
    return cells

def build(name,count):
    extra = {'Apple':6,'Pumpkin':8,'Nar':7,'Melon':2}.get(name,0)
    cells = volume(name,count-extra)
    if extra:
        z = max(p[2] for p in cells if p[0]==0 and p[1]==0)
        if name == 'Apple':
            ornaments = [(0,0,z+1,4),(0,0,z+2,4),(1,0,z+2,5),
                         (2,0,z+2,5),(1,1,z+2,5),(2,1,z+2,5)]
        elif name == 'Nar':
            ornaments = [(0,0,z+1,4),(0,0,z+2,4),(1,0,z+2,4),
                         (-1,0,z+2,4),(0,1,z+2,4),(0,-1,z+2,4),(0,0,z+3,4)]
        elif name == 'Pumpkin':
            ornaments = [(0,0,z+i,4) for i in range(1,5)] + [(1,0,z+i,5) for i in range(1,5)]
        else:
            ornaments = [(0,0,z+1,4),(0,0,z+2,4)]
        for x,y,z,c in ornaments:
            assert (x,y,z) not in cells
            cells[(x,y,z)] = c
    for (x,y,z),c in list(cells.items()):
        if c!=1: continue
        noise = (x*17+y*31+z*13)%13
        if name == 'Watermelon':
            c = 2 if math.cos(7*math.atan2(y,x)+z*.18)>.35 else (3 if noise==0 else 1)
        elif name == 'Melon':
            c = 2 if (x+y+z)%4==0 or (x-y+z)%5==0 else (3 if noise<2 else 1)
        elif name == 'Pumpkin':
            c = 3 if math.cos(10*math.atan2(y,x))<-.3 else (2 if noise<3 else 1)
            # The front lip and teeth use a subtly darker shade of the body orange.
            front = (x,y+1,z) not in cells and y>0
            eye_edge = any((z+dz==3 and abs(x+dx)==3) or
                           (z+dz==2 and 2<=abs(x+dx)<=4) or
                           (z+dz==1 and 1<=abs(x+dx)<=4)
                           for dx,dz in ((1,0),(-1,0),(0,1),(0,-1)))
            tooth = (z==-2 and x in (-2,2)) or (z==-4 and x in (-1,1))
            mouth_edge = any(pumpkin_face((x+dx,1,z+dz)) and -4<=z+dz<=-1
                             for dx,dz in ((1,0),(-1,0),(0,1),(0,-1)))
            if front and (eye_edge or tooth or mouth_edge): c=6
        elif name == 'Banana':
            c = 4 if abs(x)>=7 else (2 if z>=2 else (3 if noise<3 else 1))
        elif name == 'Ice_Cube':
            c = 4 if z==5 and (x+y)%3==0 else (2 if z>=4 or x==-4 else (3 if noise<2 else 1))
        else:
            c = 2 if noise<3 else (3 if noise==12 else 1)
        cells[(x,y,z)] = c
    assert len(cells)==count
    seen = {next(iter(cells))}
    pending = list(seen)
    for p in pending:
        for d in DIRS:
            q = tuple(p[a]+d[a] for a in range(3))
            if q in cells and q not in seen:
                seen.add(q); pending.append(q)
    assert len(seen)==count, name
    return cells

def chunk(tag,data):
    return tag+struct.pack('<ii',len(data),0)+data

def write_vox(name,cells):
    mn = [min(p[a] for p in cells) for a in range(3)]
    dims = [max(p[a] for p in cells)-mn[a]+1 for a in range(3)]
    xyz = bytes(v for p,c in sorted(cells.items()) for v in (*(p[a]-mn[a] for a in range(3)),c))
    pal = b''.join(bytes((*c,255)) for c in PALETTES[name])
    children = chunk(b'SIZE',struct.pack('<iii',*dims))+chunk(b'XYZI',struct.pack('<i',len(cells))+xyz)+chunk(b'RGBA',pal+bytes(1024-len(pal)))
    path = OUT/(name+'.vox')
    path.write_bytes(b'VOX '+struct.pack('<i',150)+b'MAIN'+struct.pack('<ii',0,len(children))+children)
    meta = path.with_suffix('.vox.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    # Read exported payload back: dimensions, count and unique occupied cells.
    data = path.read_bytes(); assert struct.unpack_from('<i',data,56)[0]==len(cells)
    exported = [tuple(xyz[i:i+3]) for i in range(0,len(xyz),4)]
    assert len(set(exported))==len(cells)
    assert all(0<=p[a]<dims[a] for p in exported for a in range(3))
    return dims

def render(name,cells,count):
    im = Image.new('RGB',(480,440),(29,35,44)); draw = ImageDraw.Draw(im)
    def project(x,y,z): return ((x-y)*.87,(x+y)*.42-z)
    points = [project(*p) for p in cells]
    lo = [min(p[a] for p in points) for a in range(2)]
    hi = [max(p[a] for p in points) for a in range(2)]
    scale = min(360/(hi[0]-lo[0]+2),315/(hi[1]-lo[1]+2))
    def screen(p):
        u,v = project(*p)
        return (240+(u-(lo[0]+hi[0])/2)*scale,230+(v-(lo[1]+hi[1])/2)*scale)
    faces = [((1,0,0),[(.5,-.5,-.5),(.5,.5,-.5),(.5,.5,.5),(.5,-.5,.5)],.78),
             ((0,1,0),[(-.5,.5,-.5),(.5,.5,-.5),(.5,.5,.5),(-.5,.5,.5)],.9),
             ((0,0,1),[(-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)],1.08)]
    for p,c in sorted(cells.items(),key=lambda pc:sum(pc[0])):
        for normal,verts,shade in faces:
            if tuple(p[a]+normal[a] for a in range(3)) in cells: continue
            color = tuple(min(255,int(v*shade)) for v in PALETTES[name][c-1])
            poly = [screen(tuple(p[a]+v[a] for a in range(3))) for v in verts]
            draw.polygon(poly,fill=color,outline=tuple(int(v*.8) for v in color))
    draw.text((18,15),f'{name} | {count} voxels',fill='white')
    im.save(PREVIEW/(name+'.png'))
    return im

def render_pumpkin_front(cells):
    im=Image.new('RGB',(600,600),(29,35,44)); draw=ImageDraw.Draw(im)
    for x in range(-10,11):
        for z in range(-8,14):
            column=[(y,c) for (cx,y,cz),c in cells.items() if cx==x and cz==z]
            if not column: continue
            y,c=max(column)
            # Light arrives from the front: the rear inner wall is in shadow.
            shade=.24 if y<=0 and pumpkin_face((x,1,z)) else (1 if y>=4 or z>6 else .8)
            color=tuple(int(v*shade) for v in PALETTES['Pumpkin'][c-1])
            left=300+x*24; top=350-z*24
            draw.rectangle((left,top,left+23,top+23),fill=color,outline=tuple(int(v*.75) for v in color))
    draw.text((18,18),'Halloween Pumpkin | 1000 voxels | hollow',fill='white')
    im.save(PREVIEW/'Pumpkin_Front.png')

def main():
    OUT.mkdir(parents=True,exist_ok=True); PREVIEW.mkdir(parents=True,exist_ok=True)
    sheet = Image.new('RGB',(480*4,440*2))
    for i,(name,count) in enumerate(SPECS):
        cells = build(name,count)
        dims = write_vox(name,cells)
        sheet.paste(render(name,cells,count),((i%4)*480,(i//4)*440))
        if name=='Pumpkin': render_pumpkin_front(cells)
        print(f'{name}: {count} unique connected voxels, dimensions {dims}')
    sheet.save(PREVIEW/'All_Models.png')

if __name__=='__main__': main()

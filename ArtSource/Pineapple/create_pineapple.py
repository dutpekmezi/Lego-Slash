"""Create a solid, colored pineapple as editable MagicaVoxel voxels."""
import math
import struct
from pathlib import Path

OUT = Path(__file__).parent
PALETTE = [
    (241, 176, 34), (255, 202, 51), (231, 153, 22), (255, 218, 77),
    (191, 120, 23), (151, 91, 24), (216, 144, 28), (249, 189, 38),
    (47, 111, 41), (67, 145, 47), (91, 170, 55), (122, 187, 65),
    (34, 83, 37), (154, 195, 76), (113, 99, 37), (139, 115, 40),
]
voxels = {}
CX = CY = 32

def put(x, y, z, color):
    p = (round(x), round(y), round(z))
    if all(0 <= a < 100 for a in p):
        voxels[p] = color + 1

# Rounded barrel with raised diamond skin. The interior is solid for slicing.
for z in range(5, 44):
    t = (z - 24) / 20.0
    radius = 14.2 * max(0, 1 - abs(t) ** 3.1) ** .5
    for x in range(16, 49):
        for y in range(16, 49):
            dx, dy = x-CX, (y-CY)/.94
            r = math.hypot(dx, dy)
            u = math.atan2(dy, dx) / (2*math.pi) * 12
            v = (z-5)/5.7
            a, b = (u+v)%1, (u-v)%1
            edge = min(a, 1-a, b, 1-b)
            local_radius = radius + (.55 if edge > .16 else -.12)
            if r > local_radius:
                continue
            if r < radius-1.65:
                color = 1
            elif edge < .115:
                color = 4 if z%3 else 6
            elif abs(a-.5)<.12 and abs(b-.5)<.12:
                color = 5  # the small recessed eye at the center of each scale
            elif edge > .32:
                color = 3 if (int(u+v)+int(u-v))%3 else 1
            else:
                color = [0, 7, 2][(int(u+v)*17+int(u-v)*11)%3]
            put(x,y,z,color)

# Small woody basal stalk and the visible neck supporting the leaf crown.
for z in range(1, 7):
    for x in range(30,35):
        for y in range(30,35):
            if (x-CX)**2+(y-CY)**2 <= 5:
                put(x,y,z,14 if (x+y)%2 else 15)
for z in range(41, 50):
    for x in range(29,36):
        for y in range(29,36):
            if (x-CX)**2+(y-CY)**2 <= (3 if z<46 else 2.5)**2:
                put(x,y,z,14 if z<46 else 8)

def leaf(angle, length, rise, width, base_z, phase):
    """Voxelize a curved, tapered, thick leaf ribbon with a bright midrib."""
    ex, ey = math.cos(angle), math.sin(angle)
    px, py = -ey, ex
    for i in range(161):
        t = i/160
        radial = 1.1 + length*(t**1.24)
        z = base_z + rise*(1.35*t-.35*t*t)
        half = width*(math.sin(math.pi*(.10+.90*t))**.75)*(1-t)**.35
        for j in range(-32,33):
            side = j/8
            if abs(side)>half:
                continue
            for depth in (0,.65,1.3):
                # Raised midrib and folded sides produce a faceted leaf.
                zz=z + .65*(1-abs(side)/max(half,.1))-depth
                col=13 if abs(side)<.4 and phase%3==0 else 11 if abs(side)<.45 else 9+(phase%2)
                if side<-.7:
                    col=8 if phase%2 else 9
                if depth>1:
                    col=12
                put(CX+ex*radial+px*side,CY+ey*radial+py*side,zz,col)

# Three tiers: spreading lower leaves, rising middle leaves, upright heart.
for n in range(7):
    leaf(2*math.pi*n/7+.16, 18+(n%3), 12+(n%3), 2.5, 46, n)
for n in range(5):
    leaf(2*math.pi*n/5+.65, 11+(n%2), 21+(n%3), 2.1, 46, n+7)
for n in range(3):
    leaf(2*math.pi*n/3+.2, 4.5, 28-(n%2), 1.6, 46, n+12)

def chunk(tag, data=b'', children=b''):
    return tag+struct.pack('<II',len(data),len(children))+data+children

size=chunk(b'SIZE',struct.pack('<III',64,64,80))
cells=bytes(v for p,c in sorted(voxels.items()) for v in (*p,c))
xyzi=chunk(b'XYZI',struct.pack('<I',len(voxels))+cells)
rgba=b''.join(bytes((*c,255)) for c in PALETTE)
rgba+=bytes((0,0,0,255))*(256-len(PALETTE))
data=b'VOX '+struct.pack('<I',150)+chunk(b'MAIN',children=size+xyzi+chunk(b'RGBA',rgba))
target=OUT/'Pineapple.vox'
target.write_bytes(data)
print(f'{target}: {len(voxels)} solid voxels, {len(PALETTE)} colors')

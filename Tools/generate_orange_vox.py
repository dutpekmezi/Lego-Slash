"""Generate a solid, exactly 300-cell orange for the project's VOX reader."""
from pathlib import Path
import struct
import uuid

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets' / 'Voxel Models'
PALETTE = [(244, 126, 18, 255), (255, 151, 28, 255),
           (224, 100, 10, 255), (119, 78, 35, 255), (67, 139, 38, 255)]

def chunk(tag, content):
    return tag + struct.pack('<ii', len(content), 0) + content

def generate():
    # Keep the closest cells to a slightly flattened sphere, including its interior.
    candidates = [(x, y, z) for x in range(9) for y in range(9) for z in range(9)]
    candidates.sort(key=lambda p: ((p[0]-4)**2 + (p[1]-4)**2 +
                                  ((p[2]-4)/0.96)**2, p[2], p[1], p[0]))
    cells = []
    for x, y, z in candidates[:294]:
        color = 2 if (x*17 + y*31 + z*13) % 11 < 2 else 1
        if z < 2:
            color = 3
        cells.append((x, y, z, color))
    cells.extend([(4, 4, 9, 4), (4, 4, 10, 4),
                  (5, 4, 10, 5), (6, 4, 10, 5), (5, 5, 10, 5), (6, 5, 10, 5)])
    assert len(cells) == len({c[:3] for c in cells}) == 300
    # Verify that all cells belong to one connected volume.
    occupied = {c[:3] for c in cells}
    pending = [next(iter(occupied))]
    visited = set(pending)
    for x, y, z in pending:
        for dx, dy, dz in [(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1)]:
            neighbor = (x+dx, y+dy, z+dz)
            if neighbor in occupied and neighbor not in visited:
                visited.add(neighbor)
                pending.append(neighbor)
    assert visited == occupied
    palette = b''.join(bytes(c) for c in PALETTE) + bytes(1024 - 4*len(PALETTE))
    children = (chunk(b'SIZE', struct.pack('<iii', 9, 9, 11)) +
                chunk(b'XYZI', struct.pack('<i', 300) + b''.join(bytes(c) for c in cells)) +
                chunk(b'RGBA', palette))
    target = OUT / 'Orange.vox'
    target.write_bytes(b'VOX ' + struct.pack('<i', 150) + b'MAIN' +
                       struct.pack('<ii', 0, len(children)) + children)
    meta = target.with_suffix('.vox.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex +
                        '\nDefaultImporter:\n  externalObjects: {}\n  userData: \n'
                        '  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
    print(f'{target}: 300 unique, connected voxels; dimensions 9 x 9 x 11')

if __name__ == '__main__':
    generate()

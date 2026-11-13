"""Cuts the map plinths (side-view ground pieces under the buildings) out of their white background.

python tools/art/cut_plinths.py
Reads art/map_src/plinth_<land>_src.png, writes unity/Assets/_Game/Art/Resources/Map/plinth_<land>.png,
trimmed, 512 px wide. The white around the piece is found by a flood fill from the picture's edge, so white
inside it (flowers, shells) stays.
"""
from collections import deque
from pathlib import Path
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "art/map_src"
OUT = ROOT / "unity/Assets/_Game/Art/Resources/Map"
WIDTH = 512


def cut(src, dst):
    im = Image.open(src).convert("RGB")
    w, h = im.size
    px = im.load()
    bg = [[False] * w for _ in range(h)]
    q = deque((x, y) for x in range(w) for y in (0, h - 1))
    q.extend((x, y) for y in range(h) for x in (0, w - 1))
    while q:
        x, y = q.popleft()
        if not (0 <= x < w and 0 <= y < h) or bg[y][x]:
            continue
        r, g, b = px[x, y]
        if min(r, g, b) < 225:
            continue
        bg[y][x] = True
        q.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    mask = Image.new("L", (w, h), 0)
    mp = mask.load()
    for y in range(h):
        for x in range(w):
            if not bg[y][x]:
                mp[x, y] = 255
    # A one-pixel soft edge instead of the white fringe.
    mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
    out = im.convert("RGBA")
    out.putalpha(mask)
    out = out.crop(mask.getbbox())
    out = out.resize((WIDTH, round(out.height * WIDTH / out.width)), Image.LANCZOS)
    out.save(dst, optimize=True)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for src in sorted(SRC.glob("plinth_*_src.png")):
        cut(src, OUT / src.name.replace("_src", ""))
        print(src.name)

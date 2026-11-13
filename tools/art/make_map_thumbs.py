"""Map of the kingdom: copies the map picture and makes a small picture of every finished building for it.

python tools/art/make_map_thumbs.py path/to/map.png
Writes unity/Assets/_Game/Art/Resources/Map/map_kingdom.png and thumb_castle_N.png (trimmed, 220 px tall).
"""
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RES = ROOT / "unity/Assets/_Game/Art/Resources"
OUT = RES / "Map"
THUMB_H = 220


def main(map_path):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.open(map_path).convert("RGB").save(OUT / "map_kingdom.png", optimize=True)
    for n in range(1, 18):
        full = Image.open(RES / f"Castles/castle_{n}/castle_{n}_full.png").convert("RGBA")
        box = full.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
        part = full.crop(box)
        w = round(part.width * THUMB_H / part.height)
        part.resize((w, THUMB_H), Image.LANCZOS).save(OUT / f"thumb_castle_{n}.png", optimize=True)


if __name__ == "__main__":
    main(sys.argv[1])

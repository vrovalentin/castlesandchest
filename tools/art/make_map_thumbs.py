"""Map of the kingdom: copies the map picture and makes the medallion pictures of every building.

python tools/art/make_map_thumbs.py path/to/map.png
Writes into unity/Assets/_Game/Art/Resources/Map/:
  map_kingdom.jpg      the map, 1024 px, JPEG (it has no transparency, so a PNG only makes it heavier)
  thumb_castle_N.png   the finished building, trimmed, 220 px tall, width padded to a multiple of 4
  sil_castle_N.png     its pale grey silhouette, for buildings still locked
"""
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
RES = ROOT / "unity/Assets/_Game/Art/Resources"
OUT = RES / "Map"
THUMB_H = 220
SIL = (196, 198, 210)


def main(map_path):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.open(map_path).convert("RGB").resize((1024, 1024), Image.LANCZOS).save(
        OUT / "map_kingdom.jpg", quality=85, optimize=True, progressive=False)
    for d in sorted((RES / "Castles").glob("castle_*")):
        if not d.is_dir():
            continue
        cid = d.name
        full = Image.open(d / f"{cid}_full.png").convert("RGBA")
        box = full.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
        part = full.crop(box)
        w = round(part.width * THUMB_H / part.height)
        part = part.resize((w, THUMB_H), Image.LANCZOS)
        # sides a multiple of 4, so the texture can be block-compressed in a build
        pad = Image.new("RGBA", ((w + 3) // 4 * 4, THUMB_H), (0, 0, 0, 0))
        pad.paste(part, ((pad.width - w) // 2, 0))
        part = pad
        part.save(OUT / f"thumb_{cid}.png", optimize=True)
        sil = Image.new("RGBA", part.size, SIL + (255,))
        sil.putalpha(part.getchannel("A"))
        sil.save(OUT / f"sil_{cid}.png", optimize=True)


if __name__ == "__main__":
    main(sys.argv[1])

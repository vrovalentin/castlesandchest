# Art review: AI tells and inconsistencies (2026-11-14, build cc7e6ea)

Checked on the build PC: all 18 `castle_N_full.png` at full size, the resident icons in `Parts/`,
and the in-game screens shot for the store (`store/yandex_v2`). Map order numbers are in brackets.

## Must fix (players notice these)

1. **Garbled clock numerals** — `castle_12` (Clock Tower, [15]). Roman numerals read XII, I, II, III,
   IIII… and the lower-left ones are broken ("ɅI"-like). Classic generation tell. Crop: `docs/art_review/clock.png`.
   Fix: repaint the dial (numerals or plain dots/sticks), or cut the dial from a clean generation.
2. **Tavern sign shows a cowboy hat** — `castle_8` (Tavern, [13]). A tavern sign should be a mug / barrel.
   Crop: `docs/art_review/sign.png`. The part is `sign` (also used by other buildings — check those).
3. **Snow-queen look-alike** — `castle_10` (Ice Palace, [10]). The lady in the doorway (platinum braid,
   ice-blue gown) reads as a well-known animated character. Store moderation / players may flag it.
   Fix: different hair colour/style and dress (e.g. a snow maiden with a kokoshnik), or a polar fox/owl resident.
4. **Princess mismatch** — `castle_2` (Castle with Towers, [5]): on the building she is brown-haired in pink;
   the new icon `part_princess.png` is blonde in purple. Same character must look the same.

## Consistency (reads as "assembled from different generations")

5. **Plinth / base differs.** Most buildings stand on an orange rock plinth; without one:
   `castle_2` (flat grey steps), `castle_5` (tree roots on grass), `castle_8` (bushes, no base),
   `castle_9` (small grass patch). Snow/volcano have their own bases (`castle_10`, `castle_18`, `castle_15`) — fine by zone,
   but the four above should get a plinth of their zone (meadow: grass-and-stone disc).
6. **Resident scale jumps.** Giants next to the door: miller (`castle_4`), innkeeper (`castle_8`), wizard (`castle_9`)
   are ~half the building height; knight (`castle_2`), owl (`castle_12`), parrot (`castle_16`) are tiny.
   Pick one rule (e.g. resident = door height) and rescale the layer.
7. **Terem (`castle_18`, [11]) is denser and more realistic** than the rest (fine ornament, thinner lines,
   darker rocks, wider camera). Next to the cartoon set it looks like another artist. A softening/saturation pass
   and the common plinth would pull it in.
8. **Light direction varies** (shadows left on some, right on others) and **saturation** varies
   (seashell/candy very pink and glowing, terem/tavern muted). One colour grade over all building and part art
   (same contrast curve, saturation, slight warm tint, same soft drop shadow) is the cheapest big win.
9. **Emelya icon** (`part_emelya.png`) — the figure is small inside its tile because the stove is included;
   at icon size he is hard to read. Crop tighter to Emelya, or keep the stove as its own part.
10. **Floating lanterns** around the wizard tower (`castle_9`) hang in the air without chains — reads as an artifact.

## In-game screens (from the store shoot)

11. Half-built buildings: hard layer edges, holes with an outline where the next part goes, pieces hanging
    in the air (pumpkin [7]: the window hole above the door). Suggest: translucent silhouette of the next
    part instead of the hole, soft 2–3 px edge on layers, a small dust puff when a part lands.
12. Top-left round button looks like "power off" but opens "Start over?"; the speaker button opens Settings.
    Use a gear for settings and a circular arrow for start over.
13. Mine: the "LOOT / Добыто" panel is half the screen and mostly empty.
14. Map on tall phones: blue strip under the picture, between the map and the resident card.
15. Hints: dense text in a big panel, sometimes 2–3 in a row. Prefer a pointing finger + one short line.

## Fine as is

- Overall cartoon style, palettes per zone, readable silhouettes on the map medallions.
- Residents' lines (funny, specific) — they do not read as generated.
- UI frame (purple panels, gold buttons, ribbons) is consistent.

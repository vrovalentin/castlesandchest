using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>The «?» on the next-part card: where this part comes from, in full. Which chest drops it, and the
    /// storage recipe with the player's own parts of the level below, plus a button that opens storage with two of
    /// them already picked.</summary>
    public sealed partial class BuildScreen
    {
        private bool _partHelpOpen;
        private GUIStyle _helpBtn, _helpHead, _helpText;

        private bool HelpButton(Rect r)
        {
            _helpBtn ??= new GUIStyle(UiArt.Style(UiArt.Round(Gold, CardEdge, 22, 3), 22, 30, Ink)) { fontStyle = FontStyle.Bold, padding = new RectOffset(0, 0, 0, 2) };
            return GUI.Button(r, "?", _helpBtn);
        }

        /// <summary>Parts of a tier in storage, the ones the building doesn't need first, then by count.</summary>
        private List<string> SpareOfTier(int tier) => Game.Catalog.Parts
            .Where(p => p.Tier == tier && State.Count(p.Id) > 0)
            .OrderBy(p => Game.Need(State, p.Id) > 0).ThenByDescending(p => State.Count(p.Id))
            .Select(p => p.Id).ToList();

        private void DrawPartHelp(float w, float h)
        {
            Dim(w, h);
            int next = Game.NextSlot(State);
            if (next < 0) { _partHelpOpen = false; return; }
            string id = Game.CastleOf(State).Slots[next];
            var rules = Game.Rules;
            int tier = Game.Catalog.TierOf(id);
            bool canMergeInto = tier > 1;
            var below = canMergeInto ? SpareOfTier(tier - 1) : new List<string>();
            int belowCount = below.Sum(p => State.Count(p));

            float pw = Mathf.Min(680, w - 40), ph = Mathf.Min(h - 120, canMergeInto ? 1010 : 720);
            var r = new Rect((w - pw) / 2, Mathf.Max(100, (h - ph) / 2), pw, ph);
            NightWindow(r, Loc.T("Где взять деталь"));
            _helpHead ??= new GUIStyle(_onNight) { fontSize = 26, alignment = TextAnchor.UpperLeft };
            _helpHead.normal.textColor = new Color32(0xff, 0xd6, 0x6e, 0xff);
            _helpText ??= new GUIStyle(_nightBody) { fontSize = 22, alignment = TextAnchor.UpperLeft };
            float x = r.x + 44, cw = r.width - 88, y = r.y + 104;

            // The part itself.
            var icon = new Rect(x, y, 110, 110);
            GUI.Box(icon, GUIContent.none, _tile);
            GameArt.DrawPart(new Rect(icon.x + 10, icon.y + 10, 90, 90), id, tier);
            var nameStyle = new GUIStyle(_helpHead) { fontSize = 32 };
            nameStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(icon.xMax + 20, y + 10, cw - 130, 44), Game.Catalog.NameOf(id), nameStyle);
            TierTag(new Rect(icon.xMax + 20, y + 62, 96, 34), tier, true);
            y += 140;

            // 1. From a chest.
            GUI.Label(new Rect(x, y, cw, 34), Loc.T("1. Найти в сундуке"), _helpHead);
            y += 42;
            int color = Mathf.Min(tier, rules.ColorTiers) - 1;
            var chestTex = GameArt.ChestClosed(color);
            if (chestTex != null) GUI.DrawTexture(new Rect(x, y, 96, 96), chestTex, ScaleMode.ScaleToFit);
            string colorName = Palette.BlockNames[color];
            string fromChest = tier <= rules.ColorTiers
                ? Loc.F("Выпадает из сундука цвета «{0}». Такой сундук получается в шахте из группы в 4 и больше блоков этого цвета. Чем больше группа, тем больше звёзд и деталей.", colorName)
                : Loc.F("Иногда выпадает из сундука цвета «{0}» на 4–5 звёзд. Такой сундук получается в шахте из большой группы блоков этого цвета.", colorName);
            float th = _helpText.CalcHeight(new GUIContent(fromChest), cw - 116);
            GUI.Label(new Rect(x + 116, y, cw - 116, th), fromChest, _helpText);
            y += Mathf.Max(110, th + 16);

            if (canMergeInto)
            {
                // 2. Merged in storage from two parts of the level below.
                GUI.Label(new Rect(x, y, cw, 34), Loc.T("2. Собрать на складе"), _helpHead);
                y += 42;
                string how = Loc.F("Две любые детали уровня {0} (можно одинаковые) собираются в одну деталь уровня {1}. На складе тапни две такие детали, потом выбери, что из них сделать: «{2}».",
                    tier - 1, tier, Game.Catalog.NameOf(id));
                th = _helpText.CalcHeight(new GUIContent(how), cw);
                GUI.Label(new Rect(x, y, cw, th), how, _helpText);
                y += th + 14;

                // The player's own parts of the level below.
                string have = belowCount == 0 ? Loc.F("Деталей уровня {0} у тебя пока нет.", tier - 1)
                    : belowCount == 1 ? Loc.F("У тебя 1 деталь уровня {0}, нужна ещё одна.", tier - 1)
                    : Loc.F("Деталей уровня {0} у тебя: {1}. Хватает, можно собрать!", tier - 1, belowCount);
                GUI.Label(new Rect(x, y, cw, 32), have, _helpText);
                y += 40;
                const float t = 84, gap = 14;
                int fit = Mathf.Max(1, Mathf.FloorToInt((cw + gap) / (t + gap)));
                for (int i = 0; i < below.Count && i < fit; i++)
                {
                    var c = new Rect(x + i * (t + gap), y, t, t);
                    GUI.Box(c, GUIContent.none, _tile);
                    GameArt.DrawPart(new Rect(c.x + 9, c.y + 9, t - 18, t - 18), below[i], tier - 1);
                    Badge(new Vector2(c.xMax - 6, c.y + 6), State.Count(below[i]));
                }
                if (below.Count > 0) y += t + 16;
            }

            // Buttons: storage with two parts already picked (when there are two), and «Понятно».
            float by = r.yMax - 140, bw = (r.width - 140 - 16) / 2;
            if (canMergeInto && belowCount >= 2)
            {
                if (GUI.Button(new Rect(r.x + 70, by + 8, bw, 80), Loc.T("Открыть склад"), _green))
                    Later(() =>
                    {
                        _partHelpOpen = false;
                        _pick.Clear();
                        var spare = SpareOfTier(tier - 1);
                        _pick.Add(spare[0]);
                        _pick.Add(State.Count(spare[0]) >= 2 || spare.Count < 2 ? spare[0] : spare[1]);
                        _storageOpen = true;
                    }, click: true);
                if (GoldButton(new Rect(r.x + 70 + bw + 16, by, bw, 96), Loc.T("Понятно"), 32)) Later(() => _partHelpOpen = false);
            }
            else if (GoldButton(new Rect(r.x + 70, by, r.width - 140, 96), Loc.T("Понятно"), 34)) Later(() => _partHelpOpen = false);
        }
    }
}

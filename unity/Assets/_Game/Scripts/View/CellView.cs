using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>One block or chest on the board.</summary>
    public sealed class CellView : MonoBehaviour
    {
        public int Id { get; private set; }
        private SpriteRenderer _body, _trim, _ice;
        private readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>();
        private CellKind _kind;
        private bool _iced;
        private int _chestStars;

        public static CellView Create(Transform parent, int id)
        {
            var go = new GameObject("Cell " + id);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<CellView>();
            v.Id = id;
            v._body = go.AddComponent<SpriteRenderer>();
            var trim = new GameObject("Trim");
            trim.transform.SetParent(go.transform, false);
            v._trim = trim.AddComponent<SpriteRenderer>();
            v._trim.sprite = SpriteFactory.ChestTrim;
            v._trim.sortingOrder = 1;
            var ice = new GameObject("Ice");
            ice.transform.SetParent(go.transform, false);
            v._ice = ice.AddComponent<SpriteRenderer>();
            v._ice.sprite = SpriteFactory.Ice;
            v._ice.sortingOrder = 4;
            ice.SetActive(false);
            return v;
        }

        public void Apply(CellKind kind, int color, int stars, bool ice = false)
        {
            _kind = kind;
            _iced = ice;
            bool chest = kind == CellKind.Chest, stone = kind == CellKind.Stone;
            var art = stone ? SpriteFactory.Stone : chest ? SpriteFactory.ChestArt(color) : SpriteFactory.BlockArt(color);
            _body.sprite = art != null ? art : chest ? SpriteFactory.ChestBody : SpriteFactory.Block;
            // Drawn art already has its colour; placeholders are grey and get tinted.
            _body.color = art != null ? Color.white : stone ? new Color(0.35f, 0.33f, 0.32f) : (Color)Palette.Block(color);
            _trim.gameObject.SetActive(chest && art == null);
            _ice.gameObject.SetActive(ice && _ice.sprite != null);
            _chestStars = kind == CellKind.Chest ? stars : 0;
            ShowStars(_chestStars, kind == CellKind.Chest ? -0.22f : 0f);
        }

        /// <summary>For blocks: how many stars a chest from this block's group would get. 0 hides it.</summary>
        public void SetHint(int stars)
        {
            if (_kind != CellKind.Block || _iced) return;
            ShowStars(stars, 0f);
        }

        private void ShowStars(int n, float y)
        {
            while (_stars.Count < n)
            {
                var go = new GameObject("Star");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Star;
                sr.sortingOrder = 3;
                var shadow = new GameObject("Shadow");
                shadow.transform.SetParent(go.transform, false);
                shadow.transform.localPosition = new Vector3(0.04f, -0.07f, 0);
                shadow.transform.localScale = Vector3.one * SpriteFactory.StarShadowScale;
                var ssr = shadow.AddComponent<SpriteRenderer>();
                ssr.sprite = SpriteFactory.StarShadow;
                ssr.sortingOrder = 2;
                _stars.Add(sr);
            }
            // Up to 3 stars in a row; 4–5 stars in two rows, as in the prototype.
            // Stars are large enough to read on a phone; neighbours overlap a little.
            const float size = 0.36f, step = 0.29f;
            int top = n <= 3 ? n : 3;
            for (int i = 0; i < _stars.Count; i++)
            {
                bool on = i < n;
                _stars[i].gameObject.SetActive(on);
                if (!on) continue;
                int row = i < top ? 0 : 1;
                int inRow = row == 0 ? top : n - top;
                int idx = row == 0 ? i : i - top;
                float x = (idx - (inRow - 1) / 2f) * step;
                float yy = y + (n > 3 ? (row == 0 ? step / 2 : -step / 2) : 0);
                _stars[i].transform.localPosition = new Vector3(x, yy, 0);
                _stars[i].transform.localScale = Vector3.one * size;
            }
        }

        public void SetAlpha(float a)
        {
            var c = _body.color; c.a = a; _body.color = c;
            var t = _trim.color; t.a = a; _trim.color = t;
            var ic = _ice.color; ic.a = a; _ice.color = ic;
            foreach (var s in _stars) { var sc = s.color; sc.a = a; s.color = sc; }
        }
    }
}

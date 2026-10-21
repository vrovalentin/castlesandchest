using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>One block or chest on the board.</summary>
    public sealed class CellView : MonoBehaviour
    {
        public int Id { get; private set; }
        private SpriteRenderer _body, _trim;
        private readonly List<SpriteRenderer> _stars = new List<SpriteRenderer>();
        private CellKind _kind;
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
            return v;
        }

        public void Apply(CellKind kind, int color, int stars)
        {
            _kind = kind;
            _body.sprite = kind == CellKind.Chest ? SpriteFactory.ChestBody : SpriteFactory.Block;
            _body.color = Palette.Block(color);
            _trim.gameObject.SetActive(kind == CellKind.Chest);
            _chestStars = kind == CellKind.Chest ? stars : 0;
            ShowStars(_chestStars, kind == CellKind.Chest ? -0.22f : 0f);
        }

        /// <summary>For blocks: how many stars a chest from this block's group would get. 0 hides it.</summary>
        public void SetHint(int stars)
        {
            if (_kind != CellKind.Block) return;
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
                sr.sortingOrder = 2;
                _stars.Add(sr);
            }
            // Up to 3 stars in a row; 4–5 stars in two rows, as in the prototype.
            const float size = 0.26f;
            int top = n <= 3 ? n : 3;
            for (int i = 0; i < _stars.Count; i++)
            {
                bool on = i < n;
                _stars[i].gameObject.SetActive(on);
                if (!on) continue;
                int row = i < top ? 0 : 1;
                int inRow = row == 0 ? top : n - top;
                int idx = row == 0 ? i : i - top;
                float x = (idx - (inRow - 1) / 2f) * size;
                float yy = y + (n > 3 ? (row == 0 ? size / 2 : -size / 2) : 0);
                _stars[i].transform.localPosition = new Vector3(x, yy, 0);
                _stars[i].transform.localScale = Vector3.one * size;
            }
        }

        public void SetAlpha(float a)
        {
            var c = _body.color; c.a = a; _body.color = c;
            var t = _trim.color; t.a = a; _trim.color = t;
            foreach (var s in _stars) { var sc = s.color; sc.a = a; s.color = sc; }
        }
    }
}

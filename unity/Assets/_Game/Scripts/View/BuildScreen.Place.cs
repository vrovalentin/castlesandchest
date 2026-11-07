using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Placing a part with a little show: the part flies from the card up to its place on the castle,
    /// drops in, lands with a knock, a puff of dust and sparkles. Until it lands the buttons wait,
    /// so tapping «Поставить» fast no longer builds the castle in a blink.
    /// </summary>
    public sealed partial class BuildScreen
    {
        private const float FlyTime = 0.42f, RowFlyTime = 0.32f, DropTime = 0.5f;
        /// <summary>When, in the drop, the part touches down.</summary>
        private const float Contact = 0.55f * DropTime;

        private int _flySlot = -1, _rowLeft;
        private string _flyId;
        private float _flyAt, _flyDur, _thudAt = -1, _doneDueAt = -1, _rowNextAt;
        private Rect _iconAt;

        /// <summary>A part is on its way or has not landed yet.</summary>
        private bool Placing => _flySlot >= 0 || _rowLeft > 0 || (_placedSlot >= 0 && Time.time - _placedAt < Contact + 0.12f);

        /// <summary>Starts placing the part of this slot (and then up to <paramref name="row"/> − 1 more in a row).</summary>
        private void StartPlace(int slot, int row = 1, bool chained = false)
        {
            if (_donePanel || _doneDueAt > 0 || (!chained && Placing)) return;
            var slots = Game.CastleOf(State).Slots;
            if (slot < 0 || slot >= slots.Length || State.Filled[slot]) return;
            // Out of order or nothing in storage: PlaceSlot says why.
            if (slot != Game.NextSlot(State) || State.Count(slots[slot]) <= 0) { PlaceSlot(slot); _rowLeft = 0; return; }
            _flySlot = slot;
            _flyId = slots[slot];
            _flyAt = Time.time;
            _flyDur = row > 1 || chained ? RowFlyTime : FlyTime;
            if (!chained) _rowLeft = row - 1;
            Sfx.Play("whoosh", 0.6f, 1.15f);
            Haptics.Tap(8);
        }

        /// <summary>Runs every frame: lands the flying part, knocks it in, starts the next one of a row.</summary>
        private void TickPlace()
        {
            if (_flySlot >= 0 && Time.time - _flyAt >= _flyDur)
            {
                int slot = _flySlot;
                _flySlot = -1;
                if (PlaceSlot(slot, quiet: true)) _thudAt = Time.time + Contact;
                else _rowLeft = 0;
                if (_rowLeft > 0) _rowNextAt = Time.time + Contact + 0.08f;
            }
            if (_thudAt > 0 && Time.time >= _thudAt)
            {
                _thudAt = -1;
                Sfx.Play("place");
                Sfx.Play("hammer", 0.8f, UnityEngine.Random.Range(0.92f, 1.08f));
                Haptics.Tap(25);
            }
            if (_rowLeft > 0 && _flySlot < 0 && Time.time >= _rowNextAt)
            {
                int next = Game.NextSlot(State);
                if (next < 0 || _doneDueAt > 0 || State.Count(Game.CastleOf(State).Slots[next]) <= 0) _rowLeft = 0;
                else { _rowLeft--; StartPlace(next, chained: true); }
            }
            if (_doneDueAt > 0 && Time.time >= _doneDueAt)
            {
                _doneDueAt = -1;
                _donePanel = true;
                _doneAt = Time.time;
                Sfx.Play("fanfare");
            }
        }

        private void ResetPlacing() { _flySlot = -1; _rowLeft = 0; _thudAt = -1; _doneDueAt = -1; _placedSlot = -1; }

        private static float EaseInOut(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        /// <summary>The flying part: from the card's icon up in an arc to its place, turning a little and fading as it arrives.</summary>
        private void DrawPlaceFlight()
        {
            if (_flySlot < 0 || Art == null || Event.current.type != EventType.Repaint) return;
            if (!Art.SlotSpot(_picRect, _flySlot, out var to, out _)) return;
            float t = Mathf.Clamp01((Time.time - _flyAt) / _flyDur), k = EaseInOut(t);
            var from = _iconAt.width > 0 ? _iconAt.center : new Vector2(to.x, to.y + 300);
            var at = Vector2.Lerp(from, to, k) + Vector2.up * (-170 * Mathf.Sin(Mathf.PI * k));
            float size = Mathf.Lerp(100, 84, k) * (1 + 0.18f * Mathf.Sin(Mathf.PI * t));
            var old = GUI.color;
            // A short trail of sparkles behind it.
            var sp = GameArt.Sparkle;
            if (sp != null)
                for (int i = 1; i <= 3; i++)
                {
                    float kt = EaseInOut(t - i * 0.07f);
                    var p = Vector2.Lerp(from, to, kt) + Vector2.up * (-170 * Mathf.Sin(Mathf.PI * kt));
                    GUI.color = new Color(1, 0.95f, 0.7f, 0.5f * (1 - i / 4f) * Mathf.Clamp01(t * 5));
                    float s = 34 - i * 6;
                    GUI.DrawTexture(new Rect(p.x - s / 2, p.y - s / 2, s, s), sp, ScaleMode.ScaleToFit);
                }
            GUI.color = new Color(1, 1, 1, t > 0.8f ? 1 - (t - 0.8f) / 0.2f : 1);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Sin(t * Mathf.PI * 2) * 10, at);
            GameArt.DrawPart(new Rect(at.x - size / 2, at.y - size / 2, size, size), _flyId, Game.Catalog.TierOf(_flyId));
            GUI.matrix = m;
            GUI.color = old;
        }

        /// <summary>After touchdown: a puff of dust along the part's bottom and sparkles over it.</summary>
        private void DrawLanding(Rect pic)
        {
            if (_placedSlot < 0 || Art == null || Event.current.type != EventType.Repaint) return;
            float t = Time.time - _placedAt - Contact;
            if (t < 0 || t > 0.6f) return;
            if (!Art.SlotSpot(pic, _placedSlot, out var c, out float bottom)) return;
            float k = t / 0.6f, e = EaseOut(k);
            var old = GUI.color;
            for (int i = 0; i < 7; i++)
            {
                float side = i - 3, s = (38 + 10 * (i % 3)) * (0.6f + 0.8f * e);
                var p = new Vector2(c.x + side * (16 + 26 * e), bottom - 6 - (8 + 18 * (i % 2)) * e);
                GUI.color = new Color(0.86f, 0.8f, 0.72f, 0.75f * (1 - k));
                GUI.DrawTexture(new Rect(p.x - s / 2, p.y - s / 2, s, s), UiArt.SoftDisc(Color.white), ScaleMode.ScaleToFit);
            }
            GUI.color = old;
            DrawBurst(c, Mathf.Clamp01(t / 0.5f));
        }

        /// <summary>A little shake of the whole castle at touchdown, in points.</summary>
        private float LandingShake()
        {
            if (_placedSlot < 0) return 0;
            float t = Time.time - _placedAt - Contact;
            return t < 0 || t > 0.22f ? 0 : Mathf.Sin(t * 70) * 3.5f * (1 - t / 0.22f);
        }
    }
}

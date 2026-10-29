using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// First-time hints: a bubble on the night-sky panel, shown once each; a tap anywhere closes it.
    /// A screen asks <see cref="Want"/> during its OnGUI, and the bubble is drawn by <see cref="Draw"/> at the end.
    /// </summary>
    public static class Hints
    {
        private const string Prefix = "castles-and-chest-hint-";

        private static string _key, _text;
        private static Rect _rect;
        private static float _shownAt;
        private static int _frame;
        private static GUIStyle _box, _body, _small;

        // Off without saves (tests), so a bubble never sits over what a test checks.
        public static bool Seen(string key) => AppController.NoSaves || PlayerPrefs.GetInt(Prefix + key, 0) == 1;

        public static void MarkSeen(string key)
        {
            PlayerPrefs.SetInt(Prefix + key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Forget every hint, for «Начать сначала».</summary>
        public static void Reset()
        {
            foreach (var k in new[] { "chests", "place", "mine", "board" }) PlayerPrefs.DeleteKey(Prefix + k);
            PlayerPrefs.Save();
        }

        /// <summary>True while a hint bubble is on screen (taps elsewhere wait until it is closed).</summary>
        public static bool Showing => _key != null && Time.frameCount - _frame <= 2;

        /// <summary>
        /// Call once per OnGUI pass, at its start. Shows the hint unless it was seen; a tap closes it and is used up.
        /// Returns true while it is shown.
        /// </summary>
        public static bool Want(string key, Rect bubble, string text)
        {
            if (Seen(key)) return false;
            if (_key != key) { _key = key; _shownAt = Time.time; }
            _text = text;
            _rect = bubble;
            _frame = Time.frameCount;
            var e = Event.current;
            // A short moment before a tap can close it, so the tap that led here doesn't close it at once.
            if (e != null && (e.type == EventType.MouseDown || e.type == EventType.MouseUp) && Time.time - _shownAt > 0.4f)
            {
                if (e.type == EventType.MouseUp) { MarkSeen(key); _key = null; }
                e.Use();
            }
            else if (e != null && (e.type == EventType.MouseDown || e.type == EventType.MouseUp)) e.Use();
            return true;
        }

        /// <summary>Draws the bubble asked for this pass, if any. Call at the end of OnGUI.</summary>
        public static void Draw()
        {
            if (_key == null || Time.frameCount != _frame || Seen(_key)) return;
            if (_box == null)
            {
                var tex = GameArt.NightPanel;
                _box = tex != null ? UiArt.Style(tex, 56, 1, Color.white) : UiArt.Style(UiArt.Round(new Color32(0x2a, 0x36, 0x6e, 0xf0), new Color32(0x78, 0x4e, 0x2a, 0xff), 22, 5), 22, 1, Color.white);
                _body = new GUIStyle { fontSize = 25, wordWrap = true, alignment = TextAnchor.MiddleCenter, richText = true, font = UiArt.TextFont };
                _body.normal.textColor = new Color32(0xfb, 0xf6, 0xe8, 0xff);
                _small = new GUIStyle(_body) { fontSize = 18 };
                _small.normal.textColor = new Color32(0xff, 0xd2, 0x6e, 0xff);
            }
            float k = Mathf.Clamp01((Time.time - _shownAt) / 0.25f);
            var r = new Rect(_rect.center.x - _rect.width * (0.9f + 0.1f * k) / 2, _rect.center.y - _rect.height * (0.9f + 0.1f * k) / 2,
                _rect.width * (0.9f + 0.1f * k), _rect.height * (0.9f + 0.1f * k));
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, k);
            GUI.Box(r, GUIContent.none, _box);
            GUI.Label(new Rect(r.x + 34, r.y + 26, r.width - 68, r.height - 80), _text, _body);
            float blink = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
            GUI.color = new Color(1, 1, 1, k * blink);
            GUI.Label(new Rect(r.x, r.yMax - 52, r.width, 30), "тапни, чтобы продолжить", _small);
            GUI.color = old;
        }
    }
}

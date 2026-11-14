using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// A bouncing gold arrow over the button a new player should tap next. Each lesson ("open a chest",
    /// "place a part", "go to the mine") is shown until the player has done it once.
    /// </summary>
    public static class Pointer
    {
        private const string Prefix = "castles-and-chest-did-";
        public static readonly string[] Lessons = { "chest", "place", "mine" };

        public static bool Done(string lesson) => AppController.NoSaves || PlayerPrefs.GetInt(Prefix + lesson, 0) == 1;

        public static void MarkDone(string lesson)
        {
            if (Done(lesson)) return;
            PlayerPrefs.SetInt(Prefix + lesson, 1);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            foreach (var k in Lessons) PlayerPrefs.DeleteKey(Prefix + k);
            PlayerPrefs.Save();
        }

        private static GUIStyle _caption;

        /// <summary>
        /// Draws the arrow above <paramref name="target"/> (GUI coordinates) unless the lesson is done.
        /// With <paramref name="into"/> its tip goes down to the middle of the target, so it stays off what is above.
        /// A <paramref name="caption"/> is one short line in a dark pill above the arrow.
        /// </summary>
        public static void Show(string lesson, Rect target, bool into = false, string caption = null)
        {
            if (Done(lesson) || Event.current.type != EventType.Repaint) return;
            const float size = 92;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 18;
            float tipY = into ? target.center.y : target.y - 4;
            var r = new Rect(target.center.x - size / 2, tipY - size - bob, size, size);
            GUI.DrawTexture(r, UiArt.Pointer, ScaleMode.ScaleToFit);
            if (string.IsNullOrEmpty(caption)) return;
            _caption ??= new GUIStyle
            {
                fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false,
                padding = new RectOffset(22, 22, 8, 8), font = UiArt.TextFont,
                normal = { background = UiArt.Round(new Color(0.12f, 0.1f, 0.2f, 0.86f), new Color32(0xff, 0xc8, 0x3c, 0xff), 22, 2), textColor = new Color32(0xfb, 0xf6, 0xe8, 0xff) },
                border = new RectOffset(22, 22, 22, 22),
            };
            var c = new GUIContent(caption);
            var sz = _caption.CalcSize(c);
            float w = Screen.width / Mathf.Max(0.01f, GUI.matrix.m00);
            float x = Mathf.Clamp(target.center.x - sz.x / 2, 12, Mathf.Max(12, w - 12 - sz.x));
            // Fixed height above the arrow's highest point, so the line stays still while the arrow bobs.
            GUI.Label(new Rect(x, tipY - size - 18 - 10 - sz.y, sz.x, sz.y), c, _caption);
        }
    }
}

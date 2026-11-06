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

        /// <summary>
        /// Draws the arrow above <paramref name="target"/> (GUI coordinates) unless the lesson is done.
        /// With <paramref name="into"/> its tip goes down to the middle of the target, so it stays off what is above.
        /// </summary>
        public static void Show(string lesson, Rect target, bool into = false)
        {
            if (Done(lesson) || Event.current.type != EventType.Repaint) return;
            const float size = 92;
            float bob = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 18;
            float tipY = into ? target.center.y : target.y - 4;
            var r = new Rect(target.center.x - size / 2, tipY - size - bob, size, size);
            GUI.DrawTexture(r, UiArt.Pointer, ScaleMode.ScaleToFit);
        }
    }
}

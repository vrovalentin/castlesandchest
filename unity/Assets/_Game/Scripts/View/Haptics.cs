using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// A short buzz on Android for merges and chests. Elsewhere (the browser, the editor) it does nothing.
    /// It can be turned off in the settings.
    /// </summary>
    public static class Haptics
    {
        private const string OffKey = "castles-and-chest-vibration-off";

        public static bool Off
        {
            get => PlayerPrefs.GetInt(OffKey, 0) == 1;
            set { PlayerPrefs.SetInt(OffKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>True where there is something to buzz: the settings show the switch only then.</summary>
        public static bool Supported => Application.platform == RuntimePlatform.Android;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _tried;
#endif
        // Unity adds the VIBRATE permission to the APK only when Handheld.Vibrate appears in the code.
        private static bool _never;

        /// <summary>A buzz of <paramref name="ms"/> milliseconds.</summary>
        public static void Tap(long ms = 25)
        {
            if (Off || !Supported) return;
            if (_never) Handheld.Vibrate();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!_tried)
                {
                    _tried = true;
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                _vibrator?.Call("vibrate", ms);
            }
            catch (System.Exception) { _vibrator = null; }
#endif
        }
    }
}

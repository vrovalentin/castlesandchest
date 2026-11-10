using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Game.Platform
{
    /// <summary>
    /// The game's only door to the store it runs in: ads, cloud save, language, server time, «game is ready».
    /// In the browser it talks to window.CastlesPlatform (WebGLTemplates/Fullscreen/platform.js), which wraps
    /// the Yandex Games SDK. Everywhere else (editor, tests, Android for now) it is a stub: no cloud,
    /// and a rewarded ad gives its reward at once.
    /// </summary>
    public static class Sdk
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int Plat_Ready();
        [DllImport("__Internal")] private static extern string Plat_Name();
        [DllImport("__Internal")] private static extern string Plat_Lang();
        [DllImport("__Internal")] private static extern double Plat_ServerTime();
        [DllImport("__Internal")] private static extern string Plat_Cloud();
        [DllImport("__Internal")] private static extern void Plat_SaveCloud(string json, double t, int flush);
        [DllImport("__Internal")] private static extern void Plat_GameReady();
        [DllImport("__Internal")] private static extern void Plat_Gameplay(int on);
        [DllImport("__Internal")] private static extern void Plat_ShowRewarded();
        [DllImport("__Internal")] private static extern void Plat_ShowInterstitial();
        private static readonly bool Web = true;
#else
        private static int Plat_Ready() => 1;
        private static string Plat_Name() => "none";
        private static string Plat_Lang() => "ru";
        private static double Plat_ServerTime() => 0;
        private static string Plat_Cloud() => "";
        private static void Plat_SaveCloud(string json, double t, int flush) { }
        private static void Plat_GameReady() { }
        private static void Plat_Gameplay(int on) { }
        private static void Plat_ShowRewarded() { }
        private static void Plat_ShowInterstitial() { }
        private static readonly bool Web = false;
#endif

        /// <summary>How often the cloud save is sent at most; the local save still happens after every action.</summary>
        private const float CloudEvery = 15f;

        private static bool _started;
        private static double _offsetMs;
        private static Action _reward, _failed;
        private static bool _rewarded;
        private static string _pendingCloud;
        private static float _lastCloud = -999f;

        /// <summary>True once the SDK has answered (or given up): language, server time and the cloud save are known.</summary>
        public static bool Ready => Plat_Ready() == 1;

        /// <summary>«yandex» inside Yandex Games, «none» elsewhere.</summary>
        public static string Name { get; private set; } = "none";

        /// <summary>The player's language from the SDK (ru, en, tr...). The game is Russian only for now.</summary>
        public static string Lang { get; private set; } = "ru";

        /// <summary>A rewarded or fullscreen ad is on screen: the game and its sound wait.</summary>
        public static bool AdOpen { get; private set; }

        /// <summary>Local time, corrected by the store's server clock when there is one (so moving the phone's clock gives nothing).</summary>
        public static DateTime Now => DateTime.Now.AddMilliseconds(_offsetMs);

        /// <summary>Milliseconds since 1970 by the same clock as <see cref="Now"/>; marks which save is newer.</summary>
        public static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)_offsetMs;

        /// <summary>Creates the object that receives the browser's events. Safe to call more than once.</summary>
        public static void Init()
        {
            if (_started || !Application.isPlaying) return;
            _started = true;
            var go = new GameObject(PlatformEvents.ObjectName) { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<PlatformEvents>();
        }

        /// <summary>Reads what the SDK found at start. Call once <see cref="Ready"/> is true (or the wait timed out).</summary>
        public static void ReadStart()
        {
            Name = Plat_Name() ?? "none";
            string lang = Plat_Lang();
            if (!string.IsNullOrEmpty(lang)) Lang = lang;
            double server = Plat_ServerTime();
            if (server > 0) _offsetMs = server - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>The cloud save (game JSON and when it was made), or null when there is none.</summary>
        public static CloudSave LoadCloud()
        {
            string raw = Plat_Cloud();
            if (string.IsNullOrEmpty(raw)) return null;
            try
            {
                var c = JsonUtility.FromJson<CloudSave>(raw);
                return c != null && !string.IsNullOrEmpty(c.save) ? c : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] cloud save could not be read: " + e.Message);
                return null;
            }
        }

        /// <summary>Queues the save for the cloud; it goes out at most every 15 s, and at once when the tab is hidden.</summary>
        public static void SaveCloud(string json)
        {
            if (!Web) return;
            _pendingCloud = json;
            if (Time.realtimeSinceStartup - _lastCloud >= CloudEvery) FlushCloud(false);
        }

        /// <summary>Sends the queued cloud save now. <paramref name="urgent"/> asks the SDK to write it without delay.</summary>
        public static void FlushCloud(bool urgent)
        {
            if (_pendingCloud == null) return;
            Plat_SaveCloud(_pendingCloud, NowMs, urgent ? 1 : 0);
            _pendingCloud = null;
            _lastCloud = Time.realtimeSinceStartup;
        }

        internal static void Tick()
        {
            if (_pendingCloud != null && Time.realtimeSinceStartup - _lastCloud >= CloudEvery) FlushCloud(false);
        }

        /// <summary>The game can be played: the store hides its loading screen (Yandex LoadingAPI.ready).</summary>
        public static void GameReady() => Plat_GameReady();

        /// <summary>Gameplay is on or off (Yandex GameplayAPI). The browser side also stops it during ads and while hidden.</summary>
        public static void Gameplay(bool on) => Plat_Gameplay(on ? 1 : 0);

        /// <summary>
        /// Shows a rewarded ad. <paramref name="reward"/> runs after the ad closes, only if it was watched to the end;
        /// <paramref name="failed"/> runs when it closed early or did not load. Without a store the reward comes at once.
        /// </summary>
        public static void Rewarded(Action reward, Action failed = null)
        {
            if (!Web) { reward?.Invoke(); return; }
            if (AdOpen || _reward != null) return;
            _reward = reward;
            _failed = failed;
            _rewarded = false;
            Plat_ShowRewarded();
        }

        /// <summary>A fullscreen ad, only at natural breaks (after a run, after a castle). The store decides if it really shows.</summary>
        public static void Interstitial()
        {
            if (!Web || AdOpen || _reward != null) return;
            Plat_ShowInterstitial();
        }

        internal static void OnAd(string ev)
        {
            switch (ev)
            {
                case "open":
                    AdOpen = true;
                    PauseGate.Ad = true;
                    break;
                case "rewarded":
                    _rewarded = true;
                    break;
                case "close":
                case "error":
                    AdOpen = false;
                    PauseGate.Ad = false;
                    var reward = _reward;
                    var failed = _failed;
                    bool got = _rewarded;
                    _reward = _failed = null;
                    _rewarded = false;
                    if (got) reward?.Invoke(); else if (reward != null) failed?.Invoke();
                    break;
            }
        }
    }

    /// <summary>The cloud save as stored by the SDK: the game's own save JSON and the time it was made.</summary>
    [Serializable]
    public sealed class CloudSave
    {
        public string save;
        public long t;
    }

    /// <summary>
    /// One place that decides whether sound and game time are paused: the tab is hidden, an ad is on,
    /// or the store asked for a pause.
    /// </summary>
    public static class PauseGate
    {
        private static bool _hidden, _ad, _store;

        public static bool Hidden { get => _hidden; set { _hidden = value; Apply(); } }
        public static bool Ad { get => _ad; set { _ad = value; Apply(); } }
        public static bool Store { get => _store; set { _store = value; Apply(); } }

        public static bool Paused => _hidden || _ad || _store;

        private static void Apply()
        {
            if (!Application.isPlaying) return;
            AudioListener.pause = Paused;
            Time.timeScale = _ad || _store ? 0f : 1f;
        }
    }

    /// <summary>Receives SendMessage calls from platform.js. Lives on a hidden object named «PlatformEvents».</summary>
    public sealed class PlatformEvents : MonoBehaviour
    {
        public const string ObjectName = "PlatformEvents";

        private void Awake() => gameObject.name = ObjectName;

        private void Update() => Sdk.Tick();

        /// <summary>open, rewarded, close or error.</summary>
        public void OnAd(string ev) => Sdk.OnAd(ev);

        /// <summary>«1» when the store asks to pause (its own ads), «0» to go on.</summary>
        public void OnPause(string on) => PauseGate.Store = on == "1";

        /// <summary>The tab was hidden («1») or shown again («0»). The cloud save goes out right away on hide.</summary>
        public void OnHidden(string on)
        {
            bool hidden = on == "1";
            PauseGate.Hidden = hidden;
            if (hidden) Sdk.FlushCloud(true);
        }

        private void OnApplicationFocus(bool focus) => PauseGate.Hidden = !focus;

        private void OnApplicationPause(bool paused)
        {
            PauseGate.Hidden = paused;
            if (paused) Sdk.FlushCloud(true);
        }
    }
}

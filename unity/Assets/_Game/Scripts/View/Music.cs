using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Background music from Resources/Music (made by tools/audio/make_music.py): one loop per screen,
    /// cross-faded when the screen changes. It can be turned off on its own, apart from the sound effects.
    /// While the game is hidden or an ad is on, all sound pauses (Game.Platform.PauseGate).
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        private const string OffKey = "castles-and-chest-music-off";
        private const float Volume = 0.45f, Fade = 0.8f;

        private static Music _it;
        private AudioSource _a, _b;

        public static bool Off
        {
            get => PlayerPrefs.GetInt(OffKey, 0) == 1;
            set { PlayerPrefs.SetInt(OffKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Starts the loop with this name (fading out the one playing), unless it is already on.</summary>
        public static void Play(string name)
        {
            if (!Application.isPlaying) return;
            if (_it == null)
            {
                var go = new GameObject("Music") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                // Nothing is heard without a listener, and the scene camera has none.
                if (FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
                _it = go.AddComponent<Music>();
            }
            _it.Switch(name);
        }

        private void Awake()
        {
            _a = Source();
            _b = Source();
        }

        private AudioSource Source()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0;
            return s;
        }

        private void Switch(string name)
        {
            if (_a.clip != null && _a.clip.name == name) return;
            var clip = Resources.Load<AudioClip>("Music/" + name);
            if (clip == null) return;
            (_a, _b) = (_b, _a);
            _a.clip = clip;
            _a.time = 0;
            _a.volume = 0;
            _a.Play();
        }

        private void Update()
        {
            float target = Off ? 0 : Volume, step = Time.unscaledDeltaTime * Volume / Fade;
            _a.volume = Mathf.MoveTowards(_a.volume, target, step);
            _b.volume = Mathf.MoveTowards(_b.volume, 0, step);
            if (_b.isPlaying && _b.volume <= 0) _b.Stop();
        }
    }
}

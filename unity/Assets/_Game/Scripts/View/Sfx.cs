using System.Collections.Generic;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Sound effects from Resources/Sfx (placeholders made by tools/audio/make_sfx.py).
    /// A small pool of AudioSources; a missing clip is simply silent.
    /// </summary>
    public static class Sfx
    {
        private static AudioSource[] _pool;
        private static int _next;
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<string, float> LastPlayed = new Dictionary<string, float>();

        private const string MuteKey = "castles-and-chest-mute";

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MuteKey, 0) == 1;
            set { PlayerPrefs.SetInt(MuteKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Plays a clip. <paramref name="pitch"/> 1 is as recorded; the same clip at most once per 40 ms.</summary>
        public static void Play(string name, float volume = 1f, float pitch = 1f)
        {
            if (Muted) return;
            if (LastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < 0.04f) return;
            var clip = Clip(name);
            if (clip == null) return;
            // A small pool, so changing the pitch of one sound doesn't bend another that is still playing.
            var src = Source();
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
            LastPlayed[name] = Time.unscaledTime;
        }

        private static AudioClip Clip(string name)
        {
            if (Clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Sfx/" + name);
            Clips[name] = c;
            return c;
        }

        private static AudioSource Source()
        {
            if (_pool == null || _pool[0] == null)
            {
                var go = new GameObject("Sfx") { hideFlags = HideFlags.HideInHierarchy };
                if (Application.isPlaying) Object.DontDestroyOnLoad(go);
                _pool = new AudioSource[6];
                for (int i = 0; i < _pool.Length; i++)
                {
                    _pool[i] = go.AddComponent<AudioSource>();
                    _pool[i].playOnAwake = false;
                }
            }
            _next = (_next + 1) % _pool.Length;
            return _pool[_next];
        }
    }
}

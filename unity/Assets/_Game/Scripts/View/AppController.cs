using System.Collections;
using Game.Core;
using Game.Platform;
using UnityEngine;

namespace Game.View
{
    /// <summary>Entry point of the game scene: loads the save and switches between the build screen and the mine.</summary>
    public sealed class AppController : MonoBehaviour
    {
        /// <summary>Tests set this before creating the app so nothing is read from or written to the real save.</summary>
        public static bool NoSaves;

        public GameSession Session { get; private set; }
        public MineController Mine { get; private set; }
        public BuildScreen Build { get; private set; }
        public bool InMine => Mine != null && Mine.gameObject.activeSelf;

        /// <summary>How long to wait for the store's SDK (cloud save, server time) before starting without it.</summary>
        private const float SdkWait = 6f;

        /// <summary>
        /// In the editor and in tests the platform is ready at once and the game starts right here.
        /// In the browser it first waits for the Yandex SDK, so the cloud save is known before anything is loaded.
        /// </summary>
        private void Awake()
        {
            Sdk.Init();
            if (NoSaves || Sdk.Ready) Boot();
            else StartCoroutine(WaitForSdk());
        }

        private IEnumerator WaitForSdk()
        {
            float until = Time.realtimeSinceStartup + SdkWait;
            while (!Sdk.Ready && Time.realtimeSinceStartup < until) yield return null;
            Boot();
        }

        private void Boot()
        {
            Sdk.ReadStart();
            SetLanguage();
            Session = GameSession.Create(!NoSaves);
            Mine = new GameObject("Mine").AddComponent<MineController>();
            Mine.transform.SetParent(transform, false);
            Mine.Init(Session, ShowBuild);
            Build = gameObject.AddComponent<BuildScreen>();
            Build.Init(Session, ShowMine);
            if (Session.State.Run != null) ShowMine(); else ShowBuild();
            StartCoroutine(ReadyAfterFirstFrame());
        }

        /// <summary>
        /// Russian or English: in the browser the store's language (Yandex knows the player's), on a phone the
        /// phone's language, in the editor and tests Russian. «?lang=en» in the page address forces one (for checks).
        /// </summary>
        private static void SetLanguage()
        {
            string lang = Application.platform switch
            {
                RuntimePlatform.WebGLPlayer => Sdk.Lang,
                RuntimePlatform.Android => Application.systemLanguage switch
                {
                    SystemLanguage.Russian or SystemLanguage.Belarusian or SystemLanguage.Ukrainian => "ru",
                    _ => "en",
                },
                _ => "ru",
            };
            var url = Application.absoluteURL ?? "";
            int at = url.IndexOf("lang=", System.StringComparison.Ordinal);
            if (at >= 0 && url.Length >= at + 7) lang = url.Substring(at + 5, 2);
            Loc.SetLanguage(lang);
            Loc.Load(Resources.Load<TextAsset>("Lang/en")?.text);
        }

        // The first frame is drawn: the store can hide its loading screen and count gameplay from here.
        private IEnumerator ReadyAfterFirstFrame()
        {
            yield return null;
            Sdk.GameReady();
        }

        public void ShowMine()
        {
            Build.enabled = false;
            Mine.gameObject.SetActive(true);
            Mine.Enter();
            Music.Play("music_mine");
        }

        public void ShowBuild()
        {
            Mine.gameObject.SetActive(false);
            Build.enabled = true;
            Session.Save();
            Music.Play("music_build");
        }

        // Android's «back» (Escape in the browser): close what is open, leave the mine, and on the build screen
        // with nothing open, leave the game.
        private void Update()
        {
            if (Session == null || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (InMine) { if (Mine.Back()) return; ShowBuild(); return; }
            if (Build.Back()) return;
            Session.Save();
            if (Application.platform == RuntimePlatform.Android) Application.Quit();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Session?.Save();
        }

        private void OnApplicationQuit() => Session?.Save();
    }
}

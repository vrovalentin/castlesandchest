using Game.Core;
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

        private void Awake()
        {
            Session = GameSession.Create(!NoSaves);
            Mine = new GameObject("Mine").AddComponent<MineController>();
            Mine.transform.SetParent(transform, false);
            Mine.Init(Session, ShowBuild);
            Build = gameObject.AddComponent<BuildScreen>();
            Build.Init(Session, ShowMine);
            if (Session.State.Run != null) ShowMine(); else ShowBuild();
        }

        public void ShowMine()
        {
            Build.enabled = false;
            Mine.gameObject.SetActive(true);
            Mine.Enter();
        }

        public void ShowBuild()
        {
            Mine.gameObject.SetActive(false);
            Build.enabled = true;
            Session.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Session?.Save();
        }

        private void OnApplicationQuit() => Session?.Save();
    }
}

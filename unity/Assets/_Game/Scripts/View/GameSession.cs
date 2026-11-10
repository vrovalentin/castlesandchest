using System;
using Game.Core;
using Game.Platform;
using UnityEngine;

namespace Game.View
{
    /// <summary>The running game: content, state, randomness and saving. Shared by all screens.</summary>
    public sealed class GameSession
    {
        public readonly GameLogic Game;
        public GameState State;
        public readonly IRandom Rng = new SystemRandom();
        public readonly bool Persistent;

        private GameSession(GameLogic game, bool persistent)
        {
            Game = game;
            Persistent = persistent;
        }

        /// <summary>Today by the store's server clock when there is one, so moving the phone's clock gives no extra gifts.</summary>
        public static string Today => Sdk.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Today's gift when it was just given (the build screen shows the login calendar), else null.</summary>
        public DailyGift? NewGift;

        /// <summary>Loads the saved game (when persistent) or starts a new one, then gives the daily gift.</summary>
        public static GameSession Create(bool persistent)
        {
            var s = new GameSession(new GameLogic(DemoContent.Create()), persistent);
            if (persistent) s.State = SaveStore.Load(s.Game.Catalog, Sdk.LoadCloud());
            if (s.State == null) s.State = s.Game.NewGame();
            s.NewGift = s.Game.DailyGrant(s.State, Today);
            if (persistent) s.TestStart(Application.absoluteURL);
            s.Save();
            return s;
        }

        /// <summary>
        /// For trying castles out in the browser: «?castle=2» opens a new game on that castle,
        /// and «&amp;parts» also puts every part it needs in storage. Without them nothing changes.
        /// </summary>
        public void TestStart(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var m = System.Text.RegularExpressions.Regex.Match(url, @"[?&]castle=(\d+)");
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int n)) return;
            State = Game.NewGame();
            NewGift = Game.DailyGrant(State, Today);
            for (int i = 1; i < n && State.CastleIndex < Game.Catalog.Castles.Count - 1; i++) Game.NextCastle(State);
            if (System.Text.RegularExpressions.Regex.IsMatch(url, @"[?&]parts\b"))
                foreach (var id in Game.CastleOf(State).Slots) State.Add(id, 1);
        }

        public void Save()
        {
            if (!Persistent) return;
            string json = JsonUtility.ToJson(SaveMapper.ToSave(State, Game.Catalog));
            SaveStore.Save(json);
            Sdk.SaveCloud(json);
        }

        /// <summary>
        /// Wipes the save and starts from the first castle, with the usual daily tickets and tools.
        /// The daily gift only tops up to its fixed amounts, so this can't be used to farm them.
        /// </summary>
        public void StartOver()
        {
            if (Persistent) SaveStore.Clear();
            State = Game.NewGame();
            Game.DailyGrant(State, Today);
            Save();
        }

        /// <summary>Start the demo over. Tickets, tools, the daily gift day and the points stay, so it can't be used to farm them.</summary>
        public void RestartDemo()
        {
            var old = State;
            State = Game.NewGame();
            State.Chests.Clear();
            State.Tickets = old.Tickets;
            State.Brooms = old.Brooms;
            State.Hammers = old.Hammers;
            State.DailyDay = old.DailyDay;
            State.Opts = old.Opts;
            State.Score = old.Score;
            State.BestRun = old.BestRun;
            State.Built = old.Built;
            Save();
        }
    }

    /// <summary>
    /// Save slot in PlayerPrefs (works on Android and WebGL alike), plus the time it was written,
    /// so that on start the newer of the local and the cloud save wins.
    /// </summary>
    public static class SaveStore
    {
        private const string Key = "castles-and-chest-save";
        private const string TimeKey = "castles-and-chest-save-t";

        /// <summary>The newer of the local save and <paramref name="cloud"/>; null when neither can be read.</summary>
        public static GameState Load(Catalog cat, CloudSave cloud = null)
        {
            string local = PlayerPrefs.GetString(Key, "");
            long.TryParse(PlayerPrefs.GetString(TimeKey, "0"), out long localT);
            bool useCloud = cloud != null && (string.IsNullOrEmpty(local) || cloud.t > localT);
            var state = Read(useCloud ? cloud.save : local, cat);
            // The chosen one is broken: fall back to the other.
            if (state == null && cloud != null) state = Read(useCloud ? local : cloud.save, cat);
            return state;
        }

        private static GameState Read(string json, Catalog cat)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return SaveMapper.FromSave(JsonUtility.FromJson<SaveData>(json), cat);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveStore] save could not be read: " + e.Message);
                return null;
            }
        }

        public static void Save(string json)
        {
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.SetString(TimeKey, Sdk.NowMs.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(TimeKey);
            PlayerPrefs.Save();
        }
    }
}

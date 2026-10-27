using System;
using Game.Core;
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

        public static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        /// <summary>Loads the saved game (when persistent) or starts a new one, then gives the daily gift.</summary>
        public static GameSession Create(bool persistent)
        {
            var s = new GameSession(new GameLogic(DemoContent.Create()), persistent);
            if (persistent) s.State = SaveStore.Load(s.Game.Catalog);
            if (s.State == null) s.State = s.Game.NewGame();
            s.Game.DailyGrant(s.State, Today);
            s.Save();
            return s;
        }

        public void Save()
        {
            if (Persistent) SaveStore.Save(SaveMapper.ToSave(State, Game.Catalog));
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

        /// <summary>Start the demo over. Tickets, tools and the daily gift day stay, so it can't be used to farm them.</summary>
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
            Save();
        }
    }

    /// <summary>Save slot in PlayerPrefs (works on Android and WebGL alike).</summary>
    public static class SaveStore
    {
        private const string Key = "castles-and-chest-save";

        public static GameState Load(Catalog cat)
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                if (string.IsNullOrEmpty(json)) return null;
                return SaveMapper.FromSave(JsonUtility.FromJson<SaveData>(json), cat);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveStore] save could not be read, starting a new game: " + e.Message);
                return null;
            }
        }

        public static void Save(SaveData data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// The build screen: castle slots, chests, storage by tier, merging 2 → 1, moving to the next castle.
    /// Drawn with IMGUI in BuildScreen.Gui.cs, laid out like the wireframe.
    /// </summary>
    public sealed partial class BuildScreen : MonoBehaviour
    {
        private GameSession _s;
        private Action _toMine;
        private GameLogic Game => _s.Game;
        private GameState State => _s.State;

        private readonly List<string> _pick = new List<string>();
        private string _message;
        private float _messageUntil;
        private bool _donePanel;
        private bool _askStartOver;
        // IMGUI must not change what it draws in the middle of an event, so clicks run on the next Update.
        private Action _pending;

        private void Later(Action a, bool click = true)
        {
            if (click) Sfx.Play("click");
            _pending += a;
        }

        private void Update()
        {
            var p = _pending;
            _pending = null;
            p?.Invoke();
        }

        public void Init(GameSession session, Action toMine)
        {
            _s = session;
            _toMine = toMine;
        }

        // ---------- actions (public for tests) ----------

        /// <summary>Opens chests (all when index is null). Returns the part ids they gave.</summary>
        public List<string> OpenChests(int? index = null)
        {
            var got = new List<string>();
            var list = index.HasValue ? new List<int> { index.Value } : Enumerable.Range(0, State.Chests.Count).Reverse().ToList();
            int opened = 0, firstColor = 0, bestStars = 0;
            foreach (int i in list)
            {
                if (i < 0 || i >= State.Chests.Count) continue;
                var ch = State.Chests[i];
                State.Chests.RemoveAt(i);
                if (opened == 0 || ch.Stars > bestStars) { firstColor = ch.Color; bestStars = ch.Stars; }
                opened++;
                got.AddRange(Game.OpenChest(State, ch, _s.Rng));
            }
            _s.Save();
            if (got.Count > 0)
            {
                _reward = new Reward { Color = firstColor, Stars = bestStars, Chests = opened, Parts = got, Since = Time.time };
                Sfx.Play("rattle");
            }
            return got;
        }

        public bool PlaceSlot(int slot)
        {
            var slots = Game.CastleOf(State).Slots;
            var id = slots[slot];
            if (State.Filled[slot]) return false;
            int next = Game.NextSlot(State);
            if (slot != next)
            {
                Say($"Замок строится снизу вверх. Сначала: {Game.Catalog.NameOf(slots[next]).ToLowerInvariant()}.");
                return false;
            }
            if (!Game.Place(State, slot))
            {
                Say($"{Game.Catalog.NameOf(id)}: нет на складе. {WhereFrom(id)}");
                return false;
            }
            _placedSlot = slot;
            _placedAt = Time.time;
            _s.Save();
            Sfx.Play("place");
            if (Game.IsComplete(State)) { State.Built++; _s.Save(); _donePanel = true; Sfx.Play("fanfare"); }
            return true;
        }

        /// <summary>A tap on a slot of the castle drawing: a built part says its name, any other is placed if it can be.</summary>
        public bool TapSlot(int slot)
        {
            var slots = Game.CastleOf(State).Slots;
            if (slot < 0 || slot >= slots.Length) return false;
            if (State.Filled[slot])
            {
                Say($"{Game.Catalog.NameOf(slots[slot])}: уже на месте.");
                return false;
            }
            return PlaceSlot(slot);
        }

        /// <summary>Places parts in building order while storage has the next one. Returns how many were placed.</summary>
        public int PlaceAll()
        {
            int n = 0;
            for (int i = Game.NextSlot(State); i >= 0 && State.Count(Game.CastleOf(State).Slots[i]) > 0 && PlaceSlot(i); i = Game.NextSlot(State)) n++;
            return n;
        }

        public bool Merge(string a, string b, string target)
        {
            if (!Game.Merge(State, a, b, target)) return false;
            _pick.Clear();
            _s.Save();
            Sfx.Play("merge");
            Say($"Собрано: {Game.Catalog.NameOf(target)}");
            return true;
        }

        public void NextCastle()
        {
            if (Game.IsLastCastle(State)) _s.RestartDemo();
            else { Game.NextCastle(State); _s.Save(); }
            _donePanel = false;
            _pick.Clear();
        }

        /// <summary>Shows the «Начать сначала?» window.</summary>
        public void AskStartOver() => _askStartOver = true;

        public bool StartOverPanel => _askStartOver;

        /// <summary>Wipes all progress and goes back to the first castle.</summary>
        public void StartOver()
        {
            _s.StartOver();
            Hints.Reset();
            _askStartOver = false;
            _donePanel = false;
            _reward = null;
            _pick.Clear();
            _scroll = Vector2.zero;
            _message = null;
            _placedSlot = -1;
            _flying.Clear();
            _pulseAt.Clear();
        }

        // ---------- helpers ----------

        private string Describe(List<string> ids) =>
            string.Join(", ", ids.GroupBy(x => x).Select(g => Game.Catalog.NameOf(g.Key) + (g.Count() > 1 ? $" ×{g.Count()}" : "")));

        private string WhereFrom(string id)
        {
            int t = Game.Catalog.TierOf(id);
            var rules = Game.Rules;
            var ways = new List<string>();
            if (t > 1) ways.Add($"собрать из 2 деталей {t - 1}-го уровня");
            if (t <= rules.ColorTiers) ways.Add($"{Palette.BlockNames[t - 1]} сундук");
            if (t >= 6) ways.Add($"{Palette.BlockNames[rules.ColorTiers - 1]} сундук ★4–5");
            return "Где взять: " + string.Join(", ", ways) + ".";
        }

        private void Say(string text)
        {
            _message = text;
            _messageUntil = Time.time + 3.5f;
        }

        // ---------- windows state (drawn in BuildScreen.Gui.cs) ----------

        /// <summary>What the last opening gave; shown in a window until "Забрать".</summary>
        private sealed class Reward
        {
            public int Color, Stars, Chests;
            public List<string> Parts;
            public float Since;
            /// <summary>How far the show's sounds have got: 0 shaking, 1 opened, 1 + n parts landed.</summary>
            public int Sounded;
        }

        private Reward _reward;

        /// <summary>True while the window with what the chests gave is shown.</summary>
        public bool RewardPanel => _reward != null;

        /// <summary>Closes the window; what it showed flies down to the storage card.</summary>
        public void CloseReward()
        {
            if (_reward == null) return;
            float now = Time.time;
            for (int i = 0; i < _rewardCells.Count; i++)
                _flying.Add(new Flight { Id = _rewardCells[i].id, From = _rewardCells[i].at, Start = now + 0.06f * i });
            foreach (var id in _reward.Parts) _pulseAt[id] = now + 0.45f;
            if (_rewardCells.Count > 0) Sfx.Play("collect", 0.55f);
            _rewardCells.Clear();
            _reward = null;
        }

        /// <summary>A part icon flying from the reward window to the storage card.</summary>
        private struct Flight
        {
            public string Id;
            public Rect From;
            public float Start;
        }

        private readonly List<Flight> _flying = new List<Flight>();
        private readonly List<(string id, Rect at)> _rewardCells = new List<(string, Rect)>();
        /// <summary>When a storage tile last got parts, for a little bounce.</summary>
        private readonly Dictionary<string, float> _pulseAt = new Dictionary<string, float>();

        /// <summary>True while the "castle built" panel is shown.</summary>
        public bool CastleDonePanel => _donePanel;

        private CastleArt _art;
        private string _artId;

        /// <summary>Layered art of the current castle, or null when it has none yet.</summary>
        public CastleArt Art
        {
            get
            {
                var castle = Game.CastleOf(State);
                if (_artId != castle.Id) { _artId = castle.Id; _art = CastleArt.Load(castle); }
                return _art;
            }
        }
    }
}

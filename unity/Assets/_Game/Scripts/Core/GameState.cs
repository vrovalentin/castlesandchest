using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Everything a save holds. Parts are stored by id, so a castle can change its part set freely.</summary>
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public int CastleIndex;
        /// <summary>While a built castle is being built again: the castle the player was building (-1 otherwise), with its slots and runs.</summary>
        public int HomeIndex = -1;
        public bool[] HomeFilled = Array.Empty<bool>();
        public int HomeRuns;
        public bool Replaying => HomeIndex >= 0;
        /// <summary>The furthest castle reached: the one being built, not a built one being built again.</summary>
        public int Frontier => Replaying ? HomeIndex : CastleIndex;
        /// <summary>Part id → count in storage. Missing id means zero.</summary>
        public Dictionary<string, int> Inventory = new Dictionary<string, int>();
        /// <summary>Collected chests waiting to be opened.</summary>
        public List<Chest> Chests = new List<Chest>();
        /// <summary>One flag per castle slot.</summary>
        public bool[] Filled = Array.Empty<bool>();
        public RunState Run;
        /// <summary>Runs played on the current castle.</summary>
        public int Runs;
        public int Brooms;
        public int Hammers;
        public int Tickets;
        /// <summary>Day of the last daily gift, like "2026-10-20". Null if never.</summary>
        public string DailyDay;
        /// <summary>Ads watched for tickets today (reset with the daily gift).</summary>
        public int AdsToday;
        /// <summary>Day of the login streak, 1..7 (0 before the first gift).</summary>
        public int Streak;
        /// <summary>Ads watched for brooms and hammers today (reset with the daily gift).</summary>
        public int ToolAdsToday;
        public int Built;
        /// <summary>All points ever earned: in the mine and on the building site.</summary>
        public int Score;
        /// <summary>Most points earned in one run.</summary>
        public int BestRun;
        public Options Opts = new Options();

        public int Count(string partId) => Inventory.TryGetValue(partId, out var n) ? n : 0;

        public void Add(string partId, int delta) => Inventory[partId] = Count(partId) + delta;
    }

    [Serializable]
    public sealed class RunState
    {
        public Board Board;
        public int Moves;
        public List<Chest> Got = new List<Chest>();
        /// <summary>The "+5 moves" offer was used this run.</summary>
        public bool ExtraUsed;
        /// <summary>Points earned in this run so far.</summary>
        public int Score;
    }

    /// <summary>Balance switches for playtests (game-design.md 3.1).</summary>
    [Serializable]
    public sealed class Options
    {
        public int Moves = 30;
        /// <summary>True: chests left on the board at the end of a run go to the player instead of burning.</summary>
        public bool KeepOnEnd;
    }

    public struct PartCount
    {
        public string Id;
        public int N;
        public PartCount(string id, int n) { Id = id; N = n; }
    }

    public sealed class BurnPreview
    {
        public int Parts;
        public int Chests;
        public List<PartCount> Kept = new List<PartCount>();
    }

    public sealed class RunSummary
    {
        public List<Chest> Got;
        public int Left;
        public bool Kept;
        /// <summary>Points for this run.</summary>
        public int Score;
        /// <summary>Best run before this one.</summary>
        public int PrevBest;
        public bool NewRecord => Score > 0 && Score > PrevBest;
    }

    public struct DailyGift
    {
        public int Brooms, Hammers, Tickets;
        /// <summary>Day of the login streak this gift is for, 1..7 (only on what DailyGrant returns).</summary>
        public int Day;
    }
}

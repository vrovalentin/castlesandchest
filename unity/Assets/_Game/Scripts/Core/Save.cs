using System;
using System.Collections.Generic;

namespace Game.Core
{
    // Save format: plain arrays only, so Unity's JsonUtility can write it (it cannot write dictionaries).
    // Parts are stored by id; the castle by id. Bump GameState.CurrentVersion and add a migration when the format changes.

    [Serializable]
    public sealed class SaveData
    {
        public int version;
        public string castleId;
        public int castleIndex;
        public string[] partIds = Array.Empty<string>();
        public int[] partCounts = Array.Empty<int>();
        public int[] chestColors = Array.Empty<int>();
        public int[] chestStars = Array.Empty<int>();
        public bool[] filled = Array.Empty<bool>();
        public int runs;
        public int brooms;
        public int hammers;
        public int tickets;
        public string dailyDay;
        public int built;
        public int optMoves;
        public bool keepOnEnd;
        public bool hasRun;
        public RunSave run = new RunSave();
    }

    [Serializable]
    public sealed class RunSave
    {
        public int moves;
        public bool extraUsed;
        public int[] gotColors = Array.Empty<int>();
        public int[] gotStars = Array.Empty<int>();
        public int rows;
        public int cols;
        public int nextId;
        /// <summary>Row by row. Kind: -1 empty, 0 block, 1 chest.</summary>
        public int[] cellIds = Array.Empty<int>();
        public int[] cellKinds = Array.Empty<int>();
        public int[] cellColors = Array.Empty<int>();
        public int[] cellStars = Array.Empty<int>();
    }

    public static class SaveMapper
    {
        public static SaveData ToSave(GameState s, Catalog cat)
        {
            var d = new SaveData
            {
                version = GameState.CurrentVersion,
                castleIndex = s.CastleIndex,
                castleId = cat.Castles[Math.Min(s.CastleIndex, cat.Castles.Count - 1)].Id,
                filled = (bool[])s.Filled.Clone(),
                runs = s.Runs,
                brooms = s.Brooms,
                hammers = s.Hammers,
                tickets = s.Tickets,
                dailyDay = s.DailyDay ?? "",
                built = s.Built,
                optMoves = s.Opts.Moves,
                keepOnEnd = s.Opts.KeepOnEnd,
                hasRun = s.Run != null,
            };
            var ids = new List<string>();
            var counts = new List<int>();
            foreach (var kv in s.Inventory)
                if (kv.Value != 0) { ids.Add(kv.Key); counts.Add(kv.Value); }
            d.partIds = ids.ToArray();
            d.partCounts = counts.ToArray();
            SplitChests(s.Chests, out d.chestColors, out d.chestStars);
            if (s.Run != null) d.run = ToSave(s.Run);
            return d;
        }

        private static RunSave ToSave(RunState run)
        {
            var b = run.Board;
            var r = new RunSave
            {
                moves = run.Moves,
                extraUsed = run.ExtraUsed,
                rows = b.Rows,
                cols = b.Cols,
                nextId = b.NextId,
            };
            SplitChests(run.Got, out r.gotColors, out r.gotStars);
            int n = b.Rows * b.Cols;
            r.cellIds = new int[n]; r.cellKinds = new int[n]; r.cellColors = new int[n]; r.cellStars = new int[n];
            for (int y = 0; y < b.Rows; y++)
                for (int x = 0; x < b.Cols; x++)
                {
                    int i = y * b.Cols + x;
                    var c = b[y, x];
                    if (c == null) { r.cellKinds[i] = -1; continue; }
                    r.cellIds[i] = c.Id;
                    r.cellKinds[i] = c.IsChest ? 1 : 0;
                    r.cellColors[i] = c.Color;
                    r.cellStars[i] = c.Stars;
                }
            return r;
        }

        private static void SplitChests(List<Chest> list, out int[] colors, out int[] stars)
        {
            colors = new int[list.Count];
            stars = new int[list.Count];
            for (int i = 0; i < list.Count; i++) { colors[i] = list[i].Color; stars[i] = list[i].Stars; }
        }

        private static List<Chest> JoinChests(int[] colors, int[] stars, int colorCount)
        {
            var list = new List<Chest>();
            if (colors == null || stars == null) return list;
            for (int i = 0; i < Math.Min(colors.Length, stars.Length); i++)
                if (colors[i] >= 0 && colors[i] < colorCount) list.Add(new Chest(colors[i], stars[i]));
            return list;
        }

        /// <summary>
        /// Restores a game. Unknown part ids and broken chests are skipped. Returns null when the save
        /// is from a newer version of the game or has no castle that still exists.
        /// </summary>
        public static GameState FromSave(SaveData d, Catalog cat)
        {
            if (d == null || d.version > GameState.CurrentVersion || cat.Castles.Count == 0) return null;
            var rules = cat.Rules;
            int castle = string.IsNullOrEmpty(d.castleId) ? -1 : cat.CastleIndexOf(d.castleId);
            if (castle < 0) castle = Math.Max(0, Math.Min(d.castleIndex, cat.Castles.Count - 1));

            var s = new GameState
            {
                CastleIndex = castle,
                Runs = d.runs,
                Brooms = d.brooms,
                Hammers = d.hammers,
                Tickets = d.tickets,
                DailyDay = string.IsNullOrEmpty(d.dailyDay) ? null : d.dailyDay,
                Built = d.built,
            };
            s.Opts.Moves = d.optMoves > 0 ? d.optMoves : cat.Castles[castle].Moves;
            s.Opts.KeepOnEnd = d.keepOnEnd;
            foreach (var p in cat.Parts) s.Inventory[p.Id] = 0;
            if (d.partIds != null && d.partCounts != null)
                for (int i = 0; i < Math.Min(d.partIds.Length, d.partCounts.Length); i++)
                    if (cat.TryGetPart(d.partIds[i], out _)) s.Inventory[d.partIds[i]] = Math.Max(0, d.partCounts[i]);
            s.Chests = JoinChests(d.chestColors, d.chestStars, rules.ColorCount);

            // If the castle got more or fewer slots since the save, keep what fits.
            int slots = cat.Castles[castle].Slots.Length;
            s.Filled = new bool[slots];
            if (d.filled != null) Array.Copy(d.filled, s.Filled, Math.Min(slots, d.filled.Length));

            if (d.hasRun && d.run != null) s.Run = FromSave(d.run, rules);
            return s;
        }

        private static RunState FromSave(RunSave r, Rules rules)
        {
            // A board of another size (rules changed) cannot be restored; the run is dropped.
            int n = rules.Rows * rules.Cols;
            if (r.rows != rules.Rows || r.cols != rules.Cols || r.cellKinds == null || r.cellKinds.Length != n
                || r.cellIds.Length != n || r.cellColors.Length != n || r.cellStars.Length != n) return null;
            var b = new Board(rules);
            int maxId = 0;
            for (int y = 0; y < b.Rows; y++)
                for (int x = 0; x < b.Cols; x++)
                {
                    int i = y * b.Cols + x;
                    if (r.cellKinds[i] < 0) continue;
                    b[y, x] = new Cell
                    {
                        Id = r.cellIds[i],
                        Kind = r.cellKinds[i] == 1 ? CellKind.Chest : CellKind.Block,
                        Color = Math.Max(0, Math.Min(r.cellColors[i], rules.ColorCount - 1)),
                        Stars = r.cellStars[i],
                    };
                    maxId = Math.Max(maxId, r.cellIds[i]);
                }
            b.NextId = Math.Max(r.nextId, maxId + 1);
            return new RunState
            {
                Board = b,
                Moves = Math.Max(0, r.moves),
                ExtraUsed = r.extraUsed,
                Got = JoinChests(r.gotColors, r.gotStars, rules.ColorCount),
            };
        }
    }
}

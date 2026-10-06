using System.Collections.Generic;
using UnityEngine;

namespace AirXonix
{
    /// <summary>
    /// Xonix capture rule: when the player closes a trail, every open region that
    /// does NOT contain an enemy ball gets claimed (plus the trail itself).
    /// </summary>
    public static class CaptureSolver
    {
        static readonly Queue<int> Frontier = new Queue<int>();

        /// <returns>number of cells newly turned to Filled</returns>
        public static int Resolve(GridModel g, IEnumerable<Vector2Int> enemyCells)
        {
            int cols = g.Cols;
            var raw = g.Raw;

            // 1. the trail becomes solid land
            for (int i = 0; i < raw.Length; i++)
                if (raw[i] == Cell.Trail) raw[i] = Cell.Filled;

            // 2. flood every open cell reachable from an enemy
            var reachable = new bool[raw.Length];
            Frontier.Clear();

            foreach (var e in enemyCells)
            {
                if (!g.InBounds(e.x, e.y)) continue;
                int idx = g.Idx(e.x, e.y);
                if (raw[idx] == Cell.Empty && !reachable[idx])
                {
                    reachable[idx] = true;
                    Frontier.Enqueue(idx);
                }
            }

            while (Frontier.Count > 0)
            {
                int idx = Frontier.Dequeue();
                int x = idx % cols;
                int y = idx / cols;
                Spread(g, raw, reachable, x + 1, y);
                Spread(g, raw, reachable, x - 1, y);
                Spread(g, raw, reachable, x, y + 1);
                Spread(g, raw, reachable, x, y - 1);
            }

            // 3. anything open that no enemy could reach is claimed
            int filled = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == Cell.Empty && !reachable[i])
                {
                    raw[i] = Cell.Filled;
                    filled++;
                }
            }
            return filled;
        }

        static void Spread(GridModel g, Cell[] raw, bool[] reachable, int x, int y)
        {
            if (!g.InBounds(x, y)) return;
            int idx = g.Idx(x, y);
            if (reachable[idx] || raw[idx] != Cell.Empty) return;
            reachable[idx] = true;
            Frontier.Enqueue(idx);
        }
    }
}

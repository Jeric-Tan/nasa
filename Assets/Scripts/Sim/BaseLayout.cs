using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // The base grid: rocks, placed modules, corridors. Built from MissionState on demand.
    public class BaseLayout
    {
        public readonly int Width, Height;
        readonly Balance b;
        readonly MissionState s;
        readonly bool[,] rock;
        readonly int[,] owner;     // module uid, 0 = none, -1 = corridor

        public BaseLayout(Balance balance, MissionState state)
        {
            b = balance;
            s = state;
            Width = b.grid.width;
            Height = b.grid.height;
            rock = new bool[Width, Height];
            owner = new int[Width, Height];
            var site = b.Site(s.site);
            if (site.rocks != null)
                for (int i = 0; i + 1 < site.rocks.Length; i += 2)
                    if (In(site.rocks[i], site.rocks[i + 1])) rock[site.rocks[i], site.rocks[i + 1]] = true;
            foreach (var m in s.modules)
                for (int x = m.x; x < m.x + m.w; x++)
                    for (int y = m.y; y < m.y + m.h; y++)
                        if (In(x, y)) owner[x, y] = m.uid;
            foreach (var c in s.corridors)
                if (In(c.x, c.y) && owner[c.x, c.y] == 0) owner[c.x, c.y] = -1;
        }

        public bool In(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsRock(int x, int y) => In(x, y) && rock[x, y];
        public bool IsCorridor(int x, int y) => In(x, y) && owner[x, y] == -1;
        public bool IsFree(int x, int y) => In(x, y) && !rock[x, y] && owner[x, y] == 0;
        public int OwnerAt(int x, int y) => In(x, y) ? owner[x, y] : 0;

        public PlacedModule ModuleAt(int x, int y)
        {
            int u = OwnerAt(x, y);
            return u > 0 ? s.modules.Find(m => m.uid == u) : null;
        }

        public PlacedModule ByUid(int uid) => s.modules.Find(m => m.uid == uid);

        public IEnumerable<PlacedModule> Modules(string id)
        {
            foreach (var m in s.modules) if (m.id == id) yield return m;
        }

        public PlacedModule First(string id) => s.modules.Find(m => m.id == id);

        // "Base" modules: everything inside the pressurised base (not panels, walls, or the pad).
        public bool IsBaseModule(PlacedModule m)
        {
            var def = b.Module(m.id);
            if (def == null || m.id == "solar") return false; // the starting solar array sits out on open ground
            return def.placement == "fixed" || def.placement == "inside" || def.placement == "connected";
        }

        // Chebyshev distance between two rectangles, in tiles (0 = overlapping or touching corners count as 1).
        public static int RectDistance(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
        {
            int dx = Math.Max(0, Math.Max(bx - (ax + aw - 1), ax - (bx + bw - 1)));
            int dy = Math.Max(0, Math.Max(by - (ay + ah - 1), ay - (by + bh - 1)));
            return Math.Max(dx, dy);
        }

        public int DistanceToBase(int x, int y, int w, int h, bool anyModule)
        {
            int best = int.MaxValue;
            foreach (var m in s.modules)
            {
                if (!anyModule && !IsBaseModule(m)) continue;
                best = Math.Min(best, RectDistance(x, y, w, h, m.x, m.y, m.w, m.h));
            }
            foreach (var c in s.corridors)
                best = Math.Min(best, RectDistance(x, y, w, h, c.x, c.y, 1, 1));
            return best;
        }

        // 4-neighbour contact between two rectangles (sharing an edge).
        public static bool Touching(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
        {
            bool xOverlap = ax < bx + bw && bx < ax + aw;
            bool yOverlap = ay < by + bh && by < ay + ah;
            bool xAdjacent = ax + aw == bx || bx + bw == ax;
            bool yAdjacent = ay + ah == by || by + bh == ay;
            return (xOverlap && yAdjacent) || (yOverlap && xAdjacent);
        }

        public static bool Touching(PlacedModule a, PlacedModule c) => Touching(a.x, a.y, a.w, a.h, c.x, c.y, c.w, c.h);

        public List<PlacedModule> Neighbours(PlacedModule m, bool builtOnly)
        {
            var list = new List<PlacedModule>();
            foreach (var o in s.modules)
            {
                if (o == m || (builtOnly && !o.built)) continue;
                if (Touching(m, o)) list.Add(o);
            }
            return list;
        }

        // A module is shielded when at least one finished wall segment touches it.
        public bool IsShielded(PlacedModule m)
        {
            if (m == null) return false;
            foreach (var o in s.modules)
                if (o.id == "wall" && o.built && Touching(m, o)) return true;
            return false;
        }

        // Walkable for the player: everything except rocks. Rooms are a walk-in cutaway.
        public bool Walkable(int x, int y) => In(x, y) && !rock[x, y];

        // Shortest walking path length (tiles) from a tile to the nearest tile of a module.
        public int PathLength(Cell from, PlacedModule to)
        {
            var path = GridPath.Find(this, from, c => to.Contains(c.x, c.y));
            return path == null ? 99 : path.Count - 1;
        }

        public Cell HabCentre()
        {
            var hab = First("hab");
            return hab == null ? new Cell(Width / 2, Height / 2) : new Cell(hab.x + hab.w / 2, hab.y + hab.h / 2);
        }

        // Walking time from the hab to a station, in quarter hours (0.25 h per N tiles, rounded).
        public int WalkQuarters(PlacedModule station, bool fastWalker)
        {
            if (station == null || station.id == "hab") return 0;
            int len = PathLength(HabCentre(), station);
            int per = Math.Max(1, b.actions.walkTilesPerQuarter * (fastWalker ? 2 : 1));
            return (len + per / 2) / per;
        }
    }

    public static class GridPath
    {
        static readonly int[] DX = { 1, -1, 0, 0 };
        static readonly int[] DY = { 0, 0, 1, -1 };

        // A* on the 4-neighbour grid. Returns the path including start and goal, or null.
        public static List<Cell> Find(BaseLayout grid, Cell start, Func<Cell, bool> isGoal, Cell? goalHint = null)
        {
            if (!grid.In(start.x, start.y)) return null;
            int w = grid.Width, h = grid.Height;
            var g = new int[w, h];
            var came = new Cell[w, h];
            var seen = new bool[w, h];
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) g[x, y] = int.MaxValue;
            var open = new List<Cell> { start };
            g[start.x, start.y] = 0;
            while (open.Count > 0)
            {
                int bi = 0, bf = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    var c = open[i];
                    int hcost = goalHint.HasValue ? Math.Abs(c.x - goalHint.Value.x) + Math.Abs(c.y - goalHint.Value.y) : 0;
                    int f = g[c.x, c.y] + hcost;
                    if (f < bf) { bf = f; bi = i; }
                }
                var cur = open[bi];
                open.RemoveAt(bi);
                if (seen[cur.x, cur.y]) continue;
                seen[cur.x, cur.y] = true;
                if (isGoal(cur))
                {
                    var path = new List<Cell> { cur };
                    while (cur.x != start.x || cur.y != start.y)
                    {
                        cur = came[cur.x, cur.y];
                        path.Add(cur);
                    }
                    path.Reverse();
                    return path;
                }
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + DX[d], ny = cur.y + DY[d];
                    if (!grid.Walkable(nx, ny) || seen[nx, ny]) continue;
                    int ng = g[cur.x, cur.y] + 1;
                    if (ng < g[nx, ny])
                    {
                        g[nx, ny] = ng;
                        came[nx, ny] = cur;
                        open.Add(new Cell(nx, ny));
                    }
                }
            }
            return null;
        }

        public static List<Cell> Find(BaseLayout grid, Cell start, Cell goal) =>
            Find(grid, start, c => c.x == goal.x && c.y == goal.y, goal);
    }
}

using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    public class PlaceCheck
    {
        public bool ok;
        public string reason;            // one plain line for the ghost preview
        public List<Cell> corridor = new List<Cell>();
        public bool far;
        public int w, h;

        public static PlaceCheck No(string why) => new PlaceCheck { ok = false, reason = why };
    }

    public static class PlacementRules
    {
        public static PlaceCheck CanPlace(Balance b, MissionState s, BaseLayout grid, string moduleId, int x, int y, bool rotated)
        {
            var def = b.Module(moduleId);
            if (def == null) return PlaceCheck.No("Unknown module");
            if (def.placement == "fixed") return PlaceCheck.No("This part of the base is already here");
            int w = rotated ? def.h : def.w, h = rotated ? def.w : def.h;

            int count = 0;
            foreach (var m in s.modules) if (m.id == moduleId) count++;
            if (count >= def.maxCount) return PlaceCheck.No("You already have the most of these");

            for (int cx = x; cx < x + w; cx++)
                for (int cy = y; cy < y + h; cy++)
                {
                    if (!grid.In(cx, cy)) return PlaceCheck.No("Off the edge of the map");
                    if (grid.IsRock(cx, cy)) return PlaceCheck.No("Rocks or a slope are in the way");
                    if (!grid.IsFree(cx, cy)) return PlaceCheck.No("Something is already here");
                }

            var result = new PlaceCheck { ok = true, w = w, h = h };
            switch (def.placement)
            {
                case "inside":
                case "connected":
                    {
                        if (TouchesBase(s, grid, x, y, w, h)) break;
                        var corridor = FindCorridor(b, s, grid, x, y, w, h);
                        if (corridor == null)
                            return PlaceCheck.No("Must connect to the base (" + b.grid.maxCorridor + " corridor tiles at most)");
                        result.corridor = corridor;
                        break;
                    }
                case "open":
                    {
                        bool fromAny = moduleId == "pad";
                        int d = grid.DistanceToBase(x, y, w, h, fromAny);
                        if (d < def.minDistance)
                            return PlaceCheck.No(fromAny
                                ? "Needs open ground " + def.minDistance + " tiles from everything (rocket blast)"
                                : "Too close: panels need " + def.minDistance + " tiles of open ground from the base");
                        result.far = def.farDistance > 0 && d >= def.farDistance;
                        break;
                    }
                case "touching":
                    {
                        bool touches = false;
                        foreach (var m in s.modules)
                            if (m.id != "wall" && BaseLayout.Touching(x, y, w, h, m.x, m.y, m.w, m.h)) { touches = true; break; }
                        if (!touches) return PlaceCheck.No("A wall segment must touch a module to shield it");
                        break;
                    }
            }
            result.reason = ReasonOk(def, result);
            return result;
        }

        static string ReasonOk(ModuleDef def, PlaceCheck r)
        {
            if (r.far) return "Far from the base: less dust, but cleaning takes longer";
            if (r.corridor.Count > 0) return "OK: " + r.corridor.Count + " corridor tile" + (r.corridor.Count > 1 ? "s" : "") + " added";
            return "OK to build here";
        }

        static bool TouchesBase(MissionState s, BaseLayout grid, int x, int y, int w, int h)
        {
            foreach (var m in s.modules)
                if (grid.IsBaseModule(m) && BaseLayout.Touching(x, y, w, h, m.x, m.y, m.w, m.h)) return true;
            foreach (var c in s.corridors)
                if (BaseLayout.Touching(x, y, w, h, c.x, c.y, 1, 1)) return true;
            return false;
        }

        // Breadth-first search over free tiles from the footprint's edge to the base.
        static List<Cell> FindCorridor(Balance b, MissionState s, BaseLayout grid, int x, int y, int w, int h)
        {
            var start = new List<Cell>();
            for (int cx = x - 1; cx <= x + w; cx++)
                for (int cy = y - 1; cy <= y + h; cy++)
                {
                    bool edge = (cx == x - 1 || cx == x + w) ^ (cy == y - 1 || cy == y + h);
                    if (!edge || !grid.IsFree(cx, cy)) continue;
                    if (cx >= x && cx < x + w && cy >= y && cy < y + h) continue;
                    start.Add(new Cell(cx, cy));
                }
            var prev = new Dictionary<long, Cell>();
            var depth = new Dictionary<long, int>();
            var queue = new Queue<Cell>();
            foreach (var c in start)
            {
                long k = Key(c);
                if (depth.ContainsKey(k)) continue;
                depth[k] = 1;
                queue.Enqueue(c);
            }
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                int d = depth[Key(c)];
                if (TouchesBase(s, grid, c.x, c.y, 1, 1))
                {
                    var path = new List<Cell> { c };
                    while (prev.TryGetValue(Key(c), out var p)) { path.Add(p); c = p; }
                    return path;
                }
                if (d >= b.grid.maxCorridor) continue;
                for (int i = 0; i < 4; i++)
                {
                    var n = new Cell(c.x + dx[i], c.y + dy[i]);
                    if (!grid.IsFree(n.x, n.y)) continue;
                    if (n.x >= x && n.x < x + w && n.y >= y && n.y < y + h) continue;
                    long k = Key(n);
                    if (depth.ContainsKey(k)) continue;
                    depth[k] = d + 1;
                    prev[k] = c;
                    queue.Enqueue(n);
                }
            }
            return null;
        }

        static long Key(Cell c) => ((long)c.x << 32) | (uint)c.y;
    }
}

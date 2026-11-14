using System.Collections.Generic;
using OutpostGale.Sim;

namespace OutpostGale.Tests
{
    // A sensible scripted player for balance runs. Uses only the public Mission API, like the UI.
    public class ScriptedCommander
    {
        readonly Mission m;
        static readonly string[] Goals = { "oxygen", "wall@hab", "greenhouse", "wall@shelter", "pad", "recycler", "gym", "panel" };

        readonly bool careful;

        // careful = false: never uses the Radio or the Storm Shelter (a player who ignores space weather).
        public ScriptedCommander(Mission mission, bool careful = true) { m = mission; this.careful = careful; }

        PlacedModule First(string id) => m.S.modules.Find(x => x.id == id);

        bool Do(string action, PlacedModule at) => at != null && m.Do(action, at.uid).ok;

        public void PlayMission(int maxSols = 100)
        {
            for (int i = 0; i < maxSols && !m.S.over; i++)
            {
                PlaySol();
                m.Sleep();
            }
        }

        public void PlaySol()
        {
            var hab = First("hab");
            if (careful) Do("radio", hab);

            foreach (var e in new List<ActiveEvent>(m.S.events))
            {
                if (e.id == "supply") { if (m.StartTrip("supply").ok) m.EndTrip(); continue; }
                if (e.id == "fatigue") { Do("rest", hab); continue; }
                var def = m.B.Event(e.id);
                var at = e.targetUid != 0 ? m.S.modules.Find(x => x.uid == e.targetUid) : First(def.station);
                Do("fix:" + e.id, at);
            }

            if (careful && m.S.forecastTonight == 2 || careful && (m.S.forecastTonight == 3 && m.S.power >= 60)) Do("seal", First("shelter"));

            WorkSites();
            TryPlace();
            WorkSites();

            if (m.S.health < 50) Do("rest", hab);

            for (int trips = 0; trips < 2 && m.S.quartersLeft >= 8 && m.S.health > 45; trips++)
            {
                string spot = PickSpot();
                if (spot == null || !m.StartTrip(spot).ok) break;
                if (!m.S.trip.warning && m.S.health > 60) m.PushLuck();
                m.EndTrip();
                TryPlace();
                WorkSites();
            }

            var gym = First("gym");
            if (gym != null && gym.built) Do("exercise", gym);
            if (m.S.health < 90) Do("rest", hab);
        }

        void WorkSites()
        {
            foreach (var site in new List<PlacedModule>(m.S.modules))
                while (!site.built && m.S.quartersLeft > 0 && Do("work", site)) { }
        }

        string NextGoal()
        {
            foreach (var g in Goals)
            {
                string id = g.Split('@')[0];
                if (!m.S.unlocked.Contains(id)) continue;
                if (g == "wall@hab" && m.Layout().IsShielded(First("hab"))) continue;
                if (g == "wall@shelter" && m.Layout().IsShielded(First("shelter"))) continue;
                if (id != "wall" && m.S.modules.Exists(x => x.id == id)) continue;
                return g;
            }
            return null;
        }

        void TryPlace()
        {
            // Never leave more than one site unfinished.
            if (m.S.modules.Exists(x => !x.built)) return;
            string goal = NextGoal();
            if (goal == null) return;
            string id = goal.Split('@')[0];
            var def = m.B.Module(id);
            if (!m.CanAfford(def)) return;
            // Keep 8 regolith for the pad once it's unlocked.
            if (id == "wall" && m.S.unlocked.Contains("pad") && !m.S.modules.Exists(x => x.id == "pad") && m.S.regolith - def.regolith < 8) return;
            var target = goal == "wall@hab" ? First("hab") : goal == "wall@shelter" ? First("shelter") : null;
            var grid = m.Layout();
            for (int y = 0; y < grid.Height; y++)
                for (int x = 0; x < grid.Width; x++)
                    foreach (bool rot in new[] { false, true })
                    {
                        if (target != null && !BaseLayout.Touching(x, y, 1, 1, target.x, target.y, target.w, target.h)) continue;
                        var check = m.CheckPlace(id, x, y, rot);
                        if (!check.ok || check.corridor.Count > 1) continue;
                        if (m.Place(id, x, y, rot).ok) return;
                    }
        }

        string PickSpot()
        {
            string goal = NextGoal();
            if (goal != null)
            {
                var def = m.B.Module(goal.Split('@')[0]);
                int reserve = m.S.unlocked.Contains("pad") && !m.S.modules.Exists(x => x.id == "pad") && def.id != "pad" ? 8 : 0;
                if (m.S.ice < def.ice) return "ice";
                if (m.S.parts < def.parts) return "ice";
                if (m.S.regolith < def.regolith + reserve) return "crater";
            }
            if (m.S.samples < 3) return "science";
            if (m.S.unlocked.Contains("pad") && !m.S.modules.Exists(x => x.id == "pad")) return "crater";
            return m.S.health > 70 ? "science" : null;
        }
    }
}

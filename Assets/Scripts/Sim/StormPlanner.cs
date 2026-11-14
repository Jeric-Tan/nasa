using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // Turns one real year of DONKI solar particle (SEP) events into storm nights for a mission.
    // The year is squeezed into the mission length, keeping the real order and dates on each card.
    public static class StormPlanner
    {
        public class Plan
        {
            public List<PlannedStorm> storms = new List<PlannedStorm>();
            public string label;
            public int year;
            public bool real;
        }

        public static Plan Make(Balance b, ModeDef mode, int sols, DonkiArchive archive, MissionRng rng)
        {
            var candidates = new List<DonkiSeason>();
            if (archive != null && archive.seasons != null)
                foreach (var season in archive.seasons)
                {
                    if (Array.IndexOf(mode.seasonKinds, season.kind) < 0) continue;
                    if (DistinctDays(season) < mode.minStorms) continue;
                    candidates.Add(season);
                }

            if (candidates.Count == 0) return Practice(b, mode, sols, rng);

            var chosen = rng.Pick(candidates);
            var plan = new Plan { year = chosen.year, real = true };
            plan.label = (chosen.kind == "stormy" ? "Stormy " : chosen.kind == "calm" ? "Calm " : "Busy ") + chosen.year;

            // One storm per real day; squeeze the year into the mission.
            var byDay = new List<DonkiStorm>();
            var days = new HashSet<string>();
            foreach (var st in chosen.storms)
                if (st != null && !string.IsNullOrEmpty(st.date) && days.Add(st.date)) byDay.Add(st);

            int target = Math.Min(Math.Max(mode.minStorms, byDay.Count), mode.maxStorms);
            var picked = EvenSample(byDay, target);
            var used = new HashSet<int>();
            foreach (var st in picked)
            {
                int doy = DayOfYear(st.date);
                int sol = 2 + (int)Math.Floor((doy - 1) / 366.0 * (sols - 1));
                sol = Math.Max(2, Math.Min(sols, sol));
                while (used.Contains(sol) && sol < sols) sol++;
                while (used.Contains(sol) && sol > 2) sol--;
                if (used.Contains(sol)) continue;
                used.Add(sol);
                plan.storms.Add(new PlannedStorm { sol = sol, record = st });
            }
            EnsureFinale(plan, sols);
            plan.storms.Sort((a, c) => a.sol.CompareTo(c.sol));
            return plan;
        }

        // The finale (Act 4) always has a storm on one of the last two nights.
        static void EnsureFinale(Plan plan, int sols)
        {
            foreach (var st in plan.storms) if (st.sol >= sols - 1) return;
            if (plan.storms.Count == 0) return;
            var last = plan.storms[plan.storms.Count - 1];
            last.sol = sols - 1;
        }

        static Plan Practice(Balance b, ModeDef mode, int sols, MissionRng rng)
        {
            var plan = new Plan { label = "Practice season", real = false };
            var fb = rng.Pick(b.fallbackSeasons);
            var used = new HashSet<int>();
            foreach (int night in fb.nights)
            {
                int sol = Math.Max(2, Math.Min(sols, (int)Math.Round(night * sols / 10.0)));
                if (used.Add(sol)) plan.storms.Add(new PlannedStorm { sol = sol, record = null });
            }
            EnsureFinale(plan, sols);
            plan.storms.Sort((a, c) => a.sol.CompareTo(c.sol));
            return plan;
        }

        public static int DistinctDays(DonkiSeason season)
        {
            var days = new HashSet<string>();
            if (season.storms != null)
                foreach (var st in season.storms) if (st != null && !string.IsNullOrEmpty(st.date)) days.Add(st.date);
            return days.Count;
        }

        static List<T> EvenSample<T>(List<T> list, int n)
        {
            if (n >= list.Count) return new List<T>(list);
            var outList = new List<T>();
            for (int i = 0; i < n; i++)
                outList.Add(list[(int)Math.Floor((i + 0.5) * list.Count / n)]);
            return outList;
        }

        static bool TryDate(string date, out DateTime d)
        {
            d = default;
            if (string.IsNullOrEmpty(date) || date.Length < 10) return false;
            return int.TryParse(date.Substring(0, 4), out int y) && int.TryParse(date.Substring(5, 2), out int m) &&
                   int.TryParse(date.Substring(8, 2), out int day) && m >= 1 && m <= 12 && day >= 1 && day <= 31 &&
                   (d = new DateTime(y, m, Math.Min(day, DateTime.DaysInMonth(y, m)))) != default;
        }

        public static int DayOfYear(string date) => TryDate(date, out var d) ? d.DayOfYear : 1;

        public static string NiceDate(string date) =>
            TryDate(date, out var d) ? d.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : date;
    }
}

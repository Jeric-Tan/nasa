using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // The night: drains, growth, storms, then the next morning. Order follows the Simulation spec.
    public partial class Mission
    {
        public NightReport Sleep()
        {
            if (S.over) return S.lastReport;
            if (TripActive) EndTrip();
            var r = new NightReport { sol = S.sol };
            var grid = Layout();
            var storm = StormOn(S.sol);
            bool dust = EventActive("dust") != null;

            // ---- Power ----
            int panels = CountActive("panel");
            int prod = (int)Math.Round((B.power.baseProduction + B.power.perPanel * panels) * (dust ? B.power.dustMultiplier : 1f));
            int use = B.power.baseUse;
            var useWhy = new List<string>();
            foreach (var m in S.modules)
            {
                if (!IsActive(m)) continue;
                var def = B.Module(m.id);
                if (def.powerUse > 0) { use += def.powerUse; useWhy.Add(def.name.ToLowerInvariant() + " " + def.powerUse); }
            }
            if (S.sealedTonight) { use += B.power.shelterUse; useWhy.Add("shelter " + B.power.shelterUse); }
            int p = S.power + prod - use;
            string pWhy = "Panels made " + prod + (dust ? " (half: panels dusty)" : "") + ", base used " + use +
                          (useWhy.Count > 0 ? " (" + string.Join(", ", useWhy) + ")" : "");
            if (p <= 0 && HasActive("battery"))
            {
                int cover = Math.Min(B.power.batteryReserve, 1 - p);
                p += cover;
                pWhy += ", battery covered " + cover;
            }
            bool down = p <= 0;
            if (down) pWhy += ". Power ran out!";
            r.lines.Add(new ReportLine { meter = "power", delta = Clamp(p) - S.power, reason = pWhy });
            S.power = Clamp(p);
            r.systemsDown = down;
            S.systemsDownLastNight = down;
            if (down) TriggerFirst("systemsdown");

            // ---- Life support ----
            int l = -B.life.crewUse;
            var lWhy = new List<string> { "Crew used " + B.life.crewUse + " air, water and food" };
            bool oxygenMaking = HasActive("oxygen") && !down;
            var oxy = B.Module("oxygen");
            if (HasActive("oxygen")) { if (down) lWhy.Add("oxygen maker had no power"); else { l += oxy.lifeGain; lWhy.Add("oxygen maker +" + oxy.lifeGain); } }

            var gh = S.modules.Find(m => m.id == "greenhouse" && m.built);
            bool ghReady = gh != null && gh.builtSol <= S.sol - B.life.greenhouseDelaySols;
            bool blight = EventActive("blight") != null;
            bool ghMaking = ghReady && !blight && !down;
            var ghDef = B.Module("greenhouse");
            if (gh != null && !ghReady) lWhy.Add("greenhouse still growing");
            if (ghReady && blight) lWhy.Add("sick crops made nothing");
            if (ghReady && !blight && down) lWhy.Add("greenhouse lamps had no power");
            if (ghMaking)
            {
                int g = ghDef.lifeGain + (HasTrait("green_thumb") ? 2 : 0);
                l += g;
                lWhy.Add("greenhouse +" + g);
            }

            if (HasActive("recycler"))
            {
                var rec = B.Module("recycler");
                if (EventActive("jam") != null) lWhy.Add("recycler jammed");
                else if (down) lWhy.Add("recycler had no power");
                else { l += rec.lifeGain; lWhy.Add("water recycler +" + rec.lifeGain); }
            }

            if (oxygenMaking && ghMaking)
            {
                var o = S.modules.Find(m => m.id == "oxygen" && m.built);
                if (o != null && BaseLayout.Touching(o, gh))
                {
                    l += B.life.adjacencyBonus;
                    lWhy.Add("oxygen maker next to greenhouse +" + B.life.adjacencyBonus);
                }
            }

            foreach (var e in S.events)
            {
                var def = B.Event(e.id);
                if (def != null && def.lifeDrain > 0) { l -= def.lifeDrain; lWhy.Add(def.name.ToLowerInvariant() + " -" + def.lifeDrain); }
            }
            if (down) { l -= B.life.systemsDownDrain; lWhy.Add("machines stopped -" + B.life.systemsDownDrain); }
            r.lines.Add(new ReportLine { meter = "life", delta = Clamp(S.life + l) - S.life, reason = string.Join(", ", lWhy) });
            S.life = Clamp(S.life + l);

            // ---- Crew health ----
            var hab = grid.First("hab");
            var shelter = grid.First("shelter");
            bool habShielded = grid.IsShielded(hab);
            int h = -(habShielded ? B.health.baselineShielded : B.health.baseline);
            var hWhy = new List<string> { "Normal radiation " + h + (habShielded ? " (shield wall)" : "") };
            int stormDmg = 0;
            if (storm != null)
            {
                S.stormsHit++;
                if (S.sealedTonight)
                {
                    stormDmg = B.health.stormSealed;
                    int neighbours = shelter == null ? 0 : grid.Neighbours(shelter, true).Count;
                    int cut = Math.Min(B.health.shelterNeighbourMax, neighbours);
                    stormDmg = Math.Max(0, stormDmg - cut);
                    if (grid.IsShielded(shelter)) stormDmg = (int)Math.Round(stormDmg / 2.0, MidpointRounding.AwayFromZero);
                    S.stormsSealed++;
                    Decide(4, "storm", "Sealed the shelter before the storm on sol " + S.sol + StormDateText(storm));
                }
                else
                {
                    stormDmg = B.health.stormUnsealed;
                    if (habShielded) stormDmg = (int)Math.Round(stormDmg / 2.0, MidpointRounding.AwayFromZero);
                    S.stormsUnsealed++;
                    Decide(5, "storm", "A storm hit on sol " + S.sol + " with nobody in the shelter" + StormDateText(storm));
                }
                h -= stormDmg;
                S.totalDose += stormDmg;
                hWhy.Add("particle storm -" + stormDmg + (S.sealedTonight ? " (shelter sealed)" : " (no shelter!)"));
                TriggerFirst("storm");
                Trigger("storm");
                if (storm.record != null && !string.IsNullOrEmpty(storm.record.id))
                {
                    S.factsFound.Add("storm:" + storm.record.id);
                    P.Add(P.logbook, "storm:" + storm.record.id);
                }
            }
            if (S.life < B.health.lowLifeThreshold) { h -= B.health.lowLifeDrain; hWhy.Add("low air and food -" + B.health.lowLifeDrain); }
            if (S.exercisedToday && HasActive("gym")) { h += B.health.gym; hWhy.Add("gym +" + B.health.gym); }
            if (EventActive("fatigue") != null) { h -= B.health.fatigueDrain; hWhy.Add("tired crew -" + B.health.fatigueDrain); }
            r.lines.Add(new ReportLine { meter = "health", delta = Clamp(S.health + h) - S.health, reason = string.Join(", ", hWhy) });
            S.health = Clamp(S.health + h);
            S.totalDose += habShielded ? B.health.baselineShielded : B.health.baseline;

            r.storm = storm != null;
            r.stormDamage = stormDmg;
            r.shelterSealed = S.sealedTonight;
            r.stormInfo = storm;
            Log("Night of sol " + S.sol + (storm != null ? ": a solar storm hit." : ": quiet night."));

            // ---- streaks ----
            S.solsInsideStreak = S.solsInsideStreak < 0 ? 0 : S.solsInsideStreak + 1;
            S.bestInsideStreak = Math.Max(S.bestInsideStreak, S.solsInsideStreak);
            if (S.quartersWorked >= Balance.Q(B.health.fatigueHours)) S.busyDaysInRow++;
            else S.busyDaysInRow = 0;

            // ---- ending? ----
            r.factId = NextFact(storm != null);
            if (S.life <= 0 || S.health <= 0)
            {
                Finish("early", S.health <= 0 ? "Crew health ran out." : "Life support ran out.");
            }
            else if (S.sol >= S.lastSol)
            {
                FinishMission();
            }
            r.ended = S.over;
            S.lastReport = r;
            if (S.over) return r;

            // ---- next morning ----
            S.sol++;
            S.quartersLeft = Balance.Q(B.start.hours);
            S.quartersWorked = 0;
            S.radioUsed = false;
            S.forecastTonight = S.forecastTomorrow == 2 ? 2 : S.forecastTomorrow == 1 ? 1 : 0;
            S.forecastTomorrow = 0;
            S.sealedTonight = false;
            S.exercisedToday = false;
            Morning(r);
            return r;
        }

        static string StormDateText(PlannedStorm storm) =>
            storm.record != null && !string.IsNullOrEmpty(storm.record.date) ? " (based on the real storm of " + StormPlanner.NiceDate(storm.record.date) + ")" : "";

        void Morning(NightReport r)
        {
            // Expire events with a deadline.
            for (int i = S.events.Count - 1; i >= 0; i--)
            {
                var e = S.events[i];
                if (e.expiresSol > 0 && S.sol > e.expiresSol)
                {
                    var def = B.Event(e.id);
                    S.events.RemoveAt(i);
                    r.morningNews.Add(def.name + " is gone" + (e.id == "supply" ? ": the parts were lost." : "."));
                    if (e.id == "supply") Decide(2, "event", "Let a supply drop go on sol " + S.sol);
                }
            }

            int act = ActFor(S.sol);
            if (act != ActFor(S.sol - 1))
            {
                Trigger("act:" + act);
                UnlockForAct(act);
            }
            Trigger("sol:" + S.sol);
            if (S.sol == S.dustSeasonStart) { Trigger("dustseason"); r.morningNews.Add("BOLT: Dust season is starting. Expect dusty panels."); }

            if (S.busyDaysInRow >= 2 && EventActive("fatigue") == null)
            {
                AddEvent("fatigue", r);
                S.busyDaysInRow = 0;
            }
            RollEvent(r);
        }

        void RollEvent(NightReport r)
        {
            if (S.sol < B.events.firstSol) return;
            int max = Mode.maxEvents;
            int rolled = 0;
            foreach (var e in S.events) if (B.Event(e.id).rolled) rolled++;
            if (rolled >= max) return;
            bool dustSeason = InDustSeason(S.sol);
            double chance = (dustSeason ? B.events.dustSeasonChance : B.events.chance) * Mode.eventChanceScale;
            if (!S.rng.Chance(chance)) return;

            var pool = new List<string>();
            var weights = new List<double>();
            foreach (var def in B.eventDefs)
            {
                if (!def.rolled || EventActive(def.id) != null) continue;
                if (!string.IsNullOrEmpty(def.requires) && !HasBuilt(def.requires)) continue;
                double w = def.weight;
                if (def.id == "dust")
                {
                    w = 1; // the starting array
                    foreach (var m in S.modules)
                        if (m.id == "panel" && m.built) w += m.far ? B.events.farPanelDustWeight : 1;
                    if (dustSeason) w *= B.events.dustSeasonDustWeight;
                }
                if (w <= 0) continue;
                pool.Add(def.id);
                weights.Add(w);
            }
            if (pool.Count == 0) return;
            double total = 0;
            foreach (var w in weights) total += w;
            double roll = S.rng.NextDouble() * total;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0 || i == pool.Count - 1) { AddEvent(pool[i], r); return; }
            }
        }

        public void AddEvent(string id, NightReport r)
        {
            var def = B.Event(id);
            var e = new ActiveEvent { id = id, since = S.sol, expiresSol = def.expiresAfter > 0 ? S.sol + def.expiresAfter - 1 : 0 };
            if (id == "dust")
            {
                // Which panel group got dusty: weighted toward near panels.
                var targets = new List<PlacedModule>();
                foreach (var m in S.modules)
                    if ((m.id == "solar" || m.id == "panel") && m.built)
                    {
                        targets.Add(m);
                        if (!m.far) targets.Add(m);
                    }
                if (targets.Count > 0) e.targetUid = S.rng.Pick(targets).uid;
            }
            S.events.Add(e);
            TriggerFirst("event:" + id);
            r?.morningNews.Add("New problem: " + def.name + ". " + def.hint + ".");
            Log("Morning problem: " + def.name + ".");
        }

        string NextFact(bool storm)
        {
            var pool = new List<string>();
            foreach (var f in D.staticFacts)
                if (!S.factsFound.Contains(f.id) && (f.trigger == "night" || (storm && f.trigger == "storm") ||
                    (f.trigger != null && f.trigger.StartsWith("build:") && HasBuilt(f.trigger.Substring(6)))))
                    pool.Add(f.id);
            if (D.osdr?.facts != null)
                foreach (var f in D.osdr.facts)
                    if (!S.factsFound.Contains(f.id)) pool.Add(f.id);
            if (pool.Count == 0) return null;
            // Not every night: about every other sol, always after a storm.
            if (!storm && !S.rng.Chance(0.5)) return null;
            string id = pool[S.rng.Range(0, pool.Count)];
            S.factsFound.Add(id);
            P.Add(P.logbook, id);
            return id;
        }

        void FinishMission()
        {
            var e = B.endings;
            bool pad = HasBuilt("pad");
            bool healthy = S.power >= e.healthyMeter && S.life >= e.healthyMeter && S.health >= e.healthyMeter;
            int ups = UpgradesBuilt(false);
            if (pad && healthy && ups >= e.thrivingUpgrades) Finish("thriving", "All meters healthy, " + ups + " upgrades and a landing pad.");
            else if (pad) Finish("ready", "The landing pad is ready, but some meters are under " + e.healthyMeter + ".");
            else Finish("holding", "Everyone made it, but there's no landing pad yet.");
        }

        void Finish(string ending, string why)
        {
            S.over = true;
            S.ending = ending;
            S.endingReason = why;
            Trigger("ending:" + ending);
            P.lastEnding = ending;
            if (ending != "early") P.missionsFinished++;
            if (ending != "early") P.jezeroUnlocked = true;
            AwardBadges();
        }

        public List<string> NewBadges = new List<string>();

        void AwardBadges()
        {
            void Give(string id) { if (P.Add(P.badges, id)) NewBadges.Add(id); }
            bool survived = S.ending != "early";
            if (survived && S.seasonReal && S.seasonLabel.StartsWith("Stormy")) Give("stormy_season");
            if (S.ending == "thriving") Give("thriving");
            if (S.ending == "thriving" && S.mode == "commander") Give("thriving_commander");
            if (S.bestInsideStreak >= 5) Give("inside_5");
            if (survived && S.stormsHit > 0 && S.stormsUnsealed == 0) Give("all_sealed");
            if (S.samples >= 5) Give("scientist");
            if (survived && S.seasonReal && HasXFlare()) Give("x_class");
        }

        bool HasXFlare()
        {
            foreach (var st in S.storms)
                if (st.record != null && !string.IsNullOrEmpty(st.record.flareClass) && st.record.flareClass.StartsWith("X")) return true;
            return false;
        }

        // The three decisions that mattered most, for the mission report.
        public List<Decision> TopDecisions(int n = 3)
        {
            var list = new List<Decision>(S.decisions);
            list.Sort((a, c) => a.weight != c.weight ? c.weight.CompareTo(a.weight) : a.sol.CompareTo(c.sol));
            var outList = new List<Decision>();
            var kinds = new HashSet<string>();
            foreach (var d in list) if (outList.Count < n && kinds.Add(d.kind + d.weight)) outList.Add(d);
            foreach (var d in list) if (outList.Count < n && !outList.Contains(d)) outList.Add(d);
            outList.Sort((a, c) => a.sol.CompareTo(c.sol));
            return outList;
        }
    }
}

using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    public class StationAction
    {
        public string id;          // rest | radio | seal | exercise | fix:<event> | work | cancel | outside
        public string label;
        public string detail;
        public int quarters;       // total cost incl. walking
        public bool enabled;
        public string why;         // when disabled
        public int targetUid;
    }

    public class ActionResult
    {
        public bool ok;
        public string message;
        public static ActionResult Fail(string why) => new ActionResult { ok = false, message = why };
        public static ActionResult Ok(string msg) => new ActionResult { ok = true, message = msg };
    }

    // The rules of a running mission. UI and world scripts call these methods and read State.
    public partial class Mission
    {
        public readonly Balance B;
        public readonly GameData D;
        public readonly MissionState S;
        public readonly Profile P;

        public Mission(Balance balance, GameData data, MissionState state, Profile profile)
        {
            B = balance;
            D = data ?? new GameData();
            S = state;
            P = profile ?? new Profile();
        }

        public ModeDef Mode => B.Mode(S.mode);
        public BaseLayout Layout() => new BaseLayout(B, S);
        public bool HasTrait(string id) => S.traits.Contains(id);

        // ---------- new mission ----------

        public static Mission Start(Balance b, GameData data, Profile profile, MissionSetup setup, int seed)
        {
            var s = new MissionState
            {
                seed = seed,
                classroomCode = setup.classroomCode,
                mode = setup.mode,
                site = setup.site,
                commanderName = string.IsNullOrEmpty(setup.commanderName) ? "Commander" : setup.commanderName,
                suitColor = setup.suitColor,
                rng = new MissionRng(seed),
            };
            var mode = b.Mode(s.mode);
            s.lastSol = mode.sols;
            s.power = b.start.power;
            s.life = b.start.life;
            s.health = b.start.health;
            s.regolith = b.start.regolith;
            s.ice = b.start.ice;
            s.parts = b.start.parts;
            s.samples = b.start.samples;
            s.quartersLeft = Balance.Q(b.start.hours);

            var site = b.Site(s.site);
            foreach (var p in site.startModules)
            {
                var def = b.Module(p.id);
                s.modules.Add(new PlacedModule
                {
                    uid = s.nextUid++, id = p.id, x = p.x, y = p.y,
                    w = p.rotated ? def.h : def.w, h = p.rotated ? def.w : def.h,
                    built = true, builtSol = 0, progressQ = Balance.Q(def.buildHours),
                });
            }

            foreach (var m in b.modules)
                if (m.unlock == "start" || (profile != null && profile.blueprints.Contains(m.id)))
                    if (!s.unlocked.Contains(m.id)) s.unlocked.Add(m.id);

            var traitIds = new List<string>();
            foreach (var t in b.traits) traitIds.Add(t.id);
            s.rng.Shuffle(traitIds);
            s.traits.Add(traitIds[0]);
            s.traits.Add(traitIds[1]);

            var plan = StormPlanner.Make(b, mode, mode.sols, data?.donki, s.rng);
            s.storms = plan.storms;
            s.seasonLabel = plan.label;
            s.seasonYear = plan.year;
            s.seasonReal = plan.real;

            // Dust season: Act 2, with a shuffled start and length.
            int a2 = ActStartSol(b, mode.sols, 2), a3 = ActStartSol(b, mode.sols, 3);
            int len = Math.Max(2, (a3 - a2) * 2 / 3 + s.rng.Range(-1, 2));
            s.dustSeasonStart = a2 + s.rng.Range(0, Math.Max(1, a3 - a2 - len + 1));
            s.dustSeasonEnd = Math.Min(a3 - 1, s.dustSeasonStart + len - 1);

            var mission = new Mission(b, data, s, profile);
            mission.Trigger("act:1");
            mission.Trigger("sol:1");
            mission.Log("Sol 1. You land at Outpost Gale. The next crew lands after sol " + s.lastSol + ".");
            return mission;
        }

        // ---------- acts ----------

        public static int ActStartSol(Balance b, int sols, int act)
        {
            float f = b.acts.starts[Math.Max(0, Math.Min(3, act - 1))];
            return Math.Max(1, (int)Math.Round(f * sols) + 1);
        }

        public int Act => ActFor(S.sol);

        public int ActFor(int sol)
        {
            int act = 1;
            for (int a = 2; a <= 4; a++) if (sol >= ActStartSol(B, S.lastSol, a)) act = a;
            return act;
        }

        public bool InDustSeason(int sol) => sol >= S.dustSeasonStart && sol <= S.dustSeasonEnd;

        public PlannedStorm StormOn(int sol) => S.storms.Find(st => st.sol == sol);
        public bool StormTonight => StormOn(S.sol) != null;

        // ---------- helpers ----------

        public void Log(string line)
        {
            S.log.Insert(0, line);
            if (S.log.Count > 40) S.log.RemoveAt(S.log.Count - 1);
        }

        public void Trigger(string t)
        {
            if (!S.triggers.Contains(t)) S.triggers.Add(t);
        }

        void TriggerFirst(string t)
        {
            string key = "first:" + t;
            if (S.seenBeats.Contains("trig:" + key)) return;
            S.seenBeats.Add("trig:" + key);
            Trigger(key);
        }

        public List<string> TakeTriggers()
        {
            var list = new List<string>(S.triggers);
            S.triggers.Clear();
            return list;
        }

        void Decide(int weight, string kind, string text) =>
            S.decisions.Add(new Decision { sol = S.sol, weight = weight, kind = kind, text = text });

        static int Clamp(int v) => Math.Max(0, Math.Min(100, v));

        public bool Spend(int quarters, bool work = true)
        {
            if (S.over || S.quartersLeft < quarters) return false;
            S.quartersLeft -= quarters;
            if (work) S.quartersWorked += quarters;
            return true;
        }

        public bool IsActive(PlacedModule m) => m != null && m.built && m.builtSol < S.sol;
        public bool HasActive(string id)
        {
            foreach (var m in S.modules) if (m.id == id && IsActive(m)) return true;
            return false;
        }
        public int CountActive(string id)
        {
            int n = 0;
            foreach (var m in S.modules) if (m.id == id && IsActive(m)) n++;
            return n;
        }
        public bool HasBuilt(string id)
        {
            foreach (var m in S.modules) if (m.id == id && m.built) return true;
            return false;
        }

        public ActiveEvent EventActive(string id) => S.events.Find(e => e.id == id);

        public static string Hours(int quarters)
        {
            float h = quarters / 4f;
            return (h == Math.Floor(h) ? ((int)h).ToString() : h.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) + " h";
        }

        public string CostText(ModuleDef d)
        {
            var parts = new List<string>();
            if (d.regolith > 0) parts.Add(d.regolith + " regolith");
            if (d.ice > 0) parts.Add(d.ice + " ice");
            if (d.parts > 0) parts.Add(d.parts + (d.parts == 1 ? " part" : " parts"));
            return parts.Count == 0 ? "free" : string.Join(", ", parts);
        }

        public bool CanAfford(ModuleDef d) => S.regolith >= d.regolith && S.ice >= d.ice && S.parts >= d.parts;

        // ---------- stations ----------

        public List<StationAction> ActionsAt(PlacedModule m)
        {
            var list = new List<StationAction>();
            if (m == null || S.over) return list;
            var grid = Layout();
            int walk = grid.WalkQuarters(m, HasTrait("fast_walker"));

            if (!m.built)
            {
                var def = B.Module(m.id);
                int left = Balance.Q(def.buildHours) - m.progressQ;
                int q = Math.Min(Balance.Q(B.actions.work), left) + walk;
                Add(list, "work", "Build: " + def.name, "Work 1 h (" + Hours(left) + " left)", q, m.uid);
                list.Add(new StationAction { id = "cancel", label = "Cancel build", detail = "Get all materials back", quarters = 0, enabled = true, targetUid = m.uid });
                return list;
            }

            switch (m.id)
            {
                case "hab":
                    Add(list, "rest", "Rest", "+" + RestAmount() + " crew health", Balance.Q(B.actions.rest), m.uid);
                    var radio = Add(list, "radio", "Radio Earth", HasActive("dish") ? "Hear the forecast for tonight and tomorrow" : "Hear tonight's space weather forecast", Balance.Q(B.actions.radio), m.uid);
                    if (S.radioUsed) { radio.enabled = false; radio.why = "Already heard today's message"; }
                    break;
                case "shelter":
                    var seal = Add(list, "seal", "Seal Storm Shelter", "Crew sleeps safe tonight; uses " + B.power.shelterUse + " power", Balance.Q(B.actions.seal) + walk, m.uid);
                    if (S.sealedTonight) { seal.enabled = false; seal.why = "Already sealed for tonight"; }
                    break;
                case "airlock":
                    Add(list, "outside", "Go outside", "Pick a spot on the map", walk, m.uid);
                    break;
                case "gym":
                    var ex = Add(list, "exercise", "Exercise", "+" + B.health.gym + " health tonight (bones and muscles)", Balance.Q(B.actions.exercise) + walk, m.uid);
                    if (S.exercisedToday) { ex.enabled = false; ex.why = "Already exercised today"; }
                    break;
            }

            foreach (var e in S.events)
            {
                var def = B.Event(e.id);
                if (def == null || def.station != m.id && !(def.station == "solar" && m.id == "panel")) continue;
                if (e.targetUid != 0 && e.targetUid != m.uid) continue;
                if (def.id == "supply" || def.id == "fatigue") continue; // fixed by a trip / by resting
                int q = Balance.Q(def.fixHours) + walk + (def.id == "dust" && m.far ? 4 : 0);
                Add(list, "fix:" + e.id, def.fixText, def.hint, q, m.uid);
            }
            return list;
        }

        StationAction Add(List<StationAction> list, string id, string label, string detail, int q, int uid)
        {
            var a = new StationAction { id = id, label = label, detail = detail, quarters = q, enabled = S.quartersLeft >= q, targetUid = uid };
            if (!a.enabled) a.why = "Not enough daylight left";
            list.Add(a);
            return a;
        }

        int RestAmount() => B.health.rest + (HasTrait("light_sleeper") ? 5 : 0);

        public ActionResult Do(string actionId, int uid)
        {
            if (S.over) return ActionResult.Fail("The mission is over");
            var m = Layout().ByUid(uid);
            var options = ActionsAt(m);
            var opt = options.Find(o => o.id == actionId);
            if (opt == null) return ActionResult.Fail("You can't do that here");
            if (!opt.enabled) return ActionResult.Fail(opt.why ?? "Not now");

            switch (actionId)
            {
                case "rest":
                {
                    Spend(opt.quarters, work: false);
                    S.health = Clamp(S.health + RestAmount());
                    var f = EventActive("fatigue");
                    if (f != null) { S.events.Remove(f); Log("The crew got some rest. Fatigue is gone."); }
                    Log("The crew rests: +" + RestAmount() + " health.");
                    return ActionResult.Ok("+" + RestAmount() + " health");
                }
                case "radio":
                {
                    Spend(opt.quarters);
                    S.radioUsed = true;
                    S.forecastTonight = StormTonight ? 2 : 1;
                    float late = Mode.lateWarningChance * (HasActive("dish") ? 0.5f : 1f);
                    if (Act >= 3 && late > 0 && S.rng.Chance(late)) S.forecastTonight = 3; // warning arrives too late
                    if (HasActive("dish")) S.forecastTomorrow = StormOn(S.sol + 1) != null ? 2 : 1;
                    TriggerFirst("radio");
                    string msg = RadioMessage();
                    Log(msg);
                    return ActionResult.Ok(msg);
                }
                case "seal":
                    Spend(opt.quarters);
                    S.sealedTonight = true;
                    Log("Storm Shelter sealed for tonight. It will use " + B.power.shelterUse + " power.");
                    return ActionResult.Ok("Shelter sealed");
                case "exercise":
                    Spend(opt.quarters);
                    S.exercisedToday = true;
                    Log("The crew worked out in the gym corner.");
                    return ActionResult.Ok("Exercise done");
                case "work":
                    return Work(m, opt.quarters);
                case "cancel":
                    return Cancel(m);
                case "outside":
                    return ActionResult.Ok("outside");
            }

            if (actionId.StartsWith("fix:"))
            {
                string id = actionId.Substring(4);
                var e = EventActive(id);
                if (e == null) return ActionResult.Fail("Already fixed");
                Spend(opt.quarters);
                S.events.Remove(e);
                var def = B.Event(id);
                if (id == "dust")
                {
                    S.health = Clamp(S.health - B.health.dustCleanDose);
                    S.totalDose += B.health.dustCleanDose;
                    S.solsInsideStreak = -1; // went outside today
                    Log("Panels cleaned (outside, -" + B.health.dustCleanDose + " health). Full solar power again.");
                }
                else Log(def.name + " fixed.");
                Decide(1, "event", "Fixed the " + def.name.ToLowerInvariant() + " on sol " + S.sol);
                return ActionResult.Ok(def.name + " fixed");
            }
            return ActionResult.Fail("Unknown action");
        }

        public string RadioMessage()
        {
            var storm = StormOn(S.sol);
            string text;
            if (S.forecastTonight == 3)
                text = "Earth's message is late and crackly: \"…Sun is very busy… can't tell yet if…\" You'll have to decide without Earth.";
            else if (storm != null)
            {
                string flare = storm.record != null && !string.IsNullOrEmpty(storm.record.flareClass)
                    ? "We saw a class " + storm.record.flareClass + " flare. " : "The Sun threw out a cloud of particles. ";
                text = "Earth's message (sent yesterday): \"" + flare + "A particle storm may reach you tonight.\"";
            }
            else text = "Earth's message (sent yesterday): \"Space weather looks calm for tonight.\"";
            if (HasActive("dish"))
                text += StormOn(S.sol + 1) != null ? " The big dish also caught a warning for tomorrow night!" : " Tomorrow looks calm too.";
            return text;
        }

        // ---------- building ----------

        public PlaceCheck CheckPlace(string moduleId, int x, int y, bool rotated)
        {
            var def = B.Module(moduleId);
            if (def == null) return PlaceCheck.No("Unknown module");
            if (!S.unlocked.Contains(moduleId)) return PlaceCheck.No("Not unlocked yet");
            if (!CanAfford(def)) return PlaceCheck.No("Need " + CostText(def));
            return PlacementRules.CanPlace(B, S, Layout(), moduleId, x, y, rotated);
        }

        public ActionResult Place(string moduleId, int x, int y, bool rotated)
        {
            if (S.over) return ActionResult.Fail("The mission is over");
            var check = CheckPlace(moduleId, x, y, rotated);
            if (!check.ok) return ActionResult.Fail(check.reason);
            var def = B.Module(moduleId);
            S.regolith -= def.regolith;
            S.ice -= def.ice;
            S.parts -= def.parts;
            var m = new PlacedModule
            {
                uid = S.nextUid++, id = moduleId, x = x, y = y, w = check.w, h = check.h, far = check.far,
                reservedRegolith = def.regolith, reservedIce = def.ice, reservedParts = def.parts,
            };
            S.modules.Add(m);
            foreach (var c in check.corridor) S.corridors.Add(c);
            Log("Construction site placed: " + def.name + ". Work there to build it.");
            return ActionResult.Ok("Site placed");
        }

        ActionResult Work(PlacedModule m, int quarters)
        {
            var def = B.Module(m.id);
            int walk = Layout().WalkQuarters(m, HasTrait("fast_walker"));
            Spend(quarters);
            m.progressQ += Math.Max(1, quarters - walk) + (HasTrait("handy") ? 1 : 0);
            if (m.progressQ >= Balance.Q(def.buildHours))
            {
                m.built = true;
                m.builtSol = S.sol;
                TriggerFirst("build");
                Trigger("build:" + m.id);
                Decide(3, "build", "Built the " + def.name.ToLowerInvariant() + " on sol " + S.sol + " (" + def.plus + "; " + def.minus.ToLowerInvariant() + ")");
                Log("Built: " + def.name + ". It switches on tomorrow. Trade-off: " + def.minus.ToLowerInvariant() + ".");
                return ActionResult.Ok(def.name + " finished!");
            }
            Log("Worked on the " + def.name.ToLowerInvariant() + ".");
            return ActionResult.Ok("Progress " + Hours(m.progressQ) + " / " + Hours(Balance.Q(def.buildHours)));
        }

        public ActionResult Cancel(PlacedModule m)
        {
            if (m == null || m.built) return ActionResult.Fail("Finished modules can't be taken apart");
            S.regolith += m.reservedRegolith;
            S.ice += m.reservedIce;
            S.parts += m.reservedParts;
            S.modules.Remove(m);
            // Drop corridor tiles nothing needs any more.
            S.corridors.RemoveAll(c => !CorridorNeeded(c));
            Log("Build cancelled. Materials returned.");
            return ActionResult.Ok("Cancelled");
        }

        bool CorridorNeeded(Cell c)
        {
            foreach (var m in S.modules)
            {
                var def = B.Module(m.id);
                if (def.placement != "connected" && def.placement != "inside") continue;
                if (BaseLayout.Touching(c.x, c.y, 1, 1, m.x, m.y, m.w, m.h) && m.id != "hab") return true;
            }
            return false;
        }

        public int UpgradesBuilt(bool includePad)
        {
            int n = 0;
            bool wall = false;
            foreach (var m in S.modules)
            {
                if (!m.built) continue;
                var def = B.Module(m.id);
                if (!def.countsAsUpgrade) continue;
                if (m.id == "pad" && !includePad) continue;
                if (m.id == "wall") { if (wall) continue; wall = true; }
                n++;
            }
            return n;
        }

        // Unlock the next blueprint when a rock sample comes home.
        void UnlockFromSample()
        {
            ModuleDef next = null;
            foreach (var d in B.modules)
                if (d.unlock == "sample" && !S.unlocked.Contains(d.id) && (next == null || d.unlockOrder < next.unlockOrder)) next = d;
            if (next == null) return;
            S.unlocked.Add(next.id);
            P.Add(P.blueprints, next.id);
            Trigger("unlock:" + next.id);
            Log("Your rock sample unlocked a new blueprint: " + next.name + "!");
        }

        void UnlockForAct(int act)
        {
            string key = act == 2 ? "act2" : act == 3 ? "act3" : null;
            if (key == null) return;
            foreach (var d in B.modules)
                if (d.unlock == key && !S.unlocked.Contains(d.id))
                {
                    S.unlocked.Add(d.id);
                    Trigger("unlock:" + d.id);
                    Log("New blueprint from Mission Control: " + d.name + ".");
                }
        }

        // ---------- expeditions ----------

        public List<SpotDef> SpotsToday()
        {
            var list = new List<SpotDef>();
            foreach (var s in B.spots)
                if (string.IsNullOrEmpty(s.onlyWithEvent) || EventActive(s.onlyWithEvent) != null) list.Add(s);
            return list;
        }

        public int AirlockWalk()
        {
            var grid = Layout();
            return grid.WalkQuarters(grid.First("airlock"), HasTrait("fast_walker"));
        }

        public int TripQuarters(SpotDef spot)
        {
            float h = spot.hours;
            if (spot.id != "supply" && HasTrait("quick_trips") && spot.hours >= 3) h -= 0.5f;
            return Balance.Q(h) + AirlockWalk();
        }

        public int TripDose(SpotDef spot) => StormTonight ? spot.stormDose : spot.dose;

        public bool TripActive => S.trip != null && !string.IsNullOrEmpty(S.trip.spotId);

        public ActionResult StartTrip(string spotId)
        {
            if (S.over) return ActionResult.Fail("The mission is over");
            if (TripActive) return ActionResult.Fail("Already outside");
            var spot = B.Spot(spotId);
            if (spot == null || !SpotsToday().Contains(spot)) return ActionResult.Fail("That spot isn't on the map today");
            int q = TripQuarters(spot);
            if (S.quartersLeft < q) return ActionResult.Fail("Not enough daylight for that trip");
            Spend(q);
            int dose = TripDose(spot);
            int amount = spot.amount + (spot.material == "ice" ? B.Site(S.site).iceBonus : 0);
            if (spot.material == "samples" && HasTrait("rock_hound") && S.rng.Chance(0.5)) amount++;
            Give(spot.material, amount);
            S.health = Clamp(S.health - dose);
            S.totalDose += dose;
            S.tripsOutside++;
            S.solsInsideStreak = -1;
            var trip = new Trip { spotId = spotId, gained = amount, dose = dose, material = spot.material };
            string msg = spot.name + ": +" + amount + " " + MaterialName(spot.material, amount) + ", -" + dose + " health from radiation.";
            if (spot.partChance > 0 && S.rng.Chance(spot.partChance))
            {
                S.parts++;
                msg += " Found an old supply crate: +1 part!";
            }
            if (spot.onlyWithEvent == "supply")
            {
                var e = EventActive("supply");
                if (e != null) S.events.Remove(e);
            }
            // Push-your-luck: sometimes a hazard shows up while you're out there.
            if (StormTonight && S.rng.Chance(0.6)) { trip.warning = true; trip.hazard = "storm"; }
            else if (S.rng.Chance(B.actions.hazardChance)) { trip.warning = true; trip.hazard = InDustSeason(S.sol) ? "dust_devil" : "flare"; }
            S.trip = trip;
            TriggerFirst("trip");
            Log(msg);
            return ActionResult.Ok(msg);
        }

        public int PushQuarters => Balance.Q(B.actions.pushLuckHours);
        public int PushDose => B.actions.pushLuckDose * (S.trip != null && S.trip.warning ? 2 : 1);

        public bool CanPush => TripActive && !S.trip.pushed && S.quartersLeft >= PushQuarters && S.trip.material != "parts";

        public ActionResult PushLuck()
        {
            if (!CanPush) return ActionResult.Fail("Time to head home");
            Spend(PushQuarters);
            int dose = PushDose;
            S.health = Clamp(S.health - dose);
            S.totalDose += dose;
            Give(S.trip.material, 1);
            S.trip.pushed = true;
            S.trip.gained++;
            S.trip.dose += dose;
            Decide(S.trip.warning ? 3 : 2, "trip", "Stayed out for one more " + MaterialName(S.trip.material, 1) + (S.trip.warning ? " even with a hazard warning" : "") + " on sol " + S.sol);
            string msg = "One more: +1 " + MaterialName(S.trip.material, 1) + ", -" + dose + " health.";
            Log(msg);
            return ActionResult.Ok(msg);
        }

        public void EndTrip()
        {
            if (!TripActive) return;
            if (S.trip.material == "samples")
                for (int i = 0; i < S.trip.gained; i++) UnlockFromSample();
            S.trip = null;
        }

        void Give(string material, int n)
        {
            switch (material)
            {
                case "regolith": S.regolith += n; break;
                case "ice": S.ice += n; break;
                case "samples": S.samples += n; break;
                case "parts": S.parts += n; break;
            }
        }

        public static string MaterialName(string material, int n)
        {
            switch (material)
            {
                case "regolith": return "regolith";
                case "ice": return "ice";
                case "samples": return n == 1 ? "rock sample" : "rock samples";
                case "parts": return n == 1 ? "spare part" : "spare parts";
            }
            return material;
        }
    }
}

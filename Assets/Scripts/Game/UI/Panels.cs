using System;
using System.Collections.Generic;
using OutpostGale.Sim;
using OutpostGale.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OutpostGale
{
    public partial class GameManager
    {
        // ---------- stations ----------

        public void QuickAction(string stationId, string action)
        {
            if (!WorldInputOpen) return;
            var m = Mission.Layout().First(stationId);
            if (m != null) World.WalkTo(m, action);
        }

        void GoFixEvent(ActiveEvent e)
        {
            if (!WorldInputOpen) return;
            var def = Balance.Event(e.id);
            if (e.id == "supply") { QuickAction("airlock", "outside"); return; }
            if (e.id == "fatigue") { QuickAction("hab", "rest"); return; }
            var at = e.targetUid != 0 ? Mission.Layout().ByUid(e.targetUid) : Mission.Layout().First(def.station);
            if (at != null) World.WalkTo(at, "fix:" + e.id);
        }

        public void OpenStation(PlacedModule m, string autoAction = null)
        {
            if (Mission.S.over) return;
            var actions = Mission.ActionsAt(m);
            if (autoAction != null)
            {
                var a = actions.Find(x => x.id == autoAction);
                if (a != null && a.enabled)
                {
                    // Trips and fixes open their own popup; every other quick job asks first.
                    if (a.id == "outside" || a.id.StartsWith("fix:")) RunAction(m, a);
                    else Queue(done => ConfirmAction(m, a, done));
                    return;
                }
                if (a != null) { Toast(a.why ?? "Not now"); AudioFx.Error(); return; }
            }
            if (actions.Count == 0)
            {
                var def = Balance.Module(m.id);
                Toast(def.name + ": " + (string.IsNullOrEmpty(def.plus) ? "nothing to do here" : def.plus));
                return;
            }
            Queue(done =>
            {
                var def = Balance.Module(m.id);
                var card = Modal(560);
                UIKit.Label(card, (m.built ? "" : "Construction site: ") + def.name, 28, Theme.Ink, TextAnchor.MiddleLeft, true);
                Origin(card, "ROOM", "You're at this room. Pick a job: each one costs daylight hours.", false);
                if (!string.IsNullOrEmpty(def.plus) && m.built)
                    UIKit.Label(card, "<color=#227A3B>+ " + def.plus + "</color>" + (string.IsNullOrEmpty(def.minus) ? "" : "   <color=#B42318>− " + def.minus + "</color>"), 15);
                if (m.id == "hab" && m.built && !World.Inside)
                {
                    var inside = UIKit.Button(card, "Go inside the base", () => { CloseModal(card); done(); World.SetInside(true); }, Theme.Life, 18);
                    UIKit.Size(inside, -1, 52, -1, 54);
                }
                foreach (var a in actions)
                {
                    var act = a;
                    string text = a.label + "  ·  " + Mission.Hours(a.quarters) + "\n<size=14>" + (a.enabled ? a.detail : a.why) + "</size>";
                    var b = UIKit.Button(card, text, () => { CloseModal(card); done(); RunAction(m, act); }, a.id == "cancel" ? Theme.Paper : Color.white, 18);
                    UIKit.Size(b, -1, 64, -1, 66);
                    b.interactable = a.enabled;
                }
                UIKit.Button(card, "Close", () => { CloseModal(card); done(); }, Theme.Paper, 16);
            });
        }

        // A quick job (shortcut key, dock button, or E at an object): say what it gives and costs, then ask.
        void ConfirmAction(PlacedModule m, StationAction a, Action done)
        {
            var card = Modal(500);
            UIKit.Label(card, a.label, 28, Theme.Ink, TextAnchor.MiddleLeft, true);
            Origin(card, "QUICK JOB", "You picked this with a shortcut. Check it before you spend the time.", false);
            UIKit.Label(card, a.detail + ".", 18);
            UIKit.Label(card, "Takes " + Mission.Hours(a.quarters) + " of daylight (" + Mission.Hours(Mission.S.quartersLeft) + " left today).", 16, Theme.Muted);
            var row = UIKit.Rect("Row", card);
            UIKit.HStack(row.gameObject, 10).childForceExpandWidth = true;
            UIKit.Size(row, -1, 56, -1, 56);
            UIKit.Button(row, "Not now", () => { CloseModal(card); done(); }, Theme.Paper);
            UIKit.Button(row, "Do it", () => { CloseModal(card); done(); RunAction(m, a); }, Theme.Power);
        }

        void RunAction(PlacedModule m, StationAction a)
        {
            if (a.id == "outside")
            {
                // Step out through the airlock onto the surface, then pick where to go.
                if (World.Inside) World.StepOutside(m);
                ShowExpedition();
                return;
            }
            if (a.id.StartsWith("fix:"))
            {
                string id = a.id.Substring(4);
                if (Profile.autoDo.Contains(id)) { Apply(m, a, true); return; }
                Queue(done => ShowHoldToFix(m, a, id, done));
                return;
            }
            Apply(m, a, false);
        }

        void Apply(PlacedModule m, StationAction a, bool auto)
        {
            var r = Mission.Do(a.id, m.uid);
            if (!r.ok) { Toast(r.message); AudioFx.Error(); return; }
            if (a.id == "work" && m.built) AudioFx.Clunk();
            else if (a.id == "seal" || a.id.StartsWith("fix:")) AudioFx.Chime();
            if (a.id == "radio") ShowRadioMessage(r.message);
            else Toast((auto ? "Auto-done: " : "") + r.message);
            AfterChange();
        }

        void ShowRadioMessage(string message)
        {
            Queue(done =>
            {
                var card = Modal(600, null, true);
                UIKit.Label(card, "Radio from Earth", 26, Theme.Gold, TextAnchor.MiddleLeft, true);
                Origin(card, "RADIO", "You used the Radio. Radio waves take minutes to cross space, so Earth's news is a sol late.", true);
                AudioFx.Radio();
                var bar = UIKit.Label(card, "Signal travelling…", 14, Theme.Hex("#D9CFE0"));
                var text = UIKit.Label(card, "", 19, Theme.Paper);
                StartCoroutine(Crawl(text, message, bar));
                UIKit.Button(card, "Got it", () => { CloseModal(card); done(); }, Theme.Gold);
            });
        }

        System.Collections.IEnumerator Crawl(Text t, string message, Text bar)
        {
            float speed = Profile.reducedMotion ? 1000 : 70;
            for (float i = 0; i <= message.Length && t; i += Time.unscaledDeltaTime * speed)
            {
                t.text = message.Substring(0, Mathf.Min(message.Length, (int)i));
                yield return null;
            }
            if (t) t.text = message;
            if (bar) bar.text = "Received. (Sent from Earth yesterday: radio takes time.)";
        }

        void ShowHoldToFix(PlacedModule m, StationAction a, string eventId, Action done)
        {
            var def = Balance.Event(eventId);
            var card = Modal(460);
            UIKit.Label(card, def.fixText, 26, Theme.Ink, TextAnchor.MiddleCenter, true);
            Origin(card, "FIXING A PROBLEM", "Problems pop up as machines wear out. You chose to fix this one.", false);
            UIKit.Label(card, "Hold the button (or Space) until the ring fills.", 16, Theme.Muted, TextAnchor.MiddleCenter);
            var holder = UIKit.Rect("Ring", card);
            UIKit.Size(holder, -1, 150, -1, 150);
            var ringBg = UIKit.Rect("Bg", holder);
            UIKit.Anchor(ringBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140));
            var bgImg = ringBg.gameObject.AddComponent<Image>();
            bgImg.sprite = Theme.Circle(128, true);
            bgImg.color = Theme.Sand;
            var ring = UIKit.Rect("Fill", holder);
            UIKit.Anchor(ring, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128, 128));
            var ringImg = ring.gameObject.AddComponent<Image>();
            ringImg.sprite = Theme.Circle(128);
            ringImg.color = Theme.Health;
            ringImg.type = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillOrigin = 2;
            ringImg.fillAmount = 0;
            var btn = UIKit.Button(card, "HOLD TO FIX", null, Theme.Power, 22);
            UIKit.Size(btn, -1, 64, -1, 64);
            var hold = btn.gameObject.AddComponent<HoldButton>();
            hold.ring = ringImg;
            hold.seconds = 1.6f;
            hold.onDone = () =>
            {
                Profile.Add(Profile.autoDo, eventId);
                CloseModal(card);
                done();
                Apply(m, a, false);
            };
            UIKit.Button(card, "Not now", () => { CloseModal(card); done(); }, Theme.Paper, 16);
        }

        // ---------- building ----------

        public void OpenBuildMenu()
        {
            if (!WorldInputOpen) return;
            Queue(done =>
            {
                var card = Modal(980);
                UIKit.Label(card, "Build an upgrade", 30, Theme.Ink, TextAnchor.MiddleLeft, true);
                UIKit.Label(card, "Every upgrade helps one thing and costs another. Place it, then walk to the site and work to build it.", 16, Theme.Muted);
                var grid = UIKit.Rect("Grid", card);
                var g = grid.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(300, 196);
                g.spacing = new Vector2(12, 12);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = 3;
                int locked = 0;
                foreach (var def in Balance.modules)
                {
                    if (def.placement == "fixed") continue;
                    if (!Mission.S.unlocked.Contains(def.id)) { locked++; continue; }
                    BuildCard(grid, def, card, done);
                }
                int rows = Mathf.CeilToInt((Balance.modules.Length - 4 - locked) / 3f);
                UIKit.Size(grid, -1, rows * 208, -1, rows * 208);
                if (locked > 0) UIKit.Label(card, locked + " more blueprint" + (locked > 1 ? "s" : "") + " to unlock: bring home rock samples from the layered rocks, and listen for Earth.", 15, Theme.Muted);
                UIKit.Button(card, "Close", () => { CloseModal(card); done(); }, Theme.Paper, 16);
            });
        }

        void BuildCard(RectTransform grid, ModuleDef def, RectTransform menu, Action done)
        {
            var p = UIKit.Panel(grid, Color.white, def.id, true, 18);
            UIKit.VStack(p.gameObject, 3, 14);
            int count = Mission.S.modules.FindAll(x => x.id == def.id).Count;
            var head = UIKit.Rect("Head", p.transform);
            var hh = UIKit.HStack(head.gameObject, 8);
            hh.childForceExpandWidth = false;
            UIKit.Size(head, -1, 36, -1, 36);
            var title = UIKit.Label(head, def.name + (def.maxCount > 1 ? "  <size=13><color=#76677F>" + count + "/" + def.maxCount + "</color></size>" : ""), 19, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Size(title, -1, 36, -1, 36, 1);
            var art = Theme.Art(def.id);
            if (art)
            {
                var pic = UIKit.Rect("Art", head);
                UIKit.Size(pic, 36, 36, 36, 36);
                var pi = pic.gameObject.AddComponent<Image>();
                pi.sprite = art;
                pi.preserveAspect = true;
                pi.raycastTarget = false;
            }
            UIKit.Label(p.transform, "<color=#227A3B>+ " + def.plus + "</color>", 14);
            UIKit.Label(p.transform, "<color=#B42318>− " + def.minus + "</color>", 14);
            UIKit.Label(p.transform, "<i>" + def.why + "</i>", 12, Theme.Muted);
            bool afford = Mission.CanAfford(def);
            bool full = count >= def.maxCount;
            string label = full ? "All built" : afford ? "Place · " + Mission.CostText(def) + " · " + def.buildHours + " h" : "Need " + Mission.CostText(def);
            var b = UIKit.Button(p.transform, label, () =>
            {
                CloseModal(menu);
                done();
                if (!Profile.badges.Contains("_first_build_card") && !Mission.S.modules.Exists(x => !Balance.Module(x.id).placement.Equals("fixed")))
                    Queue(d => FirstUpgradeQuestion(def, d));
                else StartPlacing(def.id);
            }, afford && !full ? Theme.Power : Theme.Paper, 14);
            UIKit.Size(b, -1, 40, -1, 42);
            b.interactable = afford && !full;
        }

        // The first upgrade asks the player to name what they're giving up.
        void FirstUpgradeQuestion(ModuleDef def, Action done)
        {
            var card = Modal(600);
            UIKit.Label(card, "Every choice costs something", 26, Theme.Ink, TextAnchor.MiddleLeft, true);
            Origin(card, "FIRST UPGRADE", "This question only comes once, before your first build.", false);
            UIKit.Label(card, "The " + def.name.ToLowerInvariant() + " gives you:\n<color=#227A3B><b>+ " + def.plus + "</b></color>\n\nBut it costs:\n<color=#B42318><b>− " + def.minus + "</b></color>", 19);
            UIKit.Label(card, "Are you OK giving that up?", 19);
            var row = UIKit.Rect("Row", card);
            UIKit.HStack(row.gameObject, 10).childForceExpandWidth = true;
            UIKit.Button(row, "Yes, build it", () => { Profile.Add(Profile.badges, "_first_build_card"); CloseModal(card); done(); StartPlacing(def.id); }, Theme.Power);
            UIKit.Button(row, "Not now", () => { CloseModal(card); done(); });
        }

        void StartPlacing(string id)
        {
            World.StartGhost(id);
            RefreshHud();
        }

        public void CancelBuild()
        {
            World.StopGhost();
            RefreshHud();
        }

        public void TryPlaceGhost()
        {
            var c = World.ghostCheck;
            if (c == null) return;
            if (!c.ok) { Toast(c.reason); AudioFx.Error(); return; }
            var cell = World.ghostCell;
            string id = World.ghostModule;
            var r = Mission.Place(id, cell.x, cell.y, World.ghostRotated);
            if (!r.ok) { Toast(r.message); AudioFx.Error(); return; }
            World.StopGhost();
            AudioFx.Click();
            Toast("Site placed. Click it to walk there and build (" + Balance.Module(id).buildHours + " h of work).");
            AfterChange();
        }

        // ---------- expeditions ----------

        void ShowExpedition()
        {
            Queue(done =>
            {
                var s = Mission.S;
                var card = Modal(1000, Theme.Paper);
                UIKit.Label(card, "Outside: " + Balance.Site(s.site).name, 30, Theme.Ink, TextAnchor.MiddleLeft, true);
                UIKit.Label(card, "Every trip costs daylight and a radiation dose. Farther spots have better finds. You have " + Mission.Hours(s.quartersLeft) + " left today.", 16, Theme.Muted);
                var body = UIKit.Rect("Body", card);
                UIKit.HStack(body.gameObject, 14, 0, TextAnchor.UpperLeft);
                UIKit.Size(body, -1, 360, -1, 360);
                var map = MapView(body);
                UIKit.Size(map, 360, 360, 360, 360);
                var list = UIKit.Rect("Spots", body);
                UIKit.VStack(list.gameObject, 10);
                UIKit.Size(list, 560, 360, 580, 360, 1);
                bool knownStorm = s.forecastTonight == 2;
                foreach (var spot in Mission.SpotsToday())
                {
                    var sp = spot;
                    int q = Mission.TripQuarters(sp);
                    int dose = knownStorm ? sp.stormDose : sp.dose;
                    string rad = Mission.Mode.realUnits ? "−" + dose + " health" : "Radiation: " + (dose <= 3 ? "LOW" : dose <= 5 ? "MEDIUM" : "HIGH");
                    string finds = "+" + (sp.amount + (sp.material == "ice" ? Balance.Site(s.site).iceBonus : 0)) + " " + Mission.MaterialName(sp.material, sp.amount) + (sp.partChance > 0 ? ", maybe a part" : "");
                    string text = "<b>" + SpotTitle(sp) + "</b>  ·  " + Mission.Hours(q) + " there and back  ·  " + rad + (knownStorm ? " (storm day!)" : "") +
                                  "\n<size=14>" + finds + ". " + sp.blurb + "</size>";
                    var b = UIKit.Button(list, text, () => { CloseModal(card); done(); GoOnTrip(sp.id); }, Color.white, 17);
                    b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                    UIKit.Size(b, -1, 74, -1, 76);
                    b.interactable = s.quartersLeft >= q;
                }
                UIKit.Button(card, "Go back inside", () => { CloseModal(card); done(); World.SetInside(true); }, Theme.Paper, 16);
            });
        }

        RectTransform MapView(RectTransform parent)
        {
            var frame = UIKit.Panel(parent, Theme.MarsDeep, "Map", true, 16);
            var art = Theme.Art(Mission.S.site == "jezero" ? "jezero_map" : "gale_map");
            var img = UIKit.Rect("Image", frame.transform);
            UIKit.Stretch(img, 6, 6, 6, 6);
            var i = img.gameObject.AddComponent<Image>();
            if (art) { i.sprite = art; i.preserveAspect = false; i.color = new Color(1f, 0.68f, 0.5f); } // orbital mosaic, tinted Mars-red
            else
            {
                i.sprite = Theme.Circle(128);
                i.color = Theme.Hex("#C8653A");
                var rim = UIKit.Rect("Floor", img);
                UIKit.Stretch(rim, 40, 40, 40, 40);
                var fi = rim.gameObject.AddComponent<Image>();
                fi.sprite = Theme.Circle(128);
                fi.color = Theme.Hex("#D9844F");
                var mound = UIKit.Rect("Mount Sharp", img);
                UIKit.Anchor(mound, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, -10), new Vector2(90, 70));
                var mi = mound.gameObject.AddComponent<Image>();
                mi.sprite = Theme.Circle(128);
                mi.color = Theme.Hex("#A9482A");
            }
            // Spot markers, rearranged each mission (seeded by the Classroom Code).
            var rnd = new System.Random(Mission.S.seed);
            // With the real Gale image, the base sits in Curiosity's landing ellipse (north-west crater floor).
            Vector2 basePos = art ? new Vector2(0.41f, 0.70f) : new Vector2(0.5f, 0.32f);
            Marker(img, basePos, "Base", Theme.Paper);
            // Spread the spots over the upper half of the crater, each at its own angle.
            var spots = new List<SpotDef>(Balance.spots);
            for (int k = spots.Count - 1; k > 0; k--) { int j = rnd.Next(k + 1); var t = spots[k]; spots[k] = spots[j]; spots[j] = t; }
            float offset = (float)(rnd.NextDouble() * 20 - 10);
            for (int k = 0; k < spots.Count; k++)
            {
                var spot = spots[k];
                float dist = spot.hours <= 2 ? 0.22f : 0.36f;
                float start = art ? 225 : 20, spread = art ? 110 : 140; // real map: fan out into the crater toward Mount Sharp
                float ang = (start + offset + k * spread / Mathf.Max(1, spots.Count - 1)) * Mathf.Deg2Rad;
                var pos = basePos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * (art ? 1f : 1.2f)) * dist;
                pos = new Vector2(Mathf.Clamp(pos.x, 0.1f, 0.9f), Mathf.Clamp(pos.y, 0.1f, 0.9f));
                bool open = Mission.SpotsToday().Contains(spot);
                if (open) Marker(img, pos, SpotTitle(spot), spot.material == "samples" ? Theme.Power : spot.material == "ice" ? Theme.Life : spot.material == "parts" ? Theme.Health : Theme.Sand);
            }
            var credit = UIKit.Label(frame.transform, art ? "Map: NASA/JPL-Caltech/ASU (Mars Odyssey, PIA14290)" : "Drawn map (no NASA image for this site)", 11, Theme.Paper, TextAnchor.LowerCenter);
            UIKit.Anchor(credit.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(320, 18));
            return frame.rectTransform;
        }

        static void Marker(RectTransform map, Vector2 pos, string name, Color c)
        {
            var dot = UIKit.Rect(name, map);
            dot.anchorMin = dot.anchorMax = pos;
            dot.sizeDelta = new Vector2(22, 22);
            var img = dot.gameObject.AddComponent<Image>();
            img.sprite = Theme.Circle(64, true);
            img.color = c;
            var t = UIKit.Label(dot, name, 12, Theme.Paper, TextAnchor.UpperCenter);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0);
            t.rectTransform.pivot = new Vector2(0.5f, 1);
            t.rectTransform.sizeDelta = new Vector2(120, 20);
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Theme.Ink;
        }

        void GoOnTrip(string spotId)
        {
            var r = Mission.StartTrip(spotId);
            if (!r.ok) { Toast(r.message); AudioFx.Error(); return; }
            AudioFx.Trip();
            AfterChange();
            var spot = Balance.Spot(spotId);
            Queue(done => ShowTripScene(spot, done)); // the place itself: real photo, a true fact, three finds
            Queue(done => ShowTripResult(r.message, done));
        }

        void ShowTripResult(string message, Action done)
        {
            var trip = Mission.S.trip;
            var card = Modal(620);
            UIKit.Label(card, SpotTitle(Balance.Spot(trip.spotId)), 28, Theme.Ink, TextAnchor.MiddleLeft, true);
            Origin(card, "TRIP RESULT", "What you found outside, and the radiation the crew took on the way.", false);
            UIKit.Label(card, message, 18);
            if (trip.warning)
            {
                string hz = trip.hazard == "storm" ? "The radio crackles: the Sun is acting up. A storm may be on the way!"
                    : trip.hazard == "dust_devil" ? "A dust devil is spinning across the plain toward you!"
                    : "Earth relays a flare warning from yesterday.";
                UIKit.Label(card, "<color=#B42318><b>" + hz + "</b></color>", 18);
                if (trip.hazard == "dust_devil")
                {
                    AudioFx.DustDevil();
                    UIKit.Label(card, "<size=13>You're hearing a real dust devil, recorded by NASA's Perseverance rover in 2021.</size>", 13, Theme.Muted);
                }
            }
            if (Mission.CanPush)
            {
                UIKit.Label(card, "Head home now, or grab one more and take the extra dose?", 17, Theme.Muted);
                var row = UIKit.Rect("Row", card);
                UIKit.HStack(row.gameObject, 10).childForceExpandWidth = true;
                UIKit.Size(row, -1, 66, -1, 66);
                UIKit.Button(row, "Head home now", () => { Mission.EndTrip(); CloseModal(card); done(); AfterChange(); }, Theme.Paper);
                string grab = "One more " + Mission.MaterialName(trip.material, 1) + "\n<size=14>" + Mission.Hours(Mission.PushQuarters) + " · −" + Mission.PushDose + " health</size>";
                UIKit.Button(row, grab, () =>
                {
                    var r = Mission.PushLuck();
                    Mission.EndTrip();
                    CloseModal(card);
                    done();
                    Toast(r.message);
                    AfterChange();
                }, trip.warning ? Theme.Warn : Theme.Power);
            }
            else UIKit.Button(card, "Head home", () => { Mission.EndTrip(); CloseModal(card); done(); AfterChange(); }, Theme.Power);
        }

        // ---------- morning briefing ----------

        void ShowBriefing(NightReport last, Action done)
        {
            skippingStory = false;
            if (Mission.S.over) { done(); return; }
            var s = Mission.S;
            var card = Modal(720, null, true);
            UIKit.Label(card, "Morning briefing: Sol " + s.sol, 30, Theme.Gold, TextAnchor.MiddleLeft, true);
            Origin(card, "MORNING BRIEFING", "Every sol starts with news from BOLT and Earth.", true);
            UIKit.Label(card, "Act " + Mission.Act + ": " + ActName(Mission.Act) + "   ·   " + (s.lastSol - s.sol + 1) + " sols until the next crew lands", 15, Theme.Hex("#D9CFE0"));

            var sun = UIKit.Panel(card, Theme.NightSoft, "Sun", false, 12);
            UIKit.VStack(sun.gameObject, 2, 12);
            UIKit.Label(sun.transform, Tag(LiveSun.Live ? "LIVE" : "REAL ARCHIVE") + "  <b>Today's Sun</b> (" + LiveSun.Source + ")", 14, Theme.Paper);
            UIKit.Label(sun.transform, LiveSun.Headline + " " + LiveSun.Detail, 15, Theme.Paper);

            var lines = new List<string>();
            lines.Add(Mission.InDustSeason(s.sol) ? "Dust: <b>HIGH</b>. It's dust season, so expect dusty panels." : "Dust: low.");
            if (s.forecastTonight == 2) lines.Add("The big dish caught a warning: <color=#FFD27A><b>storm likely tonight!</b></color>");
            else if (s.forecastTonight == 1) lines.Add("The big dish says tonight looks calm.");
            else lines.Add("Earth's message is waiting. Use the Radio (1 h) to hear tonight's forecast.");
            if (last != null && last.morningNews != null) foreach (var n in last.morningNews) lines.Add(n);
            foreach (var e in s.events)
            {
                var def = Balance.Event(e.id);
                lines.Add("! <b>" + def.name + "</b>: " + def.hint.ToLowerInvariant() + (e.expiresSol > 0 ? " (until sol " + e.expiresSol + ")" : "") + ".");
            }
            if (s.events.Count == 0) lines.Add("No problems right now. Enjoy it while it lasts.");
            UIKit.Label(card, "BOLT: " + string.Join("\n", lines), 17, Theme.Paper);
            if (s.sol == 1)
                UIKit.Label(card, "This mission replays " + (s.seasonReal ? "the real solar weather of " + s.seasonYear + " (" + s.seasonLabel.Split(' ')[0].ToLowerInvariant() + " season), squeezed into " + s.lastSol + " sols." : "a practice season."), 15, Theme.Gold);
            UIKit.Button(card, "Start sol " + s.sol, () => { CloseModal(card); done(); }, Theme.Gold, 20);
        }

        static string Tag(string text) => "<color=#FFD27A><b>[" + text + "]</b></color>";

        // A small line at the top of a popup saying where it comes from, in plain words.
        static void Origin(RectTransform card, string tag, string why, bool dark)
        {
            var t = UIKit.Label(card, "<b><color=" + (dark ? "#FFD27A" : "#B9502A") + ">" + tag + "</color></b>   " + why, 13, dark ? Theme.Soft : Theme.Muted);
            t.transform.SetAsFirstSibling();
        }

        // What radiation did while you slept, why, the running total, and what low health means.
        void RadiationPanel(RectTransform card, NightReport r, bool dark)
        {
            var s = Mission.S;
            bool shielded = Mission.Layout().IsShielded(Mission.Layout().First("hab"));
            var p = UIKit.Panel(card, dark ? Theme.NightSoft : Theme.Hex("#FFF0EC"), "Radiation", true, 12);
            UIKit.VStack(p.gameObject, 4, 14);
            Color ink = dark ? Theme.Paper : Theme.Ink;
            UIKit.Label(p.transform, "<color=#E5483B><b>RADIATION WHILE YOU SLEPT:  −" + lastNightDose + " crew health</b></color>", 18, ink);
            UIKit.Label(p.transform, "Mars has a thin atmosphere and no magnetic field, so space radiation reaches the base every night. " +
                (shielded ? "Your shield wall cut the normal dose in half." : "A shield wall touching the hab would cut the normal dose in half.") +
                (r.storm ? (r.shelterSealed ? " The solar storm added " + r.stormDamage + ", but the sealed shelter blocked most of it." :
                                              " The solar storm added " + r.stormDamage + " because nobody was sealed in the shelter.") : ""), 15, ink);
            UIKit.Label(p.transform, "<b>Radiation so far this mission: " + s.totalDose + "</b>   ·   Crew health " + s.health + (s.healthCap < 100 ? " (max " + s.healthCap + ")" : ""), 15, ink);
            UIKit.Label(p.transform, "If crew health reaches 0, the crew goes home early. Keep every meter at " + Balance.endings.healthyMeter + " or more for the best ending.", 14, dark ? Theme.Soft : Theme.Muted);
        }

        // ---------- night report ----------

        void ShowNightReport(NightReport r, Action done)
        {
            var card = Modal(760, null, r.storm);
            Color ink = r.storm ? Theme.Paper : Theme.Ink;
            UIKit.Label(card, r.storm ? "Stormy night (sol " + r.sol + ")" : "Night report (sol " + r.sol + ")", 30, r.storm ? Theme.Gold : Theme.Ink, TextAnchor.MiddleLeft, true);
            Origin(card, "WHILE YOU SLEPT", "This report comes every time you end a sol: what changed overnight, and why.", r.storm);
            RadiationPanel(card, r, r.storm);
            foreach (var l in r.lines)
            {
                string name = l.meter == "power" ? "Power" : l.meter == "life" ? "Life support" : "Crew health";
                string arrow = l.delta > 0 ? "<color=#3FAE5A>▲ +" + l.delta + "</color>" : l.delta < 0 ? "<color=#E5483B>▼ " + l.delta + "</color>" : "■ 0";
                UIKit.Label(card, "<b>" + name + " " + arrow + "</b>  <size=15>" + l.reason + ".</size>", 18, ink);
            }
            if (Mission.Mode.realUnits && Data.radiation != null)
                UIKit.Label(card, "<size=14>Normal radiation at Gale is about " + Data.radiation.baselineDoseEquivalent_mSvPerSol.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                                  " mSv a sol, about " + Mathf.RoundToInt(Data.radiation.baselineDoseEquivalent_mSvPerSol / Data.radiation.chestXray_mSv) + " chest X-rays (Curiosity RAD, published average).</size>", 14, ink);

            if (r.storm) StormCard(card, r);
            if (!string.IsNullOrEmpty(r.factId)) FactCardView(card, r.factId);
            UIKit.Button(card, Mission.S.over ? "See what happened" : "Start sol " + Mission.S.sol, () => { CloseModal(card); done(); }, Theme.Gold, 20);
            if (r.storm) World.Shake();
        }

        void StormCard(RectTransform card, NightReport r)
        {
            var p = UIKit.Panel(card, Theme.NightSoft, "StormCard", true, 12);
            UIKit.VStack(p.gameObject, 4, 14);
            var info = r.stormInfo;
            bool real = info != null && info.record != null && !string.IsNullOrEmpty(info.record.id);
            UIKit.Label(p.transform, Tag("REAL ARCHIVE") + "  <b>Solar storm!</b>", 17, Theme.Paper);
            if (real)
            {
                var rec = info.record;
                string flare = string.IsNullOrEmpty(rec.flareClass) ? "" : " It followed a class " + rec.flareClass + " solar flare.";
                string where = rec.marsRelated ? " Instruments near Mars saw it too." : " It was measured from near Earth; the Sun sends storms like it toward Mars too.";
                UIKit.Label(p.transform, "This storm is based on a real solar particle event on <b>" + StormPlanner.NiceDate(rec.date) + "</b>." + flare + where, 16, Theme.Paper);
                UIKit.Label(p.transform, "<size=13>Source: NASA DONKI, event " + rec.id + (string.IsNullOrEmpty(rec.instruments) ? "" : " (" + rec.instruments + ")") + "</size>", 13, Theme.Hex("#D9CFE0"));
            }
            else UIKit.Label(p.transform, "A practice storm (no archive loaded).", 16, Theme.Paper);
            UIKit.Label(p.transform, r.shelterSealed ? "Your sealed shelter blocked most of the particles (−" + r.stormDamage + " health)." : "Nobody was in a sealed shelter, so the crew took " + r.stormDamage + " damage.", 16, Theme.Gold);
        }

        void FactCardView(RectTransform card, string factId)
        {
            string title = null, text = null, source = null;
            foreach (var f in Data.staticFacts) if (f.id == factId) { title = f.title; text = f.text; source = f.source; }
            if (Data.osdr?.facts != null)
                foreach (var f in Data.osdr.facts)
                    if (f.id == factId) { title = f.title; text = f.text; source = "NASA OSDR study " + f.id + ": \"" + f.studyTitle + "\""; }
            if (title == null) return;
            var p = UIKit.Panel(card, Theme.Night, "Fact", true, 12);
            UIKit.VStack(p.gameObject, 4, 14);
            UIKit.Label(p.transform, "<color=#FFD27A><b>New fact card: " + title + "</b></color>   <size=12>You found it tonight. It's saved in your Logbook.</size>", 17, Theme.Paper);
            UIKit.Label(p.transform, text, 15, Theme.Paper);
            UIKit.Label(p.transform, "<size=12>" + source + "</size>", 12, Theme.Hex("#D9CFE0"));
        }

        // ---------- story ----------

        void ShowDialogue(StoryLine line, Action done)
        {
            var terms = new List<string>();
            string text = StoryDirector.Format(line.text, Mission.S, terms);
            foreach (var t in terms)
            {
                var term = Data.story.TermFor(t);
                if (term != null) text = text.Replace(t == "sep" ? "SEP" : term.word, "<color=#FFD27A><b>" + (t == "sep" ? "SEP" : term.word) + "</b></color>");
            }
            text = Keywords.Highlight(text); // colour-code meters, dangers, supplies, places and keys
            var dim = UIKit.Fill(modalLayer, new Color(0, 0, 0, 0.25f), "Dialogue");
            UIKit.Stretch(dim.rectTransform);
            var p = UIKit.Panel(dim.transform, Theme.Night, "Box");
            UIKit.Anchor(p.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(860, 150));
            var portrait = UIKit.Panel(p.transform, SpeakerColor(line.speaker), "Portrait", true, 40);
            UIKit.Anchor(portrait.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(100, 100));
            var boltArt = line.speaker == "bolt" ? Theme.Art("bolt") : null;
            var crewArt = line.speaker == "mira" || line.speaker == "tomas" ? Theme.Art("crew_" + line.speaker) : null;
            if (boltArt)
            {
                // BOLT's portrait is the mascot itself.
                var face = UIKit.Rect("Bolt", portrait.transform);
                UIKit.Stretch(face, 6, 6, 6, 6);
                var fi = face.gameObject.AddComponent<Image>();
                fi.sprite = boltArt;
                fi.preserveAspect = true;
                fi.raycastTarget = false;
                face.gameObject.AddComponent<Bob>();
            }
            else if (crewArt)
            {
                // The crew portrait: their drawn character, framed on the face.
                portrait.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var face = UIKit.Rect("Face", portrait.transform);
                UIKit.Anchor(face, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(190, 237));
                var fi = face.gameObject.AddComponent<Image>();
                fi.sprite = crewArt;
                fi.preserveAspect = true;
                fi.raycastTarget = false;
            }
            else
            {
                var initial = UIKit.Label(portrait.transform, Data.story.SpeakerName(line.speaker).Substring(0, 1), 46, Theme.Ink, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(initial.rectTransform);
            }
            var name = UIKit.Label(p.transform, line.speaker == "you" ? Mission.S.commanderName : Data.story.SpeakerName(line.speaker), 20, Theme.Gold, TextAnchor.UpperLeft, true);
            UIKit.Stretch(name.rectTransform, 132, 100, 16, 10);
            var body = UIKit.Label(p.transform, text, 19, Theme.Paper, TextAnchor.UpperLeft);
            UIKit.Stretch(body.rectTransform, 132, 40, 20, 42);
            var next = UIKit.Button(p.transform, "Next", null, Theme.Gold, 16);
            UIKit.Anchor((RectTransform)next.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 10), new Vector2(110, 40));
            var skip = UIKit.Button(p.transform, "Skip story", null, Theme.Paper, 13);
            UIKit.Anchor((RectTransform)skip.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-132, 10), new Vector2(110, 40));
            var newTerms = StoryDirector.NewTerms(Data.story, Mission.S, terms);
            next.onClick.AddListener(() =>
            {
                AudioFx.Click();
                Destroy(dim.gameObject);
                foreach (var t in newTerms) { var tt = t; Queue(d => ShowTerm(tt, d)); }
                done();
            });
            skip.onClick.AddListener(() =>
            {
                Destroy(dim.gameObject);
                // Drop the rest of the queued story lines; briefings and reports still show.
                skippingStory = true;
                done();
            });
        }

        bool skippingStory;

        void ShowTerm(Term t, Action done)
        {
            var card = Modal(520, null, true);
            UIKit.Label(card, "New word: " + t.word, 24, Theme.Gold, TextAnchor.MiddleLeft, true);
            Origin(card, "NEW WORD", "BOLT explains each new word the first time it comes up.", true);
            UIKit.Label(card, "BOLT: " + t.explain, 18, Theme.Paper);
            UIKit.Button(card, "Got it", () => { CloseModal(card); done(); }, Theme.Gold);
        }

        static Color SpeakerColor(string s) =>
            s == "bolt" ? Theme.Hex("#9FB7C9") : s == "mira" ? Theme.Health : s == "tomas" ? Theme.Life : s == "earth" ? Theme.Gold : Theme.Paper;

        void ShowCodeCard(Action done)
        {
            var s = Mission.S;
            var card = Modal(620);
            UIKit.Label(card, "Mission start", 28, Theme.Ink, TextAnchor.MiddleLeft, true);
            Origin(card, "MISSION START", "Shown once, when a new mission begins.", false);
            UIKit.Label(card, "Classroom Code: <b>" + s.classroomCode + "</b>\n<size=15>Anyone who types this code gets the same storms, events and map. Compare your endings!</size>", 18);
            var t1 = Balance.Trait(s.traits[0]);
            var t2 = Balance.Trait(s.traits[1]);
            UIKit.Label(card, "Your crew this time:\n• <b>Mira</b> (botanist): " + t1.name + ". " + t1.text + ".\n• <b>Tomas</b> (geologist): " + t2.name + ". " + t2.text + ".", 17);
            UIKit.Button(card, "Let's go", () => { CloseModal(card); done(); }, Theme.Power);
        }
    }
}

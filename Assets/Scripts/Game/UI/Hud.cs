using System.Collections.Generic;
using OutpostGale.Sim;
using OutpostGale.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OutpostGale
{
    // The HUD (1280x720 reference): meters top-left, sol and hours top-centre, inventory top-right,
    // BOLT bottom-left, action bar bottom-centre, Build / Logbook / End sol bottom-right.
    public partial class GameManager
    {
        class HudRefs
        {
            public readonly Dictionary<string, Image> bars = new Dictionary<string, Image>();
            public readonly Dictionary<string, Text> values = new Dictionary<string, Text>();
            public readonly Dictionary<string, Text> inv = new Dictionary<string, Text>();
            public readonly Dictionary<string, Text> captions = new Dictionary<string, Text>();
            public Text sol, act, hoursText, bolt, prompt, season, buildBar;
            public GameObject radioDot;
            public Image[] pips;
            public RectTransform events, promptBox, buildBox;
            public Button endSol, build, radioBtn, insideBtn;
            public bool insideShown;
            public RectTransform boltBox, healthBar;
            public int lastHealth = -1, lastDose;
            public readonly List<Button> actionButtons = new List<Button>();
        }

        HudRefs hud;

        void BuildHud()
        {
            UIKit.Clear(hudLayer);
            hud = new HudRefs();

            // ---- top: one slim row of glass pills (meters, sol clock, supplies) ----
            var meters = Pill("Meters", 0f, 0.42f);
            var mh = UIKit.HStack(meters.gameObject, 14, 0);
            mh.padding = new RectOffset(16, 16, 9, 8);
            mh.childForceExpandWidth = true;
            mh.childForceExpandHeight = true;
            foreach (var m in new[] { "power", "life", "health" }) MeterWidget(meters, m);

            var mid = Pill("Sol", 0.43f, 0.57f);
            hud.sol = UIKit.Label(mid, "1", 22, Theme.Paper, TextAnchor.UpperCenter, true);
            UIKit.Stretch(hud.sol.rectTransform, 0, 22, 0, 5);
            var pipRow = UIKit.Rect("Hours", mid);
            UIKit.Anchor(pipRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 9), new Vector2(170, 12));
            UIKit.HStack(pipRow.gameObject, 3, 0, TextAnchor.MiddleCenter);
            hud.pips = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                var pip = UIKit.Rect("Pip", pipRow);
                var img = pip.gameObject.AddComponent<Image>();
                img.sprite = Theme.Circle(32);
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                UIKit.Size(img, 11, 11, 11, 11);
                hud.pips[i] = img;
            }
            hud.hoursText = UIKit.Label(pipRow, "", 11, Theme.Soft, TextAnchor.MiddleLeft);
            UIKit.Size(hud.hoursText, 44, 12, 44, 12);

            var right = Pill("Supplies", 0.58f, 1f);
            var chips = UIKit.Rect("Chips", right);
            UIKit.Stretch(chips, 12, 9, 12, 9);
            var ch = UIKit.HStack(chips.gameObject, 8, 0, TextAnchor.MiddleCenter);
            ch.childForceExpandWidth = true;
            ch.childForceExpandHeight = true;
            Chip(chips, "regolith", "Regolith", Theme.Hex("#E08A5A"));
            Chip(chips, "ice", "Water", Theme.Hex("#8FD3FF"));
            Chip(chips, "parts", "Parts", Theme.Hex("#B8C2D9"));
            Chip(chips, "samples", "Samples", Theme.Gold);

            // ---- events list (under the meters) ----
            hud.events = UIKit.Rect("Events", hudLayer);
            UIKit.Anchor(hud.events, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -82), new Vector2(330, 10));
            UIKit.VStack(hud.events.gameObject, 8);
            UIKit.Fit(hud.events.gameObject);

            // ---- BOLT, bottom-left: a small speech bubble from the base robot ----
            var boltPanel = UIKit.Panel(hudLayer, Theme.Glass, "Bolt", true, 20);
            hud.boltBox = boltPanel.rectTransform;
            UIKit.Anchor(boltPanel.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(292, 76));
            var face = UIKit.Rect("Face", boltPanel.transform);
            UIKit.Anchor(face, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(52, 52));
            var faceBg = face.gameObject.AddComponent<Image>();
            faceBg.sprite = Theme.Circle(128);
            faceBg.color = Theme.Hex("#BFD4E6");
            var boltArt = Theme.Art("bolt");
            var eye = UIKit.Rect("Eye", face);
            var eyeImg = eye.gameObject.AddComponent<Image>();
            eyeImg.sprite = boltArt ? boltArt : Theme.Circle(32);
            eyeImg.preserveAspect = true;
            eyeImg.color = boltArt ? Color.white : Theme.Power;
            UIKit.Anchor(eye, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, boltArt ? new Vector2(40, 40) : new Vector2(20, 20));
            var who = UIKit.Label(boltPanel.transform, "BOLT", 11, Theme.Gold, TextAnchor.UpperLeft);
            who.font = Theme.BodyBlack;
            UIKit.Stretch(who.rectTransform, 72, 0, 12, 8);
            hud.bolt = UIKit.Label(boltPanel.transform, "", 13, Theme.Paper, TextAnchor.MiddleLeft);
            UIKit.Stretch(hud.bolt.rectTransform, 72, 6, 12, 22);

            // ---- one dock, bottom-centre: the four quick jobs and the map / inside switch ----
            var dock = UIKit.Rect("Dock", hudLayer);
            UIKit.Anchor(dock, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-4, 16), new Vector2(424, 50));
            var dh = UIKit.HStack(dock.gameObject, 8, 0, TextAnchor.MiddleCenter);
            dh.childForceExpandWidth = true;
            AddActionButton(dock, "1", "Rest", "hab", "rest");
            AddActionButton(dock, "2", "Radio", "hab", "radio");
            AddActionButton(dock, "3", "Seal", "shelter", "seal");
            AddActionButton(dock, "4", "Outside", "airlock", "outside");
            hud.radioBtn = hud.actionButtons[1];
            var dot = UIKit.Rect("New", hud.radioBtn.transform);
            UIKit.Anchor(dot, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-6, -4), new Vector2(14, 14));
            var dotImg = dot.gameObject.AddComponent<Image>();
            dotImg.sprite = Theme.Circle(32);
            dotImg.color = Theme.Warn;
            dotImg.raycastTarget = false;
            dot.gameObject.AddComponent<Pulse>();
            hud.radioDot = dot.gameObject;
            hud.insideBtn = UIKit.Button(dock, "Map", ToggleInside, Theme.Life, 14);
            UIKit.Size(hud.insideBtn, 76, 48, 80, 48);
            UIKit.KeyBadge(hud.insideBtn, "I");
            hud.insideShown = !World.Inside; // force the first label update

            // ---- right: Build, Logbook and Menu in one row, Sleep under it ----
            var rightCol = UIKit.Rect("Buttons", hudLayer);
            UIKit.Anchor(rightCol, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 16), new Vector2(236, 104));
            UIKit.VStack(rightCol.gameObject, 8);
            var row = UIKit.Rect("Row", rightCol);
            var rh = UIKit.HStack(row.gameObject, 8);
            rh.childForceExpandWidth = false;
            UIKit.Size(row, -1, 48, -1, 48);
            hud.build = UIKit.Button(row, "Build", OpenBuildMenu, Theme.Power, 16);
            UIKit.Size(hud.build, 100, 48, 124, 48, 1);
            UIKit.Icon(hud.build, "icon_build");
            UIKit.KeyBadge(hud.build, "B");
            IconButton(row, "icon_logbook", "Logbook", () => ShowLogbook(null));
            IconButton(row, "icon_menu", "Menu", ShowPause);
            hud.endSol = UIKit.Button(rightCol, "Sleep  ·  end the sol", EndSolPressed, Theme.Hex("#4B3C8F"), 16);
            UIKit.Size(hud.endSol, -1, 48, -1, 48);

            // ---- floating "E" prompt ----
            var promptBg = UIKit.Panel(hudLayer, Theme.Glass, "Prompt", true, 14);
            hud.promptBox = promptBg.rectTransform;
            hud.promptBox.anchorMin = hud.promptBox.anchorMax = Vector2.zero;
            hud.promptBox.pivot = new Vector2(0.5f, 0);
            hud.promptBox.sizeDelta = new Vector2(250, 32);
            hud.prompt = UIKit.Label(promptBg.transform, "", 13, Theme.Paper, TextAnchor.MiddleCenter);
            UIKit.Stretch(hud.prompt.rectTransform, 10, 2, 10, 2);

            // ---- build placement bar ----
            var bb = UIKit.Panel(hudLayer, Theme.Glass, "BuildBar", true, 22);
            hud.buildBox = bb.rectTransform;
            UIKit.Anchor(hud.buildBox, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 78), new Vector2(640, 62));
            hud.buildBar = UIKit.Label(bb.transform, "", 15, Theme.Paper, TextAnchor.MiddleLeft);
            UIKit.Stretch(hud.buildBar.rectTransform, 18, 4, 128, 4);
            var cancel = UIKit.Button(bb.transform, "Cancel", CancelBuild, Theme.Paper, 15);
            UIKit.Anchor((RectTransform)cancel.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 2), new Vector2(104, 42));
            hud.buildBox.gameObject.SetActive(false);

            RefreshHud();
        }

        // A square key with just an icon (its name shows if the art is missing).
        void IconButton(Transform parent, string art, string fallback, System.Action onClick)
        {
            var sprite = Theme.Art(art);
            var b = UIKit.Button(parent, sprite ? "" : fallback, onClick, Theme.Paper, 14);
            UIKit.Size(b, 48, 48, 48, 48);
            if (!sprite) return;
            var rt = UIKit.Rect("Icon", b.transform);
            UIKit.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(22, 22));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Theme.Ink;
            img.raycastTarget = false;
        }

        // A glass panel across the top, between two horizontal anchors.
        RectTransform Pill(string name, float x0, float x1)
        {
            var p = UIKit.Panel(hudLayer, Theme.Glass, name, true, 20);
            var rt = p.rectTransform;
            rt.anchorMin = new Vector2(x0, 1);
            rt.anchorMax = new Vector2(x1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(x0 == 0 ? 12 : 0, -72);
            rt.offsetMax = new Vector2(x1 == 1 ? -12 : 0, -8);
            return rt;
        }

        void Chip(RectTransform parent, string id, string label, Color color)
        {
            var chip = UIKit.Panel(parent, new Color(1, 1, 1, 0.07f), id, false, 12);
            var dot = UIKit.Rect("Dot", chip.transform);
            UIKit.Anchor(dot, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(10, 10));
            var di = dot.gameObject.AddComponent<Image>();
            di.sprite = Theme.Circle(32);
            di.color = color;
            var name = UIKit.Label(chip.transform, label, 11, Theme.Soft, TextAnchor.UpperLeft);
            UIKit.Stretch(name.rectTransform, 23, 0, 4, 2);
            var count = UIKit.Label(chip.transform, "0", 17, Theme.Paper, TextAnchor.LowerLeft, true);
            UIKit.Stretch(count.rectTransform, 23, -2, 4, 0);
            hud.inv[id] = count;
        }

        void MeterWidget(RectTransform parent, string meter)
        {
            var col = UIKit.Rect(meter, parent);
            var head = UIKit.Rect("Head", col);
            head.anchorMin = new Vector2(0, 1); head.anchorMax = new Vector2(1, 1);
            head.pivot = new Vector2(0.5f, 1);
            head.offsetMin = new Vector2(0, -18); head.offsetMax = Vector2.zero;
            var dot = UIKit.Rect("Dot", head);
            UIKit.Anchor(dot, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(10, 10));
            var di = dot.gameObject.AddComponent<Image>();
            di.sprite = Theme.Circle(32);
            di.color = Theme.MeterColor(meter);
            string label = meter == "power" ? "Power" : meter == "life" ? "Life support" : "Health";
            var name = UIKit.Label(head, label, 13, Theme.Paper);
            name.font = Theme.BodyBlack;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIKit.Stretch(name.rectTransform, 15, 0, 0, 0);
            // "max N" when radiation has lowered the health cap (empty otherwise).
            var cap = UIKit.Label(head, "", 10, Theme.Soft, TextAnchor.MiddleRight);
            UIKit.Stretch(cap.rectTransform);
            hud.captions[meter] = cap;

            var barBg = UIKit.Panel(col, Theme.Hex("#120D26"), "Bar", false, 9);
            var brt = barBg.rectTransform;
            brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.offsetMin = new Vector2(0, -44); brt.offsetMax = new Vector2(0, -22);
            if (meter == "health") hud.healthBar = brt;
            var fill = UIKit.Rect("Fill", barBg.transform);
            var img = fill.gameObject.AddComponent<Image>();
            img.sprite = Theme.Key(10);
            img.type = Image.Type.Sliced;
            img.color = Theme.MeterColor(meter);
            fill.anchorMin = new Vector2(0, 0);
            fill.anchorMax = new Vector2(0.5f, 1);
            fill.offsetMin = new Vector2(3, 3);
            fill.offsetMax = new Vector2(-1, -3);
            hud.bars[meter] = img;
            var val = UIKit.Label(barBg.transform, "", 12, Theme.Paper, TextAnchor.MiddleRight);
            val.font = Theme.BodyBlack;
            var sh = val.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.55f);
            sh.effectDistance = new Vector2(0, -1);
            UIKit.Stretch(val.rectTransform, 6, 0, 8, 0);
            hud.values[meter] = val;
        }

        void AddActionButton(RectTransform bar, string key, string text, string station, string action)
        {
            var b = UIKit.Button(bar, text, () => QuickAction(station, action), Theme.Paper, 14);
            UIKit.Size(b, 74, 48, 80, 48);
            UIKit.KeyBadge(b, key);
            hud.actionButtons.Add(b);
        }

        // When health changes, flash its bar and float the change (and whether radiation caused it).
        void HealthChangeFx(MissionState s)
        {
            if (hud.lastHealth >= 0 && s.health != hud.lastHealth && hud.healthBar)
            {
                int change = s.health - hud.lastHealth;
                int dose = s.totalDose - hud.lastDose;
                string text = change < 0 ? change + " health" + (dose > 0 ? " · radiation" : "") : "+" + change + " health";
                var pop = UIKit.Label(hudLayer, text, 15, change < 0 ? Theme.Warn : Theme.Health, TextAnchor.MiddleCenter, true);
                var rt = pop.rectTransform;
                rt.sizeDelta = new Vector2(220, 26);
                rt.position = hud.healthBar.TransformPoint(new Vector3(hud.healthBar.rect.center.x, hud.healthBar.rect.yMin - 20, 0));
                var sh = pop.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.7f);
                pop.gameObject.AddComponent<FloatAway>();
                var flash = hud.healthBar.gameObject.GetComponent<FlashImage>();
                if (!flash) flash = hud.healthBar.gameObject.AddComponent<FlashImage>(); // (Unity objects: no ?? operator)
                flash.Play(change < 0 ? Theme.Warn : Theme.Health);
                if (change < 0 && dose > 0) AudioFx.Error();
            }
            hud.lastHealth = s.health;
            hud.lastDose = s.totalDose;
        }

        static string MeterWord(int v) => v < 25 ? "LOW!" : v < 50 ? "OK" : v < 75 ? "GOOD" : "GREAT";

        void RefreshHud()
        {
            if (hud == null || Mission == null) return;
            var s = Mission.S;
            bool commander = Mission.Mode.realUnits;
            foreach (var m in new[] { "power", "life", "health" })
            {
                int v = s.Meter(m);
                var fill = hud.bars[m];
                var rt = fill.rectTransform;
                rt.anchorMax = new Vector2(Mathf.Clamp(v / 100f, 0.03f, 1f), 1);
                fill.color = v < 25 ? Theme.Warn : Theme.MeterColor(m);
                // Colour is never the only signal: words or numbers too, and a "!" when low.
                hud.values[m].text = commander ? v + (v < 25 ? " !" : "") : MeterWord(v) + "  " + v;
                // Health shows the radiation taken so far, and its lowered max once storms have hit.
                hud.captions[m].text = m != "health" ? "" :
                    (s.totalDose > 0 ? "<color=#FF8A80>radiation " + s.totalDose + "</color>" : "") +
                    (s.healthCap < 100 ? "<color=#FF8A80>  max " + s.healthCap + "</color>" : "");
            }
            hud.sol.text = "<size=12><color=#C9BEE0>SOL </color></size>" + Mathf.Min(s.sol, s.lastSol) + "<size=14><color=#C9BEE0> / " + s.lastSol + "</color></size>";
            float hours = s.quartersLeft / 4f;
            for (int i = 0; i < 8; i++)
            {
                float f = Mathf.Clamp01(hours - i);
                hud.pips[i].fillAmount = f > 0 ? f : 1;
                hud.pips[i].color = f > 0 ? Theme.Power : new Color(1f, 1f, 1f, 0.16f);
            }
            hud.hoursText.text = Mission.Hours(s.quartersLeft) + " left";
            hud.inv["regolith"].text = s.regolith.ToString();
            hud.inv["ice"].text = s.ice.ToString();
            hud.inv["parts"].text = s.parts.ToString();
            hud.inv["samples"].text = s.samples.ToString();

            UIKit.Clear(hud.events);
            foreach (var e in s.events)
            {
                var def = Balance.Event(e.id);
                var ev = e;
                var b = UIKit.Button(hud.events, def.name + "  ·  " + def.hint.ToLowerInvariant() + (e.expiresSol > 0 ? "  ·  until sol " + e.expiresSol : ""), () => GoFixEvent(ev), Theme.Hex("#FFE6DF"), 14);
                b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                UIKit.Icon(b, "icon_warning", Theme.Warn);
                UIKit.Size(b, -1, 44, -1, 44);
            }

            hud.bolt.text = Keywords.Highlight(BoltHint());
            HealthChangeFx(s);
            hud.radioDot.SetActive(!s.radioUsed);
            hud.radioBtn.interactable = !s.radioUsed;
            hud.endSol.interactable = !s.over;
        }

        public static string ActName(int act) => act == 1 ? "Touchdown" : act == 2 ? "Dust Season" : act == 3 ? "Solar Maximum" : "Arrival";

        string BoltHint()
        {
            var s = Mission.S;
            if (World.ghostModule != null) return "Pick a spot for it. Green means OK. Then walk there and work to build it.";
            if (s.forecastTonight == 2 && !s.sealedTonight) return "Earth says a storm is coming tonight! Seal the Storm Shelter (3), or the crew takes a big dose.";
            if (s.forecastTonight == 3 && !s.sealedTonight) return "Earth's warning came too late to tell. Seal the shelter just in case (uses power), or take the chance?";
            if (s.forecastTonight == 1 && s.sealedTonight) return "Earth said tonight looks calm. The sealed shelter will still use power.";
            if (s.life < 30) return "Life Support is low! An oxygen maker, a greenhouse or fixing leaks will help.";
            if (s.health < 35) return "The crew needs rest (1). Fewer trips outside for a while?";
            if (s.health < Balance.endings.healthyMeter) return "Crew health is under " + Balance.endings.healthyMeter + ". Keep it at " + Balance.endings.healthyMeter + " or more for the best ending: rest (1), and seal the shelter on storm nights.";
            if (s.power < 25) return "Power is low. Machines stop if it hits zero. Fewer machines, or more panels?";
            if (!s.radioUsed) return "No word from Earth yet today. Use the Radio (2) to hear tonight's forecast.";
            foreach (var m in s.modules) if (!m.built) return "Your " + Balance.Module(m.id).name.ToLowerInvariant() + " needs work. Click the site to build.";
            if (s.quartersLeft < 4) return "Out of daylight. Time to sleep and end the sol.";
            if (Mission.Act >= 3 && !Mission.HasBuilt("pad") && s.unlocked.Contains("pad")) return "The next crew needs a landing pad (8 regolith). Trips to the sand dunes get regolith.";
            return "Click a room to walk there and use it. Press I to switch between the map and inside. Go outside through the Airlock (4) to gather materials.";
        }

        void OutpostUpdate()
        {
            if (hud == null || Mission == null) return;
            // Floating prompt above the station the player is near.
            if (World.Inside)
            {
                string kioskPrompt = null, spotAction = null;
                PlacedModule spotModule = null;
                bool show = WorldInputOpen && World.InsideSpot(out kioskPrompt, out spotModule, out spotAction);
                if (show)
                {
                    string text = kioskPrompt;
                    if (spotModule != null)
                    {
                        var acts = Mission.ActionsAt(spotModule);
                        var a = spotAction != null ? acts.Find(x => x.id == spotAction) : acts.Find(x => x.enabled);
                        string name = spotModule.built ? Balance.Module(spotModule.id).name : "Site: " + Balance.Module(spotModule.id).name;
                        text = a != null ? "E / tap: " + a.label + " · " + Mission.Hours(a.quarters) + (a.enabled ? "" : " (not now)") : name;
                    }
                    hud.prompt.text = text;
                    hud.promptBox.anchoredPosition = (Vector2)World.InsideScreenPos() / canvas.scaleFactor;
                }
                hud.promptBox.gameObject.SetActive(show);
            }
            else
            {
            var near = WorldInputOpen ? World.StationNearPlayer() : null;
            if (near != null && World.ghostModule == null)
            {
                var acts = Mission.ActionsAt(near);
                var first = acts.Find(a => a.enabled);
                string name = near.built ? Balance.Module(near.id).name : "Site: " + Balance.Module(near.id).name;
                hud.prompt.text = first != null ? "E / tap: " + first.label + " · " + Mission.Hours(first.quarters) : name;
                Vector2 screen = World.StationScreenPos(near);
                float scale = canvas.scaleFactor;
                hud.promptBox.anchoredPosition = screen / scale;
                hud.promptBox.gameObject.SetActive(true);
            }
            else hud.promptBox.gameObject.SetActive(false);
            }

            // Narrow windows: lift BOLT's bubble above the dock so they don't overlap.
            float hudWidth = ((RectTransform)hudLayer).rect.width;
            hud.boltBox.anchoredPosition = new Vector2(12, hudWidth < 1180 ? 76 : 12);

            if (hud.insideShown != World.Inside)
            {
                hud.insideShown = World.Inside;
                UIKit.SetLabel(hud.insideBtn, World.Inside ? "Map" : "Inside");
                UIKit.Tint(hud.insideBtn, World.Inside ? Theme.Mars : Theme.Life);
            }
            hud.buildBox.gameObject.SetActive(World.ghostModule != null);
            if (World.ghostModule != null)
            {
                var c = World.ghostCheck;
                hud.buildBar.text = "Placing: <b>" + Balance.Module(World.ghostModule).name + "</b>   " +
                                    (c == null ? "Point at the map" : (c.ok ? "<color=#7CE29A>" : "<color=#FF8A80>") + c.reason + "</color>") +
                                    "\n<size=13><color=#C9BEE0>Click to place · R or right-click to rotate · Esc to cancel</color></size>";
            }

            if (!WorldInputOpen) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) QuickAction("hab", "rest");
            if (kb.digit2Key.wasPressedThisFrame) QuickAction("hab", "radio");
            if (kb.digit3Key.wasPressedThisFrame) QuickAction("shelter", "seal");
            if (kb.digit4Key.wasPressedThisFrame) QuickAction("airlock", "outside");
            if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) ToggleInside();
            if (kb.bKey.wasPressedThisFrame && World.ghostModule == null) OpenBuildMenu();
            if (kb.escapeKey.wasPressedThisFrame && World.ghostModule == null) ShowPause();
        }

        public void OnGhostChanged() { }

        // Switch between the map (outside, top-down) and the inside of the base (side-on rooms).
        public void ToggleInside()
        {
            if (!WorldInputOpen) return;
            World.SetInside(!World.Inside);
        }

        void EndSolPressed()
        {
            if (!WorldInputOpen && World.ghostModule == null) return;
            if (Mission.S.quartersLeft >= 8)
            {
                Queue(done =>
                {
                    var card = Modal(520);
                    UIKit.Label(card, "End sol " + Mission.S.sol + "?", 28, Theme.Ink, TextAnchor.MiddleLeft, true);
                    UIKit.Label(card, "You still have " + Mission.Hours(Mission.S.quartersLeft) + " of daylight. Unused hours are lost.", 18);
                    var row = UIKit.Rect("Row", card);
                    UIKit.HStack(row.gameObject, 10).childForceExpandWidth = true;
                    UIKit.Button(row, "Keep working", () => { CloseModal(card); done(); });
                    UIKit.Button(row, "Sleep now", () => { CloseModal(card); done(); EndSol(); }, Theme.Hex("#4B3C8F"));
                });
                return;
            }
            EndSol();
        }
    }

    // A floating label that drifts down a little and fades out.
    public class FloatAway : MonoBehaviour
    {
        float t;
        void Update()
        {
            t += Time.unscaledDeltaTime;
            transform.localPosition += new Vector3(0, -14f * Time.unscaledDeltaTime, 0);
            var txt = GetComponent<UnityEngine.UI.Text>();
            if (txt) { var c = txt.color; c.a = Mathf.Clamp01(2.2f - t); txt.color = c; }
            if (t > 2.2f) Destroy(gameObject);
        }
    }

    // Flashes an Image's colour, then returns it to normal.
    public class FlashImage : MonoBehaviour
    {
        UnityEngine.UI.Image img;
        Color baseColor, flash;
        float t = 1;
        public void Play(Color c)
        {
            if (!img) { img = GetComponent<UnityEngine.UI.Image>(); baseColor = img ? img.color : Color.white; }
            flash = c;
            t = 0;
        }
        void Update()
        {
            if (!img || t >= 1) return;
            t += Time.unscaledDeltaTime / 0.9f;
            float k = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3)) * (1 - t);
            img.color = Color.Lerp(baseColor, flash, k);
            if (t >= 1) img.color = baseColor;
        }
    }
}

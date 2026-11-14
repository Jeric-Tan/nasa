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
            public Text sol, act, hoursText, inventory, bolt, prompt, season, buildBar;
            public Image[] pips;
            public RectTransform events, promptBox, buildBox;
            public Button endSol, build, radioBtn;
            public Text radioBadge;
            public readonly List<Button> actionButtons = new List<Button>();
        }

        HudRefs hud;

        void BuildHud()
        {
            UIKit.Clear(hudLayer);
            hud = new HudRefs();

            // ---- top band ----
            var top = UIKit.Panel(hudLayer, Theme.Night, "TopBar");
            UIKit.Anchor(top.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(1264, 96));
            top.rectTransform.anchorMin = new Vector2(0, 1);
            top.rectTransform.anchorMax = new Vector2(1, 1);
            top.rectTransform.offsetMin = new Vector2(8, -104);
            top.rectTransform.offsetMax = new Vector2(-8, -8);

            var meters = UIKit.Rect("Meters", top.transform);
            meters.anchorMin = new Vector2(0, 0); meters.anchorMax = new Vector2(0.42f, 1);
            meters.offsetMin = new Vector2(16, 12); meters.offsetMax = new Vector2(0, -12);
            var mh = UIKit.HStack(meters.gameObject, 14);
            mh.childForceExpandWidth = true;
            mh.childForceExpandHeight = true;
            foreach (var m in new[] { "power", "life", "health" }) MeterWidget(meters, m);

            var mid = UIKit.Rect("Sol", top.transform);
            mid.anchorMin = new Vector2(0.42f, 0); mid.anchorMax = new Vector2(0.62f, 1);
            mid.offsetMin = new Vector2(10, 8); mid.offsetMax = new Vector2(-10, -8);
            hud.sol = UIKit.Label(mid, "Sol 1", 30, Theme.Paper, TextAnchor.UpperCenter, true);
            UIKit.Stretch(hud.sol.rectTransform, 0, 34, 0, -4);
            hud.act = UIKit.Label(mid, "", 13, Theme.Gold, TextAnchor.UpperCenter);
            UIKit.Stretch(hud.act.rectTransform, 0, 22, 0, 36);
            var pipRow = UIKit.Rect("Hours", mid);
            UIKit.Anchor(pipRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(230, 20));
            var ph = UIKit.HStack(pipRow.gameObject, 5, 0, TextAnchor.MiddleCenter);
            hud.pips = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                var pip = UIKit.Rect("Pip", pipRow);
                var img = pip.gameObject.AddComponent<Image>();
                img.sprite = Theme.Circle(32, true);
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                UIKit.Size(img, 18, 18, 18, 18);
                hud.pips[i] = img;
            }
            hud.hoursText = UIKit.Label(pipRow, "", 13, Theme.Paper, TextAnchor.MiddleLeft);
            UIKit.Size(hud.hoursText, 50, 18, 50, 18);

            var right = UIKit.Rect("Inventory", top.transform);
            right.anchorMin = new Vector2(0.62f, 0); right.anchorMax = new Vector2(1, 1);
            right.offsetMin = new Vector2(8, 8); right.offsetMax = new Vector2(-16, -8);
            hud.inventory = UIKit.Label(right, "", 18, Theme.Paper, TextAnchor.MiddleRight);
            UIKit.Stretch(hud.inventory.rectTransform, 0, 22, 0, 0);
            hud.season = UIKit.Label(right, "", 13, Theme.Gold, TextAnchor.LowerRight);
            UIKit.Stretch(hud.season.rectTransform, 0, 0, 0, 52);

            // ---- events list (under the meters) ----
            hud.events = UIKit.Rect("Events", hudLayer);
            UIKit.Anchor(hud.events, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -112), new Vector2(330, 10));
            UIKit.VStack(hud.events.gameObject, 6);
            UIKit.Fit(hud.events.gameObject);

            // ---- BOLT, bottom-left ----
            var boltPanel = UIKit.Panel(hudLayer, Theme.Night, "Bolt");
            UIKit.Anchor(boltPanel.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(390, 96));
            var face = UIKit.Panel(boltPanel.transform, Theme.Hex("#9FB7C9"), "Face", true, 12);
            UIKit.Anchor(face.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(54, 46));
            var eye = UIKit.Rect("Eye", face.transform);
            var eyeImg = eye.gameObject.AddComponent<Image>();
            eyeImg.sprite = Theme.Circle(32, true);
            eyeImg.color = Theme.Power;
            UIKit.Anchor(eye, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            hud.bolt = UIKit.Label(boltPanel.transform, "", 15, Theme.Paper);
            UIKit.Stretch(hud.bolt.rectTransform, 76, 8, 12, 8);

            // ---- action bar, bottom-centre ----
            var bar = UIKit.Rect("ActionBar", hudLayer);
            UIKit.Anchor(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-10, 12), new Vector2(470, 60));
            var bh = UIKit.HStack(bar.gameObject, 8, 0, TextAnchor.MiddleCenter);
            bh.childForceExpandWidth = true;
            AddActionButton(bar, "1 Rest", "hab", "rest");
            AddActionButton(bar, "2 Radio", "hab", "radio");
            AddActionButton(bar, "3 Seal", "shelter", "seal");
            AddActionButton(bar, "4 Outside", "airlock", "outside");

            // ---- right-hand buttons ----
            var rightCol = UIKit.Rect("Buttons", hudLayer);
            UIKit.Anchor(rightCol, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(250, 176));
            UIKit.VStack(rightCol.gameObject, 8);
            var row = UIKit.Rect("Row", rightCol);
            var rh = UIKit.HStack(row.gameObject, 8);
            rh.childForceExpandWidth = true;
            UIKit.Size(row, -1, 52, -1, 52);
            hud.build = UIKit.Button(row, "Build (B)", OpenBuildMenu, Theme.Paper, 17);
            UIKit.Icon(hud.build, "icon_build");
            UIKit.Icon(UIKit.Button(row, "Logbook", () => ShowLogbook(null), Theme.Paper, 17), "icon_logbook");
            var row2 = UIKit.Rect("Row2", rightCol);
            var rh2 = UIKit.HStack(row2.gameObject, 8);
            rh2.childForceExpandWidth = true;
            UIKit.Size(row2, -1, 52, -1, 52);
            hud.radioBtn = UIKit.Button(row2, "Radio", () => QuickAction("hab", "radio"), Theme.Paper, 17);
            UIKit.Icon(hud.radioBtn, "icon_radio");
            hud.radioBadge = UIKit.Label(hud.radioBtn.transform, "", 12, Theme.Warn, TextAnchor.UpperRight);
            UIKit.Stretch(hud.radioBadge.rectTransform, 0, 0, 8, 2);
            UIKit.Icon(UIKit.Button(row2, "Menu", ShowPause, Theme.Paper, 17), "icon_menu");
            hud.endSol = UIKit.Button(rightCol, "Sleep: end the sol", EndSolPressed, Theme.Night, 19);
            hud.endSol.GetComponentInChildren<Text>().color = Theme.Gold;

            // ---- floating station prompt ----
            var promptBg = UIKit.Panel(hudLayer, Theme.Paper, "Prompt", true, 12);
            hud.promptBox = promptBg.rectTransform;
            hud.promptBox.anchorMin = hud.promptBox.anchorMax = Vector2.zero;
            hud.promptBox.pivot = new Vector2(0.5f, 0);
            hud.promptBox.sizeDelta = new Vector2(300, 36);
            hud.prompt = UIKit.Label(promptBg.transform, "", 14, Theme.Ink, TextAnchor.MiddleCenter);
            UIKit.Stretch(hud.prompt.rectTransform, 8, 2, 8, 2);

            // ---- build placement bar ----
            var bb = UIKit.Panel(hudLayer, Theme.Paper, "BuildBar");
            hud.buildBox = bb.rectTransform;
            UIKit.Anchor(hud.buildBox, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 82), new Vector2(640, 64));
            hud.buildBar = UIKit.Label(bb.transform, "", 16, Theme.Ink, TextAnchor.MiddleLeft);
            UIKit.Stretch(hud.buildBar.rectTransform, 16, 4, 130, 4);
            var cancel = UIKit.Button(bb.transform, "Cancel", CancelBuild, Theme.Paper, 16);
            UIKit.Anchor((RectTransform)cancel.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(110, 46));
            hud.buildBox.gameObject.SetActive(false);

            RefreshHud();
        }

        void MeterWidget(RectTransform parent, string meter)
        {
            var col = UIKit.Rect(meter, parent);
            var v = UIKit.VStack(col.gameObject, 4);
            v.childForceExpandHeight = false;
            var head = UIKit.Rect("Head", col);
            UIKit.Size(head, -1, 22, -1, 22);
            string label = meter == "power" ? "Power" : meter == "life" ? "Life support" : "Crew health";
            var name = UIKit.Label(head, label, 15, Theme.Paper);
            UIKit.Stretch(name.rectTransform);
            var val = UIKit.Label(head, "", 15, Theme.Paper, TextAnchor.MiddleRight);
            UIKit.Stretch(val.rectTransform);
            hud.values[meter] = val;
            var barBg = UIKit.Panel(col, Theme.Hex("#4A3A58"), "Bar", true, 10);
            UIKit.Size(barBg, -1, 22, -1, 22);
            var fill = UIKit.Rect("Fill", barBg.transform);
            var img = fill.gameObject.AddComponent<Image>();
            img.sprite = Theme.Rounded(8, 0, false);
            img.type = Image.Type.Sliced;
            img.color = Theme.MeterColor(meter);
            fill.anchorMin = new Vector2(0, 0);
            fill.anchorMax = new Vector2(0.5f, 1);
            fill.offsetMin = new Vector2(4, 4);
            fill.offsetMax = new Vector2(-2, -4);
            hud.bars[meter] = img;
            var icon = UIKit.Label(col, meter == "power" ? "Panels, battery" : meter == "life" ? "Air, water, food" : "Radiation, rest", 11, Theme.Hex("#D9CFE0"));
            UIKit.Size(icon, -1, 14, -1, 14);
        }

        void AddActionButton(RectTransform bar, string text, string station, string action)
        {
            var b = UIKit.Button(bar, text, () => QuickAction(station, action), Theme.Paper, 16);
            UIKit.Size(b, 100, 54, 110, 56);
            hud.actionButtons.Add(b);
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
                hud.values[m].text = commander ? v + (v < 25 ? " !" : "") : MeterWord(v) + " " + v;
                if (m == "health" && s.healthCap < 100) hud.values[m].text += " <size=12>(max " + s.healthCap + ")</size>";
            }
            hud.sol.text = "Sol " + Mathf.Min(s.sol, s.lastSol) + " <size=16>/ " + s.lastSol + "</size>";
            hud.act.text = "Act " + Mission.Act + ": " + ActName(Mission.Act) + (Mission.InDustSeason(s.sol) ? " · dust season" : "");
            float hours = s.quartersLeft / 4f;
            for (int i = 0; i < 8; i++)
            {
                float f = Mathf.Clamp01(hours - i);
                hud.pips[i].fillAmount = f > 0 ? f : 1;
                hud.pips[i].color = f > 0 ? Theme.Power : new Color(0.43f, 0.37f, 0.48f);
            }
            hud.hoursText.text = Mission.Hours(s.quartersLeft) + " left";
            hud.inventory.text = "Regolith <b>" + s.regolith + "</b>   Ice <b>" + s.ice + "</b>   Parts <b>" + s.parts + "</b>   Science <b>" + s.samples + "</b> ★";
            hud.season.text = (s.seasonReal ? "Real solar weather: " : "") + s.seasonLabel + "  ·  " + Mission.Mode.name + "  ·  Code " + s.classroomCode;

            UIKit.Clear(hud.events);
            foreach (var e in s.events)
            {
                var def = Balance.Event(e.id);
                var ev = e;
                var b = UIKit.Button(hud.events, def.name + "  ·  " + def.hint.ToLowerInvariant() + (e.expiresSol > 0 ? "  ·  until sol " + e.expiresSol : ""), () => GoFixEvent(ev), Theme.Paper, 14);
                b.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                UIKit.Icon(b, "icon_warning", Theme.Warn);
                UIKit.Size(b, -1, 44, -1, 44);
            }

            hud.bolt.text = BoltHint();
            hud.radioBadge.text = s.radioUsed ? "" : "NEW ●";
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
            if (s.power < 25) return "Power is low. Machines stop if it hits zero. Fewer machines, or more panels?";
            if (!s.radioUsed) return "No word from Earth yet today. Use the Radio (2) to hear tonight's forecast.";
            foreach (var m in s.modules) if (!m.built) return "Your " + Balance.Module(m.id).name.ToLowerInvariant() + " needs work. Click the site to build.";
            if (s.quartersLeft < 4) return "Out of daylight. Time to sleep and end the sol.";
            if (Mission.Act >= 3 && !Mission.HasBuilt("pad") && s.unlocked.Contains("pad")) return "The next crew needs a landing pad (8 regolith). Crater rim trips get regolith.";
            return "Click a room to walk there and use it. Go outside through the Airlock (4) to gather materials.";
        }

        void OutpostUpdate()
        {
            if (hud == null || Mission == null) return;
            // Floating prompt above the station the player is near.
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

            hud.buildBox.gameObject.SetActive(World.ghostModule != null);
            if (World.ghostModule != null)
            {
                var c = World.ghostCheck;
                hud.buildBar.text = "Placing: <b>" + Balance.Module(World.ghostModule).name + "</b>   " +
                                    (c == null ? "Point at the map" : (c.ok ? "<color=#227A3B>" : "<color=#B42318>") + c.reason + "</color>") +
                                    "\n<size=13>Click to place · R or right-click to rotate · Esc to cancel</size>";
            }

            if (!WorldInputOpen) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) QuickAction("hab", "rest");
            if (kb.digit2Key.wasPressedThisFrame) QuickAction("hab", "radio");
            if (kb.digit3Key.wasPressedThisFrame) QuickAction("shelter", "seal");
            if (kb.digit4Key.wasPressedThisFrame) QuickAction("airlock", "outside");
            if (kb.bKey.wasPressedThisFrame && World.ghostModule == null) OpenBuildMenu();
            if (kb.escapeKey.wasPressedThisFrame && World.ghostModule == null) ShowPause();
        }

        public void OnGhostChanged() { }

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
                    UIKit.Button(row, "Sleep now", () => { CloseModal(card); done(); EndSol(); }, Theme.Night).GetComponentInChildren<Text>().color = Theme.Gold;
                });
                return;
            }
            EndSol();
        }
    }
}

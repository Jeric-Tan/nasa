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
        // ---------- title ----------

        void ShowTitle()
        {
            ClearScreens();
            phase = Phase.Title;
            if (World) World.gameObject.SetActive(false);
            var bg = UIKit.Fill(screenLayer, Theme.Hex("#F4A261"), "Title");
            UIKit.Stretch(bg.rectTransform);
            // Parallax Gale crater: sky, far rim, Mount Sharp, near ground.
            Layer(bg.transform, Theme.Hex("#F6C38B"), 0.0f, 1.0f, 0f, "Sky");
            Hill(bg.transform, Theme.Hex("#D9844F"), new Vector2(0.5f, 0.18f), new Vector2(2400, 520), 4f);
            Hill(bg.transform, Theme.Hex("#C0623A"), new Vector2(0.62f, 0.12f), new Vector2(1100, 520), 8f);
            Hill(bg.transform, Theme.MarsDeep, new Vector2(0.3f, -0.02f), new Vector2(2600, 360), 14f);

            var col = UIKit.Rect("Column", bg.transform);
            UIKit.Anchor(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(620, 600));
            UIKit.VStack(col.gameObject, 12, 0, TextAnchor.MiddleCenter);
            var title = UIKit.Label(col, "Outpost Gale", 88, Theme.Paper, TextAnchor.MiddleCenter, true);
            title.gameObject.AddComponent<Outline>().effectColor = Theme.Ink;
            UIKit.Size(title, -1, 110, -1, 110);
            UIKit.Label(col, "Keep your crew alive on Mars for 30 sols. Every storm really happened.", 20, Theme.Ink, TextAnchor.MiddleCenter);

            var sun = UIKit.Panel(col, Theme.Night, "Sun", true, 14);
            UIKit.VStack(sun.gameObject, 2, 12);
            UIKit.Label(sun.transform, "<color=#FFD27A><b>" + (LiveSun.Live ? "[LIVE]" : "[REAL ARCHIVE]") + "</b></color>  Today's Sun · " + LiveSun.Source, 14, Theme.Paper, TextAnchor.MiddleCenter);
            UIKit.Label(sun.transform, LiveSun.Headline + " " + LiveSun.Detail, 16, Theme.Paper, TextAnchor.MiddleCenter);

            var saved = SaveSystem.LoadMission();
            if (saved != null)
                UIKit.Button(col, "Continue: sol " + saved.sol + " (" + saved.commanderName + ")", ContinueMission, Theme.Power, 22);
            UIKit.Button(col, saved != null ? "New mission" : "Play", ShowSetup, saved != null ? Theme.Paper : Theme.Power, 22);
            var row = UIKit.Rect("Row", col);
            UIKit.HStack(row.gameObject, 10).childForceExpandWidth = true;
            UIKit.Size(row, -1, 52, -1, 52);
            UIKit.Button(row, "Science Logbook", () => ShowLogbook(null), Theme.Paper, 18);
            UIKit.Button(row, "Credits", ShowCredits, Theme.Paper, 18);
            UIKit.Button(row, Profile.muted ? "Sound: off" : "Sound: on", () => { Profile.muted = !Profile.muted; AudioFx.SetMuted(Profile.muted); if (!Profile.muted) AudioFx.Wind(true); SaveSystem.SaveProfile(Profile); ShowTitle(); }, Theme.Paper, 18);
            UIKit.Label(col, AudioFx.HasRealWind ? "Listen: this is what Mars really sounds like (NASA Perseverance microphone)." : "Click anywhere to hear the Mars wind.", 15, Theme.Ink, TextAnchor.MiddleCenter);
        }

        static void Layer(Transform parent, Color c, float y0, float y1, float drift, string name)
        {
            var img = UIKit.Fill(parent, c, name);
            img.rectTransform.anchorMin = new Vector2(0, y0);
            img.rectTransform.anchorMax = new Vector2(1, y1);
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        }

        static void Hill(Transform parent, Color c, Vector2 anchor, Vector2 size, float speed)
        {
            var rt = UIKit.Rect("Hill", parent);
            UIKit.Anchor(rt, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Circle(128);
            img.color = c;
            rt.gameObject.AddComponent<Drift>().speed = speed;
        }

        // ---------- setup ----------

        void ShowSetup()
        {
            ClearScreens();
            phase = Phase.Setup;
            var setup = new MissionSetup { commanderName = "Commander", mode = Profile.lastEnding == null ? "cadet" : "cadet" };
            var bg = UIKit.Fill(screenLayer, Theme.Mars, "Setup");
            UIKit.Stretch(bg.rectTransform);
            var card = UIKit.Panel(bg.transform, Theme.Paper, "Card");
            UIKit.Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 660));
            UIKit.VStack(card.gameObject, 10, 26);
            UIKit.Label(card.transform, "New mission", 36, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIKit.Label(card.transform, "BOLT: Hi! I'm BOLT, the base robot. Set up your mission and I'll meet you on Mars.", 17, Theme.Muted);

            UIKit.Label(card.transform, "<b>Mode</b>", 18);
            var modes = UIKit.Rect("Modes", card.transform);
            UIKit.HStack(modes.gameObject, 10).childForceExpandWidth = true;
            UIKit.Size(modes, -1, 74, -1, 74);
            Button cadet = null, commander = null;
            Action paint = () =>
            {
                cadet.GetComponent<Image>().color = setup.mode == "cadet" ? Theme.Power : Color.white;
                commander.GetComponent<Image>().color = setup.mode == "commander" ? Theme.Power : Color.white;
            };
            cadet = UIKit.Button(modes, "Cadet (ages 7–9)\n<size=14>20 sols · words not numbers · milder storms</size>", () => { setup.mode = "cadet"; paint(); }, Color.white, 18);
            commander = UIKit.Button(modes, "Commander (10+ and grown-ups)\n<size=14>30 sols · real units · the big storms</size>", () => { setup.mode = "commander"; paint(); }, Color.white, 18);
            paint();

            var nameRow = UIKit.Rect("NameRow", card.transform);
            UIKit.HStack(nameRow.gameObject, 12).childForceExpandWidth = false;
            UIKit.Size(nameRow, -1, 52, -1, 52);
            var nl = UIKit.Label(nameRow, "<b>Your name</b>", 18);
            UIKit.Size(nl, 150, 48, 150, 48);
            var nameInput = Input(nameRow, "Commander", "Type a name", 16);
            UIKit.Size(nameInput, 300, 48, 300, 48);
            nameInput.onValueChanged.AddListener(v => setup.commanderName = v);

            var suitRow = UIKit.Rect("SuitRow", card.transform);
            UIKit.HStack(suitRow.gameObject, 10);
            UIKit.Size(suitRow, -1, 52, -1, 52);
            var sl = UIKit.Label(suitRow, "<b>Suit colour</b>", 18);
            UIKit.Size(sl, 150, 48, 150, 48);
            var swatches = new List<Image>();
            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                var b = UIKit.Button(suitRow, "", null, WorldView.SuitColor(i));
                UIKit.Size(b, 48, 48, 48, 48);
                swatches.Add(b.GetComponent<Image>());
                b.onClick.AddListener(() =>
                {
                    setup.suitColor = idx;
                    for (int k = 0; k < swatches.Count; k++) swatches[k].transform.localScale = Vector3.one * (k == idx ? 1.15f : 1f);
                });
            }
            swatches[0].transform.localScale = Vector3.one * 1.15f;

            if (Profile.jezeroUnlocked)
            {
                var siteRow = UIKit.Rect("SiteRow", card.transform);
                UIKit.HStack(siteRow.gameObject, 10).childForceExpandWidth = true;
                UIKit.Size(siteRow, -1, 52, -1, 52);
                Button gale = null, jez = null;
                Action paintSite = () =>
                {
                    gale.GetComponent<Image>().color = setup.site == "gale" ? Theme.Power : Color.white;
                    jez.GetComponent<Image>().color = setup.site == "jezero" ? Theme.Power : Color.white;
                };
                gale = UIKit.Button(siteRow, "Gale crater (Curiosity)", () => { setup.site = "gale"; paintSite(); }, Color.white, 16);
                jez = UIKit.Button(siteRow, "Jezero crater (Perseverance) NEW", () => { setup.site = "jezero"; paintSite(); }, Color.white, 16);
                paintSite();
            }

            var codeRow = UIKit.Rect("CodeRow", card.transform);
            UIKit.HStack(codeRow.gameObject, 12);
            UIKit.Size(codeRow, -1, 52, -1, 52);
            var cl = UIKit.Label(codeRow, "<b>Classroom Code</b>", 18);
            UIKit.Size(cl, 150, 48, 150, 48);
            var codeInput = Input(codeRow, "", "Leave empty for a new mission", 8);
            UIKit.Size(codeInput, 300, 48, 300, 48);
            codeInput.onValueChanged.AddListener(v => setup.classroomCode = v.Trim().ToUpperInvariant());
            UIKit.Label(codeRow, "<size=14>Same code = same storms and events for everyone.</size>", 14, Theme.Muted);

            var buttons = UIKit.Rect("Buttons", card.transform);
            UIKit.HStack(buttons.gameObject, 10).childForceExpandWidth = true;
            UIKit.Size(buttons, -1, 60, -1, 60);
            UIKit.Button(buttons, "Back", ShowTitle, Theme.Paper);
            UIKit.Button(buttons, "Start the mission", () => StartMission(setup), Theme.Power, 22);
        }

        InputField Input(Transform parent, string value, string placeholder, int limit)
        {
            var bg = UIKit.Panel(parent, Color.white, "Input", true, 12);
            var input = bg.gameObject.AddComponent<InputField>();
            var text = UIKit.Label(bg.transform, "", 20);
            UIKit.Stretch(text.rectTransform, 14, 4, 14, 4);
            text.supportRichText = false;
            var ph = UIKit.Label(bg.transform, placeholder, 17, Theme.Muted);
            UIKit.Stretch(ph.rectTransform, 14, 4, 14, 4);
            ph.fontStyle = FontStyle.Italic;
            input.textComponent = text;
            input.placeholder = ph;
            input.characterLimit = limit;
            input.text = value;
            return input;
        }

        // ---------- ending ----------

        void ShowEnding()
        {
            ClearScreens();
            phase = Phase.Ending;
            World.gameObject.SetActive(false);
            AudioFx.Music("music_calm");
            var s = Mission.S;
            var ending = Data.story.EndingFor(s.ending);
            var bg = UIKit.Fill(screenLayer, s.ending == "early" ? Theme.Night : Theme.Mars, "Ending");
            UIKit.Stretch(bg.rectTransform);
            var content = UIKit.Scroll(bg.transform, out var scroll);
            UIKit.Stretch((RectTransform)scroll.transform, 60, 20, 60, 20);
            var vl = content.GetComponent<VerticalLayoutGroup>();
            vl.spacing = 14;
            vl.childAlignment = TextAnchor.UpperCenter;

            var head = UIKit.Label(content, ending.title, 64, Theme.Paper, TextAnchor.MiddleCenter, true);
            head.gameObject.AddComponent<Outline>().effectColor = Theme.Ink;
            UIKit.Label(content, ending.message + " " + s.endingReason, 22, Theme.Paper, TextAnchor.MiddleCenter);

            // Comic-style mission report: your 3 biggest decisions.
            UIKit.Label(content, "Mission report: your 3 biggest decisions", 24, Theme.Gold, TextAnchor.MiddleCenter, true);
            var strip = UIKit.Rect("Comic", content);
            var h = UIKit.HStack(strip.gameObject, 16, 0, TextAnchor.UpperCenter);
            h.childForceExpandWidth = true;
            UIKit.Size(strip, -1, 210, -1, 210);
            var top = Mission.TopDecisions();
            int panel = 1;
            foreach (var d in top)
            {
                var p = UIKit.Panel(strip, Color.white, "Panel", true, 10);
                UIKit.VStack(p.gameObject, 6, 16);
                UIKit.Label(p.transform, panel++ + ". Sol " + d.sol, 20, Theme.Ink, TextAnchor.UpperLeft, true);
                UIKit.Label(p.transform, d.text + ".", 17);
            }
            if (top.Count == 0) UIKit.Label(strip, "A quiet mission: no big decisions recorded.", 18, Theme.Paper);

            var facts = UIKit.Panel(content, Theme.Night, "Real", true, 14);
            UIKit.VStack(facts.gameObject, 6, 18);
            UIKit.Label(facts.transform, "<color=#FFD27A><b>Real NASA parallel:</b></color> " + ending.parallel, 18, Theme.Paper);
            UIKit.Label(facts.transform, "<color=#FFD27A><b>Try next time:</b></color> " + ending.tip, 18, Theme.Paper);
            UIKit.Label(facts.transform, "<b>Your season:</b> " + (s.seasonReal ? "the real solar weather of " + s.seasonYear + " (" + s.seasonLabel + ")" : s.seasonLabel) +
                                         ". Storms: " + s.stormsHit + " (" + s.stormsSealed + " in the shelter). Trips outside: " + s.tripsOutside +
                                         ". Upgrades: " + Mission.UpgradesBuilt(true) + ". Rock samples: " + s.samples + ".", 16, Theme.Paper);
            UIKit.Label(facts.transform, "<b>Classroom Code:</b> " + s.classroomCode + " · " + Mission.Mode.name + " mode. Share it to compare endings.", 16, Theme.Paper);

            if (Mission.NewBadges.Count > 0)
            {
                UIKit.Label(content, "Badges earned", 24, Theme.Gold, TextAnchor.MiddleCenter, true);
                foreach (var id in Mission.NewBadges)
                {
                    var b = Badges.Get(id);
                    if (b != null) UIKit.Label(content, "★ <b>" + b.name + "</b>: " + b.text, 18, Theme.Paper, TextAnchor.MiddleCenter);
                }
            }
            if (s.ending != "early" && s.site == "gale" && Profile.jezeroUnlocked)
                UIKit.Label(content, "New site unlocked: Jezero crater, Perseverance's home!", 18, Theme.Gold, TextAnchor.MiddleCenter);

            var row = UIKit.Rect("Buttons", content);
            UIKit.HStack(row.gameObject, 12).childForceExpandWidth = true;
            UIKit.Size(row, -1, 60, -1, 60);
            UIKit.Button(row, "Play again", ShowSetup, Theme.Power, 22);
            UIKit.Button(row, "Science Logbook", () => ShowLogbook(null), Theme.Paper, 18);
            UIKit.Button(row, "Title screen", ShowTitle, Theme.Paper, 18);
            SaveSystem.SaveProfile(Profile);
        }

        // ---------- logbook ----------

        void ShowLogbook(Action doneOuter)
        {
            Queue(done =>
            {
                var card = Modal(900);
                card.GetComponent<ContentSizeFitter>().enabled = false;
                card.sizeDelta = new Vector2(900, 640);
                int total = Data.staticFacts.Count + (Data.osdr?.facts?.Length ?? 0);
                int found = 0;
                foreach (var f in Data.staticFacts) if (Profile.logbook.Contains(f.id)) found++;
                if (Data.osdr?.facts != null) foreach (var f in Data.osdr.facts) if (Profile.logbook.Contains(f.id)) found++;
                int storms = Profile.logbook.FindAll(x => x.StartsWith("storm:")).Count;
                UIKit.Label(card, "Science Logbook", 32, Theme.Ink, TextAnchor.MiddleLeft, true);
                UIKit.Label(card, found + " of " + total + " fact cards found · " + storms + " real solar storms survived", 17, Theme.Muted);
                var list = UIKit.Scroll(card, out var scroll);
                UIKit.Size(scroll, -1, 440, -1, 440, 1);

                foreach (var f in Data.staticFacts) Entry(list, Profile.logbook.Contains(f.id), f.title, f.text, f.source);
                if (Data.osdr?.facts != null)
                    foreach (var f in Data.osdr.facts)
                        Entry(list, Profile.logbook.Contains(f.id), f.title, f.text, "NASA OSDR study " + f.id + ": " + f.studyTitle);
                foreach (var id in Profile.logbook)
                {
                    if (!id.StartsWith("storm:")) continue;
                    var rec = FindStorm(id.Substring(6));
                    if (rec == null) continue;
                    Entry(list, true, "Real storm: " + StormPlanner.NiceDate(rec.date),
                        "A solar particle event" + (string.IsNullOrEmpty(rec.flareClass) ? "" : " after a class " + rec.flareClass + " flare") + ". You lived through it on Mars.",
                        "NASA DONKI " + rec.id);
                }

                UIKit.Label(list, "Datasets used in this game", 22, Theme.Ink, TextAnchor.MiddleLeft, true);
                Entry(list, true, "DONKI (NASA space weather database)", "Every storm night is a real solar particle event (SEP) from one real year, squeezed into your mission. Flares are used for Earth's warnings. The live Today's Sun banner asks DONKI for the last 30 days.", "ccmc.gsfc.nasa.gov/DONKI");
                Entry(list, true, "Curiosity RAD (radiation at Gale crater)", "The baseline radiation and the real-units numbers use the published Curiosity RAD average (about 0.64 mSv a sol). Storm damage is a game number, simplified so the shelter choice matters.", Data.radiation != null ? Data.radiation.citation : "Hassler et al. 2014");
                Entry(list, true, "OSDR (NASA Open Science Data Repository)", "Health and plant fact cards each name a real study, like OSD-489 about bones in microgravity.", "osdr.nasa.gov");
                Entry(list, true, "Mars Trek", "The expedition map is meant to use an image of Gale crater from Mars Trek.", "trek.nasa.gov");
                UIKit.Button(card, "Close", () => { CloseModal(card); done(); doneOuter?.Invoke(); }, Theme.Paper);
            });
        }

        DonkiStorm FindStorm(string id)
        {
            if (Data.donki?.seasons == null) return null;
            foreach (var s in Data.donki.seasons)
                if (s.storms != null)
                    foreach (var st in s.storms)
                        if (st.id == id) return st;
            return null;
        }

        static void Entry(RectTransform list, bool found, string title, string text, string source)
        {
            var p = UIKit.Panel(list, found ? Color.white : Theme.Hex("#E4DED8"), "Entry", true, 12);
            UIKit.VStack(p.gameObject, 3, 14);
            UIKit.Label(p.transform, found ? "<b>" + title + "</b>" : "<b>??? Locked card</b>", 18);
            UIKit.Label(p.transform, found ? text : "Keep playing to find this one. Cards unlock at night, after storms and when you build.", 15, found ? Theme.Ink : Theme.Muted);
            if (found) UIKit.Label(p.transform, "<size=12>Source: " + source + "</size>", 12, Theme.Muted);
        }

        // ---------- credits & pause ----------

        void ShowCredits()
        {
            Queue(done =>
            {
                var card = Modal(760);
                UIKit.Label(card, "Credits", 32, Theme.Ink, TextAnchor.MiddleLeft, true);
                var credits = Resources.Load<TextAsset>("Data/Credits");
                UIKit.Label(card, credits ? credits.text : "See CREDITS.md", 15);
                UIKit.Button(card, "Close", () => { CloseModal(card); done(); }, Theme.Paper);
            });
        }

        void ShowPause()
        {
            if (!WorldInputOpen) return;
            Queue(done =>
            {
                var card = Modal(520);
                UIKit.Label(card, "Paused", 32, Theme.Ink, TextAnchor.MiddleLeft, true);
                UIKit.Button(card, "Resume", () => { CloseModal(card); done(); }, Theme.Power);
                UIKit.Button(card, "How to play", () => { CloseModal(card); done(); ShowHowToPlay(); });
                UIKit.Button(card, Profile.muted ? "Sound: off" : "Sound: on", () =>
                {
                    Profile.muted = !Profile.muted;
                    AudioFx.SetMuted(Profile.muted);
                    if (!Profile.muted) AudioFx.Wind(true);
                    SaveSystem.SaveProfile(Profile);
                    CloseModal(card); done(); ShowPause();
                });
                UIKit.Button(card, Profile.reducedMotion ? "Reduced motion: on" : "Reduced motion: off", () =>
                {
                    Profile.reducedMotion = !Profile.reducedMotion;
                    SaveSystem.SaveProfile(Profile);
                    CloseModal(card); done(); ShowPause();
                });
                UIKit.Button(card, "Save and quit to title", () => { CloseModal(card); done(); QuitToTitle(); }, Theme.Paper);
            });
        }

        void ShowHowToPlay()
        {
            Queue(done =>
            {
                var card = Modal(760);
                UIKit.Label(card, "How to play", 32, Theme.Ink, TextAnchor.MiddleLeft, true);
                UIKit.Label(card,
                    "• Keep three meters up: <b>Power</b>, <b>Life Support</b> and <b>Crew Health</b>. If Life Support or Health hits zero, the crew goes home early.\n" +
                    "• Each sol you have <b>8 hours</b>. Click a room to walk there and use it, or use the keys: 1 Rest, 2 Radio, 3 Seal shelter, 4 Go outside, B Build, E use, WASD walk.\n" +
                    "• Go outside to gather regolith, ice, rock samples and parts. Trips cost radiation.\n" +
                    "• <b>Build</b> upgrades: each one helps one meter and costs another. Place it, then work on the site.\n" +
                    "• Use the <b>Radio</b> to hear whether a solar storm is coming, and seal the <b>Storm Shelter</b> before it hits.\n" +
                    "• Before the next crew lands, build a <b>landing pad</b> (it unlocks in Act 3).", 17);
                UIKit.Button(card, "Got it", () => { CloseModal(card); done(); }, Theme.Power);
            });
        }
    }

    // Slow sideways drift for the title parallax.
    public class Drift : MonoBehaviour
    {
        public float speed = 5;
        Vector2 start;
        void Start() => start = ((RectTransform)transform).anchoredPosition;
        void Update()
        {
            var gm = GameManager.I;
            if (gm != null && gm.Profile != null && gm.Profile.reducedMotion) return;
            ((RectTransform)transform).anchoredPosition = start + new Vector2(Mathf.Sin(Time.time * 0.05f * speed / 5f) * speed * 4, 0);
        }
    }
}

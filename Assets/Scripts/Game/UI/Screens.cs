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
            var bg = Backdrop("Title", true);

            var col = UIKit.Rect("Column", bg);
            UIKit.Anchor(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(600, 640));
            UIKit.VStack(col.gameObject, 10, 0, TextAnchor.MiddleCenter);

            var tag = UIKit.Label(col, "SURVIVE ON MARS  ·  EVERY SOLAR STORM REALLY HAPPENED", 13, Theme.Gold, TextAnchor.MiddleCenter);
            tag.font = Theme.BodyBlack;
            UIKit.Size(tag, -1, 20, -1, 20);
            var title = UIKit.Label(col, "Outpost Gale", 96, Theme.Paper, TextAnchor.MiddleCenter, true);
            var s1 = title.gameObject.AddComponent<Shadow>();
            s1.effectColor = Theme.Mars;
            s1.effectDistance = new Vector2(0, -5);
            var s2 = title.gameObject.AddComponent<Shadow>();
            s2.effectColor = new Color(0.1f, 0.05f, 0.2f, 0.55f);
            s2.effectDistance = new Vector2(0, -6);
            UIKit.Size(title, -1, 116, -1, 116);
            title.gameObject.AddComponent<Bob>();
            var sub = UIKit.Label(col, "Keep your crew alive until the next crew lands.", 20, Theme.Paper, TextAnchor.MiddleCenter);
            sub.gameObject.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.05f, 0.2f, 0.5f);
            UIKit.Size(sub, -1, 34, -1, 34);

            var card = UIKit.Panel(col, Theme.Glass, "Menu", true, 26);
            var cv = UIKit.VStack(card.gameObject, 12, 22);
            cv.padding = new RectOffset(22, 22, 18, 24);

            // Live sun ticker
            var sun = UIKit.Panel(card.transform, new Color(1, 1, 1, 0.06f), "Sun", false, 16);
            var sv = UIKit.VStack(sun.gameObject, 2, 12);
            sv.padding = new RectOffset(16, 16, 10, 12);
            var head = UIKit.Rect("Head", sun.transform);
            UIKit.Size(head, -1, 20, -1, 20);
            var dot = UIKit.Rect("Dot", head);
            UIKit.Anchor(dot, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6, 0), new Vector2(11, 11));
            var di = dot.gameObject.AddComponent<Image>();
            di.sprite = Theme.Circle(32);
            di.color = LiveSun.Live ? Theme.Warn : Theme.Gold;
            dot.gameObject.AddComponent<Pulse>();
            var ht = UIKit.Label(head, "<b>" + (LiveSun.Live ? "LIVE" : "REAL ARCHIVE") + "</b>  <color=#C9BEE0>Today's Sun · " + LiveSun.Source + "</color>", 13, Theme.Gold);
            UIKit.Stretch(ht.rectTransform, 20, 0, 0, 0);
            UIKit.Label(sun.transform, LiveSun.Headline + " " + LiveSun.Detail, 15, Theme.Paper);

            var saved = SaveSystem.LoadMission();
            if (saved != null)
            {
                var cont = UIKit.Button(card.transform, "Continue  ·  sol " + saved.sol + "  ·  " + saved.commanderName, ContinueMission, Theme.Power, 21);
                UIKit.Size(cont, -1, 58, -1, 60);
            }
            var play = UIKit.Button(card.transform, saved != null ? "New mission" : "Play", ShowSetup, saved != null ? Theme.Paper : Theme.Power, 21);
            UIKit.Size(play, -1, 58, -1, 60);
            var row = UIKit.Rect("Row", card.transform);
            UIKit.HStack(row.gameObject, 10).childForceExpandWidth = true;
            UIKit.Size(row, -1, 50, -1, 50);
            var glassKey = Theme.Hex("#40357A");
            UIKit.Icon(UIKit.Button(row, "Logbook", () => ShowLogbook(null), glassKey, 16), "icon_logbook");
            UIKit.Icon(UIKit.Button(row, "Credits", ShowCredits, glassKey, 16), "icon_star");
            UIKit.Button(row, Profile.muted ? "Sound: off" : "Sound: on", () => { Profile.muted = !Profile.muted; AudioFx.SetMuted(Profile.muted); if (!Profile.muted) AudioFx.Wind(true); SaveSystem.SaveProfile(Profile); ShowTitle(); }, glassKey, 16);
            var foot = UIKit.Label(col, AudioFx.HasRealWind ? "Listen: that wind is a real recording from NASA's Perseverance rover." : "Click anywhere to hear the Mars wind.", 14, Theme.Paper, TextAnchor.MiddleCenter);
            foot.gameObject.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.05f, 0.2f, 0.5f);
            UIKit.Size(foot, -1, 26, -1, 26);
            // Space Apps rule: AI-assisted images must be labelled where players can see it.
            var ai = UIKit.Label(col, "Character and robot art made with AI assistance  ·  NASA does not endorse this game  ·  see Credits", 11, Theme.Soft, TextAnchor.MiddleCenter);
            UIKit.Size(ai, -1, 18, -1, 18);
        }

        // Mars at dusk: indigo sky fading to a dusty horizon, stars, Earth as the evening star,
        // the blue Martian sunset glow, crater-rim hills and (optionally) the outpost on the ridge.
        RectTransform Backdrop(string name, bool scenery, bool dim = false)
        {
            var root = UIKit.Rect(name, screenLayer);
            UIKit.Stretch(root);
            var sky = root.gameObject.AddComponent<Image>();
            sky.sprite = Theme.Gradient(Theme.Hex("#E9925E"), Theme.Hex("#140E35"));
            sky.raycastTarget = true;

            var rnd = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                var st = UIKit.Rect("Star", root);
                float x = (float)rnd.NextDouble(), y = 0.45f + (float)rnd.NextDouble() * 0.55f;
                float size = 2 + (float)rnd.NextDouble() * 3;
                UIKit.Anchor(st, new Vector2(x, y), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
                var img = st.gameObject.AddComponent<Image>();
                img.sprite = Theme.Circle(32);
                img.color = new Color(1, 0.97f, 0.9f, Mathf.Clamp01((y - 0.45f) * 2.2f) * 0.9f);
                img.raycastTarget = false;
                var tw = st.gameObject.AddComponent<Twinkle>();
                tw.speed = 0.6f + (float)rnd.NextDouble() * 2;
                tw.phase = (float)rnd.NextDouble() * 6;
            }

            // Mars sunsets glow blue: fine dust scatters blue light forward, near the Sun.
            Glow(root, new Vector2(0.14f, 0.24f), 760, new Color(0.55f, 0.75f, 1f, 0.22f));
            Glow(root, new Vector2(0.14f, 0.24f), 260, new Color(0.75f, 0.88f, 1f, 0.35f));
            Glow(root, new Vector2(0.14f, 0.24f), 70, new Color(0.95f, 0.98f, 1f, 0.95f));
            // Earth: from Mars it shines as a bright evening "star".
            Glow(root, new Vector2(0.84f, 0.8f), 22, new Color(0.55f, 0.8f, 1f, 0.45f));
            Glow(root, new Vector2(0.84f, 0.8f), 7, new Color(0.85f, 0.95f, 1f, 1f));
            var earth = UIKit.Label(root, "Earth", 11, new Color(0.8f, 0.9f, 1f, 0.75f), TextAnchor.MiddleCenter);
            UIKit.Anchor(earth.rectTransform, new Vector2(0.84f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(80, 16));

            Hill(root, Theme.Hex("#7A3D52"), new Vector2(0.62f, 0.2f), new Vector2(1500, 520), 3f);   // Mount Sharp, far away
            Hill(root, Theme.Hex("#8E4638"), new Vector2(0.2f, 0.06f), new Vector2(1700, 380), 6f);
            Hill(root, Theme.Hex("#5A2638"), new Vector2(0.85f, -0.02f), new Vector2(1500, 340), 9f);
            Hill(root, Theme.Hex("#3A1830"), new Vector2(0.35f, -0.12f), new Vector2(2600, 360), 13f);

            if (scenery)
            {
                var ridge = UIKit.Rect("Outpost", root);
                UIKit.Anchor(ridge, new Vector2(0.86f, 0.115f), new Vector2(0.5f, 0), Vector2.zero, new Vector2(220, 90));
                Silhouette(ridge, "hab", new Vector2(-20, 0), new Vector2(120, 80));
                Silhouette(ridge, "dish", new Vector2(70, 0), new Vector2(56, 56));
                Silhouette(ridge, "solar", new Vector2(-104, 0), new Vector2(70, 40));
                var beacon = UIKit.Rect("Beacon", ridge);
                UIKit.Anchor(beacon, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(-20, 64), new Vector2(10, 10));
                var bi = beacon.gameObject.AddComponent<Image>();
                bi.sprite = Theme.Dot();
                bi.color = Theme.Gold;
                beacon.gameObject.AddComponent<Pulse>().speed = 2;
            }
            if (dim)
            {
                var shade = UIKit.Fill(root, new Color(0.06f, 0.03f, 0.14f, 0.5f), "Dim");
                UIKit.Stretch(shade.rectTransform);
            }
            return root;
        }

        static void Glow(Transform parent, Vector2 anchor, float size, Color c)
        {
            var rt = UIKit.Rect("Glow", parent);
            UIKit.Anchor(rt, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Dot();
            img.color = c;
            img.raycastTarget = false;
        }

        static void Silhouette(Transform parent, string art, Vector2 pos, Vector2 size)
        {
            var sprite = Theme.Art(art);
            if (sprite == null) return;
            var rt = UIKit.Rect(art, parent);
            UIKit.Anchor(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Theme.Hex("#26112A");
            img.raycastTarget = false;
        }

        static void Hill(Transform parent, Color c, Vector2 anchor, Vector2 size, float speed)
        {
            var rt = UIKit.Rect("Hill", parent);
            UIKit.Anchor(rt, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Circle(128);
            img.color = c;
            img.raycastTarget = false;
            rt.gameObject.AddComponent<Drift>().speed = speed;
        }

        // ---------- setup ----------

        void ShowSetup()
        {
            ClearScreens();
            phase = Phase.Setup;
            var setup = new MissionSetup { commanderName = "Commander", mode = "cadet" };
            var bg = Backdrop("Setup", true, true);
            // The card is the inside of the lander: a riveted metal hull, the crew bay wall inside it,
            // and Mars (the backdrop) all around.
            float cardH = Profile.jezeroUnlocked ? 690 : 610;
            var card = UIKit.Panel(bg, Theme.Hex("#4A5378"), "Card", true, 30);
            UIKit.PopIn(card);
            UIKit.Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, cardH));
            var wall = UIKit.Panel(card.transform, Theme.Hex("#232842"), "Wall", false, 22);
            UIKit.Stretch(wall.rectTransform, 12, 12, 12, 12);
            Rivets(card.transform, 1000, cardH);
            Vents(wall.transform);
            NamePlate(card.transform);

            // ---- left: the suit-up bay ----
            var stage = UIKit.Panel(card.transform, Theme.Hex("#181C31"), "Stage", false, 18);
            var srt = stage.rectTransform;
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(0, 1);
            srt.pivot = new Vector2(0, 0.5f);
            srt.offsetMin = new Vector2(22, 22); srt.offsetMax = new Vector2(358, -22);
            Bay(stage.transform);
            var mound = UIKit.Rect("Dock", stage.transform);
            UIKit.Anchor(mound, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(236, 48));
            var mi = mound.gameObject.AddComponent<Image>();
            mi.sprite = Theme.Circle(128);
            mi.color = new Color(Theme.Life.r, Theme.Life.g, Theme.Life.b, 0.55f);
            mi.raycastTarget = false;
            mound.gameObject.AddComponent<Pulse>().speed = 1.2f;
            var dockTop = UIKit.Rect("DockTop", stage.transform);
            UIKit.Anchor(dockTop, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 162), new Vector2(220, 38));
            var dti = dockTop.gameObject.AddComponent<Image>();
            dti.sprite = Theme.Circle(128);
            dti.color = Theme.Hex("#3A4266");
            dti.raycastTarget = false;
            var shadow = UIKit.Rect("Shadow", stage.transform);
            UIKit.Anchor(shadow, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 166), new Vector2(150, 26));
            var shi = shadow.gameObject.AddComponent<Image>();
            shi.sprite = Theme.Dot();
            shi.color = new Color(0.08f, 0.03f, 0.1f, 0.55f);
            var hero = Character(stage.transform, WorldView.SuitTint(setup.suitColor), new Vector2(200, 250));
            UIKit.Anchor((RectTransform)hero.transform.parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(200, 250));
            hero.transform.parent.gameObject.AddComponent<Idle>();

            var rank = UIKit.Label(stage.transform, "COMMANDER", 12, Theme.Gold, TextAnchor.MiddleCenter);
            rank.font = Theme.BodyBlack;
            UIKit.Anchor(rank.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 126), new Vector2(300, 18));
            var nameTag = UIKit.Label(stage.transform, setup.commanderName, 24, Theme.Paper, TextAnchor.MiddleCenter, true);
            UIKit.Anchor(nameTag.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(300, 34));

            var sl = UIKit.Label(stage.transform, "SUIT COLOUR", 12, Theme.Soft, TextAnchor.MiddleCenter);
            sl.font = Theme.BodyBlack;
            UIKit.Anchor(sl.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 64), new Vector2(300, 18));
            var swatchRow = UIKit.Rect("Swatches", stage.transform);
            UIKit.Anchor(swatchRow, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 32), new Vector2(300, 40));
            UIKit.HStack(swatchRow.gameObject, 10, 0, TextAnchor.MiddleCenter);
            var rings = new List<Image>();
            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                var sw = UIKit.Rect("Suit" + i, swatchRow);
                UIKit.Size(sw, 38, 38, 38, 38);
                var ring = sw.gameObject.AddComponent<Image>();
                ring.sprite = Theme.Circle(64);
                ring.color = Color.clear;
                var dotRt = UIKit.Rect("Dot", sw);
                UIKit.Anchor(dotRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
                var dot = dotRt.gameObject.AddComponent<Image>();
                dot.sprite = Theme.Circle(64);
                dot.color = WorldView.SuitColor(i);
                dot.raycastTarget = false;
                var btn = sw.gameObject.AddComponent<Button>();
                btn.targetGraphic = ring;
                btn.transition = Selectable.Transition.None;
                sw.gameObject.AddComponent<Juice>();
                rings.Add(ring);
                btn.onClick.AddListener(() =>
                {
                    AudioFx.Click();
                    setup.suitColor = idx;
                    hero.color = WorldView.SuitTint(idx);
                    for (int k = 0; k < rings.Count; k++) rings[k].color = k == idx ? Theme.Paper : Color.clear;
                });
            }
            rings[0].color = Theme.Paper;

            // ---- right: the mission form ----
            var form = UIKit.Rect("Form", card.transform);
            form.anchorMin = Vector2.zero; form.anchorMax = Vector2.one;
            form.offsetMin = new Vector2(388, 28); form.offsetMax = new Vector2(-34, -30);
            UIKit.VStack(form.gameObject, 7);

            UIKit.Label(form, "New mission", 38, Theme.Paper, TextAnchor.MiddleLeft, true);
            UIKit.Label(form, "Hi! I'm BOLT, the base robot. Set up your mission and I'll meet you on Mars.", 15, Theme.Soft);

            Section(form, "MODE");
            var modes = UIKit.Rect("Modes", form);
            UIKit.VStack(modes.gameObject, 10);
            UIKit.Size(modes, -1, 150, -1, 150);
            var cadet = Choice(modes, "Cadet",ModeBlurb(Balance.Mode("cadet")), () => setup.mode = "cadet",
                               Theme.Art("icon_star"), Theme.Life, 1, "EASY");
            var commander = Choice(modes, "Commander",ModeBlurb(Balance.Mode("commander")), () => setup.mode = "commander",
                                   Theme.Art("icon_warning"), Theme.Mars, 3, "HARD");
            Action paint = () => { Select(cadet, setup.mode == "cadet"); Select(commander, setup.mode == "commander"); };
            cadet.onClick.AddListener(() => paint());
            commander.onClick.AddListener(() => paint());
            paint();
            // What's a sol? (the explanation comes from the story's glossary)
            var solTerm = Data.story?.terms != null ? Array.Find(Data.story.terms, t => t.id == "sol") : null;
            if (solTerm != null)
            {
                Gap(form, 6);
                UIKit.Label(form, "<color=#FFD27A><b>What's a sol?</b></color>  " + solTerm.explain, 13, Theme.Soft);
            }

            if (Profile.jezeroUnlocked)
            {
                Section(form, "LANDING SITE");
                var sites = UIKit.Rect("Sites", form);
                UIKit.HStack(sites.gameObject, 12).childForceExpandWidth = true;
                UIKit.Size(sites, -1, 70, -1, 70);
                var gale = Choice(sites, "Gale crater", "Curiosity's home", () => setup.site = "gale");
                var jez = Choice(sites, "Jezero crater · NEW", "Perseverance's home", () => setup.site = "jezero");
                Action paintSite = () => { Select(gale, setup.site == "gale"); Select(jez, setup.site == "jezero"); };
                gale.onClick.AddListener(() => paintSite());
                jez.onClick.AddListener(() => paintSite());
                paintSite();
            }

            Section(form, "YOUR NAME");
            var nameInput = Input(form, "Commander", "Type a name", 16);
            UIKit.Size(nameInput, -1, 50, -1, 50);
            nameInput.onValueChanged.AddListener(v =>
            {
                setup.commanderName = v;
                nameTag.text = string.IsNullOrWhiteSpace(v) ? "Commander" : v;
            });

            Section(form, "CLASSROOM CODE  <color=#8F84AD>(optional)</color>");
            var codeInput = Input(form, "", "Leave empty for a new mission", 8);
            UIKit.Size(codeInput, -1, 50, -1, 50);
            codeInput.onValueChanged.AddListener(v => setup.classroomCode = v.Trim().ToUpperInvariant());
            UIKit.Label(form, "Same code = same storms and events for everyone in your class.", 13, Theme.Soft);

            var spacer = UIKit.Rect("Spacer", form);
            UIKit.Size(spacer, -1, 0, -1, 0).flexibleHeight = 1;
            var buttons = UIKit.Rect("Buttons", form);
            UIKit.HStack(buttons.gameObject, 12);
            UIKit.Size(buttons, -1, 60, -1, 60);
            var back = UIKit.Button(buttons, "Back", ShowTitle, Theme.Hex("#40357A"), 18);
            UIKit.Size(back, 130, 56, 130, 58);
            var go = UIKit.Button(buttons, "Start the mission", () => StartMission(setup), Theme.Power, 22);
            UIKit.Size(go, 200, 56, -1, 58, 1);
        }

        // The layered astronaut (char_base tinted by the suit colour, char_detail on top), or the old sprite.
        // Returns the tinted layer so callers can recolour it.
        static Image Character(Transform parent, Color suit, Vector2 size)
        {
            var holder = UIKit.Rect("Character", parent);
            var baseArt = Theme.Art("char_base");
            var basis = UIKit.Rect("Suit", holder);
            UIKit.Stretch(basis);
            var bi = basis.gameObject.AddComponent<Image>();
            bi.sprite = baseArt ? baseArt : Theme.Art("astronaut");
            bi.preserveAspect = true;
            bi.color = suit;
            bi.raycastTarget = false;
            var detailArt = Theme.Art("char_detail");
            if (baseArt && detailArt)
            {
                var det = UIKit.Rect("Detail", holder);
                UIKit.Stretch(det);
                var di = det.gameObject.AddComponent<Image>();
                di.sprite = detailArt;
                di.preserveAspect = true;
                di.raycastTarget = false;
            }
            return bi;
        }

        // ---- lander interior pieces ----

        // Rivets around the hull band of a w x h card.
        static void Rivets(Transform card, float w, float h)
        {
            var c = Theme.Hex("#6B76A0");
            for (float x = 46; x <= w - 46; x += 52)
            {
                Rivet(card, new Vector2(0, 1), new Vector2(x, -6), c);
                Rivet(card, new Vector2(0, 0), new Vector2(x, 6), c);
            }
            for (float y = 46; y <= h - 46; y += 52)
            {
                Rivet(card, new Vector2(0, 0), new Vector2(6, y), c);
                Rivet(card, new Vector2(1, 0), new Vector2(-6, y), c);
            }
        }

        static void Rivet(Transform parent, Vector2 anchor, Vector2 pos, Color c, float size = 6)
        {
            var rt = UIKit.Rect("Rivet", parent);
            UIKit.Anchor(rt, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Circle(32);
            img.color = c;
            img.raycastTarget = false;
        }

        // Air vents in the wall's top-right corner.
        static void Vents(Transform wall)
        {
            for (int i = 0; i < 4; i++)
            {
                var v = UIKit.Rect("Vent", wall);
                UIKit.Anchor(v, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -22 - i * 9), new Vector2(64, 4));
                var img = v.gameObject.AddComponent<Image>();
                img.sprite = Theme.Key(4);
                img.color = Theme.Hex("#141829");
                img.raycastTarget = false;
            }
        }

        // The lander's name plate on the top edge of the hull, with status lights.
        static void NamePlate(Transform card)
        {
            var plate = UIKit.Panel(card, Theme.Hex("#151A2E"), "Plate", true, 10);
            UIKit.Anchor(plate.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(300, 30));
            var t = UIKit.Label(plate.transform, "LANDER  ·  CREW BAY", 12, Theme.Soft, TextAnchor.MiddleLeft);
            t.font = Theme.BodyBlack;
            UIKit.Stretch(t.rectTransform, 18, 0, 70, 0);
            Color[] lights = { Theme.Health, Theme.Power, Theme.Warn };
            for (int i = 0; i < 3; i++)
            {
                var l = UIKit.Rect("Light", plate.transform);
                UIKit.Anchor(l, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50 + i * 16, 0), new Vector2(9, 9));
                var img = l.gameObject.AddComponent<Image>();
                img.sprite = Theme.Circle(32);
                img.color = lights[i];
                img.raycastTarget = false;
                if (i == 0) l.gameObject.AddComponent<Pulse>().speed = 2;
            }
        }

        // The suit-up bay: ceiling light, a porthole looking out at Mars, a grated floor with hazard stripes.
        static void Bay(Transform stage)
        {
            // ceiling light
            var lamp = UIKit.Rect("Lamp", stage);
            UIKit.Anchor(lamp, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(150, 6));
            var li = lamp.gameObject.AddComponent<Image>();
            li.sprite = Theme.Key(3);
            li.color = Theme.Hex("#CDEBFF");
            li.raycastTarget = false;
            Glow(stage, new Vector2(0.5f, 0.8f), 150, new Color(0.7f, 0.88f, 1f, 0.08f));

            // porthole: steel ring, rivets, and Mars through the glass
            var frame = UIKit.Rect("Porthole", stage);
            UIKit.Anchor(frame, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 418), new Vector2(252, 252));
            var fi = frame.gameObject.AddComponent<Image>();
            fi.sprite = Theme.Circle(256);
            fi.color = Theme.Hex("#4A5378");
            fi.raycastTarget = false;
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2 / 10;
                Rivet(frame, new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 118, Theme.Hex("#7C87B3"), 7);
            }
            var rim = UIKit.Rect("Rim", frame);
            UIKit.Anchor(rim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(222, 222));
            var ri = rim.gameObject.AddComponent<Image>();
            ri.sprite = Theme.Circle(256);
            ri.color = Theme.Hex("#10132A");
            ri.raycastTarget = false;
            var glass = UIKit.Rect("Glass", rim);
            UIKit.Anchor(glass, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210, 210));
            var gi = glass.gameObject.AddComponent<Image>();
            gi.sprite = Theme.Circle(256);
            gi.raycastTarget = false;
            glass.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var sky = UIKit.Rect("Sky", glass);
            UIKit.Stretch(sky);
            var ski = sky.gameObject.AddComponent<Image>();
            ski.sprite = Theme.Gradient(Theme.Hex("#E9925E"), Theme.Hex("#2A1B4A"));
            ski.raycastTarget = false;
            var rnd = new System.Random(11);
            for (int i = 0; i < 14; i++)
            {
                var st = UIKit.Rect("Star", glass);
                UIKit.Anchor(st, new Vector2((float)rnd.NextDouble(), 0.55f + 0.45f * (float)rnd.NextDouble()), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3, 3));
                var sti = st.gameObject.AddComponent<Image>();
                sti.sprite = Theme.Circle(32);
                sti.color = new Color(1, 0.97f, 0.9f, 0.8f);
                sti.raycastTarget = false;
                var tw = st.gameObject.AddComponent<Twinkle>();
                tw.speed = 0.6f + 2 * (float)rnd.NextDouble();
                tw.phase = 6 * (float)rnd.NextDouble();
            }
            Glow(glass, new Vector2(0.24f, 0.36f), 120, new Color(0.6f, 0.8f, 1f, 0.35f));
            Glow(glass, new Vector2(0.24f, 0.36f), 18, new Color(0.95f, 0.98f, 1f, 0.95f));
            Hill(glass, Theme.Hex("#8E4638"), new Vector2(0.25f, 0.02f), new Vector2(380, 150), 2f);
            Hill(glass, Theme.Hex("#5A2638"), new Vector2(0.85f, -0.06f), new Vector2(330, 150), 4f);
            var ridge = UIKit.Rect("Outpost", glass);
            UIKit.Anchor(ridge, new Vector2(0.7f, 0.2f), new Vector2(0.5f, 0), Vector2.zero, new Vector2(80, 34));
            Silhouette(ridge, "hab", new Vector2(-6, 0), new Vector2(46, 30));
            Silhouette(ridge, "dish", new Vector2(28, 0), new Vector2(22, 22));
            // a soft reflection on the glass
            var shine = UIKit.Rect("Shine", glass);
            UIKit.Anchor(shine, new Vector2(0.3f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 34));
            shine.localRotation = Quaternion.Euler(0, 0, 35);
            var shi = shine.gameObject.AddComponent<Image>();
            shi.sprite = Theme.Dot();
            shi.color = new Color(1, 1, 1, 0.16f);
            shi.raycastTarget = false;

            // floor: dark grating with a hazard-striped edge
            var floor = UIKit.Rect("Floor", stage);
            floor.anchorMin = new Vector2(0, 0); floor.anchorMax = new Vector2(1, 0);
            floor.pivot = new Vector2(0.5f, 0);
            floor.offsetMin = new Vector2(0, 0); floor.offsetMax = new Vector2(0, 146);
            var fl = floor.gameObject.AddComponent<Image>();
            fl.sprite = Theme.Rounded(18, 0, false);
            fl.type = Image.Type.Sliced;
            fl.color = Theme.Hex("#11142A");
            fl.raycastTarget = false;
            for (float y = 14; y < 136; y += 12)
            {
                var g = UIKit.Rect("Grate", floor);
                g.anchorMin = new Vector2(0, 0); g.anchorMax = new Vector2(1, 0);
                g.pivot = new Vector2(0.5f, 0.5f);
                g.offsetMin = new Vector2(16, y); g.offsetMax = new Vector2(-16, y + 2);
                var gimg = g.gameObject.AddComponent<Image>();
                gimg.sprite = Theme.White();
                gimg.color = Theme.Hex("#1A1E36");
                gimg.raycastTarget = false;
            }
            var hazard = UIKit.Rect("Hazard", floor);
            hazard.anchorMin = new Vector2(0, 1); hazard.anchorMax = new Vector2(1, 1);
            hazard.pivot = new Vector2(0.5f, 1);
            hazard.offsetMin = new Vector2(0, -8); hazard.offsetMax = new Vector2(0, 2);
            var hz = hazard.gameObject.AddComponent<Image>();
            hz.sprite = Theme.Stripes();
            hz.type = Image.Type.Tiled;
            hz.color = new Color(1, 1, 1, 0.85f);
            hz.raycastTarget = false;
        }

        // What a mode actually changes, built from Balance.json so the numbers stay true.
        static string ModeBlurb(ModeDef m)
        {
            if (m == null) return "";
            // One short line a kid can read at a glance; the details are taught in play.
            return "Survive " + m.sols + " sols · " +
                   (m.lateWarningChance > 0 ? "surprise storms" : "Earth warns you about storms") + " · " +
                   (m.realUnits ? "real science numbers" : "easy meters");
        }

        static void Section(Transform parent, string text)
        {
            var t = UIKit.Label(parent, text, 12, Theme.Gold);
            t.font = Theme.BodyBlack;
            UIKit.Size(t, -1, 18, -1, 18);
        }

        static void Gap(Transform parent, float h) => UIKit.Size(UIKit.Rect("Gap", parent), -1, h, -1, h);

        // A selectable tile: title and a smaller line under it. Select() highlights it.
        static Button Choice(Transform parent, string title, string sub, Action onPick,
                             Sprite icon = null, Color accent = default, int pips = 0, string level = null)
        {
            var b = UIKit.Button(parent, "<b>" + title + "</b>\n<size=14><color=#C9BEE0>" + sub + "</color></size>", () => onPick(), Theme.NightSoft, 18);
            UIKit.Size(b, -1, 64, -1, 70);
            var label = b.transform.Find("Label").GetComponent<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.lineSpacing = 0.95f;
            label.rectTransform.offsetMin = new Vector2(icon ? 74 : 18, label.rectTransform.offsetMin.y);
            label.rectTransform.offsetMax = new Vector2(pips > 0 ? -70 : -40, label.rectTransform.offsetMax.y);
            if (icon)
            {
                // A coloured badge with the mode's icon, like a lit console button.
                var badge = UIKit.Rect("Badge", b.transform);
                UIKit.Anchor(badge, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38, 2), new Vector2(48, 48));
                var bimg = badge.gameObject.AddComponent<Image>();
                bimg.sprite = Theme.Key(14);
                bimg.color = accent;
                bimg.raycastTarget = false;
                var sh = badge.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.35f);
                sh.effectDistance = new Vector2(0, -3);
                var ic = UIKit.Rect("Icon", badge);
                UIKit.Anchor(ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
                var iimg = ic.gameObject.AddComponent<Image>();
                iimg.sprite = icon;
                iimg.preserveAspect = true;
                iimg.color = Theme.Paper;
                iimg.raycastTarget = false;
            }
            if (pips > 0)
            {
                // Difficulty: a word and 1-3 lit lights.
                var meter = UIKit.Rect("Level", b.transform);
                UIKit.Anchor(meter, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-38, 2), new Vector2(56, 40));
                var word = UIKit.Label(meter, level ?? "", 11, accent, TextAnchor.MiddleCenter);
                word.font = Theme.BodyBlack;
                word.raycastTarget = false;
                UIKit.Anchor(word.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(56, 16));
                for (int i = 0; i < 3; i++)
                {
                    var pip = UIKit.Rect("Pip", meter);
                    UIKit.Anchor(pip, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((i - 1) * 15, 4), new Vector2(11, 11));
                    var pi = pip.gameObject.AddComponent<Image>();
                    pi.sprite = Theme.Circle(32);
                    pi.color = i < pips ? accent : new Color(1, 1, 1, 0.14f);
                    pi.raycastTarget = false;
                }
            }
            var ring = b.gameObject.AddComponent<Outline>();
            ring.effectColor = Theme.Gold;
            ring.effectDistance = new Vector2(2.5f, -2.5f);
            ring.useGraphicAlpha = false;
            if (pips > 0) return b; // the tile colour and gold ring show the pick
            var tick = UIKit.Rect("Tick", b.transform);
            UIKit.Anchor(tick, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-22, 2), new Vector2(24, 24));
            var ti = tick.gameObject.AddComponent<Image>();
            ti.sprite = Theme.Circle(64);
            ti.color = Theme.Gold;
            ti.raycastTarget = false;
            var inner = UIKit.Rect("Inner", tick);
            UIKit.Anchor(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            var ii = inner.gameObject.AddComponent<Image>();
            ii.sprite = Theme.Circle(32);
            ii.color = Theme.Ink;
            ii.raycastTarget = false;
            return b;
        }

        static void Select(Button b, bool on)
        {
            b.image.color = on ? Theme.Hex("#4B3C8F") : Theme.NightSoft;
            b.GetComponent<Outline>().enabled = on;
            var tick = b.transform.Find("Tick");
            if (tick) tick.gameObject.SetActive(on);
            var badge = b.transform.Find("Badge");
            if (badge)
            {
                var img = badge.GetComponent<Image>();
                var c = img.color; c.a = on ? 1f : 0.45f; img.color = c;
            }
        }

        InputField Input(Transform parent, string value, string placeholder, int limit)
        {
            var bg = UIKit.Panel(parent, Theme.Hex("#120D26"), "Input", true, 14);
            var input = bg.gameObject.AddComponent<InputField>();
            var text = UIKit.Label(bg.transform, "", 19, Theme.Paper);
            UIKit.Stretch(text.rectTransform, 16, 4, 16, 4);
            text.supportRichText = false;
            var ph = UIKit.Label(bg.transform, placeholder, 17, new Color(0.79f, 0.75f, 0.88f, 0.5f));
            UIKit.Stretch(ph.rectTransform, 16, 4, 16, 4);
            ph.fontStyle = FontStyle.Italic;
            input.textComponent = text;
            input.placeholder = ph;
            input.characterLimit = limit;
            input.caretColor = Theme.Gold;
            input.customCaretColor = true;
            input.selectionColor = new Color(1f, 0.76f, 0.24f, 0.35f);
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
            var bg = Backdrop("Ending", s.ending != "early", true);
            var content = UIKit.Scroll(bg, out var scroll);
            UIKit.Stretch((RectTransform)scroll.transform, 60, 20, 60, 20);
            var vl = content.GetComponent<VerticalLayoutGroup>();
            vl.spacing = 14;
            vl.childAlignment = TextAnchor.UpperCenter;

            var head = UIKit.Label(content, ending.title, 64, Theme.Paper, TextAnchor.MiddleCenter, true);
            var hs = head.gameObject.AddComponent<Shadow>();
            hs.effectColor = Theme.Mars;
            hs.effectDistance = new Vector2(0, -5);
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

            var facts = UIKit.Panel(content, Theme.Glass, "Real", true, 20);
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
                    if (b != null) UIKit.Label(content, "<color=#FFD27A>Badge</color>  <b>" + b.name + "</b>: " + b.text, 18, Theme.Paper, TextAnchor.MiddleCenter);
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
                Entry(list, true, "Gale crater map and rover photos", "The trip map is a real Mars Odyssey image of Gale crater. Each Gale trip shows a real Curiosity photo of the place: Bagnold Dunes, Rocknest and Yellowknife Bay.", "NASA/JPL-Caltech/ASU (PIA14290); NASA/JPL-Caltech/MSSS (PIA19928, PIA16204, PIA16701)");
                Entry(list, true, "Water in Mars soil (Curiosity SAM)", "Gale is near the equator, where there is no shallow buried ice. Curiosity found its soil holds about 2% water, so in this game you bake soil for water.", "Leshin et al. 2013, Science 341, 1238937");
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
            var p = UIKit.Panel(list, found ? Color.white : Theme.Hex("#ECE6F0"), "Entry", found, 14);
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
                UIKit.Button(card, "Resume", () => { CloseModal(card); done(); }, Theme.Power, 20);
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
                    "• Go outside to gather regolith, water, rock samples and parts. Trips cost radiation.\n" +
                    "• <b>Build</b> upgrades: each one helps one meter and costs another. Place it, then work on the site.\n" +
                    "• Use the <b>Radio</b> to hear whether a solar storm is coming, and seal the <b>Storm Shelter</b> before it hits.\n" +
                    "• Before the next crew lands, build a <b>landing pad</b> (it unlocks in Act 3).", 17);
                UIKit.Button(card, "Got it", () => { CloseModal(card); done(); }, Theme.Power);
            });
        }
    }

    // The logo breathes and tilts a touch (layout groups ignore scale and rotation).
    public class Bob : MonoBehaviour
    {
        void Update()
        {
            var gm = GameManager.I;
            if (gm != null && gm.Profile != null && gm.Profile.reducedMotion) { transform.localRotation = Quaternion.identity; return; }
            float t = Time.unscaledTime;
            transform.localScale = Vector3.one * (1 + 0.012f * Mathf.Sin(t * 1.4f));
            transform.localRotation = Quaternion.Euler(0, 0, 1.2f * Mathf.Sin(t * 0.9f));
        }
    }

    // The character on the setup stage: a gentle idle bob and sway.
    public class Idle : MonoBehaviour
    {
        Vector2 start;
        bool started;
        void Update()
        {
            var rt = (RectTransform)transform;
            if (!started) { start = rt.anchoredPosition; started = true; }
            var gm = GameManager.I;
            if (gm != null && gm.Profile != null && gm.Profile.reducedMotion) { rt.anchoredPosition = start; rt.localRotation = Quaternion.identity; return; }
            float t = Time.unscaledTime;
            rt.anchoredPosition = start + new Vector2(0, 6 * Mathf.Sin(t * 1.8f));
            rt.localRotation = Quaternion.Euler(0, 0, 2.5f * Mathf.Sin(t * 0.9f));
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

using System.Collections.Generic;
using OutpostGale.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace OutpostGale
{
    // The inside of the base, drawn side-on: one room per indoor module, joined in a row from the hab
    // to the airlock. You walk left and right; E (or a click) uses the room you're in, through the same
    // stations as the map. Purely a view: every rule still goes through Mission.
    public partial class WorldView
    {
        public bool Inside { get; private set; }

        static readonly HashSet<string> IndoorIds = new HashSet<string> { "hab", "oxygen", "recycler", "greenhouse", "battery", "gym", "shelter", "airlock" };
        const float RoomH = 4f;
        const float InsideCamSize = 3.4f;
        static readonly Vector3 InsideOrigin = new Vector3(0, 400, 0);

        class Room
        {
            public PlacedModule m;     // null for the command deck
            public float x0, x1;
            public Light2D lamp, alarm;
            public SpriteRenderer dark, dust, machine, screen, alarmStrip;
            public TextMesh status;
            public float Centre => (x0 + x1) / 2f;
        }

        // Something you can use inside: a deck screen (kiosk) or a module, optionally with one action.
        // Its area is just the object (x ± half), so the prompt only shows when you're right at it.
        class Spot
        {
            public string kiosk, prompt, action;
            public PlacedModule m;
            public float x, half, top, bottom;
        }
        readonly List<Spot> spots = new List<Spot>();
        Spot insideWalkSpot;
        Transform spotMarker;
        TextMesh statusSupplies, directoryText, newsText, newsTitle;
        readonly Dictionary<string, Transform> statusFills = new Dictionary<string, Transform>();
        readonly Dictionary<string, TextMesh> statusValues = new Dictionary<string, TextMesh>();
        float newsTimer;

        Transform insideRoot;
        readonly List<Room> rooms = new List<Room>();
        string roomsSignature;
        Transform insidePlayer, insideBolt;
        readonly List<Crew> insideCrew = new List<Crew>();
        float insideX, insideTarget = float.NaN;
        string insideWalkAction;
        float insideMaxX;

        public void SetInside(bool on, bool sound = true)
        {
            if (M == null) return;
            if (on) StopGhost();
            Inside = on;
            if (on)
            {
                EnsureInterior();
                var board = spots.Find(k => k.kiosk == "status");
                insideX = board != null ? board.x : 1f;
                insideTarget = float.NaN;
                insideWalkSpot = null;
                insideWalkAction = null;
            }
            if (insideRoot) insideRoot.gameObject.SetActive(on);
            if (highlight) highlight.gameObject.SetActive(false);
            keyMove = 0;
            if (!on) FitCamera();
            if (sound) AudioFx.Click();
        }

        // Leave through the airlock: switch to the map with you standing at the airlock.
        public void StepOutside(PlacedModule airlock)
        {
            SetInside(false, false);
            path.Clear();
            walkTarget = null;
            walkAction = null;
            if (airlock != null)
            {
                playerCell = new Cell(airlock.x, airlock.y);
                playerPos = Tile(playerCell.x, playerCell.y);
                player.localPosition = playerPos;
            }
            AudioFx.Click();
        }

        // ---------- building the rooms ----------

        string RoomsSignature()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var m in M.S.modules)
                if (IndoorIds.Contains(m.id)) sb.Append(m.uid).Append(m.built ? 'b' : 'u').Append(',');
            return sb.ToString();
        }

        void EnsureInterior()
        {
            string sig = RoomsSignature();
            if (insideRoot && sig == roomsSignature) return;
            roomsSignature = sig;
            if (insideRoot) Destroy(insideRoot.gameObject);
            rooms.Clear();
            spots.Clear();
            statusFills.Clear();
            statusValues.Clear();
            insideCrew.Clear();
            insideRoot = new GameObject("Inside").transform;
            insideRoot.SetParent(transform, false);
            insideRoot.localPosition = InsideOrigin;
            insideRoot.gameObject.SetActive(Inside);

            // Hab first, then rooms by how close they are to it, the airlock last (it leads outside).
            var grid = M.Layout();
            var hab = grid.First("hab");
            var list = M.S.modules.FindAll(m => IndoorIds.Contains(m.id));
            Vector3 habPos = hab != null ? Rect(hab) : Vector3.zero;
            list.Sort((a, b) =>
            {
                int ka = a.id == "hab" ? 0 : a.id == "airlock" ? 2 : 1, kb = b.id == "hab" ? 0 : b.id == "airlock" ? 2 : 1;
                if (ka != kb) return ka.CompareTo(kb);
                return (Rect(a) - habPos).sqrMagnitude.CompareTo((Rect(b) - habPos).sqrMagnitude);
            });

            float x = 0;
            EndCap(x - 0.3f);
            // The command deck comes first: status board, directory and NASA news.
            var deck = new Room { m = null, x0 = 0, x1 = 9f };
            BuildDeck(deck, list);
            rooms.Add(deck);
            x = deck.x1;
            Bulkhead(x);
            foreach (var m in list)
            {
                float w = m.id == "hab" ? 7f : m.id == "greenhouse" || m.id == "shelter" ? 5f : 4f;
                var r = new Room { m = m, x0 = x, x1 = x + w };
                BuildRoom(r);
                rooms.Add(r);
                x += w;
                Bulkhead(x);
            }
            insideMaxX = x;
            EndCap(x + 0.3f);

            // People: you, BOLT, and the crew in the hab.
            var you = MakePerson(string.IsNullOrWhiteSpace(M.S.commanderName) ? "You" : M.S.commanderName, SuitTint(M.S.suitColor), 0.8f, out _, "astronaut");
            you.transform.SetParent(insideRoot, false);
            insidePlayer = you.transform;
            var boltArt = Theme.Art("bolt");
            var b = new GameObject("BOLT").transform;
            b.SetParent(insideRoot, false);
            if (boltArt) Fit(Sprite("Body", b, boltArt, Color.white, 25), 0.75f, 0.75f);
            else Sprite("Body", b, Theme.Circle(64, true), Theme.Power, 25).transform.localScale = Vector3.one * 0.4f;
            insideBolt = b;
            var habRoom = rooms.Find(r => r.m != null && r.m.id == "hab");
            if (habRoom != null)
            {
                string[] names = { "Mira", "Tomas" };
                Color[] colors = { Color.Lerp(Color.white, Theme.Health, 0.7f), Color.Lerp(Color.white, Theme.Life, 0.7f) };
                for (int i = 0; i < 2; i++)
                {
                    var go = MakePerson(names[i], colors[i], 0.72f, out var body, i == 0 ? "mira" : "tomas");
                    go.transform.SetParent(insideRoot, false);
                    var c = new Crew { t = go.transform, face = body, baseColor = CrewColor(colors[i], i == 0 ? "mira" : "tomas") };
                    c.t.localPosition = new Vector3(habRoom.x0 + 2.4f + i * 2.2f, 0.62f, 0);
                    c.target = c.t.localPosition;
                    SetOrder(c.t, 18);
                    insideCrew.Add(c);
                }
            }
            var marker = new GameObject("UseMarker").transform;
            marker.SetParent(insideRoot, false);
            var ring = Sprite("Ring", marker, Theme.Circle(64), Theme.Gold, 30);
            ring.transform.localScale = Vector3.one * 0.42f;
            Text("E", marker, "E", 0.07f, Theme.Ink, 31).transform.localPosition = new Vector3(0, -0.01f, 0);
            marker.gameObject.SetActive(false);
            spotMarker = marker;
            insideX = Mathf.Clamp(insideX, 0.5f, Mathf.Max(0.5f, insideMaxX - 0.5f));
            RefreshInterior();
        }

        SpriteRenderer Box(string name, Transform parent, Vector3 pos, Vector2 size, Color c, int order, int radius = 0)
        {
            var s = Sprite(name, parent, radius > 0 ? Theme.Rounded(radius, 0, false) : Theme.White(), c, order);
            if (radius > 0)
            {
                s.drawMode = SpriteDrawMode.Sliced;
                s.size = size;
            }
            else s.transform.localScale = new Vector3(size.x / s.sprite.bounds.size.x, size.y / s.sprite.bounds.size.y, 1);
            s.transform.localPosition = pos;
            return s;
        }

        void EndCap(float x)
        {
            Box("EndCap", insideRoot, new Vector3(x, RoomH / 2f, 0), new Vector2(0.6f, RoomH + 1.2f), Theme.Hex("#2B3150"), -28);
        }

        // The ring between two rooms: a thick frame with rivets.
        void Bulkhead(float x)
        {
            Box("Bulkhead", insideRoot, new Vector3(x, RoomH / 2f, 0), new Vector2(0.36f, RoomH + 0.6f), Theme.Hex("#2B3150"), -30);
            Box("BulkheadEdge", insideRoot, new Vector3(x, RoomH / 2f, 0), new Vector2(0.08f, RoomH + 0.6f), Theme.Hex("#4A5378"), -29);
            for (float y = 0.4f; y < RoomH; y += 0.7f)
                Box("Rivet", insideRoot, new Vector3(x - 0.1f, y, 0), new Vector2(0.06f, 0.06f), Theme.Hex("#7C87B3"), -28, 0).sprite = Theme.Circle(32);
        }

        void BuildRoom(Room r)
        {
            var root = new GameObject(r.m.id).transform;
            root.SetParent(insideRoot, false);
            float w = r.x1 - r.x0, cx = r.Centre;
            var def = M.B.Module(r.m.id);
            Color accent = Theme.Hex(string.IsNullOrEmpty(def.color) ? "#FFFFFF" : def.color);

            // Walls, floor, lights and sign; the back wall is a little tinted with the module's colour.
            Shell(root, r, Color.Lerp(Theme.Hex("#59628A"), accent, 0.12f), ShortName(def).ToUpperInvariant());

            // A window onto Mars (not in the airlock: that has its hatch).
            float winX = r.m.id == "hab" ? cx - 0.6f : cx - w * 0.22f;
            if (r.m.id != "airlock") r.dust = Window(root, new Vector3(winX, 2.35f, 0), new Vector2(w >= 5 ? 1.8f : 1.4f, 1.05f));

            switch (r.m.id)
            {
                case "hab": HabProps(root, r); break;
                case "greenhouse": GreenhouseProps(root, r); break;
                case "airlock": AirlockProps(root, r); break;
                default: MachineProps(root, r); break;
            }
            if (!r.m.built)
            {
                // A construction site: one spot in the middle to work on it.
                spots.RemoveAll(sp => sp.m == r.m);
                AddSpot(r.m, cx, 1.2f, 2.6f);
            }

            r.status = Text("Status", root, "", 0.075f, Theme.Gold, 16);
            r.status.transform.localPosition = new Vector3(r.m.id == "hab" ? cx + 0.8f : cx + w * 0.18f, 3.0f, 0);

            // Darkness when the room has no power; stripes while it is still being built.
            r.dark = Box("Dark", root, new Vector3(cx, RoomH / 2f, 0), new Vector2(w, RoomH + 1f), new Color(0.04f, 0.03f, 0.1f, 0.5f), 15);
            r.dark.enabled = false;
            if (!r.m.built)
            {
                var st = Sprite("Stripes", root, Theme.Stripes(), Color.white, 6);
                st.drawMode = SpriteDrawMode.Tiled;
                st.size = new Vector2(w, 0.22f);
                st.transform.localPosition = new Vector3(cx, 0.11f, 0);
                for (int i = 0; i < 3; i++)
                    Box("Scaffold", root, new Vector3(r.x0 + (i + 0.5f) * w / 3f, 1.6f, 0), new Vector2(0.08f, 3.2f), Theme.Hex("#B9A27A"), 5);
                Box("ScaffoldBar", root, new Vector3(cx, 2.4f, 0), new Vector2(w * 0.9f, 0.08f), Theme.Hex("#B9A27A"), 5);
            }
        }

        // A window with Mars outside: dusk sky, a hill, stars. Returns the dust overlay.
        SpriteRenderer Window(Transform root, Vector3 pos, Vector2 size)
        {
            Box("WindowFrame", root, pos, size + new Vector2(0.22f, 0.22f), Theme.Hex("#2B3150"), -46, 10);
            var sky = Sprite("Sky", root, Theme.Gradient(Theme.Hex("#E9925E"), Theme.Hex("#2A1B4A")), Color.white, -45);
            sky.transform.localScale = new Vector3(size.x / sky.sprite.bounds.size.x, size.y / sky.sprite.bounds.size.y, 1);
            sky.transform.localPosition = pos;
            var rnd = new System.Random((int)(pos.x * 100));
            for (int i = 0; i < 5; i++)
                Box("Star", root, pos + new Vector3(((float)rnd.NextDouble() - 0.5f) * size.x * 0.9f, size.y * (0.1f + 0.35f * (float)rnd.NextDouble()), 0),
                    new Vector2(0.04f, 0.04f), new Color(1, 0.97f, 0.9f, 0.8f), -44);
            Box("Hill", root, pos + new Vector3(0, -size.y * 0.32f, 0), new Vector2(size.x, size.y * 0.36f), Theme.Hex("#8E4638"), -43);
            Box("HillFar", root, pos + new Vector3(size.x * 0.2f, -size.y * 0.16f, 0), new Vector2(size.x * 0.5f, size.y * 0.1f), Theme.Hex("#7A3D52"), -44, 0);
            var dust = Box("WindowDust", root, pos, size, new Color(0.85f, 0.5f, 0.25f, 0), -42);
            Box("Glare", root, pos + new Vector3(-size.x * 0.25f, size.y * 0.25f, 0), new Vector2(size.x * 0.12f, size.y * 0.3f), new Color(1, 1, 1, 0.12f), -41);
            return dust;
        }

        void HabProps(Transform root, Room r)
        {
            float x0 = r.x0;
            // Bunks on the left.
            for (int i = 0; i < 2; i++)
            {
                float y = 0.42f + i * 0.95f;
                Box("Bunk", root, new Vector3(x0 + 1.2f, y, 0), new Vector2(1.8f, 0.32f), Theme.Hex("#2B3150"), 0, 6);
                Box("Blanket", root, new Vector3(x0 + 1.35f, y + 0.18f, 0), new Vector2(1.35f, 0.16f), i == 0 ? Theme.Life : Theme.Mars, 1, 4);
                Box("Pillow", root, new Vector3(x0 + 0.5f, y + 0.2f, 0), new Vector2(0.38f, 0.16f), Theme.Paper, 1, 4);
            }
            Box("BunkPost", root, new Vector3(x0 + 2.1f, 0.95f, 0), new Vector2(0.07f, 1.9f), Theme.Hex("#2B3150"), 0);
            AddSpot(r.m, x0 + 1.2f, 0.95f, 1.6f, "rest");
            // Table in the middle.
            Box("Table", root, new Vector3(r.Centre, 0.72f, 0), new Vector2(1.3f, 0.12f), Theme.Hex("#C9BEE0"), 0, 3);
            Box("Leg", root, new Vector3(r.Centre, 0.35f, 0), new Vector2(0.1f, 0.7f), Theme.Hex("#8F84AD"), -1);
            Box("Mug", root, new Vector3(r.Centre - 0.3f, 0.86f, 0), new Vector2(0.14f, 0.16f), Theme.Power, 1, 3);
            Box("Mug", root, new Vector3(r.Centre + 0.25f, 0.86f, 0), new Vector2(0.14f, 0.16f), Theme.Health, 1, 3);
            // Radio console on the right.
            float rx = r.x1 - 1.25f;
            Box("Desk", root, new Vector3(rx, 0.42f, 0), new Vector2(1.6f, 0.84f), Theme.Hex("#2B3150"), 0, 8);
            for (int i = 0; i < 4; i++)
                Box("Knob", root, new Vector3(rx - 0.5f + i * 0.33f, 0.62f, 0), new Vector2(0.12f, 0.12f), i == 0 ? Theme.Warn : Theme.Soft, 1).sprite = Theme.Circle(32);
            Box("ScreenFrame", root, new Vector3(rx, 1.55f, 0), new Vector2(1.3f, 0.85f), Theme.Hex("#151A2E"), 0, 8);
            r.screen = Box("Screen", root, new Vector3(rx, 1.55f, 0), new Vector2(1.1f, 0.65f), Theme.Hex("#1F5F8F"), 1, 5);
            var icon = Theme.Art("icon_radio");
            if (icon)
            {
                var ic = Sprite("RadioIcon", root, icon, Theme.Paper, 2);
                Fit(ic, 0.4f, 0.4f);
                ic.transform.localPosition = new Vector3(rx, 1.55f, 0);
            }
            Box("Antenna", root, new Vector3(rx + 0.55f, 2.15f, 0), new Vector2(0.04f, 0.4f), Theme.Soft, 0);
            AddSpot(r.m, rx, 0.85f, 2.1f, "radio");
            Box("Plant", root, new Vector3(r.x1 - 2.4f, 0.3f, 0), new Vector2(0.3f, 0.3f), Theme.Hex("#8E4638"), 0, 4);
            Box("Leaves", root, new Vector3(r.x1 - 2.4f, 0.6f, 0), new Vector2(0.45f, 0.4f), Theme.Health, 1).sprite = Theme.Circle(64);
        }

        void GreenhouseProps(Transform root, Room r)
        {
            float rx = r.Centre + 0.9f;
            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 0.35f + shelf * 0.8f;
                Box("Shelf", root, new Vector3(rx, y, 0), new Vector2(2.4f, 0.1f), Theme.Hex("#C9BEE0"), 0, 3);
                for (int p = 0; p < 5; p++)
                {
                    float px = rx - 1f + p * 0.5f;
                    Box("Pot", root, new Vector3(px, y + 0.12f, 0), new Vector2(0.22f, 0.16f), Theme.Hex("#8E4638"), 1, 3);
                    Box("Leaf", root, new Vector3(px, y + 0.33f, 0), new Vector2(0.34f, 0.3f), Theme.Health, 2).sprite = Theme.Circle(64);
                }
            }
            Box("GrowLight", root, new Vector3(rx, 2.8f, 0), new Vector2(2.4f, 0.08f), new Color(1f, 0.55f, 0.95f), 1, 4);
            AddSpot(r.m, rx, 1.25f, 2.3f);
        }

        void AirlockProps(Transform root, Room r)
        {
            // The round hatch out to Mars, with hazard stripes around its sill.
            var hatchPos = new Vector3(r.x1 - 1.25f, 1.55f, 0);
            AddSpot(r.m, hatchPos.x, 0.95f, 2.5f);
            Box("HatchRing", root, hatchPos, new Vector2(1.9f, 1.9f), Theme.Hex("#6B76A0"), -40).sprite = Theme.Circle(128);
            Box("Hatch", root, hatchPos, new Vector2(1.6f, 1.6f), Theme.Hex("#3A4266"), -39).sprite = Theme.Circle(128);
            r.dust = null;
            var port = Box("Port", root, hatchPos + new Vector3(0, 0.25f, 0), new Vector2(0.6f, 0.6f), Theme.Hex("#E9925E"), -38);
            port.sprite = Theme.Circle(64);
            Box("Wheel", root, hatchPos + new Vector3(0, -0.35f, 0), new Vector2(0.5f, 0.08f), Theme.Hex("#C9BEE0"), -37, 3);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                Box("Bolt", root, hatchPos + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.86f, new Vector2(0.08f, 0.08f), Theme.Hex("#9AA4CC"), -38).sprite = Theme.Circle(32);
            }
            var st = Sprite("Sill", root, Theme.Stripes(), Color.white, -19);
            st.drawMode = SpriteDrawMode.Tiled;
            st.size = new Vector2(r.x1 - r.x0, 0.18f);
            st.transform.localPosition = new Vector3(r.Centre, -0.05f, 0);
            // Spare suits: two empty suits on hangers, helmets on the shelf above (drawn art), so they read as gear.
            var rack = Theme.Art("airlock_rack");
            if (rack)
            {
                var rs = Sprite("SuitRack", root, rack, Color.white, -35);
                Fit(rs, 1.7f, 1.49f);
                rs.transform.localPosition = new Vector3(r.x0 + 1.05f, 1.95f, 0);
            }
        }

        // Any other room: its machine (the module's art) on a plinth, with a control panel.
        void MachineProps(Transform root, Room r)
        {
            float w = r.x1 - r.x0;
            float mx = r.Centre + w * 0.18f;
            AddSpot(r.m, mx, 0.95f, 2.0f);
            Box("Plinth", root, new Vector3(mx, 0.12f, 0), new Vector2(1.8f, 0.24f), Theme.Hex("#2B3150"), 0, 4);
            var art = Theme.Art(r.m.id);
            if (art)
            {
                r.machine = Sprite("Machine", root, art, Color.white, 1);
                Fit(r.machine, 1.7f, 1.7f);
                r.machine.transform.localPosition = new Vector3(mx, 1.1f, 0);
            }
            else
                r.machine = Box("Machine", root, new Vector3(mx, 0.95f, 0), new Vector2(1.5f, 1.4f), Theme.Hex(string.IsNullOrEmpty(M.B.Module(r.m.id).color) ? "#C9BEE0" : M.B.Module(r.m.id).color), 1, 12);
            // Control panel with blinking lights.
            float px = r.x0 + 0.7f;
            Box("Panel", root, new Vector3(px, 1.7f, 0), new Vector2(0.75f, 0.95f), Theme.Hex("#151A2E"), -10, 6);
            for (int i = 0; i < 6; i++)
            {
                var l = Box("Led", root, new Vector3(px - 0.18f + (i % 3) * 0.18f, 1.85f - (i / 3) * 0.3f, 0), new Vector2(0.1f, 0.1f),
                            i % 2 == 0 ? Theme.Health : Theme.Power, -9);
                l.sprite = Theme.Circle(32);
                if (i == 1 || i == 4) l.gameObject.AddComponent<Blink>().speed = 1.3f + i * 0.4f;
            }
        }

        // ---------- per-frame and refresh ----------

        void RefreshInterior()
        {
            if (!insideRoot) return;
            var grid = M.Layout();
            RefreshDeck();
            foreach (var r in rooms)
            {
                if (r.m == null || !M.S.modules.Contains(r.m)) continue;
                var def = M.B.Module(r.m.id);
                bool active = M.IsActive(r.m);
                bool powered = active && !M.S.systemsDownLastNight;
                bool usesPower = def.powerUse > 0 || r.m.id == "hab" || r.m.id == "shelter";
                r.dark.enabled = !r.m.built || (usesPower && !powered);
                r.status.text = ModuleStatus(r.m, grid);
                bool trouble = M.S.events.Exists(x => x.targetUid == r.m.uid || (x.targetUid == 0 && M.B.Event(x.id)?.station == r.m.id));
                r.status.color = trouble ? Theme.Warn : Theme.Gold;
                if (r.machine) r.machine.color = r.dark.enabled ? new Color(0.55f, 0.52f, 0.6f) : Color.white;
            }
        }

        void InteriorUpdate()
        {
            if (!insideRoot) return;
            EnsureInterior();
            // Camera follows you along the row of rooms.
            cam.orthographic = true;
            float aspect = Mathf.Max(0.5f, (float)Screen.width / Mathf.Max(1, Screen.height));
            cam.orthographicSize = Mathf.Max(InsideCamSize, 4.6f / aspect);
            cam.backgroundColor = Theme.Hex("#10132A");
            float half = cam.orthographicSize * aspect;
            float cx = insideMaxX <= half * 2 - 0.6f ? insideMaxX / 2f : Mathf.Clamp(insideX, half - 0.6f, insideMaxX - half + 0.6f);
            // Keep the floor just above the bottom HUD band whatever the zoom.
            cam.transform.position = InsideOrigin + new Vector3(cx, cam.orthographicSize * 0.62f - 0.1f, -10);
            if (shake > 0 && !gm.Profile.reducedMotion)
            {
                shake -= Time.deltaTime;
                cam.transform.position += (Vector3)Random.insideUnitCircle * 0.1f * Mathf.Clamp01(shake * 3);
            }

            if (gm.WorldInputOpen) InteriorInput();
            else insideTarget = float.NaN;

            // Walk.
            float prevX = insideX;
            if (!float.IsNaN(insideTarget))
            {
                insideX = Mathf.MoveTowards(insideX, insideTarget, 3.2f * Time.deltaTime);
                if (Mathf.Abs(insideX - insideTarget) < 0.01f)
                {
                    insideTarget = float.NaN;
                    var spot = insideWalkSpot;
                    var act = insideWalkAction;
                    insideWalkSpot = null;
                    insideWalkAction = null;
                    if (spot != null) UseSpot(spot, act);
                }
            }
            insideX = Mathf.Clamp(insideX + keyMove * 3.2f * Time.deltaTime, 0.4f, Mathf.Max(0.4f, insideMaxX - 0.4f));
            float vx = (insideX - prevX) / Mathf.Max(0.0001f, Time.deltaTime);
            bool walking = Mathf.Abs(vx) > 0.05f;
            float bob = walking && !gm.Profile.reducedMotion ? Mathf.Abs(Mathf.Sin(Time.time * 10)) * 0.05f : 0;
            insidePlayer.localPosition = new Vector3(insideX, 0.68f + bob, 0);
            Pose(insidePlayer, new Vector2(vx, 0));
            SetOrder(insidePlayer, 20);
            var bt = new Vector3(insideX + (lastFacing < 0 ? 0.7f : -0.7f), 0.95f + Mathf.Sin(Time.time * 3) * 0.08f, 0); // low, beside you, clear of the wall screens
            if (walking) lastFacing = vx;
            insideBolt.localPosition = Vector3.Lerp(insideBolt.localPosition, bt, Time.deltaTime * 4);

            // The "E" marker floats over the thing you can use right now.
            var near = gm.WorldInputOpen ? SpotAt(insideX) : null;
            if (spotMarker)
            {
                spotMarker.gameObject.SetActive(near != null);
                if (near != null)
                    spotMarker.localPosition = new Vector3(near.x, near.top + 0.3f + (gm.Profile.reducedMotion ? 0 : Mathf.Sin(Time.time * 4) * 0.06f), 0);
            }

            // Crew potter about the hab.
            var hab = rooms.Find(r => r.m != null && r.m.id == "hab");
            float mood = Mathf.Min(M.S.health, M.S.life) / 100f;
            foreach (var c in insideCrew)
            {
                c.wait -= Time.deltaTime;
                if (c.wait <= 0 && hab != null)
                {
                    c.target = new Vector3(Random.Range(hab.x0 + 1.4f, hab.x1 - 1.4f), 0.62f, 0);
                    c.wait = Random.Range(2.5f, 6f);
                }
                var before = c.t.localPosition;
                c.t.localPosition = Vector3.MoveTowards(c.t.localPosition, c.target, Time.deltaTime * 0.9f);
                Pose(c.t, (c.t.localPosition - before) / Mathf.Max(0.0001f, Time.deltaTime));
                c.face.color = Color.Lerp(Theme.Muted, c.baseColor, Mathf.Clamp01(mood * 2f));
            }

            // Lights: brighter at night, off without power; storms flash the alarm, dust browns the windows.
            float t = Time.time;
            foreach (var r in rooms)
            {
                bool lit = r.m == null || (r.m.built && !r.dark.enabled);
                r.lamp.intensity = lit ? 0.35f + nightAmount * 0.9f : 0.05f;
                bool alarm = stormFx || (r.m != null && M.S.events.Exists(x => x.targetUid == r.m.uid));
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
                r.alarm.intensity = alarm ? pulse * 1.2f : 0;
                r.alarmStrip.enabled = alarm && pulse > 0.5f;
                if (r.dust) r.dust.color = new Color(0.85f, 0.5f, 0.25f, Mathf.Clamp01(dustLevel * 3f));
                if (r.screen) r.screen.color = Color.Lerp(Theme.Hex("#1F5F8F"), Theme.Hex("#3BA7F0"), 0.5f + 0.5f * Mathf.Sin(t * 2f));
            }
            // The news screen picks up the live Sun report when it arrives.
            newsTimer -= Time.deltaTime;
            if (newsTimer <= 0) { newsTimer = 3f; RefreshNews(); }
        }

        float keyMove, lastFacing = 1;

        void InteriorInput()
        {
            var kb = Keyboard.current;
            keyMove = 0;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keyMove = -1;
                else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keyMove = 1;
                if (keyMove != 0) { insideTarget = float.NaN; insideWalkSpot = null; insideWalkAction = null; }
                if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                {
                    var here = SpotAt(insideX);
                    if (here != null) UseSpot(here, null);
                }
            }
            if (Clicked() && !PointerOverUi() && cam)
            {
                Vector2 screen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) screen = Touchscreen.current.primaryTouch.position.ReadValue();
                var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10)) - InsideOrigin;
                if (w.y > -0.6f && w.y < RoomH + 0.6f)
                {
                    // Clicking a usable thing walks you to it and uses it; clicking anywhere else just walks.
                    Spot hit = null;
                    foreach (var s in spots)
                        if (Mathf.Abs(w.x - s.x) <= s.half && w.y <= s.top + 0.4f && w.y >= s.bottom - 0.2f && (hit == null || Mathf.Abs(w.x - s.x) < Mathf.Abs(w.x - hit.x))) hit = s;
                    insideWalkAction = null;
                    if (hit != null)
                    {
                        insideWalkSpot = hit;
                        insideTarget = SpotAt(insideX) == hit ? insideX : hit.x;
                    }
                    else
                    {
                        insideWalkSpot = null;
                        insideTarget = Mathf.Clamp(w.x, 0.4f, Mathf.Max(0.4f, insideMaxX - 0.4f));
                    }
                }
            }
        }

        void UseSpot(Spot s, string actionOverride)
        {
            if (s.kiosk != null) { gm.OpenKiosk(s.kiosk); return; }
            if (s.m != null && M.S.modules.Contains(s.m)) gm.OpenStation(s.m, actionOverride ?? s.action);
        }

        Room RoomAt(float x) => rooms.Find(r => x >= r.x0 && x < r.x1);

        // The usable thing you're standing at: the nearest spot whose area you're in.
        Spot SpotAt(float x)
        {
            Spot best = null;
            foreach (var s in spots)
                if (Mathf.Abs(x - s.x) <= s.half && (best == null || Mathf.Abs(x - s.x) < Mathf.Abs(x - best.x))) best = s;
            return best;
        }

        void AddSpot(PlacedModule m, float x, float half, float top, string action = null) =>
            spots.Add(new Spot { m = m, x = x, half = half, top = top, action = action });

        // For the HUD prompt: what you can use here (a deck screen, or a module with an optional action).
        public bool InsideSpot(out string kioskPrompt, out PlacedModule m, out string action)
        {
            var s = Inside ? SpotAt(insideX) : null;
            kioskPrompt = s?.prompt;
            m = s?.m;
            action = s?.action;
            return s != null;
        }

        PlacedModule InsideStation() => SpotAt(insideX)?.m;

        // Screen point just above the thing you can use (or above you).
        public Vector3 InsideScreenPos()
        {
            if (!cam) return Vector3.zero;
            var s = SpotAt(insideX);
            var p = s != null ? new Vector3(s.x, s.top + 0.75f, 0) : new Vector3(insideX, 2.3f, 0);
            return cam.WorldToScreenPoint(InsideOrigin + p);
        }

        // Walking to a station while inside: go to its spot (or its room), then use it.
        bool InsideWalkTo(PlacedModule m, string action)
        {
            var spot = spots.Find(s => s.m == m && s.action == action) ?? spots.Find(s => s.m == m);
            if (spot == null)
            {
                var room = rooms.Find(r => r.m == m);
                if (room == null) return false;
                spot = new Spot { m = m, x = room.Centre, half = 0.5f, top = 2f };
            }
            insideWalkSpot = spot;
            insideWalkAction = action;
            insideTarget = SpotAt(insideX) == spot ? insideX : spot.x;
            return true;
        }

        // ---------- side-view walking ----------

        // Four walk frames named prefix0..prefix3, or null if any is missing.
        static Sprite[] Frames(string prefix)
        {
            var f = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                f[i] = Theme.Art(prefix + i);
                if (!f[i]) return null;
            }
            return f;
        }

        // Side view walk cycle while moving sideways (flipped to face left), front view when standing still.
        void Pose(Transform person, Vector2 velocity)
        {
            var art = person.GetComponent<PersonArt>();
            if (!art || art.side == null) return;
            var suit = person.Find("Suit")?.GetComponent<SpriteRenderer>();
            var detail = person.Find("Detail")?.GetComponent<SpriteRenderer>();
            if (!suit) return;
            bool side = Mathf.Abs(velocity.x) > 0.05f && Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y) * 0.8f;
            int f = gm.Profile.reducedMotion ? 0 : (int)(Time.time * 8f) % 4;
            suit.sprite = side ? art.side[f] : art.front;
            suit.flipX = side && velocity.x < 0;
            if (detail && art.frontDetail)
            {
                detail.sprite = side && art.sideDetail != null ? art.sideDetail[f] : art.frontDetail;
                detail.flipX = suit.flipX;
            }
        }
    }

    // The sprites a person swaps between: front view, and the 4-frame side walk (base and detail layers).
    public class PersonArt : MonoBehaviour
    {
        public Sprite front, frontDetail;
        public Sprite[] side, sideDetail;
    }

    // A small light that blinks on and off.
    public class Blink : MonoBehaviour
    {
        public float speed = 1.5f;
        SpriteRenderer sr;
        void Update()
        {
            if (!sr) sr = GetComponent<SpriteRenderer>();
            if (sr) sr.enabled = Mathf.Sin(Time.time * speed * Mathf.PI * 2) > -0.3f;
        }
    }
}

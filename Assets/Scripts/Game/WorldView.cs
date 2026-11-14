using System;
using System.Collections.Generic;
using OutpostGale.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace OutpostGale
{
    // Draws the base grid and handles world input (click/tap to move, WASD, E to use, build ghost).
    // Reads MissionState; every rule goes through Mission.
    public class WorldView : MonoBehaviour
    {
        public GameManager gm;
        Mission M => gm.Mission;

        Transform ground, modulesRoot, fxRoot;
        readonly Dictionary<int, ModuleVisual> visuals = new Dictionary<int, ModuleVisual>();
        readonly List<GameObject> corridorObjs = new List<GameObject>();
        Light2D globalLight;
        Camera cam;
        int width, height;

        // player
        Transform player, bolt;
        SpriteRenderer playerBody;
        Cell playerCell;
        readonly List<Cell> path = new List<Cell>();
        Vector3 playerPos;
        PlacedModule walkTarget;
        float keyRepeat;
        readonly List<Crew> crew = new List<Crew>();

        // build ghost
        public string ghostModule;
        public bool ghostRotated;
        GameObject ghost;
        SpriteRenderer ghostSprite;
        readonly List<GameObject> ghostCorridor = new List<GameObject>();
        public PlaceCheck ghostCheck;
        public Cell ghostCell;

        // night / fx
        float nightAmount, nightTarget;
        float shake;
        SpriteRenderer haze, highlight;
        readonly List<Transform> particles = new List<Transform>();
        float particleTimer;
        public bool stormFx;

        class ModuleVisual
        {
            public GameObject root;
            public SpriteRenderer body, face, dust, progressBg, progressFill;
            public TextMesh label, status;
            public Light2D glow;
            public string signature;
        }

        class Crew
        {
            public Transform t;
            public Vector3 target;
            public float wait;
            public SpriteRenderer face;
        }

        public static Vector3 TileCentre(int x, int y, int w, int h) => new Vector3(x + 0.5f - w / 2f, y + 0.5f - h / 2f, 0);
        Vector3 Tile(int x, int y) => TileCentre(x, y, width, height);
        Vector3 Rect(PlacedModule m) => new Vector3(m.x + m.w / 2f - width / 2f, m.y + m.h / 2f - height / 2f, 0);

        public void Build()
        {
            width = M.B.grid.width;
            height = M.B.grid.height;
            cam = Camera.main;
            globalLight = FindAnyObjectByType<Light2D>();
            foreach (Transform c in transform) Destroy(c.gameObject);
            visuals.Clear();
            corridorObjs.Clear();
            crew.Clear();
            particles.Clear();
            ground = new GameObject("Ground").transform;
            ground.SetParent(transform, false);
            modulesRoot = new GameObject("Modules").transform;
            modulesRoot.SetParent(transform, false);
            fxRoot = new GameObject("Fx").transform;
            fxRoot.SetParent(transform, false);

            var grid = M.Layout();
            // Backdrop beyond the grid, then tiles.
            var back = Sprite("Backdrop", ground, Theme.White(), Theme.MarsDeep, -100);
            back.transform.localScale = new Vector3(width + 40, height + 30, 1);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var g = Sprite("Tile", ground, Theme.Ground((x * 7 + y * 3) % 3), Color.white, -90);
                    g.transform.localPosition = Tile(x, y);
                    if (grid.IsRock(x, y))
                    {
                        var r = Sprite("Rock", ground, Theme.Art("rock") ?? Theme.Rock(), Color.white, Order(y) + 1);
                        r.transform.localPosition = Tile(x, y) + new Vector3(0, -0.15f, 0);
                        r.transform.localScale = Vector3.one * 0.95f;
                    }
                }
            // Grid lines, faint.
            for (int x = 0; x <= width; x++)
            {
                var l = Sprite("GridV", ground, Theme.White(), new Color(0, 0, 0, 0.06f), -80);
                l.transform.localPosition = new Vector3(x - width / 2f, 0, 0);
                l.transform.localScale = new Vector3(0.03f, height, 1);
            }
            for (int y = 0; y <= height; y++)
            {
                var l = Sprite("GridH", ground, Theme.White(), new Color(0, 0, 0, 0.06f), -80);
                l.transform.localPosition = new Vector3(0, y - height / 2f, 0);
                l.transform.localScale = new Vector3(width, 0.03f, 1);
            }

            highlight = Sprite("Hover", fxRoot, Theme.Rounded(10, 3), new Color(1, 1, 1, 0.35f), 400);
            highlight.drawMode = SpriteDrawMode.Sliced;
            highlight.size = new Vector2(1, 1);
            highlight.gameObject.SetActive(false);

            haze = Sprite("Dust haze", fxRoot, Theme.White(), new Color(0.72f, 0.45f, 0.25f, 0), 500);
            haze.transform.localScale = new Vector3(width + 40, height + 30, 1);

            // Player, crew and BOLT.
            var hab = grid.First("hab");
            playerCell = grid.HabCentre();
            playerPos = Tile(playerCell.x, playerCell.y);
            player = MakePerson("Commander", SuitColor(M.S.suitColor), 0.62f, out playerBody).transform;
            player.localPosition = playerPos;
            var boltGo = new GameObject("BOLT");
            boltGo.transform.SetParent(transform, false);
            var bb = Sprite("Body", boltGo.transform, Theme.Rounded(8, 3), Theme.Hex("#9FB7C9"), 0);
            bb.drawMode = SpriteDrawMode.Sliced;
            bb.size = new Vector2(0.36f, 0.3f);
            var eye = Sprite("Eye", boltGo.transform, Theme.Circle(32, true), Theme.Power, 1);
            eye.transform.localScale = Vector3.one * 0.14f;
            eye.transform.localPosition = new Vector3(0, 0.02f, 0);
            bolt = boltGo.transform;
            bolt.localPosition = playerPos + new Vector3(0.5f, 0.2f, 0);

            string[] names = { "Mira", "Tomas" };
            Color[] colors = { Theme.Health, Theme.Life };
            for (int i = 0; i < 2; i++)
            {
                var go = MakePerson(names[i], colors[i], 0.5f, out var body);
                var c = new Crew { t = go.transform, face = body };
                c.t.localPosition = hab != null ? Rect(hab) + new Vector3(i == 0 ? -0.8f : 0.8f, -0.2f, 0) : Vector3.zero;
                c.target = c.t.localPosition;
                crew.Add(c);
            }

            FitCamera();
            Refresh();
        }

        public static Color SuitColor(int i)
        {
            Color[] suits = { Color.white, Theme.Power, Theme.Hex("#F28C8C"), Theme.Life, Theme.Health, Theme.Hex("#B9A7D9") };
            return suits[Mathf.Abs(i) % suits.Length];
        }

        GameObject MakePerson(string name, Color suit, float scale, out SpriteRenderer body)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var shadow = Sprite("Shadow", go.transform, Theme.Dot(), new Color(0, 0, 0, 0.35f), 0);
            shadow.transform.localScale = new Vector3(0.9f, 0.35f, 1) * scale;
            shadow.transform.localPosition = new Vector3(0, -0.28f * scale, 0);
            body = Sprite("Suit", go.transform, Theme.Circle(64, true), suit, 1);
            body.transform.localScale = Vector3.one * scale;
            var visor = Sprite("Visor", go.transform, Theme.Rounded(10, 0, false), Theme.Night, 2);
            visor.drawMode = SpriteDrawMode.Sliced;
            visor.size = new Vector2(0.5f, 0.22f) * scale;
            visor.transform.localPosition = new Vector3(0, 0.08f * scale, 0);
            var shine = Sprite("Shine", go.transform, Theme.Circle(16), new Color(1, 1, 1, 0.8f), 3);
            shine.transform.localScale = Vector3.one * 0.07f * scale / 0.6f;
            shine.transform.localPosition = new Vector3(-0.12f * scale, 0.12f * scale, 0);
            var tag = Text("Name", go.transform, name, 0.06f, Theme.Paper, 6);
            tag.transform.localPosition = new Vector3(0, -0.5f * scale - 0.08f, 0);
            return go;
        }

        public void FitCamera()
        {
            if (!cam) return;
            cam.orthographic = true;
            cam.backgroundColor = Theme.MarsDeep;
            float aspect = Mathf.Max(0.5f, (float)Screen.width / Mathf.Max(1, Screen.height));
            // Leave room for the HUD bands: ~15% top, ~18% bottom.
            float usable = 0.66f;
            float sizeForHeight = (height + 0.6f) / 2f / usable;
            float sizeForWidth = (width + 0.6f) / 2f / aspect;
            cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth) * (1 + nightAmount * 0.06f);
            float offset = cam.orthographicSize * 2 * 0.015f;
            cam.transform.position = new Vector3(0, -offset, -10);
        }

        // ---------- refresh from state ----------

        public void Refresh()
        {
            if (M == null) return;
            var grid = M.Layout();
            var alive = new HashSet<int>();
            foreach (var m in M.S.modules)
            {
                alive.Add(m.uid);
                if (!visuals.TryGetValue(m.uid, out var v)) { v = MakeModule(m); visuals[m.uid] = v; }
                UpdateModule(m, v, grid);
            }
            foreach (var uid in new List<int>(visuals.Keys))
                if (!alive.Contains(uid)) { Destroy(visuals[uid].root); visuals.Remove(uid); }

            foreach (var o in corridorObjs) Destroy(o);
            corridorObjs.Clear();
            foreach (var c in M.S.corridors)
            {
                var s = Sprite("Corridor", modulesRoot, Theme.Rounded(8, 3), Theme.Hex("#C9BFB8"), Order(c.y) - 2);
                s.drawMode = SpriteDrawMode.Sliced;
                s.size = new Vector2(0.92f, 0.92f);
                s.transform.localPosition = Tile(c.x, c.y);
                corridorObjs.Add(s.gameObject);
            }

            float dust = M.EventActive("dust") != null || M.InDustSeason(M.S.sol) ? (M.EventActive("dust") != null ? 0.22f : 0.1f) : 0f;
            haze.color = new Color(0.72f, 0.45f, 0.25f, dust);
        }

        ModuleVisual MakeModule(PlacedModule m)
        {
            var def = M.B.Module(m.id);
            var v = new ModuleVisual { root = new GameObject(def.name) };
            v.root.transform.SetParent(modulesRoot, false);
            v.root.transform.localPosition = Rect(m);
            int order = Order(m.y);
            var art = Theme.Art(m.id);
            // Front face for a 2.5D look.
            v.face = Sprite("Face", v.root.transform, Theme.Rounded(12, 4), Color.white, order);
            v.face.drawMode = SpriteDrawMode.Sliced;
            v.face.size = new Vector2(m.w - 0.1f, 0.42f);
            v.face.transform.localPosition = new Vector3(0, -m.h / 2f + 0.17f, 0);
            v.body = Sprite("Body", v.root.transform, art ?? Theme.Rounded(m.id == "wall" ? 8 : 16, 4), Color.white, order + 1);
            if (art == null)
            {
                v.body.drawMode = SpriteDrawMode.Sliced;
                v.body.size = new Vector2(m.w - 0.1f, m.h - 0.22f);
            }
            else v.body.transform.localScale = new Vector3((m.w - 0.1f) / art.bounds.size.x, (m.h - 0.1f) / art.bounds.size.y, 1);
            v.body.transform.localPosition = new Vector3(0, 0.08f, 0);

            v.dust = Sprite("Dust", v.root.transform, Theme.Rounded(16, 0, false), new Color(0.55f, 0.33f, 0.18f, 0.55f), order + 2);
            v.dust.drawMode = SpriteDrawMode.Sliced;
            v.dust.size = new Vector2(m.w - 0.25f, m.h - 0.35f);
            v.dust.transform.localPosition = new Vector3(0, 0.08f, 0);

            v.label = Text("Label", v.root.transform, ShortName(def), m.w >= 2 ? 0.075f : 0.055f, Theme.Ink, order + 3);
            v.label.transform.localPosition = new Vector3(0, m.h > 1 ? 0.25f : 0.12f, 0);
            v.status = Text("Status", v.root.transform, "", 0.05f, Theme.Ink, order + 3);
            v.status.transform.localPosition = new Vector3(0, m.h > 1 ? -0.12f : -0.1f, 0);

            v.progressBg = Sprite("BarBg", v.root.transform, Theme.Rounded(6, 2), Theme.Night, order + 4);
            v.progressBg.drawMode = SpriteDrawMode.Sliced;
            v.progressBg.size = new Vector2(Mathf.Max(0.8f, m.w - 0.4f), 0.2f);
            v.progressBg.transform.localPosition = new Vector3(0, -m.h / 2f + 0.45f, 0);
            v.progressFill = Sprite("BarFill", v.progressBg.transform, Theme.White(), Theme.Power, order + 5);

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(v.root.transform, false);
            v.glow = lightGo.AddComponent<Light2D>();
            v.glow.lightType = Light2D.LightType.Point;
            v.glow.pointLightOuterRadius = Mathf.Max(m.w, m.h) * 0.9f + 0.4f;
            v.glow.pointLightInnerRadius = 0.2f;
            v.glow.intensity = 0;
            v.glow.color = Theme.Gold;
            return v;
        }

        static string ShortName(ModuleDef d)
        {
            switch (d.id)
            {
                case "panel": return "Panel";
                case "wall": return "Wall";
                case "oxygen": return "Oxygen";
                case "recycler": return "Recycler";
                case "battery": return "Battery";
                case "dish": return "Dish";
                case "gym": return "Gym";
                case "shelter": return "Shelter";
                case "solar": return "Solar array";
                case "pad": return "Landing pad";
            }
            return d.name;
        }

        void UpdateModule(PlacedModule m, ModuleVisual v, BaseLayout grid)
        {
            var def = M.B.Module(m.id);
            Color baseColor = Theme.Hex(string.IsNullOrEmpty(def.color) ? "#FFFFFF" : def.color);
            bool active = M.IsActive(m);
            bool powered = active && !M.S.systemsDownLastNight;
            bool usesPower = def.powerUse > 0 || m.id == "hab" || m.id == "shelter";
            Color c = baseColor;
            if (!m.built) c = Color.Lerp(baseColor, Color.white, 0.45f);
            else if (usesPower && !powered) c = Color.Lerp(baseColor, Theme.Night, 0.55f);
            if (m.id == "shelter" && M.S.sealedTonight) c = Color.Lerp(baseColor, Theme.Gold, 0.35f);
            c.a = m.built ? 1f : 0.7f;
            v.body.color = c;
            v.face.color = Color.Lerp(c, Theme.Ink, 0.45f);
            v.label.color = m.built && usesPower && !powered ? Theme.Paper : Theme.Ink;
            v.status.color = v.label.color;

            var e = M.S.events.Find(x => x.targetUid == m.uid || (x.targetUid == 0 && M.B.Event(x.id)?.station == m.id));
            bool dusty = M.S.events.Exists(x => x.id == "dust" && x.targetUid == m.uid);
            v.dust.enabled = dusty;

            string status = "";
            if (!m.built)
            {
                float p = m.progressQ / (float)Balance.Q(def.buildHours);
                status = "Building " + Mathf.RoundToInt(p * 100) + "%";
                v.progressBg.enabled = v.progressFill.enabled = true;
                float w = v.progressBg.size.x - 0.06f;
                v.progressFill.transform.localScale = new Vector3(Mathf.Max(0.001f, w * p), 0.12f, 1);
                v.progressFill.transform.localPosition = new Vector3(-w / 2f + w * p / 2f, 0, 0);
            }
            else
            {
                v.progressBg.enabled = v.progressFill.enabled = false;
                if (!active) status = "On tomorrow";
                else if (e != null) status = "! " + M.B.Event(e.id).name;
                else if (m.id == "shelter") status = M.S.sealedTonight ? "Sealed" : "";
                else if (m.id == "greenhouse") status = M.EventActive("blight") != null ? "Sick crops" : m.builtSol <= M.S.sol - M.B.life.greenhouseDelaySols ? "Growing food" : "Seedlings";
                else if (usesPower && !powered) status = "No power";
                else if (grid.IsShielded(m)) status = "Shielded";
            }
            v.status.text = status;
            v.glow.enabled = powered && usesPower;
        }

        // ---------- per-frame ----------

        void Update()
        {
            if (M == null || player == null) return;
            nightAmount = Mathf.MoveTowards(nightAmount, nightTarget, Time.deltaTime * 0.8f);
            if (globalLight) globalLight.intensity = Mathf.Lerp(1f, 0.28f, nightAmount);
            foreach (var v in visuals.Values) if (v.glow && v.glow.enabled) v.glow.intensity = nightAmount * 1.3f;
            FitCamera();
            if (shake > 0 && cam && !gm.Profile.reducedMotion)
            {
                shake -= Time.deltaTime;
                cam.transform.position += (Vector3)UnityEngine.Random.insideUnitCircle * 0.12f * Mathf.Clamp01(shake * 3);
            }

            MovePlayer();
            MoveCrew();
            Particles();

            if (!gm.WorldInputOpen) { highlight.gameObject.SetActive(false); if (ghost) ghost.SetActive(false); return; }
            HandleInput();
        }

        public void SetNight(bool night) => nightTarget = night ? 1 : 0;
        public void Shake() => shake = 0.5f;

        bool PointerOverUi()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        bool PointerTile(out Cell cell, out Vector2 screen)
        {
            cell = default;
            screen = default;
            Vector2? pos = null;
            if (Mouse.current != null) pos = Mouse.current.position.ReadValue();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) pos = Touchscreen.current.primaryTouch.position.ReadValue();
            if (pos == null || !cam) return false;
            screen = pos.Value;
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            int x = Mathf.FloorToInt(w.x + width / 2f), y = Mathf.FloorToInt(w.y + height / 2f);
            cell = new Cell(x, y);
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        bool Clicked() =>
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

        void HandleInput()
        {
            var kb = Keyboard.current;
            bool overUi = PointerOverUi();
            bool onGrid = PointerTile(out var cell, out _);

            if (ghostModule != null)
            {
                UpdateGhost(onGrid && !overUi, cell);
                if (kb != null && kb.rKey.wasPressedThisFrame) RotateGhost();
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) RotateGhost();
                if (kb != null && kb.escapeKey.wasPressedThisFrame) gm.CancelBuild();
                if (Clicked() && onGrid && !overUi) gm.TryPlaceGhost();
                return;
            }

            highlight.gameObject.SetActive(onGrid && !overUi);
            if (onGrid && !overUi)
            {
                var m = M.Layout().ModuleAt(cell.x, cell.y);
                if (m != null)
                {
                    highlight.transform.localPosition = Rect(m);
                    highlight.size = new Vector2(m.w, m.h);
                }
                else
                {
                    highlight.transform.localPosition = Tile(cell.x, cell.y);
                    highlight.size = Vector2.one;
                }
                if (Clicked())
                {
                    if (m != null) WalkTo(m);
                    else WalkToCell(cell);
                }
            }

            if (kb != null)
            {
                Vector2Int dir = Vector2Int.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir = Vector2Int.up;
                else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir = Vector2Int.down;
                else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir = Vector2Int.left;
                else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir = Vector2Int.right;
                keyRepeat -= Time.deltaTime;
                if (dir != Vector2Int.zero && path.Count == 0 && keyRepeat <= 0)
                {
                    var next = new Cell(playerCell.x + dir.x, playerCell.y + dir.y);
                    if (M.Layout().Walkable(next.x, next.y)) { path.Add(next); walkTarget = null; }
                    keyRepeat = 0.12f;
                }
                if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                {
                    var here = StationNearPlayer();
                    if (here != null) gm.OpenStation(here);
                }
            }
        }

        public PlacedModule StationNearPlayer()
        {
            var grid = M.Layout();
            var m = grid.ModuleAt(playerCell.x, playerCell.y);
            if (m != null) return m;
            foreach (var d in new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) })
            {
                m = grid.ModuleAt(playerCell.x + d.x, playerCell.y + d.y);
                if (m != null && m.id != "wall") return m;
            }
            return null;
        }

        public Vector3 StationScreenPos(PlacedModule m)
        {
            if (!cam || m == null) return Vector3.zero;
            return cam.WorldToScreenPoint(transform.TransformPoint(Rect(m) + new Vector3(0, m.h / 2f + 0.2f, 0)));
        }

        // Walk to a module; when you arrive, its station menu opens.
        public void WalkTo(PlacedModule m, string action = null)
        {
            if (m == null) return;
            var p = GridPath.Find(M.Layout(), playerCell, c => m.Contains(c.x, c.y));
            path.Clear();
            walkTarget = m;
            walkAction = action;
            if (p == null) return;
            for (int i = 1; i < p.Count; i++) path.Add(p[i]);
            if (path.Count == 0) Arrive();
        }

        void WalkToCell(Cell c)
        {
            var p = GridPath.Find(M.Layout(), playerCell, c);
            path.Clear();
            walkTarget = null;
            walkAction = null;
            if (p == null) return;
            for (int i = 1; i < p.Count; i++) path.Add(p[i]);
        }

        string walkAction;

        void Arrive()
        {
            var t = walkTarget;
            string action = walkAction;
            walkTarget = null;
            walkAction = null;
            if (t != null && M.S.modules.Contains(t)) gm.OpenStation(t, action);
        }

        void MovePlayer()
        {
            float speed = 4.5f;
            if (path.Count > 0)
            {
                var target = Tile(path[0].x, path[0].y);
                playerPos = Vector3.MoveTowards(playerPos, target, speed * Time.deltaTime);
                if ((playerPos - target).sqrMagnitude < 0.0004f)
                {
                    playerCell = path[0];
                    path.RemoveAt(0);
                    AudioFx.Step();
                    if (path.Count == 0 && walkTarget != null) Arrive();
                }
            }
            player.localPosition = playerPos + new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 10)) * (path.Count > 0 ? 0.06f : 0), 0);
            SetOrder(player, Order(playerCell.y) + 20);
            var boltTarget = playerPos + new Vector3(0.55f, 0.35f + Mathf.Sin(Time.time * 3) * 0.06f, 0);
            bolt.localPosition = Vector3.Lerp(bolt.localPosition, boltTarget, Time.deltaTime * 4);
            SetOrder(bolt, Order(playerCell.y) + 21);
        }

        void MoveCrew()
        {
            var hab = M.Layout().First("hab");
            if (hab == null) return;
            foreach (var c in crew)
            {
                c.wait -= Time.deltaTime;
                if (c.wait <= 0)
                {
                    c.target = Rect(hab) + new Vector3(UnityEngine.Random.Range(-hab.w / 2f + 0.4f, hab.w / 2f - 0.4f), UnityEngine.Random.Range(-0.4f, 0.3f), 0);
                    c.wait = UnityEngine.Random.Range(2f, 5f);
                }
                c.t.localPosition = Vector3.MoveTowards(c.t.localPosition, c.target, Time.deltaTime * 0.8f);
                // Faces: tired, hungry or happy, shown by suit tint.
                float mood = Mathf.Min(M.S.health, M.S.life) / 100f;
                c.face.color = Color.Lerp(Color.Lerp(c.face.color, Theme.Muted, 0.0f), c.face.color, 1f);
                SetOrder(c.t, Order(hab.y) + 15);
                c.t.localScale = Vector3.one * (0.95f + 0.05f * Mathf.Sin(Time.time * (mood > 0.4f ? 4 : 1.5f)));
            }
        }

        // Radiation dots fall during a storm; shielded rooms block them.
        void Particles()
        {
            if (stormFx)
            {
                particleTimer -= Time.deltaTime;
                if (particleTimer <= 0 && particles.Count < 160)
                {
                    particleTimer = 0.02f;
                    var p = Sprite("Particle", fxRoot, Theme.Dot(), new Color(1f, 0.95f, 0.5f, 0.9f), 450);
                    p.transform.localScale = Vector3.one * 0.18f;
                    p.transform.localPosition = new Vector3(UnityEngine.Random.Range(-width / 2f, width / 2f), height / 2f + 2, 0);
                    particles.Add(p.transform);
                }
            }
            var grid = M.Layout();
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var t = particles[i];
                t.localPosition += new Vector3(0.6f, -7f, 0) * Time.deltaTime;
                int x = Mathf.FloorToInt(t.localPosition.x + width / 2f), y = Mathf.FloorToInt(t.localPosition.y + height / 2f);
                var m = grid.ModuleAt(x, y);
                bool blocked = m != null && (grid.IsShielded(m) || m.id == "wall" || (m.id == "shelter" && M.S.sealedTonight));
                if (blocked || t.localPosition.y < -height / 2f - 1)
                {
                    Destroy(t.gameObject);
                    particles.RemoveAt(i);
                }
            }
        }

        // ---------- build ghost ----------

        public void StartGhost(string moduleId)
        {
            ghostModule = moduleId;
            ghostRotated = false;
            if (!ghost)
            {
                ghost = new GameObject("Ghost");
                ghost.transform.SetParent(fxRoot, false);
                ghostSprite = Sprite("Body", ghost.transform, Theme.Rounded(14, 4), Color.white, 460);
                ghostSprite.drawMode = SpriteDrawMode.Sliced;
            }
            ghost.SetActive(true);
        }

        public void StopGhost()
        {
            ghostModule = null;
            if (ghost) ghost.SetActive(false);
            foreach (var g in ghostCorridor) Destroy(g);
            ghostCorridor.Clear();
            ghostCheck = null;
        }

        public void RotateGhost()
        {
            ghostRotated = !ghostRotated;
            ghostCheck = null;
        }

        void UpdateGhost(bool visible, Cell cell)
        {
            if (!ghost) return;
            ghost.SetActive(visible);
            if (!visible) { ghostCheck = null; return; }
            var def = M.B.Module(ghostModule);
            int w = ghostRotated ? def.h : def.w, h = ghostRotated ? def.w : def.h;
            // Centre the footprint on the pointer.
            var origin = new Cell(cell.x - (w - 1) / 2, cell.y - (h - 1) / 2);
            if (ghostCheck != null && origin.x == ghostCell.x && origin.y == ghostCell.y) return;
            ghostCell = origin;
            ghostCheck = M.CheckPlace(ghostModule, origin.x, origin.y, ghostRotated);
            ghost.transform.localPosition = new Vector3(origin.x + w / 2f - width / 2f, origin.y + h / 2f - height / 2f, 0);
            ghostSprite.size = new Vector2(w - 0.08f, h - 0.08f);
            ghostSprite.color = ghostCheck.ok ? new Color(0.35f, 0.9f, 0.45f, 0.6f) : new Color(0.95f, 0.3f, 0.25f, 0.55f);
            foreach (var g in ghostCorridor) Destroy(g);
            ghostCorridor.Clear();
            if (ghostCheck.ok)
                foreach (var c in ghostCheck.corridor)
                {
                    var s = Sprite("CorridorGhost", fxRoot, Theme.Rounded(8, 3), new Color(1, 1, 1, 0.5f), 455);
                    s.drawMode = SpriteDrawMode.Sliced;
                    s.size = new Vector2(0.9f, 0.9f);
                    s.transform.localPosition = Tile(c.x, c.y);
                    ghostCorridor.Add(s.gameObject);
                }
            gm.OnGhostChanged();
        }

        // ---------- helpers ----------

        int Order(int y) => 200 - y * 10;

        static void SetOrder(Transform t, int baseOrder)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                int local = r.gameObject.name == "Shadow" ? 0 : r.gameObject.name == "Suit" || r.gameObject.name == "Body" ? 1 : r.gameObject.name == "Name" ? 4 : 2;
                r.sortingOrder = baseOrder + local;
            }
        }

        static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        static TextMesh Text(string name, Transform parent, string text, float size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Theme.BodyBlack;
            tm.fontSize = 64;
            tm.characterSize = size * 0.25f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Theme.BodyBlack.material;
            mr.sortingOrder = order;
            return tm;
        }
    }
}

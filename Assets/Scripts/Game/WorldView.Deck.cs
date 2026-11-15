using System.Collections.Generic;
using System.Text;
using OutpostGale.Sim;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OutpostGale
{
    // The command deck: the first room inside the base, where every mission starts. Three wall
    // screens you can walk up to: the status board (meters and supplies), the base directory, and
    // NASA news (real data only: today's Sun from DONKI and a NASA OSDR study).
    public partial class WorldView
    {
        static readonly string[] MeterIds = { "power", "life", "health" };

        void BuildDeck(Room r, List<PlacedModule> list)
        {
            var root = new GameObject("Deck").transform;
            root.SetParent(insideRoot, false);
            Shell(root, r, Theme.Hex("#5A6390"), "COMMAND DECK");
            float x0 = r.x0;

            // ---- status board ----
            var status = Board(root, new Vector3(x0 + 2.0f, 2.2f, 0), new Vector2(3.0f, 2.1f), "BASE STATUS", Theme.Gold);
            string[] labels = { "POWER", "LIFE", "HEALTH" };
            for (int i = 0; i < 3; i++)
            {
                float y = 0.38f - i * 0.4f;
                var label = Text("Meter", status, labels[i], 0.045f, Theme.Soft, -8);
                label.anchor = TextAnchor.MiddleLeft;
                label.alignment = TextAlignment.Left;
                label.transform.localPosition = new Vector3(-1.32f, y, 0);
                Box("BarBg", status, new Vector3(0.22f, y, 0), new Vector2(1.5f, 0.2f), Theme.Hex("#232842"), -9, 4);
                var fill = Box("BarFill", status, new Vector3(0.22f, y, 0), new Vector2(1.46f, 0.15f), Theme.MeterColor(MeterIds[i]), -8);
                statusFills[MeterIds[i]] = fill.transform;
                var value = Text("Value", status, "", 0.045f, Theme.Paper, -8);
                value.anchor = TextAnchor.MiddleRight;
                value.alignment = TextAlignment.Right;
                value.transform.localPosition = new Vector3(1.38f, y, 0);
                statusValues[MeterIds[i]] = value;
            }
            statusSupplies = Text("Supplies", status, "", 0.034f, Theme.Soft, -8);
            statusSupplies.transform.localPosition = new Vector3(0, -0.8f, 0);
            spots.Add(new Spot { kiosk = "status", x = x0 + 2.0f, half = 1.2f, top = 3.25f, bottom = 1.1f, prompt = "E / tap: How your base works" });

            // ---- directory ----
            var dir = Board(root, new Vector3(x0 + 4.9f, 2.15f, 0), new Vector2(2.2f, 2.3f), "DIRECTORY", Theme.Gold);
            var sb = new StringBuilder();
            int shown = 0;
            foreach (var m in list)
            {
                if (shown == 6) { sb.Append("…and more\n"); break; }
                sb.Append("> ").Append(ShortName(M.B.Module(m.id)).ToUpperInvariant()).Append(m.built ? "" : " (building)").Append('\n');
                shown++;
            }
            sb.Append("\nMAP VIEW: PRESS I");
            directoryText = Text("Rooms", dir, sb.ToString(), 0.036f, Theme.Paper, -8);
            directoryText.anchor = TextAnchor.UpperLeft;
            directoryText.alignment = TextAlignment.Left;
            directoryText.transform.localPosition = new Vector3(-0.92f, 0.62f, 0);
            spots.Add(new Spot { kiosk = "directory", x = x0 + 4.9f, half = 0.9f, top = 3.3f, bottom = 1.0f, prompt = "E / tap: Base directory" });

            // ---- NASA news ----
            var news = Board(root, new Vector3(x0 + 7.45f, 2.2f, 0), new Vector2(2.6f, 2.1f), "", Theme.Gold);
            newsTitle = Text("Title", news, "NASA NEWS", 0.055f, Theme.Gold, -8);
            newsTitle.transform.localPosition = new Vector3(0, 0.78f, 0);
            var live = Box("Live", news, new Vector3(-1.05f, 0.8f, 0), new Vector2(0.12f, 0.12f), Theme.Warn, -8);
            live.sprite = Theme.Circle(32);
            live.gameObject.AddComponent<Blink>().speed = 0.8f;
            newsText = Text("News", news, "", 0.034f, Theme.Paper, -8);
            newsText.anchor = TextAnchor.UpperLeft;
            newsText.alignment = TextAlignment.Left;
            newsText.transform.localPosition = new Vector3(-1.15f, 0.55f, 0);
            spots.Add(new Spot { kiosk = "news", x = x0 + 7.45f, half = 1.0f, top = 3.25f, bottom = 1.1f, prompt = "E / tap: NASA news" });

            // Welcome sign on the lower wall.
            Text("Welcome", root, "WELCOME TO OUTPOST GALE", 0.05f, new Color(1, 1, 1, 0.35f), -40).transform.localPosition = new Vector3(r.Centre, 0.55f, 0);
            RefreshNews();
        }

        // A wall screen: dark frame, glowing face, optional title. Returns the face (children sit on it).
        Transform Board(Transform root, Vector3 pos, Vector2 size, string title, Color titleColor)
        {
            Box("BoardFrame", root, pos, size + new Vector2(0.16f, 0.16f), Theme.Hex("#151A2E"), -12, 10);
            var face = Box("Board", root, pos, size, Theme.Hex("#12233A"), -11, 8);
            var t = face.transform;
            if (!string.IsNullOrEmpty(title))
            {
                var tm = Text("Title", t, title, 0.055f, titleColor, -8);
                tm.transform.localPosition = new Vector3(0, size.y / 2f - 0.22f, 0);
            }
            var glow = new GameObject("Glow");
            glow.transform.SetParent(t, false);
            var l = glow.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.pointLightOuterRadius = Mathf.Max(size.x, size.y) * 0.8f;
            l.color = Theme.Life;
            l.intensity = 0.25f;
            return t;
        }

        // Walls, floor, ceiling, sign and lights shared by every room.
        void Shell(Transform root, Room r, Color wall, string sign)
        {
            float w = r.x1 - r.x0, cx = r.Centre;
            Box("Wall", root, new Vector3(cx, RoomH / 2f, 0), new Vector2(w, RoomH), wall, -50);
            Box("Lower", root, new Vector3(cx, 0.55f, 0), new Vector2(w, 1.1f), Theme.Hex("#454D72"), -49);
            for (float sx = r.x0 + 1f; sx < r.x1 - 0.2f; sx += 1f)
                Box("Seam", root, new Vector3(sx, RoomH / 2f, 0), new Vector2(0.03f, RoomH), Theme.Hex("#3E4566"), -48);
            Box("Rail", root, new Vector3(cx, 1.1f, 0), new Vector2(w, 0.06f), Theme.Hex("#6B76A0"), -47);
            Box("Floor", root, new Vector3(cx, -0.3f, 0), new Vector2(w, 0.6f), Theme.Hex("#232842"), -20);
            Box("FloorEdge", root, new Vector3(cx, -0.02f, 0), new Vector2(w, 0.05f), Theme.Hex("#6B76A0"), -19);
            Box("Ceiling", root, new Vector3(cx, RoomH + 0.25f, 0), new Vector2(w, 0.5f), Theme.Hex("#232842"), -20);
            Box("Lamp", root, new Vector3(cx, RoomH - 0.05f, 0), new Vector2(w * 0.45f, 0.09f), Theme.Hex("#E6F4FF"), -18, 4);

            var plate = Box("Sign", root, new Vector3(cx, RoomH - 0.42f, 0), new Vector2(0.4f + sign.Length * 0.15f, 0.36f), Theme.Hex("#151A2E"), -16, 8);
            Text("SignText", plate.transform, sign, 0.07f, Theme.Gold, -15).transform.localPosition = Vector3.zero;

            var lampGo = new GameObject("Light");
            lampGo.transform.SetParent(root, false);
            lampGo.transform.localPosition = new Vector3(cx, RoomH - 0.4f, 0);
            r.lamp = lampGo.AddComponent<Light2D>();
            r.lamp.lightType = Light2D.LightType.Point;
            r.lamp.pointLightOuterRadius = w * 0.75f + 1f;
            r.lamp.pointLightInnerRadius = 0.5f;
            r.lamp.color = r.m != null && r.m.id == "greenhouse" ? new Color(1f, 0.55f, 0.95f) : new Color(1f, 0.95f, 0.85f);
            var alarmGo = new GameObject("Alarm");
            alarmGo.transform.SetParent(root, false);
            alarmGo.transform.localPosition = new Vector3(cx, RoomH - 0.3f, 0);
            r.alarm = alarmGo.AddComponent<Light2D>();
            r.alarm.lightType = Light2D.LightType.Point;
            r.alarm.pointLightOuterRadius = w * 0.7f + 1f;
            r.alarm.color = Theme.Warn;
            r.alarm.intensity = 0;
            r.alarmStrip = Box("AlarmStrip", root, new Vector3(cx, RoomH - 0.05f, 0), new Vector2(w * 0.45f, 0.09f), Theme.Warn, -17, 4);
            r.alarmStrip.enabled = false;
        }

        void RefreshDeck()
        {
            if (statusValues.Count == 0) return;
            var s = M.S;
            int[] values = { s.power, s.life, s.health };
            for (int i = 0; i < 3; i++)
            {
                float p = Mathf.Clamp01(values[i] / 100f);
                var fill = statusFills[MeterIds[i]];
                float full = 1.46f;
                fill.localScale = new Vector3(Mathf.Max(0.001f, full * p), fill.localScale.y, 1);
                fill.localPosition = new Vector3(0.22f - full / 2f + full * p / 2f, fill.localPosition.y, 0);
                statusValues[MeterIds[i]].text = values[i].ToString();
                statusValues[MeterIds[i]].color = values[i] < 25 ? Theme.Warn : Theme.Paper;
            }
            statusSupplies.text = "REGOLITH " + s.regolith + "  ·  WATER " + s.ice + "  ·  PARTS " + s.parts + "  ·  SAMPLES " + s.samples;
        }

        void RefreshNews()
        {
            if (!newsText || gm.Data == null) return;
            var sb = new StringBuilder();
            sb.Append("<color=#FFD27A>TODAY'S SUN").Append(LiveSun.Live ? " (LIVE)" : "").Append("</color>\n");
            sb.Append(Wrap(LiveSun.Headline, 34, 3)).Append('\n');
            var facts = gm.Data.osdr?.facts;
            if (facts != null && facts.Length > 0)
            {
                var f = facts[Mathf.Abs(M.S.sol) % facts.Length];
                sb.Append("\n<color=#FFD27A>SPACE SCIENCE</color>\n").Append(Wrap(f.title, 34, 2)).Append('\n');
            }
            sb.Append("\n<color=#9FB0D0>Tap for the full story</color>");
            newsText.text = sb.ToString();
        }

        // Break text into lines of at most width characters (TextMesh doesn't wrap).
        static string Wrap(string text, int width, int maxLines)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var lines = new List<string>();
            var line = new StringBuilder();
            foreach (var word in text.Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > width)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                }
                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }
            if (line.Length > 0) lines.Add(line.ToString());
            if (lines.Count > maxLines)
            {
                lines.RemoveRange(maxLines, lines.Count - maxLines);
                lines[maxLines - 1] = lines[maxLines - 1].TrimEnd('.', ',') + "…";
            }
            return string.Join("\n", lines);
        }
    }
}

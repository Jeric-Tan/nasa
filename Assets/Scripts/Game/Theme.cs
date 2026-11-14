using System.Collections.Generic;
using UnityEngine;

namespace OutpostGale
{
    // Look tokens from the plan's mockup, plus procedurally drawn placeholder sprites.
    public static class Theme
    {
        public static readonly Color Ink = Hex("#2A1A2E");
        public static readonly Color Mars = Hex("#E9773E");
        public static readonly Color MarsDeep = Hex("#B9502A");
        public static readonly Color Sand = Hex("#F3D9B8");
        public static readonly Color Paper = Hex("#FFF6EA");
        public static readonly Color Night = Hex("#2A2230");
        public static readonly Color NightSoft = Hex("#3A3046");
        public static readonly Color Power = Hex("#F2B705");
        public static readonly Color Life = Hex("#2F95D6");
        public static readonly Color Health = Hex("#3FAE5A");
        public static readonly Color Warn = Hex("#E5483B");
        public static readonly Color Muted = Hex("#6B5A63");
        public static readonly Color Gold = Hex("#FFD27A");
        public static readonly Color Plus = Hex("#227A3B");
        public static readonly Color Minus = Hex("#B42318");

        public static Color MeterColor(string m) => m == "power" ? Power : m == "life" ? Life : Health;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        static Font body, bodyBlack, display;
        public static Font Body => body ? body : body = LoadFont("Fonts/Nunito-Bold");
        public static Font BodyBlack => bodyBlack ? bodyBlack : bodyBlack = LoadFont("Fonts/Nunito-Black");
        public static Font Display => display ? display : display = LoadFont("Fonts/Baloo2-ExtraBold");

        static Font LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            return f ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // ---------- procedural sprites ----------

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // White rounded rectangle with an ink outline, 9-sliced. Tint it with Image/SpriteRenderer colour.
        public static Sprite Rounded(int radius = 14, int outline = 4, bool outlined = true)
        {
            string key = "r" + radius + "_" + outline + "_" + outlined;
            if (cache.TryGetValue(key, out var s)) return s;
            int size = radius * 2 + 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedDist(x + 0.5f, y + 0.5f, size, size, radius);
                    float a = Mathf.Clamp01(0.5f - d);
                    Color c = Color.white;
                    if (outlined)
                    {
                        float inner = Mathf.Clamp01(0.5f - (d + outline));
                        c = Color.Lerp(Ink, Color.white, inner);
                    }
                    c.a = a;
                    px[y * size + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            float b = radius + 2;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            cache[key] = s;
            return s;
        }

        static float RoundedDist(float x, float y, float w, float h, float r)
        {
            float qx = Mathf.Abs(x - w / 2f) - (w / 2f - r);
            float qy = Mathf.Abs(y - h / 2f) - (h / 2f - r);
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        public static Sprite Circle(int size = 64, bool outlined = false)
        {
            string key = "c" + size + outlined;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude - (r - 1);
                    Color c = Color.white;
                    if (outlined) c = Color.Lerp(Ink, Color.white, Mathf.Clamp01(-(d + size / 14f)));
                    c.a = Mathf.Clamp01(0.5f - d);
                    px[y * size + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            cache[key] = s;
            return s;
        }

        // Soft dot for radiation particles and glows.
        public static Sprite Dot()
        {
            if (cache.TryGetValue("dot", out var s)) return s;
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - 16, y + 0.5f - 16).magnitude / 16f;
                    px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(1 - d * d));
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            cache["dot"] = s;
            return s;
        }

        // A 1x1 white sprite for plain quads.
        public static Sprite White()
        {
            if (cache.TryGetValue("white", out var s)) return s;
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            cache["white"] = s;
            return s;
        }

        // Ground tile with a little speckle, so the plain reads as Mars soil.
        public static Sprite Ground(int variant)
        {
            string key = "g" + variant;
            if (cache.TryGetValue(key, out var s)) return s;
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var rnd = new System.Random(variant * 31 + 7);
            var baseC = Color.Lerp(Mars, Sand, 0.08f + variant * 0.05f);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                float n = (float)rnd.NextDouble();
                px[i] = n > 0.94f ? Color.Lerp(baseC, MarsDeep, 0.5f) : n < 0.04f ? Color.Lerp(baseC, Sand, 0.4f) : baseC;
            }
            tex.SetPixels(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            cache[key] = s;
            return s;
        }

        public static Sprite Rock()
        {
            if (cache.TryGetValue("rock", out var s)) return s;
            int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - 24f) / 20f, dy = (y - 18f) / 15f;
                    float d = dx * dx + dy * dy;
                    var c = Color.Lerp(MarsDeep, Ink, y < 14 ? 0.35f : 0.1f);
                    if (d > 0.82f) c = Ink;
                    c.a = d < 1f ? 1f : 0f;
                    px[y * size + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.35f), size);
            cache["rock"] = s;
            return s;
        }

        // Optional imported art: Resources/Art/<name>. Returns null when not imported yet.
        public static Sprite Art(string name)
        {
            string key = "art_" + name;
            if (cache.TryGetValue(key, out var s)) return s;
            s = Resources.Load<Sprite>("Art/" + name);
            cache[key] = s;
            return s;
        }
    }
}

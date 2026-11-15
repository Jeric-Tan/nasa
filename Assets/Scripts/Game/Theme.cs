using System.Collections.Generic;
using UnityEngine;

namespace OutpostGale
{
    // Look tokens from the plan's mockup, plus procedurally drawn placeholder sprites.
    public static class Theme
    {
        public static readonly Color Ink = Hex("#241A33");
        public static readonly Color Mars = Hex("#FF7A45");
        public static readonly Color MarsDeep = Hex("#B9502A");
        public static readonly Color Sand = Hex("#F3D9B8");
        public static readonly Color Paper = Hex("#FFF9F2");
        public static readonly Color Night = Hex("#1E1838");
        public static readonly Color NightSoft = Hex("#2F2754");
        public static readonly Color Glass = Hex("#1C1638");
        public static readonly Color Power = Hex("#FFC23D");
        public static readonly Color Life = Hex("#3BA7F0");
        public static readonly Color Health = Hex("#46C66E");
        public static readonly Color Warn = Hex("#FF5A4E");
        public static readonly Color Muted = Hex("#76677F");
        public static readonly Color Gold = Hex("#FFD27A");
        public static readonly Color Plus = Hex("#1F8A44");
        public static readonly Color Minus = Hex("#D23A2E");
        public static readonly Color Soft = Hex("#C9BEE0"); // secondary text on dark glass

        public static Color MeterColor(string m) => m == "power" ? Power : m == "life" ? Life : Health;

        public static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        public static Color TextOn(Color fill) => Luma(fill) < 0.6f ? Paper : Ink;

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

        // Rounded key face for buttons: a soft top-to-bottom gloss, no outline. 9-sliced, tint with colour.
        public static Sprite Key(int radius = 16)
        {
            string key = "key" + radius;
            if (cache.TryGetValue(key, out var s)) return s;
            int size = radius * 2 + 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedDist(x + 0.5f, y + 0.5f, size, size, radius);
                    float t = y / (float)(size - 1);
                    float shade = Mathf.Lerp(0.9f, 1f, Mathf.SmoothStep(0, 1, t));
                    // a thin bright line just inside the top edge
                    if (d > -3.5f && d < -1.5f && t > 0.6f) shade = 1f;
                    var c = new Color(shade, shade, shade, Mathf.Clamp01(0.5f - d));
                    px[y * size + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            float b = radius + 2;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            cache[key] = s;
            return s;
        }

        // A vertical gradient strip (bottom colour to top colour), for skies and bars.
        public static Sprite Gradient(Color bottom, Color top, int steps = 64)
        {
            string key = "grad" + ColorUtility.ToHtmlStringRGBA(bottom) + ColorUtility.ToHtmlStringRGBA(top);
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(2, steps, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < steps; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(steps - 1));
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 2, steps), new Vector2(0.5f, 0.5f), 100);
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

        // Diagonal hazard stripes (gold and ink), tileable: use with Image.Type.Tiled.
        public static Sprite Stripes()
        {
            if (cache.TryGetValue("stripes", out var s)) return s;
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float t = ((x + y) % size) / (float)size;
                    // soft edges between the bands
                    float k = Mathf.Clamp01(Mathf.Min(Mathf.Abs(t - 0.5f), Mathf.Min(t, 1 - t)) * size * 0.5f);
                    bool gold = t < 0.5f;
                    var c = gold ? Color.Lerp(Ink, Power, 0.5f + 0.5f * k) : Color.Lerp(Power, Ink, 0.5f + 0.5f * k);
                    px[y * size + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
            cache["stripes"] = s;
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
            var loaded = Resources.Load<Sprite>("Art/" + name);
            s = loaded ? loaded : null; // real null, so callers can use ??
            cache[key] = s;
            return s;
        }
    }
}

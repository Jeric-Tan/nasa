using UnityEngine;

namespace OutpostGale
{
    // Procedural Mars ground around the base: rolling hills, wind ripples, dark basalt patches,
    // bright dust streaks, craters and grit, hill-shaded by a low sun from the upper left.
    // Purely cosmetic (not game state), so it uses its own System.Random and Mathf.PerlinNoise.
    // Drop Resources/Art/terrain.png to replace it with a real image.
    public static class MarsTerrain
    {
        public const float WidthUnits = 46, HeightUnits = 32;
        const int Ppu = 20;

        static Sprite cached;
        static int cachedKey = int.MinValue;

        static readonly Color Dust = Theme.Hex("#C2703F");
        static readonly Color Bright = Theme.Hex("#DE9A63");
        static readonly Color Basalt = Theme.Hex("#7A3E2B");
        static readonly Color Pad = Theme.Hex("#D3A07A");
        static readonly Color ShadowTint = Theme.Hex("#5A2C3A");

        // The colour beyond the texture edge (the camera background), so the border never shows.
        public static Color Edge => Dust * 0.5f;

        public static Sprite Ground(int seed, int gridW, int gridH)
        {
            int key = seed * 31 + gridW * 7 + gridH;
            if (cached && cachedKey == key) return cached;

            int W = (int)(WidthUnits * Ppu), H = (int)(HeightUnits * Ppu);
            var rnd = new System.Random(seed);
            float ox = (float)rnd.NextDouble() * 500f, oy = (float)rnd.NextDouble() * 500f;
            float windA = 0.35f + (float)rnd.NextDouble() * 0.5f;
            float wc = Mathf.Cos(windA), ws = Mathf.Sin(windA);
            float gx0 = (WidthUnits - gridW) / 2f, gy0 = (HeightUnits - gridH) / 2f;

            var h = new float[W * H];
            var wildness = new float[W * H];
            var col = new Color[W * H];

            for (int y = 0; y < H; y++)
            {
                float v = y / (float)Ppu;
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)Ppu;
                    int i = y * W + x;
                    float dx = Mathf.Max(0, Mathf.Max(gx0 - u, u - (gx0 + gridW)));
                    float dy = Mathf.Max(0, Mathf.Max(gy0 - v, v - (gy0 + gridH)));
                    float outside = Mathf.Sqrt(dx * dx + dy * dy);
                    float wild = Mathf.SmoothStep(0, 1, Mathf.Clamp01((outside - 0.2f) / 2.5f));
                    wildness[i] = outside;

                    float across = -u * ws + v * wc, along = u * wc + v * ws;
                    float hills = (Fbm(u * 0.12f + ox, v * 0.12f + oy, 4) - 0.5f) * 4.4f;
                    float duneMask = Mathf.Clamp01(Mathf.PerlinNoise(u * 0.07f + oy, v * 0.07f + ox) * 2.4f - 0.9f);
                    float ripple = Mathf.Sin(across * 7f + Mathf.PerlinNoise(u * 0.4f + ox, v * 0.4f + oy) * 6f) * 0.028f * duneMask;
                    float grit = (Mathf.PerlinNoise(u * 1.7f + oy, v * 1.7f + ox) - 0.5f) * 0.05f;
                    // The pad is levelled: almost no relief or ripples inside it.
                    h[i] = hills * (0.12f + 0.88f * wild) + ripple * (0.05f + 0.95f * wild) + grit * (0.5f + 0.5f * wild);

                    float basalt = Mathf.Clamp01((Fbm(u * 0.18f + ox * 2, v * 0.18f + oy * 2, 2) - 0.52f) * 3.2f);
                    float streak = Mathf.Clamp01((Mathf.PerlinNoise(along * 0.06f + ox, across * 0.3f + oy) - 0.5f) * 2.6f);
                    var c = Color.Lerp(Dust, Basalt, basalt * 0.75f);
                    c = Color.Lerp(c, Bright, streak * 0.35f);
                    c = Color.Lerp(Color.Lerp(c, Pad, 0.22f), c, wild); // the base pad is compacted, paler regolith
                    col[i] = c;
                }
            }

            // Craters, kept off the buildable pad so they never look like obstacles.
            for (int k = 0; k < 60; k++)
            {
                float r = 0.3f + Mathf.Pow((float)rnd.NextDouble(), 2.5f) * 3.2f;
                float cx = (float)rnd.NextDouble() * WidthUnits, cy = (float)rnd.NextDouble() * HeightUnits;
                float ex = Mathf.Max(0, Mathf.Max(gx0 - cx, cx - (gx0 + gridW)));
                float ey = Mathf.Max(0, Mathf.Max(gy0 - cy, cy - (gy0 + gridH)));
                if (Mathf.Sqrt(ex * ex + ey * ey) < r * 1.2f + 0.3f) continue;
                float depth = r * 0.32f;
                int x0 = Mathf.Max(0, (int)((cx - r * 1.8f) * Ppu)), x1 = Mathf.Min(W - 1, (int)((cx + r * 1.8f) * Ppu));
                int y0 = Mathf.Max(0, (int)((cy - r * 1.8f) * Ppu)), y1 = Mathf.Min(H - 1, (int)((cy + r * 1.8f) * Ppu));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float t = new Vector2(x / (float)Ppu - cx, y / (float)Ppu - cy).magnitude / r;
                        if (t > 1.8f) continue;
                        int i = y * W + x;
                        float bowl = t < 1 ? -depth * (1 - t * t) : 0;
                        float rim = depth * 0.5f * Mathf.Exp(-Mathf.Pow((t - 1) / 0.22f, 2));
                        h[i] += bowl + rim;
                        if (t < 0.9f) col[i] = Color.Lerp(col[i], Basalt, 0.25f * (1 - t));
                        else if (t > 1.05f) col[i] = Color.Lerp(col[i], Bright, 0.25f * (1 - (t - 1.05f) / 0.75f)); // ejecta
                    }
            }

            // Hill-shade: light from the upper left, low over the horizon.
            var light = new Vector3(-0.55f, 0.6f, 0.6f).normalized;
            float flat = light.z;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    float hx = (h[y * W + Mathf.Min(W - 1, x + 1)] - h[y * W + Mathf.Max(0, x - 1)]) * Ppu * 0.5f;
                    float hy = (h[Mathf.Min(H - 1, y + 1) * W + x] - h[Mathf.Max(0, y - 1) * W + x]) * Ppu * 0.5f;
                    var n = new Vector3(-hx, -hy, 1).normalized;
                    float lit = Mathf.Clamp(Vector3.Dot(n, light) / flat, 0.2f, 1.7f);
                    var c = col[i] * (0.3f + 0.7f * lit);
                    if (lit < 1) c = Color.Lerp(c, c * ShadowTint * 1.6f, (1 - lit) * 0.6f);

                    uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791);
                    hash = (hash ^ (hash >> 13)) * 1274126177u;
                    uint p = hash % 1000;
                    if (p < 7) c *= 0.62f;
                    else if (p < 11) c = Color.Lerp(c, Bright, 0.6f);

                    // Vignette: the land fades darker away from the base.
                    c *= Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((wildness[i] - 2.5f) / 11f));
                    c.a = 1;
                    px[i] = c;
                }

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "MarsTerrain"
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            if (cached) Object.Destroy(cached.texture);
            cached = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            cachedKey = key;
            return cached;
        }

        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int o = 0; o < octaves; o++)
            {
                sum += Mathf.PerlinNoise(x, y) * amp;
                norm += amp;
                x = x * 2.03f + 17.1f;
                y = y * 2.03f + 9.7f;
                amp *= 0.5f;
            }
            return sum / norm;
        }
    }
}

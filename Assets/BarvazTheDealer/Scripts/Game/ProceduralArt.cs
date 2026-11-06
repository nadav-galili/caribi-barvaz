using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarvazTheDealer.Game
{
    /// <summary>
    /// Generates every sprite the table needs at runtime (felt, rail, chips, card backs...),
    /// so the vertical slice runs with no art assets except Barvaz's photo.
    /// Swap any of these for real art later.
    /// </summary>
    public static class ProceduralArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // ---------- public sprites ----------

        /// <summary>Solid anti-aliased circle (tint it with Image.color; stretch it for ellipses).</summary>
        public static Sprite Circle
        {
            get { return Get("circle", () => Make(256, 256, (x, y) => White(CircleAlpha(x, y, 256)))); }
        }

        /// <summary>Soft radial falloff for glows, shadows and light pools.</summary>
        public static Sprite SoftCircle
        {
            get
            {
                return Get("soft", () => Make(128, 128, (x, y) =>
                {
                    float r = Radius(x, y);
                    float a = Mathf.Clamp01(1f - r);
                    return new Color(1, 1, 1, a * a * (3f - 2f * a));
                }));
            }
        }

        /// <summary>Thin anti-aliased ring.</summary>
        public static Sprite Ring
        {
            get
            {
                return Get("ring", () => Make(512, 512, (x, y) =>
                {
                    float px = Radius(x, y) * 256f;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(px - 250f) / 2.5f);
                    return new Color(1, 1, 1, a);
                }));
            }
        }

        /// <summary>Green casino felt with a centre-lit gradient and a little cloth noise.</summary>
        public static Sprite Felt
        {
            get
            {
                return Get("felt", () =>
                {
                    var rng = new System.Random(7);
                    var center = new Color(0.09f, 0.43f, 0.24f);
                    var edge = new Color(0.02f, 0.19f, 0.10f);
                    return Make(512, 512, (x, y) =>
                    {
                        float r = Radius(x, y);
                        Color c = Color.Lerp(center, edge, Mathf.Pow(Mathf.Clamp01(r), 1.6f));
                        float n = ((float)rng.NextDouble() - 0.5f) * 0.035f;
                        c.r += n; c.g += n; c.b += n;
                        c.a = CircleAlpha(x, y, 512);
                        return c;
                    });
                });
            }
        }

        /// <summary>Padded black-leather rail: a rounded highlight across the outer band.</summary>
        public static Sprite Rail
        {
            get
            {
                return Get("rail", () => Make(512, 512, (x, y) =>
                {
                    float r = Radius(x, y);
                    float t = Mathf.Clamp01((r - 0.86f) / 0.14f);
                    float light = 0.35f + 0.9f * Mathf.Sin(t * Mathf.PI);
                    // light from the top: upper half slightly brighter
                    float top = Mathf.Clamp01((y / 512f) * 0.6f + 0.6f);
                    var baseCol = new Color(0.085f, 0.06f, 0.055f);
                    Color c = baseCol * light * top;
                    c.a = CircleAlpha(x, y, 512);
                    return c;
                }));
            }
        }

        /// <summary>Rounded rectangle, 9-sliced so it scales to any size.</summary>
        public static Sprite RoundedRect
        {
            get
            {
                return Get("rrect", () =>
                {
                    const int size = 64;
                    const float rad = 16f;
                    return Make(size, size, (x, y) =>
                    {
                        float cx = Mathf.Clamp(x + 0.5f, rad, size - rad);
                        float cy = Mathf.Clamp(y + 0.5f, rad, size - rad);
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        return White(Mathf.Clamp01(rad - d + 0.5f));
                    }, new Vector4(rad, rad, rad, rad));
                });
            }
        }

        /// <summary>The house card back: burgundy lattice with a white border.</summary>
        public static Sprite CardBack
        {
            get
            {
                return Get("cardback", () =>
                {
                    const int w = 128, h = 180;
                    const float rad = 12f;
                    var white = new Color(0.97f, 0.96f, 0.93f);
                    var red = new Color(0.50f, 0.06f, 0.10f);
                    var red2 = new Color(0.66f, 0.12f, 0.16f);
                    var gold = new Color(0.86f, 0.70f, 0.36f);
                    return Make(w, h, (x, y) =>
                    {
                        float a = RoundedAlpha(x, y, w, h, rad);
                        Color c;
                        bool inner = x >= 9 && x < w - 9 && y >= 9 && y < h - 9;
                        bool frame = inner && (x < 12 || x >= w - 12 || y < 12 || y >= h - 12);
                        if (!inner) c = white;
                        else if (frame) c = gold;
                        else
                        {
                            // diamond lattice
                            int u = (x + y) % 16, v = (x - y + 1000) % 16;
                            c = (u < 2 || v < 2) ? red2 : red;
                        }
                        c.a = a;
                        return c;
                    });
                });
            }
        }

        /// <summary>White chip edge inserts, drawn over a tinted Circle to make a casino chip.</summary>
        public static Sprite ChipStripes
        {
            get
            {
                return Get("chipstripes", () => Make(128, 128, (x, y) =>
                {
                    float r = Radius(x, y);
                    float ang = Mathf.Atan2(y - 63.5f, x - 63.5f) * Mathf.Rad2Deg + 360f;
                    float a = 0f;
                    if (r > 0.74f && r < 0.97f && (ang % 60f) < 18f) a = 1f;          // edge spots
                    if (Mathf.Abs(r - 0.66f) < 0.025f && (ang % 20f) < 12f) a = 0.8f;  // dashed inner ring
                    a *= CircleAlpha(x, y, 128);
                    return new Color(1, 1, 1, a);
                }));
            }
        }

        /// <summary>Dark edges around the screen.</summary>
        public static Sprite Vignette
        {
            get
            {
                return Get("vignette", () => Make(256, 144, (x, y) =>
                {
                    float dx = (x - 127.5f) / 128f, dy = (y - 71.5f) / 72f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.0f, d));
                    return new Color(0, 0, 0, a * 0.85f);
                }));
            }
        }

        /// <summary>Vertical gradient for the back wall of the hall.</summary>
        public static Sprite WallGradient
        {
            get
            {
                return Get("wall", () => Make(4, 256, (x, y) =>
                {
                    float t = y / 255f;
                    var bottom = new Color(0.03f, 0.02f, 0.025f);
                    var top = new Color(0.20f, 0.07f, 0.07f);
                    return Color.Lerp(bottom, top, t * t);
                }));
            }
        }

        // ---------- helpers ----------

        private static Sprite Get(string key, Func<Sprite> make)
        {
            Sprite s;
            if (!Cache.TryGetValue(key, out s) || s == null)
            {
                s = make();
                s.name = "Procedural_" + key;
                Cache[key] = s;
            }
            return s;
        }

        private static Sprite Make(int w, int h, Func<int, int, Color> pixel, Vector4 border = default(Vector4))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            _currentSize = w;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = pixel(x, y);
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Color White(float a)
        {
            return new Color(1, 1, 1, a);
        }

        // Make() runs synchronously on the main thread, so it records the width of the
        // texture being generated and Radius() uses it (all radial textures are square).
        private static int _currentSize;

        /// <summary>Distance from the centre of the square texture being generated: 0 centre, 1 edge.</summary>
        private static float Radius(int x, int y)
        {
            return RadiusIn(x, y, _currentSize);
        }

        private static float RadiusIn(int x, int y, int size)
        {
            float c = size * 0.5f;
            float dx = (x + 0.5f - c) / c, dy = (y + 0.5f - c) / c;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float CircleAlpha(int x, int y, int size)
        {
            float c = size * 0.5f;
            float dx = x + 0.5f - c, dy = y + 0.5f - c;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(c - d - 0.5f);
        }

        private static float RoundedAlpha(int x, int y, int w, int h, float rad)
        {
            float cx = Mathf.Clamp(x + 0.5f, rad, w - rad);
            float cy = Mathf.Clamp(y + 0.5f, rad, h - rad);
            float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
            return Mathf.Clamp01(rad - d + 0.5f);
        }
    }
}

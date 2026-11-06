using System.Collections.Generic;
using BarvazTheDealer.Core;
using UnityEngine;

namespace BarvazTheDealer.Game
{
    /// <summary>
    /// Loads art, fonts and audio from Resources. Every sprite also works if the texture was
    /// imported as a plain Texture2D (it is converted at runtime), so import settings can't break the game.
    /// </summary>
    public static class GameAssets
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        private static Font _display, _bold, _medium;

        /// <summary>9-slice borders (left, bottom, right, top) for frame sprites, in source pixels.</summary>
        private static Vector4 BorderFor(string path)
        {
            if (path.StartsWith("UI/btn_")) return new Vector4(48, 40, 48, 44);
            if (path == "UI/panel") return new Vector4(36, 36, 36, 36);
            if (path == "UI/bubble") return new Vector4(44, 44, 44, 44);
            return Vector4.zero;
        }

        public static Sprite Sprite(string path)
        {
            Sprite s;
            if (Sprites.TryGetValue(path, out s) && s != null) return s;

            Vector4 border = BorderFor(path);
            s = Resources.Load<Sprite>(path);
            if (s != null && border != Vector4.zero && s.border == Vector4.zero)
                s = null; // imported without borders: rebuild below so 9-slicing works
            if (s == null)
            {
                var tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                }
                else
                {
                    Debug.LogWarning("[Barvaz] Missing art: Resources/" + path);
                }
            }
            Sprites[path] = s;
            return s;
        }

        public static Sprite CardFace(Card c)
        {
            char suit = c.Suit == Suit.Spades ? 'S' : c.Suit == Suit.Hearts ? 'H' : c.Suit == Suit.Diamonds ? 'D' : 'C';
            return Sprite("Cards/" + suit + (int)c.Rank);
        }

        public static Sprite CardBack
        {
            get { return Sprite("Cards/back"); }
        }

        public static readonly int[] ChipValues = { 500, 100, 25, 10, 5 };

        public static Sprite Chip(int value)
        {
            return Sprite("Chips/chip" + value);
        }

        /// <summary>Breaks an amount into casino chips, biggest first (at most maxChips; the rest is merged into the last chip).</summary>
        public static List<int> BreakIntoChips(int amount, int maxChips = 8)
        {
            var list = new List<int>();
            foreach (int v in ChipValues)
            {
                while (amount >= v && list.Count < maxChips)
                {
                    list.Add(v);
                    amount -= v;
                }
            }
            if (amount > 0 && list.Count == 0) list.Add(5);
            return list;
        }

        public static Font Display
        {
            get { return _display != null ? _display : (_display = LoadFont("Fonts/SecularOne-Regular")); }
        }

        public static Font Bold
        {
            get { return _bold != null ? _bold : (_bold = LoadFont("Fonts/Rubik-Bold")); }
        }

        public static Font Medium
        {
            get { return _medium != null ? _medium : (_medium = LoadFont("Fonts/Rubik-Medium")); }
        }

        private static Font LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f != null) return f;
            Debug.LogWarning("[Barvaz] Missing font " + path + ", using an OS font.");
            return Font.CreateDynamicFontFromOSFont(new[] { "Arial Hebrew", "Arial", "Helvetica" }, 32);
        }

        public static AudioClip Clip(string name)
        {
            AudioClip c;
            if (Clips.TryGetValue(name, out c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null) Debug.LogWarning("[Barvaz] Missing sound: Resources/Audio/" + name);
            Clips[name] = c;
            return c;
        }
    }
}

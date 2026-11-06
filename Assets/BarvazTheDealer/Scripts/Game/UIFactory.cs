using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BarvazTheDealer.Game
{
    /// <summary>Small helpers for building uGUI from code.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Vector2 pos, Vector2 size, Sprite sprite, Color color)
        {
            var rt = Rect(name, parent, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>A text label. Hebrew is converted to visual order; maxChars wraps long lines.</summary>
        public static Text Text(string name, Transform parent, string text, int size, Color color, Vector2 pos, Vector2 box,
            TextAnchor align = TextAnchor.MiddleCenter, Font font = null, int maxChars = 0)
        {
            var rt = Rect(name, parent, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font != null ? font : GameAssets.Bold;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            SetHeb(t, text, maxChars);
            return t;
        }

        public static void SetHeb(Text t, string logical, int maxChars = 0)
        {
            t.text = Rtl.Visual(logical ?? "", maxChars);
        }

        public static void AddShadow(Graphic g, float alpha = 0.6f, float distance = 2f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, alpha);
            s.effectDistance = new Vector2(distance, -distance);
        }

        public static void AddOutline(Graphic g, Color color, float distance = 2f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>A glossy 9-sliced button with a Hebrew label, hover/press juice and a click sound.</summary>
        public static Button Button(string name, Transform parent, string label, Vector2 pos, Vector2 size,
            string skin, Color textColor, UnityAction onClick)
        {
            var img = Image(name, parent, pos, size, GameAssets.Sprite("UI/" + skin), Color.white);
            img.raycastTarget = true;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            img.gameObject.AddComponent<ButtonJuice>();

            var t = Text("Label", img.transform, label, 30, textColor, new Vector2(0, 3), size, TextAnchor.MiddleCenter, GameAssets.Bold);
            AddShadow(t, 0.45f, 2f);
            return btn;
        }

        public static void SetLabel(Button b, string logical)
        {
            SetHeb(b.GetComponentInChildren<Text>(), logical);
        }
    }
}

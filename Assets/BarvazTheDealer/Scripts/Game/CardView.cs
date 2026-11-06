using System;
using System.Collections;
using BarvazTheDealer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarvazTheDealer.Game
{
    /// <summary>One card on the table: rendered face/back sprites, deal, flip, select-to-swap, and a win glow.</summary>
    public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform _rt;     // moves around the table
        private RectTransform _visual; // lifts/tilts locally (selection, hover)
        private Image _face;
        private Image _back;
        private Image _glow;
        private Image _shadow;
        private Vector2 _home;
        private float _lift;
        private float _liftTarget;
        private bool _hover;

        public bool IsFaceUp { get; private set; }
        public bool Selected { get; private set; }
        public bool Selectable { get; set; }
        public Action<CardView> Clicked;
        public RectTransform RectTransform { get { return _rt; } }

        public static CardView Create(Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = UIFactory.Rect("Card", parent, pos, size);
            var view = rt.gameObject.AddComponent<CardView>();
            view.Build(size);
            view._home = pos;
            return view;
        }

        private void Build(Vector2 size)
        {
            _rt = (RectTransform)transform;
            // An invisible raycast target the size of the card, so it can be clicked.
            var hit = _rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;

            _visual = UIFactory.Rect("Visual", _rt, Vector2.zero, size);
            _glow = UIFactory.Image("Glow", _visual, Vector2.zero, size + new Vector2(30, 30), ProceduralArt.RoundedRect, new Color(1f, 0.82f, 0.3f, 0.9f));
            _glow.enabled = false;
            _shadow = UIFactory.Image("Shadow", _visual, new Vector2(6, -9), size, ProceduralArt.RoundedRect, new Color(0, 0, 0, 0.5f));
            _face = UIFactory.Image("Face", _visual, Vector2.zero, size, null, Color.white);
            _back = UIFactory.Image("Back", _visual, Vector2.zero, size, GameAssets.CardBack, Color.white);
            _face.type = _back.type = Image.Type.Simple;
            SetFaceUp(false);
        }

        public void SetCard(Card card)
        {
            _face.sprite = GameAssets.CardFace(card);
            SetHighlight(false);
            SetSelected(false);
        }

        public void SetFaceUp(bool faceUp)
        {
            IsFaceUp = faceUp;
            _face.gameObject.SetActive(faceUp);
            _back.gameObject.SetActive(!faceUp);
        }

        public void SetHighlight(bool on)
        {
            _glow.enabled = on;
            _glow.color = new Color(1f, 0.82f, 0.3f, 0.9f);
        }

        public void SetSelected(bool on)
        {
            Selected = on;
            _liftTarget = on ? 34f : 0f;
            _glow.enabled = on;
            _glow.color = new Color(0.45f, 0.9f, 1f, 0.95f);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (Selectable && Clicked != null) Clicked(this);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _hover = true;
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hover = false;
        }

        private void Update()
        {
            float target = _liftTarget + (Selectable && _hover && !Selected ? 12f : 0f);
            _lift = Mathf.Lerp(_lift, target, 1f - Mathf.Exp(-Time.deltaTime * 18f));
            _visual.anchoredPosition = new Vector2(0, _lift);
            _shadow.rectTransform.anchoredPosition = new Vector2(6 + _lift * 0.25f, -9 - _lift * 0.45f);
            if (_glow.enabled)
            {
                var c = _glow.color;
                c.a = 0.65f + 0.3f * Mathf.Sin(Time.time * 6f);
                _glow.color = c;
            }
        }

        /// <summary>Slides the card from the shoe to its spot, spinning in, optionally turning face up on arrival.</summary>
        public IEnumerator DealFrom(Vector2 from, Vector2 to, bool faceUp, float duration)
        {
            gameObject.SetActive(true);
            _home = to;
            SetFaceUp(false);
            Sfx.Play("card_deal", 0.7f, 0.12f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = EaseOutBack(Mathf.Clamp01(t / duration));
                _rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                _rt.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(-200f, 0f, Mathf.Clamp01(t / duration) * (2f - Mathf.Clamp01(t / duration))));
                _rt.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(t / duration));
                yield return null;
            }
            _rt.anchoredPosition = to;
            _rt.localRotation = Quaternion.identity;
            _rt.localScale = Vector3.one;
            if (faceUp) yield return Flip(true, 0.2f);
        }

        /// <summary>Throws the card away (to the muck) and hides it.</summary>
        public IEnumerator Discard(Vector2 to, float duration)
        {
            Vector2 from = _rt.anchoredPosition + _visual.anchoredPosition;
            SetSelected(false);
            _lift = 0f;
            Sfx.Play("swap", 0.8f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                _rt.anchoredPosition = Vector2.Lerp(from, to, k * k);
                _rt.localRotation = Quaternion.Euler(0, 0, 360f * k);
                _rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, k);
                yield return null;
            }
            gameObject.SetActive(false);
            _rt.localRotation = Quaternion.identity;
            _rt.localScale = Vector3.one;
            _rt.anchoredPosition = _home;
        }

        /// <summary>Turns the card over by squashing it horizontally and swapping sides halfway.</summary>
        public IEnumerator Flip(bool faceUp, float duration)
        {
            if (faceUp == IsFaceUp) yield break;
            Sfx.Play("card_flip", 0.8f, 0.1f);
            float half = duration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / half);
                _rt.localScale = new Vector3(Mathf.Lerp(1f, 0f, k), 1f + 0.12f * k, 1f);
                yield return null;
            }
            SetFaceUp(faceUp);
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / half);
                _rt.localScale = new Vector3(Mathf.Lerp(0f, 1f, k), 1.12f - 0.12f * k, 1f);
                yield return null;
            }
            _rt.localScale = Vector3.one;
        }

        /// <summary>Quick hop, used when a winning card lights up.</summary>
        public IEnumerator Hop(float delay)
        {
            yield return new WaitForSeconds(delay);
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float k = t / 0.3f;
                _rt.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            _rt.localScale = Vector3.one;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.4f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}

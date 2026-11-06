using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarvazTheDealer.Game
{
    /// <summary>
    /// Barvaz, seated behind the table: a body layer and a separate head layer (cut from his photo)
    /// so the head can breathe, sway, laugh, and snap sideways when slapped.
    /// Child coordinates are in source-photo pixels (1202 x 1600); the root is scaled down.
    /// </summary>
    public class DealerView : MonoBehaviour
    {
        // Layout of the cut-out photo (source pixels): see Resources/Art/barvaz_*.png
        private const float SrcW = 1202f, SrcH = 1600f;
        private static readonly Rect HeadBox = Rect.MinMaxRect(385, 127, 972, 1025);
        private static readonly Rect BodyBox = Rect.MinMaxRect(0, 896, 1202, 1600);
        private static readonly Vector2 NeckPivot = new Vector2(635, 985);
        private static readonly Vector2 Cheek = new Vector2(790, 690);

        public const float Scale = 0.34f;

        private RectTransform _root;
        private RectTransform _body;
        private RectTransform _head;
        private Image _headImg;
        private Image _handprint;
        private Image _dizzy;
        private ClickRelay _click;

        // springs
        private float _angle, _angVel;
        private float _offX, _offXVel;
        private float _squash, _squashVel;
        private float _flush;
        private float _printAlpha;
        private float _dizzyTime;
        private float _talkTime;
        private float _laughTime;
        private float _worryTime;
        private bool _clickable;

        private class ClickRelay : MonoBehaviour, IPointerClickHandler
        {
            public Action OnClick;
            public void OnPointerClick(PointerEventData e)
            {
                if (OnClick != null) OnClick();
            }
        }

        public static DealerView Create(Transform parent, Vector2 pos)
        {
            var rt = UIFactory.Rect("Barvaz", parent, pos, new Vector2(SrcW, SrcH));
            rt.localScale = Vector3.one * Scale;
            var view = rt.gameObject.AddComponent<DealerView>();
            view.Build();
            return view;
        }

        private static Vector2 Local(Vector2 src)
        {
            return new Vector2(src.x - SrcW * 0.5f, SrcH * 0.5f - src.y);
        }

        private static Vector2 Center(Rect r)
        {
            return Local(r.center);
        }

        private void Build()
        {
            _root = (RectTransform)transform;

            // soft shadow on the wall behind him
            UIFactory.Image("Wall Shadow", _root, Local(new Vector2(640, 900)), new Vector2(1500, 1500), ProceduralArt.SoftCircle, new Color(0, 0, 0, 0.55f));

            _body = UIFactory.Image("Body", _root, Center(BodyBox), BodyBox.size, GameAssets.Sprite("Art/barvaz_body"), Color.white).rectTransform;
            _body.pivot = new Vector2(0.5f, 0f);
            _body.anchoredPosition = Local(new Vector2(BodyBox.center.x, BodyBox.yMax));

            // Head container pivots at the neck.
            _head = UIFactory.Rect("Head", _root, Local(NeckPivot), HeadBox.size);
            _head.pivot = new Vector2((NeckPivot.x - HeadBox.xMin) / HeadBox.width, (HeadBox.yMax - NeckPivot.y) / HeadBox.height);
            _head.anchoredPosition = Local(NeckPivot);

            _headImg = UIFactory.Image("Face", _head, Vector2.zero, HeadBox.size, GameAssets.Sprite("Art/barvaz_head"), Color.white);
            _headImg.rectTransform.anchorMin = Vector2.zero;
            _headImg.rectTransform.anchorMax = Vector2.one;
            _headImg.rectTransform.offsetMin = _headImg.rectTransform.offsetMax = Vector2.zero;
            _headImg.raycastTarget = true;
            _click = _headImg.gameObject.AddComponent<ClickRelay>();

            // Handprint lives on the head so it moves with it. Position is relative to the head box centre.
            Vector2 cheekLocal = new Vector2(Cheek.x - HeadBox.center.x, HeadBox.center.y - Cheek.y);
            _handprint = UIFactory.Image("Handprint", _headImg.transform, cheekLocal, new Vector2(300, 300), GameAssets.Sprite("FX/handprint"), new Color(1, 1, 1, 0));
            _handprint.rectTransform.localRotation = Quaternion.Euler(0, 0, 28);

            _dizzy = UIFactory.Image("Dizzy", _head, new Vector2(0, HeadBox.height * 0.5f), new Vector2(380, 380), GameAssets.Sprite("FX/dizzy"), new Color(1, 1, 1, 0));
        }

        public void SetClickable(bool on, Action onClick)
        {
            _clickable = on;
            _click.OnClick = on ? onClick : null;
        }

        /// <summary>The cheek's position in the given parent's local space (for aiming the slap hand).</summary>
        public Vector2 CheekIn(RectTransform space)
        {
            Vector3 world = _handprint.rectTransform.TransformPoint(Vector3.zero);
            return space.InverseTransformPoint(world);
        }

        public Vector2 HeadTopIn(RectTransform space)
        {
            Vector3 world = _head.TransformPoint(new Vector3(0, HeadBox.height * 0.95f, 0));
            return space.InverseTransformPoint(world);
        }

        public void Talk(float seconds)
        {
            _talkTime = Mathf.Max(_talkTime, seconds);
        }

        public void Laugh()
        {
            _laughTime = 1.4f;
        }

        public void Worry()
        {
            _worryTime = 1.6f;
        }

        /// <summary>The slap lands: the head snaps sideways, flushes red, gets a handprint and sees stars.</summary>
        public void Impact(float power)
        {
            _angVel += 520f * power;
            _offXVel -= 1500f * power;
            _squashVel -= 3.5f * power;
            _flush = 1f;
            _printAlpha = Mathf.Min(1f, _printAlpha + 0.75f);
            _dizzyTime = 2.4f;
            _laughTime = 0f;
            _worryTime = 0f;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float t = Time.time;

            // springs back to rest
            _angVel += (-260f * _angle - 10f * _angVel) * dt;
            _angle += _angVel * dt;
            _offXVel += (-260f * _offX - 12f * _offXVel) * dt;
            _offX += _offXVel * dt;
            _squashVel += (-300f * _squash - 14f * _squashVel) * dt;
            _squash += _squashVel * dt;

            float extraAngle = 0f, extraY = 0f;
            if (_talkTime > 0f)
            {
                _talkTime -= dt;
                extraAngle += Mathf.Sin(t * 13f) * 1.2f;
                extraY += Mathf.Abs(Mathf.Sin(t * 13f)) * 6f;
            }
            if (_laughTime > 0f)
            {
                _laughTime -= dt;
                extraAngle += Mathf.Sin(t * 26f) * 3.5f;
                extraY += Mathf.Abs(Mathf.Sin(t * 13f)) * 22f;
            }
            if (_worryTime > 0f)
            {
                _worryTime -= dt;
                extraAngle += Mathf.Sin(t * 30f) * 1.4f - 4f;
            }
            if (_dizzyTime > 0f)
            {
                extraAngle += Mathf.Sin(t * 5f) * 5f * Mathf.Clamp01(_dizzyTime);
            }

            float breathe = Mathf.Sin(t * 1.7f);
            float sway = Mathf.Sin(t * 0.6f) * 1.3f;
            _head.localRotation = Quaternion.Euler(0, 0, sway + _angle + extraAngle);
            _head.anchoredPosition = Local(NeckPivot) + new Vector2(_offX, breathe * 4f + extraY);
            _head.localScale = new Vector3(1f + _squash * 0.08f, 1f - _squash * 0.05f, 1f);
            _body.localScale = new Vector3(1f + breathe * 0.004f, 1f + breathe * 0.008f, 1f);

            // red flush fades
            _flush = Mathf.MoveTowards(_flush, 0f, dt * 0.7f);
            float pulse = _clickable ? 0.08f + 0.08f * Mathf.Sin(t * 8f) : 0f;
            _headImg.color = new Color(1f + pulse, 1f - 0.3f * _flush + pulse * 0.5f, 1f - 0.35f * _flush + pulse * 0.2f, 1f);

            // handprint fades slowly
            _printAlpha = Mathf.MoveTowards(_printAlpha, 0f, dt * 0.12f);
            _handprint.color = new Color(1, 1, 1, Mathf.Clamp01(_printAlpha) * 0.85f);

            // stars orbit above the head
            if (_dizzyTime > 0f)
            {
                _dizzyTime -= dt;
                _dizzy.color = new Color(1, 1, 1, Mathf.Clamp01(_dizzyTime * 1.5f));
                _dizzy.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 240f);
                _dizzy.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 4f) * 40f, HeadBox.height * 0.47f);
            }
            else
            {
                _dizzy.color = new Color(1, 1, 1, 0);
            }
        }
    }
}

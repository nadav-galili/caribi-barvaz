using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarvazTheDealer.Game
{
    /// <summary>Hover grow, press squash, click sound, and an optional attention pulse for buttons.</summary>
    public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool Pulse;

        private Selectable _sel;
        private float _target = 1f;
        private float _scale = 1f;
        private float _vel;

        private void Awake()
        {
            _sel = GetComponent<Selectable>();
        }

        private void OnEnable()
        {
            _scale = 0.6f;
            _target = 1f;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (_sel == null || _sel.interactable) _target = 1.06f;
        }

        public void OnPointerExit(PointerEventData e)
        {
            _target = 1f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_sel != null && !_sel.interactable) return;
            _scale = 0.9f;
            Sfx.Play("click", 0.6f);
        }

        public void OnPointerUp(PointerEventData e)
        {
            _target = 1f;
        }

        private void Update()
        {
            float target = _target;
            if (Pulse && (_sel == null || _sel.interactable)) target *= 1f + 0.05f * Mathf.Sin(Time.time * 7f);
            // critically-damped-ish spring for a springy feel
            _scale = Mathf.SmoothDamp(_scale, target, ref _vel, 0.07f);
            transform.localScale = new Vector3(_scale, _scale, 1f);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace Aethvirse.Input.Touch
{
    /// <summary>
    /// Floating joystick: the stick appears where the thumb lands inside its zone and feeds a
    /// virtual gamepad stick, so gameplay reads it through the normal Move action.
    /// </summary>
    public sealed class FloatingJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] string stickPath = "<Gamepad>/leftStick";
        [Tooltip("Thumb travel for full tilt, in canvas units.")]
        [SerializeField] float radius = 110f;

        RectTransform _zone;
        RectTransform _base;
        RectTransform _knob;
        int _pointerId = int.MinValue;

        protected override string controlPathInternal { get => stickPath; set => stickPath = value; }

        public void Setup(RectTransform stickBase, RectTransform knob, float travel)
        {
            _zone = (RectTransform)transform;
            _base = stickBase;
            _knob = knob;
            radius = travel;
            SetVisible(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = eventData.pointerId;
            _base.anchoredPosition = ToZone(eventData);
            _knob.anchoredPosition = Vector2.zero;
            SetVisible(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Vector2 offset = Vector2.ClampMagnitude(ToZone(eventData) - _base.anchoredPosition, radius);
            _knob.anchoredPosition = offset;
            SendValueToControl(offset / radius);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = int.MinValue;
            SendValueToControl(Vector2.zero);
            SetVisible(false);
        }

        protected override void OnDisable()
        {
            _pointerId = int.MinValue;
            SendValueToControl(Vector2.zero);
            base.OnDisable();
        }

        Vector2 ToZone(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, eventData.position, eventData.pressEventCamera, out Vector2 local);
            // Base/knob are anchored at the zone's bottom-left corner.
            return local - _zone.rect.min;
        }

        void SetVisible(bool on)
        {
            if (_base != null) _base.gameObject.SetActive(on);
        }
    }
}

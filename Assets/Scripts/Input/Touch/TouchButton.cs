using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace Aethvirse.Input.Touch
{
    /// <summary>On-screen button that presses a virtual gamepad button; dims its image while held.</summary>
    public sealed class TouchButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {
        [InputControl(layout = "Button")]
        [SerializeField] string buttonPath = "<Gamepad>/buttonSouth";
        [SerializeField] Graphic graphic;

        static readonly Color Idle = new Color(1f, 1f, 1f, 0.35f);
        static readonly Color Held = new Color(0.55f, 0.9f, 1f, 0.75f);

        protected override string controlPathInternal { get => buttonPath; set => buttonPath = value; }

        public void SetGraphic(Graphic target)
        {
            graphic = target;
            if (graphic != null) graphic.color = Idle;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SendValueToControl(1f);
            if (graphic != null) graphic.color = Held;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        protected override void OnDisable()
        {
            Release();
            base.OnDisable();
        }

        void Release()
        {
            SendValueToControl(0f);
            if (graphic != null) graphic.color = Idle;
        }
    }
}

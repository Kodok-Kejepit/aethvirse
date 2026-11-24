using Aethvirse.Cameras;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Aethvirse.Input.Touch
{
    /// <summary>Drag on the right side of the screen to turn the camera (one finger at a time).</summary>
    public sealed class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // Godot used 0.006 rad per pixel at a 1080 p reference height.
        const float DegreesPerReferencePixel = 0.006f * Mathf.Rad2Deg;
        const float ReferenceHeight = 1080f;

        PlayerCameraRig _rig;
        int _pointerId = int.MinValue;

        public void Setup(PlayerCameraRig rig) => _rig = rig;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId == int.MinValue) _pointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId || _rig == null) return;
            // Same feel on every screen size: scale pixels to a 1080 p screen.
            float scale = ReferenceHeight / Mathf.Max(Screen.height, 1);
            _rig.AddLookDegrees(eventData.delta * (scale * DegreesPerReferencePixel));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) _pointerId = int.MinValue;
        }
    }
}

using UnityEngine;

namespace Aethvirse.Input.Touch
{
    /// <summary>Keeps the controls inside the phone's safe area (notch, rounded corners).</summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        public RectTransform Target;
        Rect _applied;

        void Update()
        {
            Rect safe = Screen.safeArea;
            if (Target == null || safe == _applied || Screen.width <= 0 || Screen.height <= 0) return;
            _applied = safe;
            Target.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            Target.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }
    }
}

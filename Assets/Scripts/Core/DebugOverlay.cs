using Aethvirse.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aethvirse.Core
{
    /// <summary>F3 debug overlay: FPS, player state, speed, stamina and position.</summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] bool visibleOnStart = true;

        const float FpsSmoothing = 0.1f;
        bool _visible;
        float _smoothedDelta;
        GUIStyle _style;

        void Awake()
        {
            _visible = visibleOnStart;
            _smoothedDelta = 1f / 60f;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) _visible = !_visible;
            _smoothedDelta = Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, FpsSmoothing);
        }

        void OnGUI()
        {
            if (!_visible) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
            string text = $"FPS {1f / _smoothedDelta:0}";
            if (player != null)
            {
                Vector3 p = player.transform.position;
                text += $"\n{player.DebugState}\npos {p.x:0.0} {p.y:0.0} {p.z:0.0}";
            }
            text += "\nF3 hide · click = capture mouse · Esc = release";
            GUI.Label(new Rect(12, 8, 520, 120), text, _style);
        }
    }
}

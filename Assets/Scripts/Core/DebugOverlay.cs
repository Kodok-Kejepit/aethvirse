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
            text += "\nF3 hide · camera: mouse / drag / arrow keys · click = capture, Esc = release";
            var area = new Rect(8, 8, 620, 104);
            // Dark backing so the text stays readable over a bright sky.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 8, area.y + 4, area.width - 16, area.height - 8), text, _style);
        }
    }
}

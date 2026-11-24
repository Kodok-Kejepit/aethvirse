using Aethvirse.Combat;
using Aethvirse.Player;
using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Graybox HUD (M2): HP and stamina bars, lock-on target name and the perfect-dodge callout.
    /// The combo counter is the <see cref="ComboFlameHud"/>. The real Codex-style HUD comes in M4.
    /// </summary>
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] PlayerCombat combat;
        [SerializeField] LockOnTargeter lockOn;
        [SerializeField] Damageable health;

        const float CalloutTime = 1.2f;
        float _callout;
        GUIStyle _small;
        GUIStyle _center;

        void OnEnable()
        {
            if (combat == null) return;
            combat.PerfectDodge += OnPerfectDodge;
        }

        void OnDisable()
        {
            if (combat == null) return;
            combat.PerfectDodge -= OnPerfectDodge;
        }

        void OnPerfectDodge() => _callout = CalloutTime;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _callout = Mathf.Max(_callout - dt, 0f);
        }

        void OnGUI()
        {
            float s = Screen.height / 1080f;
            _small ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } };
            _center ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _small.fontSize = Mathf.RoundToInt(20 * s);

            // Bottom left: HP + stamina.
            float x = 24 * s, w = 420 * s, y = Screen.height - 96 * s;
            if (health != null)
                Bar(new Rect(x, y, w, 26 * s), health.Health.Ratio, new Color(0.85f, 0.2f, 0.25f), $"HP {health.Health.Current} / {health.Health.maxHealth}");
            if (player != null)
                Bar(new Rect(x, y + 34 * s, w * 0.75f, 14 * s), player.Stamina.Ratio,
                    player.Stamina.Exhausted ? new Color(0.6f, 0.5f, 0.2f) : new Color(0.95f, 0.8f, 0.25f), null);

            // Top centre: lock-on target.
            ILockable target = lockOn != null ? lockOn.Target : null;
            if (target != null && !(target is Object o && o == null))
            {
                _center.fontSize = Mathf.RoundToInt(24 * s);
                _center.normal.textColor = new Color(1f, 0.45f, 0.45f);
                GUI.Label(new Rect(0, 20 * s, Screen.width, 36 * s), "> " + target.DisplayName + " <", _center);
            }

            if (_callout > 0f)
            {
                _center.fontSize = Mathf.RoundToInt(48 * s);
                _center.normal.textColor = new Color(0.55f, 0.9f, 1f, Mathf.Clamp01(_callout / 0.3f));
                GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 70 * s), "PERFECT DODGE", _center);
            }
        }

        void Bar(Rect r, float ratio, Color fill, string label)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(ratio), r.height - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (label != null) GUI.Label(new Rect(r.x + 8, r.y, r.width, r.height), label, _small);
        }

    }
}

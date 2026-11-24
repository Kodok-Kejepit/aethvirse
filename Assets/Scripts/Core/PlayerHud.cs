using Aethvirse.Combat;
using Aethvirse.Player;
using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Graybox HUD (M2): HP and stamina bars, combo counter with rank, lock-on target name and the
    /// perfect-dodge callout. The real Codex-style HUD comes in M4.
    /// </summary>
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] PlayerCombat combat;
        [SerializeField] LockOnTargeter lockOn;
        [SerializeField] Damageable health;

        const float CalloutTime = 1.2f;
        int _combo;
        ComboRank _rank;
        float _comboPop;
        float _callout;
        GUIStyle _small;
        GUIStyle _big;
        GUIStyle _center;

        void OnEnable()
        {
            if (combat == null) return;
            combat.ComboChanged += OnCombo;
            combat.PerfectDodge += OnPerfectDodge;
        }

        void OnDisable()
        {
            if (combat == null) return;
            combat.ComboChanged -= OnCombo;
            combat.PerfectDodge -= OnPerfectDodge;
        }

        void OnCombo(int count, ComboRank rank)
        {
            if (count > _combo) _comboPop = 0.15f;
            _combo = count;
            _rank = rank;
        }

        void OnPerfectDodge() => _callout = CalloutTime;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _comboPop = Mathf.Max(_comboPop - dt, 0f);
            _callout = Mathf.Max(_callout - dt, 0f);
        }

        void OnGUI()
        {
            float s = Screen.height / 1080f;
            _small ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } };
            _big ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            _center ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _small.fontSize = Mathf.RoundToInt(20 * s);

            // Bottom left: HP + stamina.
            float x = 24 * s, w = 420 * s, y = Screen.height - 96 * s;
            if (health != null)
                Bar(new Rect(x, y, w, 26 * s), health.Health.Ratio, new Color(0.85f, 0.2f, 0.25f), $"HP {health.Health.Current} / {health.Health.maxHealth}");
            if (player != null)
                Bar(new Rect(x, y + 34 * s, w * 0.75f, 14 * s), player.Stamina.Ratio,
                    player.Stamina.Exhausted ? new Color(0.6f, 0.5f, 0.2f) : new Color(0.95f, 0.8f, 0.25f), null);

            // Right middle: combo.
            if (_combo > 0)
            {
                _big.fontSize = Mathf.RoundToInt((56 + _comboPop * 120) * s);
                _big.normal.textColor = RankColor(_rank);
                GUI.Label(new Rect(Screen.width - 420 * s, Screen.height * 0.38f, 380 * s, 70 * s), $"{_combo} HITS", _big);
                _big.fontSize = Mathf.RoundToInt(40 * s);
                GUI.Label(new Rect(Screen.width - 420 * s, Screen.height * 0.38f + 64 * s, 380 * s, 50 * s), _rank.ToString(), _big);
            }

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

        static Color RankColor(ComboRank rank) => rank switch
        {
            ComboRank.SSS => new Color(1f, 0.3f, 0.9f),
            ComboRank.S => new Color(1f, 0.8f, 0.2f),
            ComboRank.A => new Color(1f, 0.5f, 0.25f),
            ComboRank.B => new Color(0.45f, 0.85f, 1f),
            ComboRank.C => new Color(0.6f, 1f, 0.6f),
            _ => Color.white,
        };
    }
}

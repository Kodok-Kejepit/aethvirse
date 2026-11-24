using Aethvirse.Combat;
using Aethvirse.Player;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aethvirse.Core
{
    /// <summary>
    /// F3 debug overlay: FPS, player state, speed, stamina and position. Test cheats (until the cheat
    /// console exists): F6 = one level up, F7 = full HP/MP and no cooldowns. Gate them for release (M12).
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] bool visibleOnStart = true;

        const float FpsSmoothing = 0.1f;
        bool _visible;
        float _smoothedDelta;
        GUIStyle _style;
        PlayerStats _stats;
        SkillCaster _caster;

        void Start()
        {
            _stats = FindFirstObjectByType<PlayerStats>();
            _caster = FindFirstObjectByType<SkillCaster>();
        }

        void Cheats(Keyboard kb)
        {
            if (_stats != null && kb.f6Key.wasPressedThisFrame)
                _stats.AddXp(Progression.XpNeeded(_stats.Progression.level, _stats.Tuning) - _stats.Progression.xp);
            if (kb.f8Key.wasPressedThisFrame)
            {
                var clock = World.GameSession.Instance.Clock;
                World.GameSession.Instance.SetTime((clock.Hour + 1) % 24, clock.Minute);
            }
            if (kb.f9Key.wasPressedThisFrame)
            {
                var session = World.GameSession.Instance;
                var next = (World.WeatherKind)(((int)session.Weather.current + 1) % System.Enum.GetValues(typeof(World.WeatherKind)).Length);
                session.SetWeather(next, lockIt: true);
            }
            if (kb.f7Key.wasPressedThisFrame)
            {
                if (_stats != null) _stats.Recompute(refill: true);
                if (_caster != null) _caster.ResetCooldowns();
                var health = player != null ? player.GetComponent<Damageable>() : null;
                if (health != null) health.Health.Heal(health.Health.maxHealth);
            }
        }

        void Awake()
        {
            _visible = visibleOnStart;
            _smoothedDelta = 1f / 60f;
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f3Key.wasPressedThisFrame) _visible = !_visible;
                Cheats(kb);
            }
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
            text += "\nTest: F6 level up · F7 full HP/MP, no cooldowns · F8 +1 hour · F9 next weather · C status · P pause";
            var area = new Rect(8, 8, 640, 128);
            // Dark backing so the text stays readable over a bright sky.
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 8, area.y + 4, area.width - 16, area.height - 8), text, _style);
        }
    }
}

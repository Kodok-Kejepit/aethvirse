using Aethvirse.Enemies;
using Aethvirse.Player;
using Aethvirse.Secrets;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Endgame
{
    /// <summary>The Hollow Throne: The Unwritten wakes when you come near; its fall writes the ending of the first world.</summary>
    public sealed class ThroneBoss : MonoBehaviour
    {
        [SerializeField] string bossType = "the_unwritten";
        [SerializeField] float wakeRange = 25f;

        Enemy _boss;
        int _phase;
        bool _done;
        string _ending;
        GUIStyle _style;

        void Update()
        {
            if (GameSession.Instance.Flags.Has("unwritten_defeated") || _done) return;
            Transform p = PlayerTarget.Current;
            if (_boss == null && p != null && (p.position - transform.position).magnitude < wakeRange && EnemyCatalog.Instance != null)
            {
                RegionData r = RegionController.Current;
                _boss = EnemyCatalog.Instance.Spawn(bossType, transform.position, r != null ? r.levelMax : 350, 60f, true);
                if (_boss != null) _boss.MakeBoss(60f, 2.5f, 2.2f);
                Codex.Notify("[ ??? ]  The throne is not empty.", CodexTone.Anomaly);
            }
            if (_boss == null) return;
            if (_boss.State == EnemyState.Dead) { End(); return; }
            int phase = _boss.HealthRatio <= 0.15f ? 3 : _boss.HealthRatio <= 0.4f ? 2 : _boss.HealthRatio <= 0.7f ? 1 : 0;
            if (phase > _phase) { _phase = phase; _boss.Enrage(1.2f, 0.75f); Codex.Notify("The world around the throne flickers.", CodexTone.Anomaly); }
        }

        void End()
        {
            _done = true;
            GameSession.Instance.Flags.SetFlag("unwritten_defeated");
            int shadows = PlayerStats.Current != null ? PlayerStats.Current.Shadows.soldiers.Count : 0;
            char e = Endings.Pick(shadows, SecretSystem.Instance.State.found.Count);
            GameSession.Instance.Flags.SetFlag("ending_" + e);
            _ending = e switch
            {
                'B' => "ENDING B — An army of echoes kneels before an empty seat.",
                'C' => "ENDING C — You read every line this world kept. The book reads you back.",
                _ => "ENDING A — The throne stays empty. You walk away from it.",
            };
            Codex.Notify("The last line of this world waits somewhere without a map.  [ ??? ]", CodexTone.Anomaly);
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(_ending)) return;
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold };
            _style.fontSize = Mathf.RoundToInt(36 * Screen.height / 1080f);
            GUI.Label(new Rect(Screen.width * 0.1f, Screen.height * 0.3f, Screen.width * 0.8f, 200), _ending, _style);
            if (GUI.Button(new Rect(Screen.width * 0.4f, Screen.height * 0.55f, Screen.width * 0.2f, 60), "Continue")) _ending = null;
        }
    }
}

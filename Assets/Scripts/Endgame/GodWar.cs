using System.Collections.Generic;
using Aethvirse.Enemies;
using Aethvirse.Jobs;
using Aethvirse.Player;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Endgame
{
    /// <summary>
    /// The finale (STORY §6.5): the war at the Seat. Allies from every world fight beside you through three waves of
    /// the God's hosts, then the God itself comes down in four phases, calling its hosts again as it weakens.
    /// </summary>
    public sealed class GodWar : MonoBehaviour
    {
        public const string DefeatedFlag = "god_defeated";

        [SerializeField] string[] hostTypes = { "seraph_husk", "choir_lancer", "halo_warden" };
        [SerializeField] string allyType = "vanguard";
        [SerializeField] string godType = "the_god";
        [SerializeField] int allies = 6;
        [SerializeField] float startRange = 30f;

        enum Step { Waiting, Waves, God, Done }

        Step _step;
        int _wave;
        int _phase;
        Enemy _god;
        readonly List<Enemy> _hosts = new List<Enemy>();
        string _ending;
        GUIStyle _style;

        static readonly string[] WaveLines =
        {
            "The sky opens. Its hosts come down.",
            "Every world you walked is fighting with you.",
            "The last host. Behind it, something is watching.",
        };

        static readonly string[] PhaseLines =
        {
            "It speaks without a voice: 'You were written. Nothing more.'",
            "The ground forgets its shape.",
            "Every page you filled is burning. The Codex holds.",
        };

        int Level => RegionController.Current != null ? RegionController.Current.levelMax : 500;

        void Update()
        {
            if (GameSession.Instance.Flags.Has(DefeatedFlag) && _step != Step.Done) { _step = Step.Done; return; }
            Transform p = PlayerTarget.Current;
            switch (_step)
            {
                case Step.Waiting:
                    if (p == null || EnemyCatalog.Instance == null || (p.position - transform.position).magnitude > startRange) return;
                    SpawnAllies();
                    _step = Step.Waves;
                    StartWave();
                    break;
                case Step.Waves:
                    _hosts.RemoveAll(e => e == null || e.State == EnemyState.Dead);
                    if (_hosts.Count > 0) return;
                    if (++_wave < WaveLines.Length) { StartWave(); return; }
                    _step = Step.God;
                    _god = EnemyCatalog.Instance.Spawn(godType, transform.position + Vector3.forward * 12f, Level, 80f, false);
                    if (_god != null) { _god.MakeBoss(120f, 2.6f, 3f); BossArena.Begin(_god); }
                    Codex.Notify("[ ??? ]  It has come down.", CodexTone.Anomaly);
                    break;
                case Step.God:
                    if (_god == null) { _step = Step.Done; return; }
                    if (_god.State == EnemyState.Dead) { Win(); return; }
                    int phase = BossPhases.For(_god.HealthRatio);
                    if (phase > _phase)
                    {
                        _phase = phase;
                        _god.Enrage(1.15f, 0.75f);
                        Codex.Notify(PhaseLines[Mathf.Min(phase - 1, PhaseLines.Length - 1)], CodexTone.Anomaly);
                        for (int i = 0; i < 2 + phase; i++) SpawnHost(hostTypes[i % hostTypes.Length], 14f);
                    }
                    break;
            }
        }

        void StartWave()
        {
            Codex.Notify(WaveLines[_wave], CodexTone.Anomaly);
            int count = 4 + _wave * 2;
            for (int i = 0; i < count; i++) SpawnHost(hostTypes[(i + _wave) % hostTypes.Length], 18f);
        }

        void SpawnHost(string type, float radius)
        {
            float a = Random.value * Mathf.PI * 2f;
            Enemy e = EnemyCatalog.Instance.Spawn(type, transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius, Level, 80f, true);
            if (e == null) return;
            e.RespawnAtHome = false;
            _hosts.Add(e);
        }

        void SpawnAllies()
        {
            Transform p = PlayerTarget.Current;
            for (int i = 0; i < allies; i++)
            {
                Vector3 at = p.position + Quaternion.Euler(0f, i * 360f / allies, 0f) * Vector3.forward * 3f;
                Enemy body = EnemyCatalog.Instance.Spawn(allyType, at, Level, 0f, false);
                if (body != null) Minion.Convert(body, 0.6f, 1.5f, null);
            }
            Codex.Notify("Allies from every world stand with you.", CodexTone.System);
        }

        void Win()
        {
            _step = Step.Done;
            GameSession.Instance.Flags.SetFlag(DefeatedFlag);
            foreach (Enemy e in _hosts) if (e != null) Destroy(e.gameObject);
            _ending = "THE END\n\nThe Seat is empty. Every line is yours now.\n\n(Phase 1 build — thank you for playing)";
            Codex.Notify("The Codex remembers everything.", CodexTone.System);
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(_ending)) return;
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold };
            _style.fontSize = Mathf.RoundToInt(36 * Screen.height / 1080f);
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width * 0.1f, Screen.height * 0.2f, Screen.width * 0.8f, Screen.height * 0.4f), _ending, _style);
            if (GUI.Button(new Rect(Screen.width * 0.4f, Screen.height * 0.65f, Screen.width * 0.2f, 60), "Continue")) _ending = null;
        }
    }
}

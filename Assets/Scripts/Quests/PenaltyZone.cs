using System.Collections.Generic;
using Aethvirse.Enemies;
using Aethvirse.Player;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Quests
{
    /// <summary>
    /// The Penalty Zone (§10): survive 90 s while waves grow (1 + wave enemies every 15 s, your level, no loot).
    /// Then the exit portal appears and the penalty is cleared.
    /// </summary>
    public sealed class PenaltyZone : MonoBehaviour
    {
        public const float Duration = 90f;
        const float WaveInterval = 15f;
        const float ArenaRadius = 16f;

        [SerializeField] List<GameObject> enemyPrefabs = new List<GameObject>();
        [SerializeField] GameObject exitPortal;

        float _remaining = Duration;
        float _waveTimer;
        int _wave;
        bool _done;
        readonly List<GameObject> _spawned = new List<GameObject>();
        readonly System.Random _rng = new System.Random();
        GUIStyle _style;

        public float Remaining => _remaining;

        void Start()
        {
            if (exitPortal != null) exitPortal.SetActive(false);
            UI.Codex.Notify("[ PENALTY ZONE ] Survive.", UI.CodexTone.Anomaly);
        }

        void Update()
        {
            if (_done || PlayerTarget.Current == null) return;
            _remaining = Mathf.Max(_remaining - Time.deltaTime, 0f);
            _waveTimer -= Time.deltaTime;
            if (_waveTimer <= 0f)
            {
                _waveTimer = WaveInterval;
                SpawnWave();
            }
            if (_remaining <= 0f) Finish();
        }

        void SpawnWave()
        {
            if (enemyPrefabs.Count == 0) return;
            _wave++;
            int level = PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;
            for (int i = 0; i < 1 + _wave; i++)
            {
                float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
                Vector3 at = new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a)) * (ArenaRadius - 2f);
                GameObject go = Instantiate(enemyPrefabs[_rng.Next(enemyPrefabs.Count)], at, Quaternion.identity);
                Enemy e = go.GetComponent<Enemy>();
                if (e != null)
                {
                    e.SetLevel(level, aggroRadius: 60f, dropsLoot: false);
                    e.RespawnAtHome = false;
                }
                _spawned.Add(go);
            }
        }

        void Finish()
        {
            _done = true;
            foreach (GameObject go in _spawned) if (go != null) Destroy(go);
            QuestSystem.Instance.PenaltyServed();
            if (exitPortal != null) exitPortal.SetActive(true);
        }

        void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _style.fontSize = Mathf.RoundToInt(40 * Screen.height / 1080f);
            _style.normal.textColor = _done ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.4f, 0.4f);
            string text = _done ? "SURVIVED — find the exit" : $"SURVIVE  {Mathf.FloorToInt(_remaining / 60f)}:{Mathf.FloorToInt(_remaining) % 60:00}";
            GUI.Label(new Rect(0, 60, Screen.width, 60), text, _style);
        }
    }
}

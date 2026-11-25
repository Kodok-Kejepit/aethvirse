using System.Collections.Generic;
using Aethvirse.Enemies;
using Aethvirse.Gates;
using Aethvirse.UI;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>
    /// Runs dynamic Gates in a region scene: rolls new Gates on the hour, turns forgotten ones into Dungeon Breaks
    /// (a wave pours out; clearing it collapses the Gate and pays out).
    /// </summary>
    public sealed class WorldGatesDriver : MonoBehaviour
    {
        readonly System.Random _rng = new System.Random();
        readonly Dictionary<int, List<Enemy>> _breakWaves = new Dictionary<int, List<Enemy>>();
        int _spotCount;

        RegionData _r;
        RegionData Region => _r != null ? _r : _r = RegionController.In(gameObject.scene) ?? RegionController.Current;

        void Start()
        {
            _spotCount = Region != null ? Region.gateSpots.Count : 0;
            GameSession.Instance.HourChanged += OnHour;
            OnHour(GameSession.Instance.Clock.Hour);
            // Gates that broke while you were away are still pouring out.
            if (Region != null)
                foreach (OpenGate g in GameSession.Instance.Gates.In(Region.id))
                    if (g.broken) StartBreak(g);
        }

        void OnDestroy()
        {
            if (GameSession.Exists) GameSession.Instance.HourChanged -= OnHour;
        }

        int RegionTier()
        {
            RegionData r = Region;
            return r != null && WorldAtlas.Instance != null ? Mathf.Clamp(new List<RegionData>(WorldAtlas.Instance.AllRegions()).IndexOf(r), 0, 5) : 0;
        }

        void OnHour(int hour)
        {
            GameSession s = GameSession.Instance;
            if (Region == null) return;
            OpenGate g = s.Gates.Roll(Region.id, _spotCount, RegionTier(), s.Clock.totalMinutes, _rng);
            if (g != null) Codex.Notify($"A rank {WorldGates.RankNames[g.rank]} {(g.red ? "RED Gate" : "Gate")} has opened nearby.", g.red ? CodexTone.Warning : CodexTone.Info);
            foreach (OpenGate broke in s.Gates.CheckBreaks(s.Clock.totalMinutes))
                if (broke.region == Region.id) StartBreak(broke);
        }

        void StartBreak(OpenGate g)
        {
            if (_breakWaves.ContainsKey(g.uid) || Region == null) return;
            RegionData r = Region;
            Vector3 at = r.gateSpots[Mathf.Clamp(g.spot, 0, r.gateSpots.Count - 1)];
            int level = Mathf.Clamp(r.levelMin + g.rank * 5, r.levelMin, r.levelMax);
            var wave = new List<Enemy>();
            for (int i = 0; i < 4 + 2 * g.rank; i++)
            {
                Vector3 p = at + Quaternion.Euler(0f, i * 37f, 0f) * Vector3.forward * (3f + i % 3 * 2f);
                Enemy e = EnemyCatalog.Instance != null && r.enemies.Count > 0 ? EnemyCatalog.Instance.Spawn(r.enemies[i % r.enemies.Count], p, level, 40f) : null;
                if (e != null) wave.Add(e);
            }
            _breakWaves[g.uid] = wave;
            Codex.Notify("[ DUNGEON BREAK ] Monsters are pouring out of a Gate!", CodexTone.Warning);
        }

        void Update()
        {
            if (_breakWaves.Count == 0) return;
            foreach (int uid in new List<int>(_breakWaves.Keys))
            {
                List<Enemy> wave = _breakWaves[uid];
                wave.RemoveAll(e => e == null || e.State == EnemyState.Dead);
                if (wave.Count > 0) continue;
                _breakWaves.Remove(uid);
                OpenGate g = GameSession.Instance.Gates.Get(uid);
                int rank = g != null ? g.rank : 0;
                GameSession.Instance.Gates.Close(uid);
                GameSession.Instance.AddGold(100 * (rank + 1));
                Quests.QuestSystem.Instance.AddAcademyPoints(20 + 10 * rank);
                Codex.Notify("Dungeon Break contained. The Gate collapses.", CodexTone.Reward);
            }
        }
    }
}

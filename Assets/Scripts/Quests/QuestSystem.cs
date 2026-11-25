using System;
using System.Collections.Generic;
using Aethvirse.Core;
using Aethvirse.Items;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Quests
{
    /// <summary>
    /// Runs quests across scenes (Godot QuestSystem + DailyQuests): starts / auto-starts, counts game events,
    /// advances stages, pays rewards, academy rank, the Codex daily quest with its Penalty Zone, emergency quests.
    /// </summary>
    public sealed class QuestSystem : MonoBehaviour
    {
        public const string PenaltyScene = "PenaltyZone";
        const float PenaltyWarning = 10f;
        const float EmergencyChancePerHour = 0.12f;

        static QuestSystem _instance;
        public static QuestSystem Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("QuestSystem");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<QuestSystem>();
                return _instance;
            }
        }

        public QuestLog Log { get; private set; } = new QuestLog();
        QuestDatabase _db;
        readonly Dictionary<string, QuestData> _runtime = new Dictionary<string, QuestData>();
        readonly System.Random _rng = new System.Random();
        float _penaltyTimer = -1f;
        int _emergencySerial;
        float _distanceCarry;

        public event Action Changed;
        public event Action<QuestData> Completed;

        void Awake()
        {
            _db = Resources.Load<QuestDatabase>("QuestDatabase");
            GameEvents.EnemyKilled += id => Count(ObjectiveType.Kill, id, 1);
            GameEvents.Attacked += () => Count(ObjectiveType.Attack, "", 1);
            GameEvents.Dodged += () => Count(ObjectiveType.Dodge, "", 1);
            GameEvents.Moved += OnMoved;
            GameEvents.PickedUp += (id, n) => Count(ObjectiveType.Pickup, id, n);
            GameEvents.Crafted += id => Count(ObjectiveType.Craft, id, 1);
            GameEvents.ItemUsed += id => Count(ObjectiveType.UseItem, id, 1);
            GameEvents.SkillUsed += id => Count(ObjectiveType.Skill, id, 1);
            GameEvents.Talked += id => Count(ObjectiveType.Talk, id, 1);
            GameEvents.AreaEntered += id => Count(ObjectiveType.ReachArea, id, 1);
            GameEvents.GateCleared += id => Count(ObjectiveType.ClearGate, id, 1);
            GameSession.Instance.FlagSet += f => { if (!f.StartsWith("stage_")) Count(ObjectiveType.Flag, f, 1); };
            GameSession.Instance.DayChanged += OnDay;
            GameSession.Instance.HourChanged += OnHour;
        }

        public QuestData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_runtime.TryGetValue(id, out QuestData q)) return q;
            return _db != null ? _db.Get(id) : null;
        }

        public IEnumerable<QuestData> ActiveQuests()
        {
            foreach (QuestLog.State s in Log.active)
            {
                QuestData q = Get(s.id);
                if (q != null) yield return q;
            }
        }

        /// <summary>Fresh log for a new game (also re-registers today's daily).</summary>
        public void ResetForNewGame()
        {
            Log = new QuestLog();
            _runtime.Clear();
            _penaltyTimer = -1f;
            Changed?.Invoke();
        }

        public void Restore(QuestLog log)
        {
            Log = log ?? new QuestLog();
            _runtime.Clear();
            // Emergency quests are runtime-only.
            Log.active.RemoveAll(s => s.id.StartsWith("emergency_"));
            Changed?.Invoke();
        }

        /// <summary>Called by the player when a scene with gameplay starts (daily quest, auto-start story).</summary>
        public void OnWorldReady()
        {
            EnsureDaily(GameSession.Instance.Clock.Day);
            CheckAutoStart();
            if (GameSession.Instance.PenaltyPending && _penaltyTimer < 0f && !InPenaltyZone) SchedulePenalty();
        }

        public bool StartQuest(string id)
        {
            QuestData q = Get(id);
            if (q == null || !Log.Start(q, Time.time)) return false;
            if (q.kind == QuestKind.Sealed) Codex.Notify($"[ ??? ]  A sealed mission has been detected.\n{q.title}", CodexTone.Anomaly);
            else Codex.Notify(q.kind == QuestKind.Daily ? "[ DAILY QUEST ] has arrived." : $"New quest: {q.title}", q.kind == QuestKind.Emergency ? CodexTone.Warning : CodexTone.Quest);
            RefreshCollect(q);
            TryComplete(q);
            SyncStageFlag(q);
            Changed?.Invoke();
            return true;
        }

        void Update()
        {
            // Timed quests fail when their time runs out.
            for (int i = Log.active.Count - 1; i >= 0; i--)
            {
                QuestLog.State s = Log.active[i];
                QuestData q = Get(s.id);
                if (q != null && q.timeLimit > 0f && Time.time - s.startedAt > q.timeLimit)
                {
                    Log.Fail(q.id);
                    Codex.Notify($"Failed: {q.title}", CodexTone.Warning);
                    Changed?.Invoke();
                }
            }
            if (_penaltyTimer >= 0f && GameSession.Instance.WorldRunning)
            {
                _penaltyTimer -= Time.deltaTime;
                if (_penaltyTimer < 0f) Save.SceneTravel.Go(PenaltyScene, null, rememberReturn: true);
            }
        }

        bool InPenaltyZone => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == PenaltyScene;

        // --- Events -----------------------------------------------------------------------------------

        void OnMoved(float metres)
        {
            _distanceCarry += metres;
            if (_distanceCarry < 1f) return;
            int whole = Mathf.FloorToInt(_distanceCarry);
            _distanceCarry -= whole;
            Count(ObjectiveType.Distance, "", whole);
        }

        void Count(ObjectiveType type, string target, int amount)
        {
            bool any = false;
            foreach (QuestData q in new List<QuestData>(ActiveQuests()))
            {
                if (Log.Notify(q, type, target, amount)) any = true;
                if (type == ObjectiveType.Pickup) RefreshCollect(q);
                TryComplete(q);
            }
            if (any) Changed?.Invoke();
            if (type == ObjectiveType.Flag || type == ObjectiveType.Kill || type == ObjectiveType.Talk || type == ObjectiveType.ReachArea) CheckAutoStart();
        }

        /// <summary>COLLECT objectives mirror what is in the bag.</summary>
        void RefreshCollect(QuestData q)
        {
            PlayerInventory inv = PlayerInventory.Current;
            if (inv == null) return;
            for (int i = 0; i < q.objectives.Count; i++)
                if (q.objectives[i].type == ObjectiveType.Collect) Log.SetProgress(q, i, inv.Bag.CountOf(q.objectives[i].target));
        }

        /// <summary>Dialog can react to a quest's stage through the flag stage_&lt;quest&gt;_&lt;n&gt; (only the current one is set).</summary>
        void SyncStageFlag(QuestData q)
        {
            WorldFlags flags = GameSession.Instance.Flags;
            QuestLog.State st = Log.Get(q.id);
            string want = st != null ? $"stage_{q.id}_{st.stage}" : null;
            for (int i = 0; i <= q.StageCount; i++)
            {
                string f = $"stage_{q.id}_{i}";
                if (f != want && flags.Has(f)) flags.Clear(f);
            }
            if (want != null && !flags.Has(want)) flags.SetFlag(want);
        }

        void TryComplete(QuestData q)
        {
            bool done = Log.IsActive(q.id) && Log.AdvanceIfReady(q);
            SyncStageFlag(q);
            if (!done) return;
            Log.Complete(q.id);
            SyncStageFlag(q);
            PayRewards(q);
            Codex.Notify(q.kind == QuestKind.Sealed ? $"[ SEALED MISSION COMPLETE ]  {q.title}" : $"Quest complete: {q.title}", q.kind == QuestKind.Sealed ? CodexTone.Anomaly : CodexTone.Reward);
            Completed?.Invoke(q);
            if (q.kind == QuestKind.Daily) GameSession.Instance.PenaltyPending = false;
            Changed?.Invoke();
        }

        void PayRewards(QuestData q)
        {
            GameSession s = GameSession.Instance;
            if (q.academyPoints > 0) AddAcademyPoints(q.academyPoints);
            foreach (Reward r in q.rewards)
            {
                switch (r.type)
                {
                    case RewardType.Item:
                        PlayerInventory.Current?.Bag.Add(r.key, r.amount);
                        break;
                    case RewardType.Gold: s.AddGold(r.amount); break;
                    case RewardType.Xp: PlayerStats.Current?.AddXp(r.amount); break;
                    case RewardType.Flag: s.Flags.SetFlag(r.key); break;
                    case RewardType.AcademyPoints: AddAcademyPoints(r.amount); break;
                    case RewardType.CodexPoints: s.CodexPoints += r.amount; break;
                    case RewardType.CodexMessage: Codex.Notify(r.text, CodexTone.Anomaly); break;
                    case RewardType.StartQuest: StartQuest(r.key); break;
                }
            }
        }

        public void AddAcademyPoints(int amount)
        {
            GameSession s = GameSession.Instance;
            int before = AcademyRank.For(s.AcademyPoints);
            s.AcademyPoints += amount;
            int after = AcademyRank.For(s.AcademyPoints);
            if (after > before) Codex.Notify($"[ RANK UP ]  Academy rank {AcademyRank.Name(after)}", CodexTone.Reward);
        }

        void CheckAutoStart()
        {
            if (_db == null) return;
            foreach (QuestData q in _db.quests)
            {
                if (q == null || !q.autoStart || Log.IsActive(q.id) || Log.IsCompleted(q.id) || Log.failed.Contains(q.id)) continue;
                if (Condition.All(q.startConditions, GameContext.Instance)) StartQuest(q.id);
            }
        }

        // --- Daily / emergency / penalty ------------------------------------------------------------

        void OnDay(int day)
        {
            int previous = day - 1;
            if (Log.IsActive(DailyQuests.IdFor(previous)))
            {
                Log.Fail(DailyQuests.IdFor(previous));
                GameSession.Instance.PenaltyPending = true;
            }
            EnsureDaily(day);
            if (GameSession.Instance.PenaltyPending) SchedulePenalty();
        }

        void EnsureDaily(int day)
        {
            string id = DailyQuests.IdFor(day);
            if (!_runtime.ContainsKey(id))
            {
                int level = PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;
                StatTuning t = PlayerStats.Current != null ? PlayerStats.Current.Tuning : null;
                int xp = t != null ? Mathf.RoundToInt(Progression.XpNeeded(level, t) * 0.15f) : 15;
                _runtime[id] = DailyQuests.Build(day, level, xp);
            }
            if (!Log.IsActive(id) && !Log.IsCompleted(id) && !Log.failed.Contains(id)) StartQuest(id);
        }

        void OnHour(int hour)
        {
            // Sealed missions can wake at a certain hour.
            CheckAutoStart();
            if (InPenaltyZone || !GameSession.Instance.WorldRunning) return;
            foreach (QuestLog.State s in Log.active) if (s.id.StartsWith("emergency_")) return;
            if (_rng.NextDouble() >= EmergencyChancePerHour) return;
            int level = PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;
            QuestData q = DailyQuests.Emergency(++_emergencySerial, level);
            _runtime[q.id] = q;
            StartQuest(q.id);
        }

        void SchedulePenalty()
        {
            if (InPenaltyZone) return;
            _penaltyTimer = PenaltyWarning;
            Codex.Notify("[ WARNING ] Daily quest not completed. Penalty enforcement in 10 seconds.", CodexTone.Anomaly);
        }

        /// <summary>The Penalty Zone ends: back to where you were.</summary>
        public void PenaltyServed()
        {
            GameSession.Instance.PenaltyPending = false;
            _penaltyTimer = -1f;
            Codex.Notify("Penalty served. The Codex is satisfied — for now.", CodexTone.System);
        }
    }
}

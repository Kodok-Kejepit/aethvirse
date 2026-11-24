using System;
using System.Collections.Generic;

namespace Aethvirse.Quests
{
    /// <summary>Quest progress (pure, saved): active quests with stage + per-objective counts, completed, failed, tracked.</summary>
    [Serializable]
    public sealed class QuestLog
    {
        [Serializable]
        public sealed class State
        {
            public string id;
            public int stage;
            public List<int> progress = new List<int>();
            public float startedAt;
        }

        public List<State> active = new List<State>();
        public List<string> completed = new List<string>();
        public List<string> failed = new List<string>();
        public string tracked = "";

        public State Get(string id) => active.Find(s => s.id == id);
        public bool IsActive(string id) => Get(id) != null;
        public bool IsCompleted(string id) => completed.Contains(id);

        public bool Start(QuestData q, float now)
        {
            if (IsActive(q.id) || completed.Contains(q.id)) return false;
            var s = new State { id = q.id, startedAt = now };
            for (int i = 0; i < q.objectives.Count; i++) s.progress.Add(0);
            active.Add(s);
            if (string.IsNullOrEmpty(tracked)) tracked = q.id;
            return true;
        }

        public int Progress(string id, int index)
        {
            State s = Get(id);
            return s != null && index < s.progress.Count ? s.progress[index] : 0;
        }

        public bool IsObjectiveDone(QuestData q, int index) => Progress(q.id, index) >= q.objectives[index].count;

        /// <summary>The first unfinished required objective of the current stage (for the tracker).</summary>
        public QuestObjective Current(QuestData q, out int index)
        {
            index = -1;
            State s = Get(q.id);
            if (s == null) return null;
            for (int i = 0; i < q.objectives.Count; i++)
            {
                if (q.objectives[i].stage != s.stage || IsObjectiveDone(q, i)) continue;
                if (index < 0 || (!q.objectives[i].optional && q.objectives[index].optional)) index = i;
            }
            return index >= 0 ? q.objectives[index] : null;
        }

        /// <summary>Counts an event for the quest's current stage. Returns true if any objective moved.</summary>
        public bool Notify(QuestData q, ObjectiveType type, string target, int amount)
        {
            State s = Get(q.id);
            if (s == null) return false;
            bool moved = false;
            for (int i = 0; i < q.objectives.Count; i++)
            {
                QuestObjective o = q.objectives[i];
                if (o.stage != s.stage || o.type != type || IsObjectiveDone(q, i)) continue;
                if (!string.IsNullOrEmpty(o.target) && o.target != target) continue;
                s.progress[i] = Math.Min(o.count, s.progress[i] + amount);
                moved = true;
            }
            return moved;
        }

        /// <summary>Sets a COLLECT-style objective to an absolute value (items currently held).</summary>
        public bool SetProgress(QuestData q, int index, int value)
        {
            State s = Get(q.id);
            if (s == null || q.objectives[index].stage != s.stage) return false;
            int v = Math.Min(q.objectives[index].count, value);
            if (s.progress[index] == v) return false;
            s.progress[index] = v;
            return true;
        }

        /// <summary>Advances past finished stages. Returns true when the whole quest is done.</summary>
        public bool AdvanceIfReady(QuestData q)
        {
            State s = Get(q.id);
            if (s == null) return false;
            while (StageDone(q, s.stage))
            {
                s.stage++;
                if (s.stage >= q.StageCount) return true;
            }
            return false;
        }

        bool StageDone(QuestData q, int stage)
        {
            bool any = false;
            for (int i = 0; i < q.objectives.Count; i++)
            {
                if (q.objectives[i].stage != stage) continue;
                any = true;
                if (!q.objectives[i].optional && !IsObjectiveDone(q, i)) return false;
            }
            return any || stage < q.StageCount;
        }

        public void Complete(string id)
        {
            active.RemoveAll(s => s.id == id);
            if (!completed.Contains(id)) completed.Add(id);
            if (tracked == id) tracked = active.Count > 0 ? active[0].id : "";
        }

        public void Fail(string id)
        {
            active.RemoveAll(s => s.id == id);
            if (!failed.Contains(id)) failed.Add(id);
            if (tracked == id) tracked = active.Count > 0 ? active[0].id : "";
        }
    }
}

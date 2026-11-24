using System.Collections.Generic;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Jobs
{
    /// <summary>All jobs (Resources/JobRegistry): which ones a player may take, and the lineage of the current one.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Job Registry", fileName = "JobRegistry")]
    public sealed class JobRegistry : ScriptableObject
    {
        public List<JobData> jobs = new List<JobData>();

        static JobRegistry _loaded;
        public static JobRegistry Instance => _loaded != null ? _loaded : _loaded = Resources.Load<JobRegistry>("JobRegistry");

        public JobData Get(string id) => string.IsNullOrEmpty(id) ? null : jobs.Find(j => j != null && j.id == id);

        /// <summary>The current job and everything before it, oldest first.</summary>
        public List<JobData> Lineage(string currentJob)
        {
            var list = new List<JobData>();
            JobData j = Get(currentJob);
            int guard = 0;
            while (j != null && guard++ < 8)
            {
                list.Insert(0, j);
                j = Get(j.parent);
            }
            return list;
        }

        /// <summary>Jobs on offer now: next tier after the current job (or tier 1), level met, flags met.</summary>
        public List<JobData> Options(string classId, string currentJob, int level, System.Func<string, bool> hasFlag)
        {
            var list = new List<JobData>();
            foreach (JobData j in jobs)
            {
                if (j == null || level < j.requiredLevel) continue;
                if (!string.IsNullOrEmpty(j.baseClass) && j.baseClass != classId) continue;
                bool fits = string.IsNullOrEmpty(currentJob) ? string.IsNullOrEmpty(j.parent) : j.parent == currentJob;
                if (!fits) continue;
                if (!string.IsNullOrEmpty(j.requiredFlag) && !hasFlag(j.requiredFlag)) continue;
                list.Add(j);
            }
            return list;
        }
    }
}

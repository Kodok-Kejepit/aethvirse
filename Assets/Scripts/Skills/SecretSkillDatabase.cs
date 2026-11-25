using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Skills
{
    /// <summary>Secret skills (taught by skill books, any class) and every skill rune (Resources/SecretSkillDatabase).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Secret Skill Database", fileName = "SecretSkillDatabase")]
    public sealed class SecretSkillDatabase : ScriptableObject
    {
        public List<SkillData> skills = new List<SkillData>();
        public List<SkillRuneData> runes = new List<SkillRuneData>();

        static SecretSkillDatabase _loaded;
        public static SecretSkillDatabase Instance => _loaded != null ? _loaded : _loaded = Resources.Load<SecretSkillDatabase>("SecretSkillDatabase");

        public SkillData Skill(string id) => skills.Find(s => s != null && s.id == id);
        public SkillRuneData Rune(string id) => runes.Find(r => r != null && r.id == id);
    }
}

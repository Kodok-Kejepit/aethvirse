using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>One step of a set bonus: with this many pieces worn, a stat goes up.</summary>
    [Serializable]
    public sealed class SetTier
    {
        public int pieces = 2;
        public string stat = "";
        public float value;
        public string text = "";
    }

    [Serializable]
    public sealed class GearSet
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string lore = "";
        public List<SetTier> tiers = new List<SetTier>();
    }

    /// <summary>Gear sets (§11: 2 / 4 / 6-piece bonuses), Resources/GearSetDatabase.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Gear Set Database", fileName = "GearSetDatabase")]
    public sealed class GearSetDatabase : ScriptableObject
    {
        public List<GearSet> sets = new List<GearSet>();

        static GearSetDatabase _loaded;
        public static GearSetDatabase Instance => _loaded != null ? _loaded : _loaded = Resources.Load<GearSetDatabase>("GearSetDatabase");

        public GearSet Get(string id) => sets.Find(s => s.id == id);

        /// <summary>Total of one stat from every active set tier (pure; tested).</summary>
        public float Bonus(string stat, IReadOnlyDictionary<string, int> worn)
        {
            float total = 0f;
            foreach (GearSet set in sets)
            {
                if (!worn.TryGetValue(set.id, out int n)) continue;
                foreach (SetTier t in set.tiers) if (n >= t.pieces && t.stat == stat) total += t.value;
            }
            return total;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>All item definitions by id (one asset, built by ItemDataBuilder).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject
    {
        public List<ItemData> items = new List<ItemData>();
        /// <summary>Crafting recipes and the Quartermaster's stock (data, so they grow without rebuilding scenes).</summary>
        public List<Recipe> recipes = new List<Recipe>();
        public List<ShopEntry> stock = new List<ShopEntry>();
        Dictionary<string, ItemData> _byId;

        public ItemData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null || _byId.Count != items.Count)
            {
                _byId = new Dictionary<string, ItemData>();
                foreach (ItemData i in items) if (i != null) _byId[i.id] = i;
            }
            return _byId.TryGetValue(id, out ItemData item) ? item : null;
        }

        public IEnumerable<ItemData> DropBases()
        {
            foreach (ItemData i in items) if (i != null && i.isDropBase) yield return i;
        }
    }
}

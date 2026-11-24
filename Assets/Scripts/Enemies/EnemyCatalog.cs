using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Enemies
{
    /// <summary>All enemy types by id plus the base prefab they are built on (Resources/EnemyCatalog).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Enemy Catalog", fileName = "EnemyCatalog")]
    public sealed class EnemyCatalog : ScriptableObject
    {
        public GameObject basePrefab;
        public List<EnemyData> enemies = new List<EnemyData>();

        static EnemyCatalog _loaded;
        public static EnemyCatalog Instance => _loaded != null ? _loaded : _loaded = Resources.Load<EnemyCatalog>("EnemyCatalog");

        public EnemyData Get(string id) => enemies.Find(e => e != null && e.id == id);

        /// <summary>Spawns an enemy of a type at a level (waves, spawners, Gates, Dungeon Breaks).</summary>
        public Enemy Spawn(string id, Vector3 at, int level, float aggro = 0f, bool loot = true, bool respawnAtHome = false)
        {
            EnemyData data = Get(id);
            if (data == null || basePrefab == null) return null;
            GameObject go = Instantiate(basePrefab, at, Quaternion.identity);
            Enemy e = go.GetComponent<Enemy>();
            e.SetData(data);
            e.SetLevel(level, aggro, loot);
            e.RespawnAtHome = respawnAtHome;
            return e;
        }
    }
}
